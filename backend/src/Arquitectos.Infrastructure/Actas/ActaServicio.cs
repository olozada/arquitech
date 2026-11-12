using System.Globalization;
using System.Linq.Expressions;
using Arquitectos.Application.Actas;
using Arquitectos.Application.Auditoria;
using Arquitectos.Application.Usuarios;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Auditoria;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Infrastructure.Actas;

public sealed class ActaServicio : IActaServicio
{
    private readonly ArquitectosDbContext _context;
    private readonly IAuditoria _auditoria;

    public ActaServicio(ArquitectosDbContext context, IAuditoria? auditoria = null)
    {
        _context = context;
        _auditoria = auditoria ?? SinAuditoria.Instancia;
    }

    public async Task<ResultadoActa<ActaCreadaDto>> CrearAsync(
        CrearActaSolicitud solicitud,
        int? usuarioId,
        CancellationToken cancellationToken)
    {
        var error = await ValidarSolicitud(solicitud, cancellationToken);
        if (error is not null)
        {
            return ResultadoActa<ActaCreadaDto>.Invalido(error);
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
            acta.Detalles.Add(CrearDetalle(linea, PerfilesUsuario.Arquitecto));
        }

        await using var transaccion = await _context.Database.BeginTransactionAsync(cancellationToken);
        _context.Actas.Add(acta);
        await _context.SaveChangesAsync(cancellationToken);
        await transaccion.CommitAsync(cancellationToken);

        return ResultadoActa<ActaCreadaDto>.Ok(new ActaCreadaDto(acta.NumeroActa));
    }

    public async Task<ResultadoActa<ActaDto>> ActualizarAsync(
        int numeroActa,
        CrearActaSolicitud solicitud,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        var error = await ValidarSolicitud(solicitud, cancellationToken);
        if (error is not null)
        {
            return ResultadoActa<ActaDto>.Invalido(error);
        }

        var actor = await CargarActorAsync(usuarioId, cancellationToken);
        if (actor is null)
        {
            return ResultadoActa<ActaDto>.Invalido("No se encontró el usuario.");
        }

        var acta = await _context.Actas
            .Include(item => item.Detalles)
            .SingleOrDefaultAsync(item => item.NumeroActa == numeroActa, cancellationToken);
        if (acta is null || !PuedeVer(acta, actor))
        {
            return ResultadoActa<ActaDto>.NoEncontrado("No se encontró el acta.");
        }

        if (VersionDistinta(acta.RowVersion, solicitud.RowVersion))
        {
            return ResultadoActa<ActaDto>.Conflicto("Otra persona modificó esta acta.");
        }

        if (acta.Cerrada)
        {
            return ResultadoActa<ActaDto>.Invalido(CrearActaValidador.MensajeCerrada);
        }

        if (actor.TipoUsuarioId == PerfilesUsuario.Revisor)
        {
            return await RevisarAsync(acta, solicitud, usuarioId, cancellationToken);
        }

        if (actor.TipoUsuarioId != PerfilesUsuario.Administrador)
        {
            return await ActualizarPerfilAsync(acta, solicitud, actor, cancellationToken);
        }

        var ids = solicitud.Detalles.Where(linea => linea.Id is > 0).Select(linea => linea.Id!.Value).ToList();
        if (ids.Distinct().Count() != ids.Count)
        {
            return ResultadoActa<ActaDto>.Invalido("Hay un ítem repetido en el acta.");
        }

        var actuales = acta.Detalles.ToDictionary(detalle => detalle.Id);
        if (ids.Any(id => !actuales.ContainsKey(id)))
        {
            return ResultadoActa<ActaDto>.Invalido("Hay un ítem que no pertenece al acta.");
        }

        acta.FechaInicial = solicitud.FechaInicial;
        acta.FechaFinal = solicitud.FechaFinal;
        acta.ArquitectoId = solicitud.ArquitectoId;
        acta.RevisorId = solicitud.RevisorId;
        acta.ContratistaId = solicitud.ContratistaId;
        acta.Observacion = Texto(solicitud.Observacion);

        var conservar = ids.ToHashSet();
        foreach (var detalle in acta.Detalles.Where(detalle => !conservar.Contains(detalle.Id)).ToList())
        {
            acta.Detalles.Remove(detalle);
            _context.ActaDetalles.Remove(detalle);
        }

        foreach (var linea in solicitud.Detalles)
        {
            if (linea.Id is > 0)
            {
                CopiarDetalle(actuales[linea.Id.Value], linea);
            }
            else
            {
                acta.Detalles.Add(CrearDetalle(linea, PerfilesUsuario.Arquitecto));
            }
        }

        var conflicto = await GuardarAsync(cancellationToken);
        if (conflicto is not null)
        {
            return ResultadoActa<ActaDto>.Conflicto(conflicto);
        }

        var actualizada = await CargarAsync(numeroActa, cancellationToken);
        return ResultadoActa<ActaDto>.Ok(actualizada!);
    }

