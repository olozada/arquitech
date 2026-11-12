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

public sealed partial class ActaServicio : IActaServicio
{
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


}
