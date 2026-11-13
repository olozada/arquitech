using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Arquitectos.Application.Actas;
using Arquitectos.Application.Movil;
using Arquitectos.Application.Usuarios;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arquitectos.Infrastructure.Movil;

public sealed class SincronizadorMovil : ISincronizadorMovil
{
    public const string MensajeDependencia = "Depende de una operación que no se aplicó.";

    private readonly ArquitectosDbContext _context;
    private readonly IOperacionesLineaActa _operaciones;
    private readonly OpcionesMovil _opciones;
    private readonly TimeProvider _reloj;
    private readonly ILogger<SincronizadorMovil> _logger;

    public SincronizadorMovil(
        ArquitectosDbContext context,
        IOperacionesLineaActa operaciones,
        IOptions<OpcionesMovil> opciones,
        TimeProvider reloj,
        ILogger<SincronizadorMovil> logger)
    {
        _context = context;
        _operaciones = operaciones;
        _opciones = opciones.Value;
        _reloj = reloj;
        _logger = logger;
    }

    public async Task<ResultadoSincronizacion> SincronizarAsync(
        int usuarioId,
        long? sesionId,
        SincronizacionMovilSolicitud solicitud,
        CancellationToken cancellationToken)
    {
        var cronometro = Stopwatch.StartNew();
        var usuario = await _context.Usuarios.AsNoTracking()
            .Where(item => item.Id == usuarioId)
            .Select(item => new UsuarioSync(item.Id, item.TipoUsuarioId, item.ArquitectoId, item.ContratistaId, item.RevisorId, item.Activo))
            .FirstOrDefaultAsync(cancellationToken);
        if (usuario is null || !usuario.Activo)
        {
            return ResultadoSincronizacion.NoAutorizado("La sesión no es válida.");
        }

        if (!PerfilesUsuario.EsOperativo(usuario.TipoUsuarioId))
        {
            return ResultadoSincronizacion.Prohibido("La aplicación móvil no está disponible para el administrador.");
        }

        if (solicitud.VersionEsquema != _opciones.VersionEsquema)
        {
            return ResultadoSincronizacion.SolicitudInvalida("La versión del esquema no coincide.");
        }

        if (solicitud.Operaciones.Count > _opciones.MaxOperacionesLote)
        {
            return ResultadoSincronizacion.SolicitudInvalida($"El lote supera el máximo de {_opciones.MaxOperacionesLote} operaciones.");
        }

        if (solicitud.Operaciones.Select(item => item.IdOperacion).Distinct().Count() != solicitud.Operaciones.Count ||
            solicitud.Operaciones.Select(item => item.Secuencia).Distinct().Count() != solicitud.Operaciones.Count)
        {
            return ResultadoSincronizacion.SolicitudInvalida("Hay operaciones o secuencias repetidas.");
        }

        var dispositivo = await _context.Dispositivos.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == solicitud.DispositivoId, cancellationToken);
        if (dispositivo is null || dispositivo.UsuarioId != usuarioId)
        {
            return ResultadoSincronizacion.Prohibido("El dispositivo no está registrado para este usuario.");
        }

        if (dispositivo.RevocadoEn is not null)
        {
            return ResultadoSincronizacion.DispositivoRevocado("Este dispositivo fue revocado.");
        }

        if (sesionId is long sesion)
        {
            var dispositivoSesion = await _context.SesionesUsuario.AsNoTracking()
                .Where(item => item.Id == sesion && item.UsuarioId == usuarioId)
                .Select(item => item.DispositivoId)
                .FirstOrDefaultAsync(cancellationToken);
            if (dispositivoSesion is Guid esperado && esperado != solicitud.DispositivoId)
            {
                return ResultadoSincronizacion.Prohibido("El dispositivo no corresponde a la sesión.");
            }
        }

        var actor = new ActorActa(usuario.Id, usuario.TipoUsuarioId, usuario.ArquitectoId, usuario.ContratistaId, usuario.RevisorId);
        var ahora = _reloj.GetUtcNow().UtcDateTime;
        var clientes = new Dictionary<Guid, EstadoCliente>();
        var resultados = new List<ResultadoLineaMovil>();
        var bloqueo = _context.Database.IsSqlServer()
            ? await _context.Database.BeginTransactionAsync(cancellationToken)
            : null;
        try
        {
            await AdquirirBloqueoDispositivoAsync(solicitud.DispositivoId, cancellationToken);
            foreach (var operacion in solicitud.Operaciones.OrderBy(item => item.Secuencia))
            {
                var resultado = await UnaAsync(operacion, actor, solicitud.DispositivoId, ahora, clientes, cancellationToken);
                resultados.Add(resultado);
                if (operacion.IdCliente is Guid cliente)
                {
                    clientes[cliente] = new EstadoCliente(resultado.Estado, resultado.LineaId, resultado.RowVersion);
                }
            }

            if (bloqueo is not null)
            {
                await bloqueo.CommitAsync(cancellationToken);
            }
        }
        finally
        {
            if (bloqueo is not null)
            {
                await bloqueo.DisposeAsync();
            }
        }