    public async Task<ResultadoActa<ActaDto>> CambiarCierreAsync(
        int numeroActa,
        bool cerrada,
        int usuarioId,
        CancellationToken cancellationToken,
        string? rowVersion = null)
    {
        var actor = await CargarActorAsync(usuarioId, cancellationToken);
        if (actor is null || actor.TipoUsuarioId != PerfilesUsuario.Administrador)
        {
            return ResultadoActa<ActaDto>.Invalido("Solo el administrador puede cerrar o abrir el acta.");
        }

        var acta = await _context.Actas
            .SingleOrDefaultAsync(item => item.NumeroActa == numeroActa, cancellationToken);
        if (acta is null)
        {
            return ResultadoActa<ActaDto>.NoEncontrado("No se encontró el acta.");
        }

        if (VersionDistinta(acta.RowVersion, rowVersion))
        {
            return ResultadoActa<ActaDto>.Conflicto("Otra persona modificó esta acta.");
        }

        if (acta.Cerrada != cerrada)
        {
            await _auditoria.RegistrarAsync(
                "Acta",
                acta.NumeroActa.ToString(),
                cerrada ? AccionAuditoria.Cerrar : AccionAuditoria.Reabrir,
                usuarioId,
                CambiosAuditoria.Json("Cerrada", acta.Cerrada, cerrada),
                cancellationToken);
        }

        acta.Cerrada = cerrada;
        var conflictoCierre = await GuardarAsync(cancellationToken);
        if (conflictoCierre is not null)
        {
            return ResultadoActa<ActaDto>.Conflicto(conflictoCierre);
        }

        return ResultadoActa<ActaDto>.Ok((await CargarAsync(numeroActa, cancellationToken))!);
    }

