using Arquitectos.Application.Actas;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Infrastructure.Actas;

public sealed class ActaServicio : IActaServicio
{
    private readonly ArquitectosDbContext _context;

    public ActaServicio(ArquitectosDbContext context)
    {
        _context = context;
    }

    public async Task<ResultadoActa<ActaCreadaDto>> CrearAsync(
        CrearActaSolicitud solicitud,
        int? usuarioId,
        CancellationToken cancellationToken)
    {
        var fechas = ValidarRango(solicitud.FechaInicial, solicitud.FechaFinal);
        if (fechas is not null)
        {
            return ResultadoActa<ActaCreadaDto>.Invalido(fechas);
        }

        if (solicitud.Detalles is not { Count: > 0 })
        {
            return ResultadoActa<ActaCreadaDto>.Invalido(CrearActaValidador.MensajeDetalle);
        }

        if (!await ExisteActivo(_context.Arquitectos, solicitud.ArquitectoId, cancellationToken) ||
            !await ExisteActivo(_context.Revisores, solicitud.RevisorId, cancellationToken) ||
            !await ExisteActivo(_context.Contratistas, solicitud.ContratistaId, cancellationToken))
        {
            return ResultadoActa<ActaCreadaDto>.Invalido(CrearActaValidador.MensajeEncabezado);
        }

        if (!await TodosActivos(_context.Construcciones, solicitud.Detalles.Select(linea => linea.ConstruccionId), cancellationToken) ||
            !await TodosActivos(_context.Conceptos, solicitud.Detalles.Select(linea => linea.ConceptoId), cancellationToken) ||
            !await TodosActivos(_context.UnidadesMedida, solicitud.Detalles.Select(linea => linea.UnidadMedidaId), cancellationToken))
        {
            return ResultadoActa<ActaCreadaDto>.Invalido(CrearActaValidador.MensajeDetalle);
        }

        var materiales = solicitud.Detalles
            .Where(linea => linea.MaterialId is > 0)
            .Select(linea => linea.MaterialId!.Value)
            .ToList();
        if (materiales.Count > 0 &&
            !await TodosActivos(_context.Materiales, materiales, cancellationToken))
        {
            return ResultadoActa<ActaCreadaDto>.Invalido(CrearActaValidador.MensajeDetalle);
        }

        if (solicitud.Detalles.Any(linea =>
                decimal.Round(linea.Cantidad, 4, MidpointRounding.AwayFromZero) <= 0 ||
                decimal.Round(linea.Precio, 2, MidpointRounding.AwayFromZero) < 0))
        {
            return ResultadoActa<ActaCreadaDto>.Invalido(CrearActaValidador.MensajeDetalle);
        }

        int? usuarioRegistroId = usuarioId is int id &&
            await _context.Usuarios.AnyAsync(usuario => usuario.Id == id && usuario.Activo, cancellationToken)
            ? id
            : null;

        var acta = new Acta
        {
            FechaInicial = solicitud.FechaInicial,
            FechaFinal = solicitud.FechaFinal,
            ArquitectoId = solicitud.ArquitectoId,
            RevisorId = solicitud.RevisorId,
            ContratistaId = solicitud.ContratistaId,
            Observacion = Texto(solicitud.Observacion),
            FechaRegistro = DateTime.UtcNow,
            UsuarioRegistroId = usuarioRegistroId
        };

        foreach (var linea in solicitud.Detalles)
        {
            acta.Detalles.Add(new ActaDetalle
            {
                ConstruccionId = linea.ConstruccionId,
                ConceptoId = linea.ConceptoId,
                UnidadMedidaId = linea.UnidadMedidaId,
                MaterialId = linea.MaterialId,
                Cantidad = decimal.Round(linea.Cantidad, 4, MidpointRounding.AwayFromZero),
                Precio = decimal.Round(linea.Precio, 2, MidpointRounding.AwayFromZero),
                Descripcion = linea.Descripcion.Trim()
            });
        }

        await using var transaccion = await _context.Database.BeginTransactionAsync(cancellationToken);
        _context.Actas.Add(acta);
        await _context.SaveChangesAsync(cancellationToken);
        await transaccion.CommitAsync(cancellationToken);

        return ResultadoActa<ActaCreadaDto>.Ok(new ActaCreadaDto(acta.NumeroActa));
    }

    public async Task<ResultadoActa<ConsultaActasDto>> ConsultarAsync(
        DateOnly? desde,
        DateOnly? hasta,
        int? arquitectoId,
        CancellationToken cancellationToken)
    {
        var fechas = ValidarRango(desde, hasta);
        if (fechas is not null)
        {
            return ResultadoActa<ConsultaActasDto>.Invalido(fechas);
        }

        var consulta = Filtrar(_context.Actas.AsNoTracking(), desde, hasta, arquitectoId);
        var filas = await consulta
            .OrderByDescending(acta => acta.NumeroActa)
            .Select(acta => new
            {
                acta.NumeroActa,
                acta.FechaInicial,
                acta.FechaFinal,
                Contratista = acta.Contratista.Nombre,
                acta.Observacion
            })
            .ToListAsync(cancellationToken);

        if (filas.Count == 0)
        {
            return ResultadoActa<ConsultaActasDto>.Ok(new ConsultaActasDto([], 0));
        }

        var numeros = filas.Select(fila => fila.NumeroActa).ToList();
        var subtotales = await _context.ActaDetalles
            .AsNoTracking()
            .Where(detalle => numeros.Contains(detalle.NumeroActa))
            .Select(detalle => new { detalle.NumeroActa, detalle.Subtotal })
            .ToListAsync(cancellationToken);
        var totales = subtotales
            .GroupBy(detalle => detalle.NumeroActa)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.Sum(detalle => detalle.Subtotal));