        await _context.Dispositivos
            .Where(item => item.Id == solicitud.DispositivoId && item.RevocadoEn == null)
            .ExecuteUpdateAsync(llamadas => llamadas.SetProperty(item => item.UltimaSincronizacion, ahora), cancellationToken);
        cronometro.Stop();
        _logger.LogInformation(
            "Sincronización móvil del usuario {UsuarioId} en el dispositivo {DispositivoId}. Aplicadas {Aplicadas}, rechazadas {Rechazadas}, conflictos {Conflictos}. Duración {DuracionMs} ms.",
            usuarioId,
            solicitud.DispositivoId,
            resultados.Count(item => item.Estado == "Aplicada"),
            resultados.Count(item => item.Estado == "Rechazada"),
            resultados.Count(item => item.Estado == "Conflicto"),
            cronometro.ElapsedMilliseconds);
        return ResultadoSincronizacion.Ok(new SincronizacionMovilRespuesta(
            resultados,
            resultados.Any(item => item.Estado is "Aplicada" or "Conflicto")));
    }

    private async Task<ResultadoLineaMovil> UnaAsync(
        OperacionMovilSolicitud operacion,
        ActorActa actor,
        Guid dispositivoId,
        DateTime ahora,
        Dictionary<Guid, EstadoCliente> clientes,
        CancellationToken cancellationToken)
    {
        var guardada = await _context.OperacionesMovil.AsNoTracking()
            .Where(item => item.IdOperacion == operacion.IdOperacion)
            .Select(item => new { item.UsuarioId, item.ResultadoJson })
            .FirstOrDefaultAsync(cancellationToken);
        if (guardada is not null)
        {
            if (guardada.UsuarioId == actor.UsuarioId && !string.IsNullOrEmpty(guardada.ResultadoJson))
            {
                return JsonSerializer.Deserialize<ResultadoLineaMovil>(guardada.ResultadoJson, JsonPaquete.Opciones)
                    ?? Rechazar(operacion, "La operación ya fue registrada.");
            }

            return Rechazar(operacion, "La operación ya fue registrada.");
        }

        try
        {
            ResultadoLineaMovil resultado;
            if (operacion.IdCliente is Guid cliente &&
                clientes.TryGetValue(cliente, out var previa) &&
                previa.Estado != "Aplicada")
            {
                resultado = Rechazar(operacion, MensajeDependencia);
            }
            else
            {
                resultado = await AplicarAsync(operacion, actor, clientes, cancellationToken);
            }

            _context.ChangeTracker.Clear();
            _context.OperacionesMovil.Add(new OperacionMovil
            {
                IdOperacion = operacion.IdOperacion,
                DispositivoId = dispositivoId,
                UsuarioId = actor.UsuarioId,
                Tipo = operacion.Tipo,
                NumeroActa = operacion.NumeroActa,
                Estado = resultado.Estado,
                ResultadoJson = JsonSerializer.Serialize(resultado, JsonPaquete.Opciones),
                RecibidaEn = ahora,
                CreadaEnDispositivo = operacion.CreadaEn == default
                    ? ahora
                    : operacion.CreadaEn.Kind == DateTimeKind.Utc
                        ? operacion.CreadaEn
                        : DateTime.SpecifyKind(operacion.CreadaEn, DateTimeKind.Utc)
            });
            await _context.SaveChangesAsync(cancellationToken);
            return resultado;
        }
        catch (DbUpdateException ex) when (EsOperacionDuplicada(ex))
        {
            _context.ChangeTracker.Clear();
            return await ResultadoOperacionGuardadaAsync(operacion.IdOperacion, actor.UsuarioId, operacion, cancellationToken)
                ?? Rechazar(operacion, "La operación ya fue registrada.");
        }
    }

    private async Task AdquirirBloqueoDispositivoAsync(Guid dispositivoId, CancellationToken cancellationToken)
    {
        if (!_context.Database.IsSqlServer())
        {
            return;
        }

        var recurso = $"movil-sync-{dispositivoId:D}";
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"EXEC sp_getapplock @Resource = {recurso}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 30000",
            cancellationToken);
    }

    private static bool EsOperacionDuplicada(DbUpdateException ex)
    {
        if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql)
        {
            return sql.Number is 2601 or 2627;
        }

        return ex.InnerException is SqliteException sqlite && sqlite.SqliteErrorCode == 19;
    }

    private async Task<ResultadoLineaMovil?> ResultadoOperacionGuardadaAsync(
        Guid idOperacion,
        int usuarioId,
        OperacionMovilSolicitud operacion,
        CancellationToken cancellationToken)
    {
        var guardada = await _context.OperacionesMovil.AsNoTracking()
            .Where(item => item.IdOperacion == idOperacion && item.UsuarioId == usuarioId)
            .Select(item => item.ResultadoJson)
            .FirstOrDefaultAsync(cancellationToken);
        if (string.IsNullOrEmpty(guardada))
        {
            return null;
        }

        return JsonSerializer.Deserialize<ResultadoLineaMovil>(guardada, JsonPaquete.Opciones)
            ?? Rechazar(operacion, "La operación ya fue registrada.");
    }

    private async Task<ResultadoLineaMovil> AplicarAsync(
        OperacionMovilSolicitud operacion,
        ActorActa actor,
        Dictionary<Guid, EstadoCliente> clientes,
        CancellationToken cancellationToken)
    {
        try
        {
            var aplicado = operacion.Tipo switch
            {
                "AgregarLinea" => await _operaciones.AgregarAsync(
                    operacion.NumeroActa,
                    operacion.IdCliente ?? Guid.Empty,
                    Mapear(operacion.Linea!, actor.TipoUsuarioId),
                    actor,
                    cancellationToken),
                "EditarLinea" => await EditarOEliminarAsync(operacion, actor, clientes, editar: true, cancellationToken),
                "EliminarLinea" => await EditarOEliminarAsync(operacion, actor, clientes, editar: false, cancellationToken),
                "RevisarLinea" => await RevisarAsync(operacion, actor, clientes, cancellationToken),
                _ => ResultadoActa<LineaAplicadaDto>.Invalido("El tipo de operación no es válido.")
            };
            return await TraducirAsync(operacion, aplicado, cancellationToken);
        }
        catch (DbUpdateException)
        {
            _context.ChangeTracker.Clear();
            return new ResultadoLineaMovil(operacion.IdOperacion, "Rechazada", null, operacion.IdCliente, null, "No se pudo guardar la operación.", null);
        }
    }

    private async Task<ResultadoActa<LineaAplicadaDto>> EditarOEliminarAsync(
        OperacionMovilSolicitud operacion,
        ActorActa actor,
        Dictionary<Guid, EstadoCliente> clientes,
        bool editar,
        CancellationToken cancellationToken)
    {
        var lineaId = await ResolverLineaAsync(operacion, clientes, cancellationToken);
        if (lineaId is null)
        {
            return ResultadoActa<LineaAplicadaDto>.Conflicto(CrearActaValidador.MensajeConflictoLinea);
        }

        var version = VersionDe(operacion, clientes);
        return editar
            ? await _operaciones.EditarAsync(operacion.NumeroActa, lineaId.Value, version, Mapear(operacion.Linea!, actor.TipoUsuarioId), actor, cancellationToken)
            : await _operaciones.EliminarAsync(operacion.NumeroActa, lineaId.Value, version, actor, cancellationToken);
    }

    private async Task<ResultadoActa<LineaAplicadaDto>> RevisarAsync(
        OperacionMovilSolicitud operacion,
        ActorActa actor,
        Dictionary<Guid, EstadoCliente> clientes,
        CancellationToken cancellationToken)
    {
        var lineaId = await ResolverLineaAsync(operacion, clientes, cancellationToken);
        if (lineaId is null)
        {
            return ResultadoActa<LineaAplicadaDto>.Conflicto(CrearActaValidador.MensajeConflictoLinea);
        }

        return await _operaciones.RevisarAsync(
            operacion.NumeroActa,
            lineaId.Value,
            VersionDe(operacion, clientes),
            operacion.Aprobado ?? false,
            actor,
            cancellationToken);
    }

    private async Task<ResultadoLineaMovil> TraducirAsync(
        OperacionMovilSolicitud operacion,
        ResultadoActa<LineaAplicadaDto> aplicado,
        CancellationToken cancellationToken)
    {
        if (aplicado.Estado == EstadoActa.Ok && aplicado.Valor is LineaAplicadaDto linea)
        {
            return new ResultadoLineaMovil(operacion.IdOperacion, "Aplicada", linea.Id, linea.IdCliente ?? operacion.IdCliente, linea.RowVersion, null, null);
        }

        if (aplicado.Estado == EstadoActa.Conflicto)
        {
            _context.ChangeTracker.Clear();
            var servidor = await LineaActualAsync(operacion, cancellationToken);
            return new ResultadoLineaMovil(
                operacion.IdOperacion,
                "Conflicto",
                servidor?.Id,
                servidor?.IdCliente ?? operacion.IdCliente,
                servidor?.RowVersion,
                aplicado.Mensaje ?? CrearActaValidador.MensajeConflictoLinea,
                servidor);
        }

        return Rechazar(operacion, aplicado.Mensaje ?? "No se pudo aplicar la operación.");
    }

    private async Task<int?> ResolverLineaAsync(
        OperacionMovilSolicitud operacion,
        Dictionary<Guid, EstadoCliente> clientes,
        CancellationToken cancellationToken)
    {
        if (operacion.LineaId is > 0)
        {
            return operacion.LineaId;
        }

        if (operacion.IdCliente is Guid cliente &&
            clientes.TryGetValue(cliente, out var previa) &&
            previa.LineaId is int conocida)
        {
            return conocida;
        }

        if (operacion.IdCliente is Guid idCliente)
        {
            return await _context.ActaDetalles.AsNoTracking()
                .Where(detalle => detalle.IdCliente == idCliente)
                .Select(detalle => (int?)detalle.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return null;
    }

    private async Task<LineaServidorMovil?> LineaActualAsync(OperacionMovilSolicitud operacion, CancellationToken cancellationToken)
    {
        var consulta = _context.ActaDetalles.AsNoTracking().AsQueryable();
        if (operacion.LineaId is > 0)
        {
            consulta = consulta.Where(detalle => detalle.Id == operacion.LineaId);
        }
        else if (operacion.IdCliente is Guid cliente)
        {
            consulta = consulta.Where(detalle => detalle.IdCliente == cliente);
        }
        else
        {
            return null;
        }

        var detalle = await consulta.FirstOrDefaultAsync(cancellationToken);
        return detalle is null ? null : new LineaServidorMovil(
            detalle.Id,
            detalle.IdCliente,
            detalle.NumeroActa,
            detalle.ConstruccionId,
            detalle.ConceptoId,
            detalle.UnidadMedidaId,
            detalle.InsumoId,
            detalle.PresupuestoItemId,
            Texto(detalle.Cantidad),
            Texto(detalle.Precio),
            Texto(detalle.Subtotal),
            detalle.Descripcion,
            detalle.TipoUsuarioId,
            detalle.Aprobado,
            Convert.ToBase64String(detalle.RowVersion ?? []));
    }

    private static string VersionDe(OperacionMovilSolicitud operacion, Dictionary<Guid, EstadoCliente> clientes)
    {
        if (operacion.IdCliente is Guid cliente &&
            clientes.TryGetValue(cliente, out var previa) &&
            previa.Estado == "Aplicada" &&
            !string.IsNullOrEmpty(previa.Version))
        {
            return previa.Version;
        }

        return operacion.RowVersionLinea ?? "";
    }

    private static LineaActaSolicitud Mapear(LineaMovilSolicitud linea, int tipoUsuarioId) =>
        new()
        {
            ConstruccionId = linea.ConstruccionId,
            ConceptoId = linea.ConceptoId,
            UnidadMedidaId = linea.UnidadMedidaId,
            InsumoId = linea.InsumoId,
            PresupuestoItemId = linea.PresupuestoItemId,
            Cantidad = linea.Cantidad,
            Precio = linea.Precio,
            Descripcion = linea.Descripcion,
            TipoUsuarioId = tipoUsuarioId,
            Aprobado = true
        };

    private static ResultadoLineaMovil Rechazar(OperacionMovilSolicitud operacion, string mensaje) =>
        new(operacion.IdOperacion, "Rechazada", null, operacion.IdCliente, null, mensaje, null);

    private static string Texto(decimal valor) => valor.ToString(CultureInfo.InvariantCulture);

    private sealed record UsuarioSync(int Id, int TipoUsuarioId, int? ArquitectoId, int? ContratistaId, int? RevisorId, bool Activo);

    private sealed record EstadoCliente(string Estado, int? LineaId, string? Version);
}