    public async Task<ResultadoActa<ConsultaActasDto>> ConsultarAsync(
        DateOnly? desde,
        DateOnly? hasta,
        int? arquitectoId,
        int? revisorId,
        int? contratistaId,
        int? construccionId,
        bool? cerrada,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        var fechas = ValidarRango(desde, hasta);
        if (fechas is not null)
        {
            return ResultadoActa<ConsultaActasDto>.Invalido(fechas);
        }

        var actor = await CargarActorAsync(usuarioId, cancellationToken);
        if (actor is null)
        {
            return ResultadoActa<ConsultaActasDto>.Invalido("No se encontró el usuario.");
        }

        var consulta = Filtrar(
            _context.Actas.AsNoTracking(),
            desde,
            hasta,
            arquitectoId,
            revisorId,
            contratistaId,
            construccionId,
            cerrada,
            actor);
        var filas = await consulta
            .OrderByDescending(acta => acta.NumeroActa)
            .Select(acta => new
            {
                acta.NumeroActa,
                acta.FechaInicial,
                acta.FechaFinal,
                Contratista = acta.Contratista.Nombre,
                acta.Observacion,
                acta.Cerrada,
                Arquitecto = acta.Arquitecto.Nombre,
                Revisor = acta.Revisor.Nombre
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
            .Select(detalle => new { detalle.NumeroActa, detalle.Subtotal, detalle.Aprobado })
            .ToListAsync(cancellationToken);
        var totales = subtotales
            .GroupBy(detalle => detalle.NumeroActa)
            .ToDictionary(grupo => grupo.Key, grupo => TotalActa(grupo.Select(detalle => (detalle.Subtotal, detalle.Aprobado))));

        var actas = filas
            .Select(fila => new ActaResumenDto(
                fila.NumeroActa,
                fila.FechaInicial,
                fila.FechaFinal,
                fila.Contratista,
                fila.Observacion,
                totales.GetValueOrDefault(fila.NumeroActa),
                fila.Cerrada,
                fila.Arquitecto,
                fila.Revisor))
            .ToList();

        return ResultadoActa<ConsultaActasDto>.Ok(new ConsultaActasDto(actas, actas.Sum(acta => acta.Total)));
    }

    public async Task<ResultadoActa<ActaDto>> ObtenerAsync(
        int numeroActa,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        var actor = await CargarActorAsync(usuarioId, cancellationToken);
        if (actor is null)
        {
            return ResultadoActa<ActaDto>.Invalido("No se encontró el usuario.");
        }

        var acta = await CargarAsync(numeroActa, cancellationToken);
        return acta is null || !PuedeVer(acta, actor)
            ? ResultadoActa<ActaDto>.NoEncontrado("No se encontró el acta.")
            : ResultadoActa<ActaDto>.Ok(acta);
    }

    public async Task<ResultadoActa<byte[]>> GenerarPdfAsync(
        int numeroActa,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        var acta = await ObtenerAsync(numeroActa, usuarioId, cancellationToken);
        return acta.Estado != EstadoActa.Ok
            ? acta.Estado == EstadoActa.NoEncontrado
                ? ResultadoActa<byte[]>.NoEncontrado(acta.Mensaje ?? "No se encontró el acta.")
                : ResultadoActa<byte[]>.Invalido(acta.Mensaje ?? "No se encontró el usuario.")
            : ResultadoActa<byte[]>.Ok(GeneradorPdfActa.Generar(acta.Valor!));
    }

    public async Task<ResultadoActa<byte[]>> ExportarAsync(
        DateOnly? desde,
        DateOnly? hasta,
        int? arquitectoId,
        int? revisorId,
        int? contratistaId,
        int? construccionId,
        bool? cerrada,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        var consulta = await ConsultarAsync(
            desde,
            hasta,
            arquitectoId,
            revisorId,
            contratistaId,
            construccionId,
            cerrada,
            usuarioId,
            cancellationToken);
        if (consulta.Estado != EstadoActa.Ok)
        {
            return ResultadoActa<byte[]>.Invalido(consulta.Mensaje ?? CrearActaValidador.MensajeFechas);
        }

        return ResultadoActa<byte[]>.Ok(GeneradorExcelActas.Generar(consulta.Valor!));
    }

    public async Task<ResultadoActa<InformeItemsDto>> ConsultarItemsAsync(
        DateOnly? desde,
        DateOnly? hasta,
        int? arquitectoId,
        int? revisorId,
        int? contratistaId,
        int? construccionId,
        bool? cerrada,
        bool agrupado,
        int usuarioId,
        string? orden,
        bool descendente,
        bool romperPorActa,
        CancellationToken cancellationToken)
    {
        var fechas = ValidarRango(desde, hasta);
        if (fechas is not null)
        {
            return ResultadoActa<InformeItemsDto>.Invalido(fechas);
        }

        var actor = await CargarActorAsync(usuarioId, cancellationToken);
        if (actor is null)
        {
            return ResultadoActa<InformeItemsDto>.Invalido("No se encontró el usuario.");
        }

        var actas = Filtrar(
            _context.Actas.AsNoTracking(),
            desde,
            hasta,
            arquitectoId,
            revisorId,
            contratistaId,
            construccionId,
            cerrada,
            actor);
        var detalles = _context.ActaDetalles
            .AsNoTracking()
            .Where(detalle => actas.Any(acta => acta.NumeroActa == detalle.NumeroActa));
        if (construccionId is > 0)
        {
            detalles = detalles.Where(detalle => detalle.ConstruccionId == construccionId);
        }

        var filas = await detalles
            .Select(detalle => new
            {
                detalle.Id,
                detalle.NumeroActa,
                Construccion = detalle.Construccion.Descripcion,
                Concepto = detalle.Concepto.Descripcion,
                Material = detalle.Insumo != null ? detalle.Insumo.Descripcion : "",
                detalle.Descripcion,
                Unidad = detalle.UnidadMedida != null ? detalle.UnidadMedida.Nombre : "",
                detalle.Cantidad,
                detalle.Precio,
                detalle.Subtotal,
                detalle.Aprobado
            })
            .ToListAsync(cancellationToken);

        List<ItemInformeDto> items;
        if (agrupado && romperPorActa)
        {
            items = filas
                .GroupBy(fila => new { fila.NumeroActa, fila.Construccion, fila.Concepto, fila.Material, fila.Unidad, fila.Precio, fila.Aprobado })
                .OrderByDescending(grupo => grupo.Key.NumeroActa)
                .ThenByDescending(grupo => grupo.Sum(fila => fila.Aprobado ? fila.Subtotal : 0))
                .ThenBy(grupo => grupo.Min(fila => fila.Id))
                .Select(grupo => new ItemInformeDto(
                    null,
                    grupo.Key.NumeroActa,
                    grupo.Key.Construccion,
                    grupo.Key.Concepto,
                    grupo.Key.Material,
                    "",
                    grupo.Key.Unidad,
                    grupo.Sum(fila => fila.Cantidad),
                    grupo.Key.Precio,
                    grupo.Sum(fila => fila.Aprobado ? fila.Subtotal : 0),
                    grupo.Key.Aprobado))
                .ToList();
        }
        else if (agrupado)
        {
            items = filas
                .GroupBy(fila => new { fila.Construccion, fila.Concepto, fila.Material, fila.Unidad, fila.Precio, fila.Aprobado })
                .OrderByDescending(grupo => grupo.Sum(fila => fila.Aprobado ? fila.Subtotal : 0))
                .ThenByDescending(grupo => grupo.Max(fila => fila.NumeroActa))
                .ThenBy(grupo => grupo.Min(fila => fila.Id))
                .Select(grupo => new ItemInformeDto(
                    null,
                    null,
                    grupo.Key.Construccion,
                    grupo.Key.Concepto,
                    grupo.Key.Material,
                    "",
                    grupo.Key.Unidad,
                    grupo.Sum(fila => fila.Cantidad),
                    grupo.Key.Precio,
                    grupo.Sum(fila => fila.Aprobado ? fila.Subtotal : 0),
                    grupo.Key.Aprobado))
                .ToList();
        }
        else
        {
            items = filas
                .OrderByDescending(fila => fila.Aprobado ? fila.Subtotal : 0)
                .ThenByDescending(fila => fila.NumeroActa)
                .ThenBy(fila => fila.Id)
                .Select(fila => new ItemInformeDto(
                    fila.Id,
                    fila.NumeroActa,
                    fila.Construccion,
                    fila.Concepto,
                    fila.Material,
                    fila.Descripcion,
                    fila.Unidad,
                    fila.Cantidad,
                    fila.Precio,
                    fila.Aprobado ? fila.Subtotal : 0,
                    fila.Aprobado))
                .ToList();
        }

        items = romperPorActa ? OrdenarPorActa(items, orden, descendente) : Ordenar(items, orden, descendente);
        var encabezado = await EncabezadoAsync(
            desde,
            hasta,
            arquitectoId,
            revisorId,
            contratistaId,
            construccionId,
            cerrada,
            cancellationToken);

        return ResultadoActa<InformeItemsDto>.Ok(
            new InformeItemsDto(
                agrupado,
                desde,
                hasta,
                items,
                items.Sum(item => item.Subtotal),
                romperPorActa,
                construccionId is > 0,
                encabezado));
    }

    public async Task<ResultadoActa<InformeMaterialesDto>> ConsultarMaterialesAsync(
        DateOnly? desde,
        DateOnly? hasta,
        int? arquitectoId,
        int? revisorId,
        int? contratistaId,
        int? construccionId,
        bool? cerrada,
        int? proveedorId,
        int usuarioId,
        CancellationToken cancellationToken,
        string? orden = null,
        bool descendente = false,
        bool agrupado = true)
    {
        var fechas = ValidarRango(desde, hasta);
        if (fechas is not null)
        {
            return ResultadoActa<InformeMaterialesDto>.Invalido(fechas);
        }

        var actor = await CargarActorAsync(usuarioId, cancellationToken);
        if (actor is null)
        {
            return ResultadoActa<InformeMaterialesDto>.Invalido("No se encontró el usuario.");
        }

        var actas = Filtrar(
            _context.Actas.AsNoTracking(),
            desde,
            hasta,
            arquitectoId,
            revisorId,
            contratistaId,
            construccionId,
            cerrada,
            actor);
        var detalles = _context.ActaDetalles
            .AsNoTracking()
            .Where(detalle => actas.Any(acta => acta.NumeroActa == detalle.NumeroActa));
        if (construccionId is > 0)
        {
            detalles = detalles.Where(detalle => detalle.ConstruccionId == construccionId);
        }

        detalles = detalles.Where(detalle => detalle.InsumoId != null);
        if (proveedorId is > 0)
        {
            detalles = detalles.Where(detalle => _context.ProveedorMateriales.Any(oferta =>
                oferta.ProveedorId == proveedorId && oferta.InsumoId == detalle.InsumoId));
        }

        var filas = await detalles
            .Select(detalle => new
            {
                detalle.NumeroActa,
                detalle.InsumoId,
                Codigo = detalle.Insumo != null ? detalle.Insumo.Codigo ?? "" : "",
                Material = detalle.Insumo != null ? detalle.Insumo.Descripcion : "",
                Unidad = detalle.UnidadMedida != null ? detalle.UnidadMedida.Nombre : "",
                detalle.Cantidad,
                detalle.Precio,
                detalle.Subtotal,
                detalle.Aprobado
            })
            .ToListAsync(cancellationToken);

        var comparador = StringComparer.Create(CultureInfo.GetCultureInfo("es-CO"), ignoreCase: true);
        var items = agrupado
            ? filas
                .GroupBy(fila => new { fila.InsumoId, fila.Codigo, fila.Material, fila.Unidad, fila.Aprobado })
                .Select(grupo =>
                {
                    var cantidad = grupo.Sum(fila => fila.Cantidad);
                    var precio = cantidad == 0 ? 0 : grupo.Sum(fila => fila.Cantidad * fila.Precio) / cantidad;
                    return new ItemMaterialInformeDto(
                        grupo.Key.Codigo,
                        grupo.Key.Material,
                        grupo.Key.Unidad,
                        cantidad,
                        precio,
                        grupo.Sum(fila => fila.Aprobado ? fila.Subtotal : 0),
                        grupo.Key.Aprobado);
                })
                .OrderByDescending(item => item.Subtotal)
                .ThenBy(item => item.Material, comparador)
                .ToList()
            : filas
                .Select(fila => new ItemMaterialInformeDto(
                    fila.Codigo,
                    fila.Material,
                    fila.Unidad,
                    fila.Cantidad,
                    fila.Precio,
                    fila.Aprobado ? fila.Subtotal : 0,
                    fila.Aprobado,
                    fila.NumeroActa))
                .OrderBy(item => item.NumeroActa)
                .ThenBy(item => item.Material, comparador)
                .ThenByDescending(item => item.Subtotal)
                .ToList();
        items = OrdenarMateriales(items, orden, descendente);

        var encabezado = await EncabezadoAsync(
            desde,
            hasta,
            arquitectoId,
            revisorId,
            contratistaId,
            construccionId,
            cerrada,
            cancellationToken,
            proveedorId);

        return ResultadoActa<InformeMaterialesDto>.Ok(
            new InformeMaterialesDto(desde, hasta, items, items.Sum(item => item.Subtotal), encabezado, agrupado));
    }

    public async Task<ResultadoActa<byte[]>> ExportarMaterialesAsync(
        DateOnly? desde,
        DateOnly? hasta,
        int? arquitectoId,
        int? revisorId,
        int? contratistaId,
        int? construccionId,
        bool? cerrada,
        int? proveedorId,
        int usuarioId,
        CancellationToken cancellationToken,
        bool agrupado = true)
    {
        var consulta = await ConsultarMaterialesAsync(
            desde,
            hasta,
            arquitectoId,
            revisorId,
            contratistaId,
            construccionId,
            cerrada,
            proveedorId,
            usuarioId,
            cancellationToken,
            agrupado: agrupado);
        return consulta.Estado == EstadoActa.Ok
            ? ResultadoActa<byte[]>.Ok(GeneradorExcelMateriales.Generar(consulta.Valor!))
            : ResultadoActa<byte[]>.Invalido(consulta.Mensaje ?? CrearActaValidador.MensajeFechas);
    }

    public async Task<ResultadoActa<byte[]>> ImprimirMaterialesAsync(
        DateOnly? desde,
        DateOnly? hasta,
        int? arquitectoId,
        int? revisorId,
        int? contratistaId,
        int? construccionId,
        bool? cerrada,
        int? proveedorId,
        int usuarioId,
        string? orden,
        bool descendente,
        CancellationToken cancellationToken,
        bool agrupado = true)
    {
        var consulta = await ConsultarMaterialesAsync(
            desde,
            hasta,
            arquitectoId,
            revisorId,
            contratistaId,
            construccionId,
            cerrada,
            proveedorId,
            usuarioId,
            cancellationToken,
            orden,
            descendente,
            agrupado);
        return consulta.Estado == EstadoActa.Ok
            ? ResultadoActa<byte[]>.Ok(GeneradorPdfInformeMateriales.Generar(consulta.Valor!))
            : ResultadoActa<byte[]>.Invalido(consulta.Mensaje ?? CrearActaValidador.MensajeFechas);
    }

    public async Task<ResultadoActa<byte[]>> ImprimirItemsAsync(
        DateOnly? desde,
        DateOnly? hasta,
        int? arquitectoId,
        int? revisorId,
        int? contratistaId,
        int? construccionId,
        bool? cerrada,
        bool agrupado,
        int usuarioId,
        string? orden,
        bool descendente,
        bool romperPorActa,
        CancellationToken cancellationToken)
    {
        var consulta = await ConsultarItemsAsync(
            desde,
            hasta,
            arquitectoId,
            revisorId,
            contratistaId,
            construccionId,
            cerrada,
            agrupado,
            usuarioId,
            orden,
            descendente,
            romperPorActa,
            cancellationToken);
        return consulta.Estado == EstadoActa.Ok
            ? ResultadoActa<byte[]>.Ok(GeneradorPdfInformeItems.Generar(consulta.Valor!))
            : ResultadoActa<byte[]>.Invalido(consulta.Mensaje ?? CrearActaValidador.MensajeFechas);
    }

    private static List<ItemMaterialInformeDto> OrdenarMateriales(List<ItemMaterialInformeDto> items, string? orden, bool descendente)
    {
        var campo = orden?.Trim().ToLowerInvariant();
        if (campo is not ("acta" or "codigo" or "material" or "unidad" or "cantidad" or "precio" or "estado" or "subtotal"))
        {
            return items;
        }

        var filas = items.Select((item, indice) => (item, indice)).ToList();
        filas.Sort((izquierda, derecha) =>
        {
            var comparacion = campo switch
            {
                "acta" => (izquierda.item.NumeroActa ?? 0).CompareTo(derecha.item.NumeroActa ?? 0),
                "cantidad" => izquierda.item.Cantidad.CompareTo(derecha.item.Cantidad),
                "precio" => izquierda.item.Precio.CompareTo(derecha.item.Precio),
                "subtotal" => izquierda.item.Subtotal.CompareTo(derecha.item.Subtotal),
                "estado" => string.Compare(
                    izquierda.item.Aprobado ? "aprobado" : "rechazado",
                    derecha.item.Aprobado ? "aprobado" : "rechazado",
                    StringComparison.CurrentCultureIgnoreCase),
                "codigo" => Comparacion.Compare(izquierda.item.Codigo, derecha.item.Codigo, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace),
                "unidad" => Comparacion.Compare(izquierda.item.Unidad, derecha.item.Unidad, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace),
                _ => Comparacion.Compare(izquierda.item.Material, derecha.item.Material, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace)
            };
            if (descendente)
            {
                comparacion = -comparacion;
            }

            return comparacion == 0 ? izquierda.indice.CompareTo(derecha.indice) : comparacion;
        });
        return filas.Select(fila => fila.item).ToList();
    }

    private async Task<IReadOnlyList<string>> EncabezadoAsync(
        DateOnly? desde,
        DateOnly? hasta,
        int? arquitectoId,
        int? revisorId,
        int? contratistaId,
        int? construccionId,
        bool? cerrada,
        CancellationToken cancellationToken,
        int? proveedorId = null)
    {
        var lineas = new List<string>();
        if (desde is DateOnly inicio && hasta is DateOnly fin)
        {
            lineas.Add($"Del {inicio:dd/MM/yyyy} al {fin:dd/MM/yyyy}");
        }
        else if (desde is DateOnly soloDesde)
        {
            lineas.Add($"Desde {soloDesde:dd/MM/yyyy}");
        }
        else if (hasta is DateOnly soloHasta)
        {
            lineas.Add($"Hasta {soloHasta:dd/MM/yyyy}");
        }

        if (await TextoCatalogoAsync(_context.Construcciones, construccionId, item => item.Descripcion, cancellationToken) is string construccion)
        {
            lineas.Add($"Construcción: {construccion}");
        }

        if (await TextoCatalogoAsync(_context.Arquitectos, arquitectoId, item => item.Nombre, cancellationToken) is string arquitecto)
        {
            lineas.Add($"Arquitecto: {arquitecto}");
        }

        if (await TextoCatalogoAsync(_context.Revisores, revisorId, item => item.Nombre, cancellationToken) is string revisor)
        {
            lineas.Add($"Revisor: {revisor}");
        }

        if (await TextoCatalogoAsync(_context.Contratistas, contratistaId, item => item.Nombre, cancellationToken) is string contratista)
        {
            lineas.Add($"Contratista: {contratista}");
        }

        if (await TextoCatalogoAsync(_context.Proveedores, proveedorId, item => item.Nombre, cancellationToken) is string proveedor)
        {
            lineas.Add($"Proveedor: {proveedor}");
        }

        if (cerrada is bool estado)
        {
            lineas.Add(estado ? "Estado: Cerrada" : "Estado: Abierta");
        }

        return lineas;
    }

    private static async Task<string?> TextoCatalogoAsync<T>(
        IQueryable<T> consulta,
        int? id,
        Expression<Func<T, string>> texto,
        CancellationToken cancellationToken)
        where T : class, ICatalogo
    {
        if (id is not > 0)
        {
            return null;
        }

        var nombre = await consulta.AsNoTracking().Where(item => item.Id == id).Select(texto).FirstOrDefaultAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(nombre) ? null : nombre;
    }

    private static readonly CompareInfo Comparacion = CultureInfo.GetCultureInfo("es-CO").CompareInfo;

    private static List<ItemInformeDto> Ordenar(List<ItemInformeDto> items, string? orden, bool descendente)
    {
        var campo = orden?.Trim().ToLowerInvariant();
        if (campo is not ("acta" or "construccion" or "concepto" or "material" or "unidad" or "cantidad" or "precio" or "estado" or "subtotal"))
        {
            return items;
        }

        var filas = items.Select((item, indice) => (item, indice)).ToList();
        filas.Sort((izquierda, derecha) =>
        {
            var comparacion = CompararItem(izquierda.item, derecha.item, campo);
            if (descendente)
            {
                comparacion = -comparacion;
            }

            return comparacion == 0 ? izquierda.indice.CompareTo(derecha.indice) : comparacion;
        });
        return filas.Select(fila => fila.item).ToList();
    }

    private static List<ItemInformeDto> OrdenarPorActa(List<ItemInformeDto> items, string? orden, bool descendente)
    {
        var campo = orden?.Trim().ToLowerInvariant();
        var campoValido = campo is "acta" or "construccion" or "concepto" or "material" or "unidad" or "cantidad" or "precio" or "estado" or "subtotal";
        var filas = items.Select((item, indice) => (item, indice)).ToList();
        filas.Sort((izquierda, derecha) =>
        {
            if (campoValido && campo == "acta")
            {
                var porNumero = (izquierda.item.NumeroActa ?? 0).CompareTo(derecha.item.NumeroActa ?? 0);
                if (descendente)
                {
                    porNumero = -porNumero;
                }

                return porNumero == 0 ? izquierda.indice.CompareTo(derecha.indice) : porNumero;
            }

            var porActa = (derecha.item.NumeroActa ?? 0).CompareTo(izquierda.item.NumeroActa ?? 0);
            if (porActa != 0 || !campoValido)
            {
                return porActa != 0 ? porActa : izquierda.indice.CompareTo(derecha.indice);
            }

            var detalle = CompararItem(izquierda.item, derecha.item, campo!);
            if (descendente)
            {
                detalle = -detalle;
            }

            return detalle == 0 ? izquierda.indice.CompareTo(derecha.indice) : detalle;
        });
        return filas.Select(fila => fila.item).ToList();
    }

    private static int CompararItem(ItemInformeDto izquierda, ItemInformeDto derecha, string campo)
    {
        return campo switch
        {
            "acta" => (izquierda.NumeroActa ?? 0).CompareTo(derecha.NumeroActa ?? 0),
            "cantidad" => izquierda.Cantidad.CompareTo(derecha.Cantidad),
            "precio" => izquierda.Precio.CompareTo(derecha.Precio),
            "subtotal" => izquierda.Subtotal.CompareTo(derecha.Subtotal),
            "estado" => string.Compare(
                izquierda.Aprobado ? "aprobado" : "rechazado",
                derecha.Aprobado ? "aprobado" : "rechazado",
                StringComparison.Ordinal),
            "construccion" => Comparacion.Compare(izquierda.Construccion, derecha.Construccion, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace),
            "concepto" => Comparacion.Compare(izquierda.Concepto, derecha.Concepto, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace),
            "material" => Comparacion.Compare(izquierda.Material, derecha.Material, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace),
            "unidad" => Comparacion.Compare(izquierda.Unidad, derecha.Unidad, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace),
            _ => 0
        };
    }

    private async Task<ResultadoActa<ActaDto>> RevisarAsync(
        Acta acta,
        CrearActaSolicitud solicitud,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        if (solicitud.Detalles.Any(linea => linea.Id is not > 0))
        {
            return ResultadoActa<ActaDto>.Invalido(CrearActaValidador.MensajeRevisorAgrega);
        }

        var actuales = acta.Detalles.ToDictionary(detalle => detalle.Id);
        var ids = solicitud.Detalles.Select(linea => linea.Id!.Value).ToList();
        if (ids.Distinct().Count() != ids.Count)
        {
            return ResultadoActa<ActaDto>.Invalido("Hay un ítem repetido en el acta.");
        }

        if (ids.Any(id => !actuales.ContainsKey(id)))
        {
            return ResultadoActa<ActaDto>.Invalido("Hay un ítem que no pertenece al acta.");
        }

        foreach (var linea in solicitud.Detalles)
        {
            var detalle = actuales[linea.Id!.Value];
            if (detalle.Aprobado == linea.Aprobado)
            {
                continue;
            }

            await _auditoria.RegistrarAsync(
                "ActaDetalle",
                detalle.Id.ToString(),
                linea.Aprobado ? AccionAuditoria.Aprobar : AccionAuditoria.Rechazar,
                usuarioId,
                CambiosAuditoria.Json("Aprobado", detalle.Aprobado, linea.Aprobado),
                cancellationToken);
            detalle.Aprobado = linea.Aprobado;
        }

        var conflictoRevision = await GuardarAsync(cancellationToken);
        if (conflictoRevision is not null)
        {
            return ResultadoActa<ActaDto>.Conflicto(conflictoRevision);
        }

        return ResultadoActa<ActaDto>.Ok((await CargarAsync(acta.NumeroActa, cancellationToken))!);
    }

    private static decimal TotalActa(IEnumerable<(decimal Subtotal, bool Aprobado)> lineas) =>
        lineas.Sum(linea => linea.Aprobado ? linea.Subtotal : 0);

    private async Task<ResultadoActa<ActaDto>> ActualizarPerfilAsync(
        Acta acta,
        CrearActaSolicitud solicitud,
        ActorActa actor,
        CancellationToken cancellationToken)
    {
        var ids = solicitud.Detalles.Where(linea => linea.Id is > 0).Select(linea => linea.Id!.Value).ToList();
        if (ids.Distinct().Count() != ids.Count)
        {
            return ResultadoActa<ActaDto>.Invalido("Hay un ítem repetido en el acta.");
        }

        var actuales = acta.Detalles.ToDictionary(detalle => detalle.Id);
        if (ids.Any(id => !actuales.ContainsKey(id)))
        {
            return ResultadoActa<ActaDto>.Invalido("Hay un ítem que no pertenece al acta.");
        }

        var propias = acta.Detalles
            .Where(detalle => detalle.TipoUsuarioId == actor.TipoUsuarioId)
            .Select(detalle => detalle.Id)
            .ToHashSet();
        var conservadas = ids.Where(propias.Contains).ToHashSet();
        foreach (var detalle in acta.Detalles.Where(detalle => propias.Contains(detalle.Id) && !conservadas.Contains(detalle.Id)).ToList())
        {
            acta.Detalles.Remove(detalle);
            _context.ActaDetalles.Remove(detalle);
        }

        foreach (var linea in solicitud.Detalles)
        {
            if (linea.Id is > 0)
            {
                var detalle = actuales[linea.Id.Value];
                if (detalle.TipoUsuarioId == actor.TipoUsuarioId)
                {
                    CopiarDetalle(detalle, linea);
                }
            }
            else
            {
                acta.Detalles.Add(CrearDetalle(linea, actor.TipoUsuarioId));
            }
        }

        var conflictoPerfil = await GuardarAsync(cancellationToken);
        if (conflictoPerfil is not null)
        {
            return ResultadoActa<ActaDto>.Conflicto(conflictoPerfil);
        }

        return ResultadoActa<ActaDto>.Ok((await CargarAsync(acta.NumeroActa, cancellationToken))!);
    }

    private async Task<string?> GuardarAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateConcurrencyException)
        {
            return "Otra persona modificó esta acta.";
        }
    }