        var actas = filas
            .Select(fila => new ActaResumenDto(
                fila.NumeroActa,
                fila.FechaInicial,
                fila.FechaFinal,
                fila.Contratista,
                fila.Observacion,
                totales.GetValueOrDefault(fila.NumeroActa)))
            .ToList();

        return ResultadoActa<ConsultaActasDto>.Ok(new ConsultaActasDto(actas, actas.Sum(acta => acta.Total)));
    }

    public async Task<ResultadoActa<ActaDto>> ObtenerAsync(int numeroActa, CancellationToken cancellationToken)
    {
        var acta = await CargarAsync(numeroActa, cancellationToken);
        return acta is null
            ? ResultadoActa<ActaDto>.NoEncontrado("No se encontró el acta.")
            : ResultadoActa<ActaDto>.Ok(acta);
    }

    public async Task<ResultadoActa<byte[]>> GenerarPdfAsync(int numeroActa, CancellationToken cancellationToken)
    {
        var acta = await CargarAsync(numeroActa, cancellationToken);
        return acta is null
            ? ResultadoActa<byte[]>.NoEncontrado("No se encontró el acta.")
            : ResultadoActa<byte[]>.Ok(GeneradorPdfActa.Generar(acta));
    }

    public async Task<ResultadoActa<byte[]>> ExportarAsync(
        DateOnly? desde,
        DateOnly? hasta,
        int? arquitectoId,
        CancellationToken cancellationToken)
    {
        var consulta = await ConsultarAsync(desde, hasta, arquitectoId, cancellationToken);
        if (consulta.Estado != EstadoActa.Ok)
        {
            return ResultadoActa<byte[]>.Invalido(consulta.Mensaje ?? CrearActaValidador.MensajeFechas);
        }

        return ResultadoActa<byte[]>.Ok(GeneradorExcelActas.Generar(consulta.Valor!));
    }

    private IQueryable<Acta> Filtrar(IQueryable<Acta> consulta, DateOnly? desde, DateOnly? hasta, int? arquitectoId)
    {
        if (desde is DateOnly inicio)
        {
            consulta = consulta.Where(acta => acta.FechaFinal >= inicio);
        }

        if (hasta is DateOnly fin)
        {
            consulta = consulta.Where(acta => acta.FechaInicial <= fin);
        }

        if (arquitectoId is int id)
        {
            consulta = consulta.Where(acta => acta.ArquitectoId == id);
        }

        return consulta;
    }

    private async Task<ActaDto?> CargarAsync(int numeroActa, CancellationToken cancellationToken)
    {
        var acta = await _context.Actas
            .AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.Arquitecto)
            .Include(item => item.Revisor)
            .Include(item => item.Contratista)
            .Include(item => item.Detalles).ThenInclude(detalle => detalle.Construccion)
            .Include(item => item.Detalles).ThenInclude(detalle => detalle.Concepto)
            .Include(item => item.Detalles).ThenInclude(detalle => detalle.UnidadMedida)
            .Include(item => item.Detalles).ThenInclude(detalle => detalle.Material)
            .SingleOrDefaultAsync(item => item.NumeroActa == numeroActa, cancellationToken);

        if (acta is null)
        {
            return null;
        }

        var detalles = acta.Detalles
            .OrderBy(detalle => detalle.Construccion.Descripcion)
            .ThenBy(detalle => detalle.Id)
            .Select(detalle => new LineaActaDto(
                detalle.Id,
                detalle.ConstruccionId,
                detalle.Construccion.Descripcion,
                detalle.ConceptoId,
                detalle.Concepto.Descripcion,
                detalle.UnidadMedidaId,
                detalle.UnidadMedida.Nombre,
                detalle.MaterialId,
                detalle.Material?.Descripcion,
                detalle.Cantidad,
                detalle.Precio,
                detalle.Descripcion,
                detalle.Subtotal))
            .ToList();

        return new ActaDto(
            acta.NumeroActa,
            acta.FechaInicial,
            acta.FechaFinal,
            acta.ArquitectoId,
            acta.Arquitecto.Nombre,
            acta.RevisorId,
            acta.Revisor.Nombre,
            acta.ContratistaId,
            acta.Contratista.Nombre,
            acta.Observacion,
            acta.FechaRegistro,
            acta.UsuarioRegistroId,
            detalles.Sum(detalle => detalle.Subtotal),
            detalles);
    }

    private static string? ValidarRango(DateOnly? desde, DateOnly? hasta) =>
        desde is DateOnly inicio && hasta is DateOnly fin && fin < inicio
            ? CrearActaValidador.MensajeFechas
            : null;

    private static string? Texto(string? valor)
    {
        var texto = valor?.Trim();
        return string.IsNullOrEmpty(texto) ? null : texto;
    }

    private static async Task<bool> ExisteActivo<TEntidad>(
        IQueryable<TEntidad> consulta,
        int id,
        CancellationToken cancellationToken)
        where TEntidad : class, ICatalogo =>
        await consulta.AnyAsync(entidad => entidad.Id == id && entidad.Activo, cancellationToken);

    private static async Task<bool> TodosActivos<TEntidad>(
        IQueryable<TEntidad> consulta,
        IEnumerable<int> ids,
        CancellationToken cancellationToken)
        where TEntidad : class, ICatalogo
    {
        var distintos = ids.Distinct().ToList();
        var encontrados = await consulta.CountAsync(
            entidad => distintos.Contains(entidad.Id) && entidad.Activo,
            cancellationToken);
        return encontrados == distintos.Count;
    }
}