    private static bool VersionDistinta(byte[]? actual, string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            return !(actual ?? []).SequenceEqual(Convert.FromBase64String(token));
        }
        catch (FormatException)
        {
            return true;
        }
    }

    private async Task<ActorActa?> CargarActorAsync(int usuarioId, CancellationToken cancellationToken)
    {
        return await _context.Usuarios
            .AsNoTracking()
            .Where(usuario => usuario.Id == usuarioId && usuario.Activo)
            .Select(usuario => new ActorActa(
                usuario.TipoUsuarioId,
                usuario.ArquitectoId,
                usuario.ContratistaId,
                usuario.RevisorId))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static bool PuedeVer(Acta acta, ActorActa actor) =>
        PuedeVer(acta.ArquitectoId, acta.ContratistaId, acta.RevisorId, actor);

    private static bool PuedeVer(ActaDto acta, ActorActa actor) =>
        PuedeVer(acta.ArquitectoId, acta.ContratistaId, acta.RevisorId, actor);

    private static bool PuedeVer(int arquitectoId, int contratistaId, int revisorId, ActorActa actor) =>
        actor.TipoUsuarioId switch
        {
            PerfilesUsuario.Administrador => true,
            PerfilesUsuario.Arquitecto => arquitectoId == actor.ArquitectoId,
            PerfilesUsuario.Contratista => contratistaId == actor.ContratistaId,
            PerfilesUsuario.Revisor => revisorId == actor.RevisorId,
            _ => false
        };

    private IQueryable<Acta> Filtrar(
        IQueryable<Acta> consulta,
        DateOnly? desde,
        DateOnly? hasta,
        int? arquitectoId,
        int? revisorId,
        int? contratistaId,
        int? construccionId,
        bool? cerrada,
        ActorActa actor)
    {
        consulta = actor.TipoUsuarioId switch
        {
            PerfilesUsuario.Arquitecto => consulta.Where(acta => acta.ArquitectoId == actor.ArquitectoId),
            PerfilesUsuario.Contratista => consulta.Where(acta => acta.ContratistaId == actor.ContratistaId),
            PerfilesUsuario.Revisor => consulta.Where(acta => acta.RevisorId == actor.RevisorId),
            PerfilesUsuario.Administrador => consulta,
            _ => consulta.Where(acta => false)
        };

        if (desde is DateOnly inicio)
        {
            consulta = consulta.Where(acta => acta.FechaFinal >= inicio);
        }

        if (hasta is DateOnly fin)
        {
            consulta = consulta.Where(acta => acta.FechaInicial <= fin);
        }

        if (arquitectoId is > 0)
        {
            consulta = consulta.Where(acta => acta.ArquitectoId == arquitectoId);
        }

        if (revisorId is > 0)
        {
            consulta = consulta.Where(acta => acta.RevisorId == revisorId);
        }

        if (contratistaId is > 0)
        {
            consulta = consulta.Where(acta => acta.ContratistaId == contratistaId);
        }

        if (construccionId is > 0)
        {
            consulta = consulta.Where(acta => acta.Detalles.Any(detalle => detalle.ConstruccionId == construccionId));
        }

        if (cerrada is bool estado)
        {
            consulta = consulta.Where(acta => acta.Cerrada == estado);
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
            .Include(item => item.Detalles).ThenInclude(detalle => detalle.Insumo)
            .Include(item => item.Detalles).ThenInclude(detalle => detalle.TipoUsuario)
            .Include(item => item.Detalles).ThenInclude(detalle => detalle.PresupuestoItem)
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
                detalle.UnidadMedida?.Nombre ?? "",
                detalle.InsumoId,
                detalle.Insumo?.Descripcion,
                detalle.Cantidad,
                detalle.Precio,
                detalle.Descripcion,
                detalle.Aprobado ? detalle.Subtotal : 0,
                detalle.TipoUsuarioId,
                detalle.TipoUsuario.Nombre,
                detalle.Aprobado,
                detalle.PresupuestoItemId,
                detalle.PresupuestoItem is null ? null : detalle.PresupuestoItem.Codigo + " " + detalle.PresupuestoItem.Descripcion))
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
            TotalActa(detalles.Select(detalle => (detalle.Subtotal, detalle.Aprobado))),
            detalles,
            acta.Cerrada,
            Convert.ToBase64String(acta.RowVersion ?? []));
    }

    private async Task<string?> ValidarSolicitud(CrearActaSolicitud solicitud, CancellationToken cancellationToken)
    {
        var fechas = ValidarRango(solicitud.FechaInicial, solicitud.FechaFinal);
        if (fechas is not null)
        {
            return fechas;
        }

        solicitud.Detalles ??= [];

        if (!await ExisteActivo(_context.Arquitectos, solicitud.ArquitectoId, cancellationToken))
        {
            return CrearActaValidador.MensajeArquitecto;
        }

        if (!await ExisteActivo(_context.Revisores, solicitud.RevisorId, cancellationToken))
        {
            return CrearActaValidador.MensajeRevisor;
        }

        if (!await ExisteActivo(_context.Contratistas, solicitud.ContratistaId, cancellationToken))
        {
            return CrearActaValidador.MensajeContratista;
        }

        if (!await TodosActivos(_context.Construcciones, solicitud.Detalles.Select(linea => linea.ConstruccionId), cancellationToken))
        {
            return CrearActaValidador.MensajeConstruccion;
        }

        if (!await TodosActivos(_context.Conceptos, solicitud.Detalles.Select(linea => linea.ConceptoId), cancellationToken))
        {
            return CrearActaValidador.MensajeConcepto;
        }

        var unidades = solicitud.Detalles
            .Where(linea => linea.UnidadMedidaId is > 0)
            .Select(linea => linea.UnidadMedidaId!.Value)
            .ToList();
        if (unidades.Count > 0 &&
            !await TodosActivos(_context.UnidadesMedida, unidades, cancellationToken))
        {
            return CrearActaValidador.MensajeUnidad;
        }

        var insumos = solicitud.Detalles
            .Where(linea => linea.InsumoId is > 0)
            .Select(linea => linea.InsumoId!.Value)
            .ToList();
        if (insumos.Count > 0 &&
            !await TodosActivos(_context.Insumos, insumos, cancellationToken))
        {
            return "Debe seleccionar un insumo válido.";
        }

        var presupuestoIds = solicitud.Detalles.Where(linea => linea.PresupuestoItemId is > 0).Select(linea => linea.PresupuestoItemId!.Value).Distinct().ToList();
        if (presupuestoIds.Count > 0)
        {
            var validos = await _context.Set<PresupuestoItem>()
                .Where(item => presupuestoIds.Contains(item.Id) && item.Capitulo.Presupuesto.Estado == EstadoPresupuesto.Aprobado)
                .Select(item => new { item.Id, item.Capitulo.Presupuesto.ConstruccionId })
                .ToListAsync(cancellationToken);
            if (solicitud.Detalles.Any(linea => linea.PresupuestoItemId is > 0 && !validos.Any(item => item.Id == linea.PresupuestoItemId && item.ConstruccionId == linea.ConstruccionId)))
            {
                return "El ítem de presupuesto no pertenece a la construcción o el presupuesto no está aprobado.";
            }
        }

        if (solicitud.Detalles.Any(linea => decimal.Round(linea.Cantidad, 4, MidpointRounding.AwayFromZero) <= 0))
        {
            return CrearActaValidador.MensajeCantidad;
        }

        if (solicitud.Detalles.Any(linea => decimal.Round(linea.Precio, 2, MidpointRounding.AwayFromZero) < 0))
        {
            return CrearActaValidador.MensajePrecio;
        }

        return null;
    }

    private static ActaDetalle CrearDetalle(LineaActaSolicitud linea, int tipoUsuarioId)
    {
        var detalle = new ActaDetalle
        {
            Descripcion = (linea.Descripcion ?? "").Trim(),
            TipoUsuarioId = tipoUsuarioId,
            Aprobado = true
        };
        CopiarDetalle(detalle, linea);
        return detalle;
    }

    private sealed record ActorActa(int TipoUsuarioId, int? ArquitectoId, int? ContratistaId, int? RevisorId);

    private static void CopiarDetalle(ActaDetalle detalle, LineaActaSolicitud linea)
    {
        detalle.ConstruccionId = linea.ConstruccionId;
        detalle.ConceptoId = linea.ConceptoId;
        detalle.UnidadMedidaId = linea.UnidadMedidaId is > 0 ? linea.UnidadMedidaId : null;
        detalle.InsumoId = linea.InsumoId is > 0 ? linea.InsumoId : null;
        detalle.Cantidad = decimal.Round(linea.Cantidad, 4, MidpointRounding.AwayFromZero);
        detalle.Precio = decimal.Round(linea.Precio, 2, MidpointRounding.AwayFromZero);
        detalle.Descripcion = (linea.Descripcion ?? "").Trim();
        detalle.PresupuestoItemId = linea.PresupuestoItemId is > 0 ? linea.PresupuestoItemId : null;
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
