using Arquitectos.Application;
using Arquitectos.Application.Facturas;
using Arquitectos.Application.Presupuestos;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Infrastructure.Facturas;

public sealed class FacturaServicio : IFacturaServicio
{
    private readonly ArquitectosDbContext _context;

    public FacturaServicio(ArquitectosDbContext context)
    {
        _context = context;
    }

    public async Task<ResultadoFactura<ConsultaFacturasDto>> ConsultarAsync(
        DateOnly? desde,
        DateOnly? hasta,
        int? proveedorId,
        int? construccionId,
        string? numero,
        string? nombreProveedor,
        int pagina,
        int tamano,
        CancellationToken cancellationToken)
    {
        if (desde is not null && hasta is not null && hasta < desde)
        {
            return ResultadoFactura<ConsultaFacturasDto>.Invalido("La fecha final debe ser mayor o igual que la fecha inicial.");
        }

        var (paginaNormal, tamanoNormal) = Paginacion.Normalizar(pagina, tamano);
        var consulta = Filtrar(desde, hasta, proveedorId, construccionId, numero, nombreProveedor);
        var total = await consulta.CountAsync(cancellationToken);
        var totalGeneral = await consulta
            .SelectMany(factura => factura.Detalles)
            .SumAsync(detalle => (decimal?)detalle.Subtotal, cancellationToken) ?? 0;
        var items = await consulta
            .OrderByDescending(factura => factura.Fecha)
            .ThenByDescending(factura => factura.Id)
            .Skip((paginaNormal - 1) * tamanoNormal)
            .Take(tamanoNormal)
            .Select(factura => new FacturaResumenDto(
                factura.Id,
                factura.Numero,
                factura.Fecha,
                factura.Proveedor.Nombre,
                factura.OrdenCompra,
                factura.Observacion,
                factura.Detalles.Sum(detalle => detalle.Subtotal)))
            .ToListAsync(cancellationToken);

        return ResultadoFactura<ConsultaFacturasDto>.Ok(
            new ConsultaFacturasDto(items, total, paginaNormal, tamanoNormal, totalGeneral));
    }

    public async Task<ResultadoFactura<FacturaDto>> ObtenerAsync(int id, CancellationToken cancellationToken)
    {
        var factura = await CargarAsync(id, cancellationToken);
        return factura is null
            ? ResultadoFactura<FacturaDto>.NoEncontrado(CrearFacturaValidador.MensajeNoDisponible)
            : ResultadoFactura<FacturaDto>.Ok(factura);
    }

    public async Task<ResultadoFactura<FacturaDto>> CrearAsync(
        CrearFacturaSolicitud solicitud,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        var error = ValidarForma(solicitud) ?? await ValidarReferenciasAsync(solicitud, cancellationToken);
        if (error is not null)
        {
            return ResultadoFactura<FacturaDto>.Invalido(error);
        }

        if (await NumeroRepetidoAsync(0, solicitud, cancellationToken))
        {
            return ResultadoFactura<FacturaDto>.Invalido(CrearFacturaValidador.MensajeDuplicada);
        }

        var unidades = await UnidadesDeMaterialesAsync(solicitud, cancellationToken);
        var ahora = DateTime.UtcNow;
        var factura = new Factura
        {
            Numero = solicitud.Numero.Trim(),
            Fecha = solicitud.Fecha,
            ProveedorId = solicitud.ProveedorId,
            OrdenCompra = Texto(solicitud.OrdenCompra),
            Observacion = Texto(solicitud.Observacion),
            FechaRegistro = ahora,
            ModificadaEn = ahora,
            UsuarioRegistroId = usuarioId
        };
        foreach (var linea in solicitud.Detalles)
        {
            factura.Detalles.Add(CrearDetalle(linea, unidades));
        }

        _context.Facturas.Add(factura);
        await _context.SaveChangesAsync(cancellationToken);
        return ResultadoFactura<FacturaDto>.Ok((await CargarAsync(factura.Id, cancellationToken))!);
    }

    public async Task<ResultadoFactura<FacturaDto>> ActualizarAsync(
        int id,
        CrearFacturaSolicitud solicitud,
        CancellationToken cancellationToken)
    {
        var factura = await _context.Facturas
            .Include(item => item.Detalles)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (factura is null)
        {
            return ResultadoFactura<FacturaDto>.NoEncontrado(CrearFacturaValidador.MensajeNoDisponible);
        }

        if (ReglasPresupuesto.VersionDistinta(factura.RowVersion, solicitud.RowVersion))
        {
            return ResultadoFactura<FacturaDto>.Conflicto(CrearFacturaValidador.MensajeConflicto);
        }

        var error = ValidarForma(solicitud) ?? await ValidarReferenciasAsync(solicitud, cancellationToken);
        if (error is not null)
        {
            return ResultadoFactura<FacturaDto>.Invalido(error);
        }

        if (await NumeroRepetidoAsync(id, solicitud, cancellationToken))
        {
            return ResultadoFactura<FacturaDto>.Invalido(CrearFacturaValidador.MensajeDuplicada);
        }

        factura.Numero = solicitud.Numero.Trim();
        factura.Fecha = solicitud.Fecha;
        factura.ProveedorId = solicitud.ProveedorId;
        factura.OrdenCompra = Texto(solicitud.OrdenCompra);
        factura.Observacion = Texto(solicitud.Observacion);
        factura.ModificadaEn = DateTime.UtcNow;
        var unidades = await UnidadesDeMaterialesAsync(solicitud, cancellationToken);
        _context.FacturaDetalles.RemoveRange(factura.Detalles);
        factura.Detalles.Clear();
        foreach (var linea in solicitud.Detalles)
        {
            factura.Detalles.Add(CrearDetalle(linea, unidades));
        }

        await _context.SaveChangesAsync(cancellationToken);
        return ResultadoFactura<FacturaDto>.Ok((await CargarAsync(id, cancellationToken))!);
    }

    public async Task<ResultadoFactura<bool>> EliminarAsync(int id, CancellationToken cancellationToken)
    {
        var factura = await _context.Facturas.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (factura is null)
        {
            return ResultadoFactura<bool>.NoEncontrado(CrearFacturaValidador.MensajeNoDisponible);
        }

        _context.Facturas.Remove(factura);
        await _context.SaveChangesAsync(cancellationToken);
        return ResultadoFactura<bool>.Ok(true);
    }

    public async Task<ResultadoFactura<byte[]>> PdfAsync(int id, CancellationToken cancellationToken)
    {
        var factura = await CargarAsync(id, cancellationToken);
        return factura is null
            ? ResultadoFactura<byte[]>.NoEncontrado(CrearFacturaValidador.MensajeNoDisponible)
            : ResultadoFactura<byte[]>.Ok(GeneradorPdfFactura.Generar(factura));
    }

    public Task<ResultadoFactura<InformeFacturasDto>> ConsultarItemsAsync(
        DateOnly? desde,
        DateOnly? hasta,
        int? proveedorId,
        int? construccionId,
        bool agrupado,
        int pagina,
        int tamano,
        bool todo,
        CancellationToken cancellationToken) =>
        ArmarInformeAsync(desde, hasta, proveedorId, construccionId, agrupado, pagina, tamano, todo, cancellationToken);

    public async Task<ResultadoFactura<byte[]>> ExcelItemsAsync(
        DateOnly? desde,
        DateOnly? hasta,
        int? proveedorId,
        int? construccionId,
        bool agrupado,
        CancellationToken cancellationToken)
    {
        var informe = await ArmarInformeAsync(desde, hasta, proveedorId, construccionId, agrupado, 1, 1, true, cancellationToken);
        return informe.Estado == EstadoFactura.Ok
            ? ResultadoFactura<byte[]>.Ok(GeneradorExcelFacturas.Generar(informe.Valor!))
            : ResultadoFactura<byte[]>.Invalido(informe.Mensaje ?? "");
    }

    public async Task<ResultadoFactura<byte[]>> PdfItemsAsync(
        DateOnly? desde,
        DateOnly? hasta,
        int? proveedorId,
        int? construccionId,
        bool agrupado,
        CancellationToken cancellationToken)
    {
        var informe = await ArmarInformeAsync(desde, hasta, proveedorId, construccionId, agrupado, 1, 1, true, cancellationToken);
        if (informe.Estado != EstadoFactura.Ok)
        {
            return ResultadoFactura<byte[]>.Invalido(informe.Mensaje ?? "");
        }

        return ResultadoFactura<byte[]>.Ok(GeneradorPdfFactura.GenerarInforme(informe.Valor!));
    }

    private async Task<ResultadoFactura<InformeFacturasDto>> ArmarInformeAsync(
        DateOnly? desde,
        DateOnly? hasta,
        int? proveedorId,
        int? construccionId,
        bool agrupado,
        int pagina,
        int tamano,
        bool todo,
        CancellationToken cancellationToken)
    {
        if (desde is not null && hasta is not null && hasta < desde)
        {
            return ResultadoFactura<InformeFacturasDto>.Invalido("La fecha final debe ser mayor o igual que la fecha inicial.");
        }

        var (paginaNormal, tamanoNormal) = Paginacion.Normalizar(pagina, tamano);
        var lineas = FiltrarLineas(desde, hasta, proveedorId, construccionId);
        var totalValor = await lineas.SumAsync(detalle => (decimal?)detalle.Subtotal, cancellationToken) ?? 0;
        var encabezado = await EncabezadoAsync(desde, hasta, proveedorId, construccionId, cancellationToken);
        List<ItemFacturaInformeDto> items;
        int cantidad;
        if (agrupado)
        {
            var grupos = lineas.GroupBy(detalle => new
            {
                Construccion = detalle.Construccion.Descripcion,
                Codigo = detalle.Insumo.Codigo ?? "",
                Insumo = detalle.Insumo.Descripcion,
                Unidad = detalle.UnidadMedida != null ? detalle.UnidadMedida.Nombre : detalle.Insumo.UnidadCompra
            });
            cantidad = await grupos.CountAsync(cancellationToken);
            var consulta = grupos
                .Select(grupo => new
                {
                    grupo.Key.Construccion,
                    grupo.Key.Codigo,
                    grupo.Key.Insumo,
                    grupo.Key.Unidad,
                    Cantidad = grupo.Sum(detalle => detalle.Cantidad),
                    Subtotal = grupo.Sum(detalle => detalle.Subtotal)
                })
                .OrderByDescending(grupo => grupo.Subtotal)
                .ThenBy(grupo => grupo.Insumo);
            var filas = todo
                ? await consulta.ToListAsync(cancellationToken)
                : await consulta.Skip((paginaNormal - 1) * tamanoNormal).Take(tamanoNormal).ToListAsync(cancellationToken);
            items = filas.Select(fila => new ItemFacturaInformeDto(
                null,
                null,
                null,
                fila.Construccion,
                fila.Codigo,
                fila.Insumo,
                "",
                fila.Unidad,
                fila.Cantidad,
                fila.Cantidad == 0 ? 0 : decimal.Round(fila.Subtotal / fila.Cantidad, 2, MidpointRounding.AwayFromZero),
                fila.Subtotal)).ToList();
        }
        else
        {
            cantidad = await lineas.CountAsync(cancellationToken);
            var consulta = lineas
                .OrderByDescending(detalle => detalle.Factura.Fecha)
                .ThenByDescending(detalle => detalle.FacturaId)
                .ThenBy(detalle => detalle.Id)
                .Select(detalle => new ItemFacturaInformeDto(
                    detalle.FacturaId,
                    detalle.Factura.Numero,
                    detalle.Factura.Fecha,
                    detalle.Construccion.Descripcion,
                    detalle.Insumo.Codigo ?? "",
                    detalle.Insumo.Descripcion,
                    detalle.Descripcion,
                    detalle.UnidadMedida != null ? detalle.UnidadMedida.Nombre : detalle.Insumo.UnidadCompra,
                    detalle.Cantidad,
                    detalle.Precio,
                    detalle.Subtotal));
            items = todo
                ? await consulta.ToListAsync(cancellationToken)
                : await consulta.Skip((paginaNormal - 1) * tamanoNormal).Take(tamanoNormal).ToListAsync(cancellationToken);
        }

        return ResultadoFactura<InformeFacturasDto>.Ok(new InformeFacturasDto(
            agrupado,
            desde,
            hasta,
            items,
            totalValor,
            encabezado,
            cantidad,
            todo ? 1 : paginaNormal,
            todo ? cantidad : tamanoNormal));
    }

    private IQueryable<Factura> Filtrar(
        DateOnly? desde,
        DateOnly? hasta,
        int? proveedorId,
        int? construccionId,
        string? numero,
        string? nombreProveedor)
    {
        var consulta = _context.Facturas.AsNoTracking();
        if (desde is not null)
        {
            consulta = consulta.Where(factura => factura.Fecha >= desde);
        }

        if (hasta is not null)
        {
            consulta = consulta.Where(factura => factura.Fecha <= hasta);
        }

        if (proveedorId is > 0)
        {
            consulta = consulta.Where(factura => factura.ProveedorId == proveedorId);
        }

        if (construccionId is > 0)
        {
            consulta = consulta.Where(factura => factura.Detalles.Any(detalle => detalle.ConstruccionId == construccionId));
        }

        var textoNumero = numero?.Trim();
        if (!string.IsNullOrEmpty(textoNumero))
        {
            consulta = consulta.Where(factura => factura.Numero.Contains(textoNumero));
        }

        var textoProveedor = nombreProveedor?.Trim();
        if (!string.IsNullOrEmpty(textoProveedor))
        {
            consulta = consulta.Where(factura => factura.Proveedor.Nombre.Contains(textoProveedor));
        }

        return consulta;
    }

    private IQueryable<FacturaDetalle> FiltrarLineas(
        DateOnly? desde,
        DateOnly? hasta,
        int? proveedorId,
        int? construccionId)
    {
        var consulta = _context.FacturaDetalles.AsNoTracking();
        if (desde is not null)
        {
            consulta = consulta.Where(detalle => detalle.Factura.Fecha >= desde);
        }

        if (hasta is not null)
        {
            consulta = consulta.Where(detalle => detalle.Factura.Fecha <= hasta);
        }

        if (proveedorId is > 0)
        {
            consulta = consulta.Where(detalle => detalle.Factura.ProveedorId == proveedorId);
        }

        if (construccionId is > 0)
        {
            consulta = consulta.Where(detalle => detalle.ConstruccionId == construccionId);
        }

        return consulta;
    }

    private async Task<IReadOnlyList<string>> EncabezadoAsync(
        DateOnly? desde,
        DateOnly? hasta,
        int? proveedorId,
        int? construccionId,
        CancellationToken cancellationToken)
    {
        var lineas = new List<string>();
        if (desde is not null && hasta is not null)
        {
            lineas.Add($"Del {desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}");
        }
        else if (desde is not null)
        {
            lineas.Add($"Desde {desde:dd/MM/yyyy}");
        }
        else if (hasta is not null)
        {
            lineas.Add($"Hasta {hasta:dd/MM/yyyy}");
        }

        if (construccionId is > 0)
        {
            var nombre = await _context.Construcciones
                .Where(item => item.Id == construccionId)
                .Select(item => item.Descripcion)
                .FirstOrDefaultAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(nombre))
            {
                lineas.Add($"Construcción: {nombre}");
            }
        }

        if (proveedorId is > 0)
        {
            var nombre = await _context.Proveedores
                .Where(item => item.Id == proveedorId)
                .Select(item => item.Nombre)
                .FirstOrDefaultAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(nombre))
            {
                lineas.Add($"Proveedor: {nombre}");
            }
        }

        return lineas;
    }

    private async Task<string?> ValidarReferenciasAsync(CrearFacturaSolicitud solicitud, CancellationToken cancellationToken)
    {
        var proveedorOk = await _context.Proveedores
            .AnyAsync(proveedor => proveedor.Id == solicitud.ProveedorId && proveedor.Activo, cancellationToken);
        if (!proveedorOk)
        {
            return CrearFacturaValidador.MensajeProveedor;
        }

        var construcciones = solicitud.Detalles.Select(linea => linea.ConstruccionId).Distinct().ToList();
        var construccionesOk = await _context.Construcciones
            .CountAsync(obra => construcciones.Contains(obra.Id) && obra.Activo, cancellationToken);
        if (construccionesOk != construcciones.Count)
        {
            return CrearFacturaValidador.MensajeConstruccion;
        }

        var insumos = solicitud.Detalles.Select(linea => linea.InsumoId).Distinct().ToList();
        var insumosOk = await _context.Insumos
            .CountAsync(insumo => insumos.Contains(insumo.Id) && insumo.Activo, cancellationToken);
        if (insumosOk != insumos.Count)
        {
            return CrearFacturaValidador.MensajeInsumo;
        }

        return null;
    }

    private async Task<Dictionary<int, int?>> UnidadesDeMaterialesAsync(
        CrearFacturaSolicitud solicitud,
        CancellationToken cancellationToken)
    {
        var ids = solicitud.Detalles.Select(linea => linea.InsumoId).Distinct().ToList();
        var materiales = await _context.Insumos
            .Where(insumo => ids.Contains(insumo.Id))
            .Select(insumo => new { insumo.Id, insumo.UnidadCompra, insumo.UnidadUso })
            .ToListAsync(cancellationToken);
        var catalogo = await _context.UnidadesMedida
            .Where(unidad => unidad.Activo)
            .Select(unidad => new { unidad.Id, unidad.Nombre })
            .ToListAsync(cancellationToken);
        var resultado = new Dictionary<int, int?>();
        foreach (var material in materiales)
        {
            var texto = string.IsNullOrWhiteSpace(material.UnidadCompra) ? material.UnidadUso : material.UnidadCompra;
            var coincidencia = catalogo.FirstOrDefault(unidad =>
                string.Equals(unidad.Nombre.Trim(), texto.Trim(), StringComparison.OrdinalIgnoreCase));
            resultado[material.Id] = coincidencia?.Id;
        }

        return resultado;
    }

    private Task<bool> NumeroRepetidoAsync(int id, CrearFacturaSolicitud solicitud, CancellationToken cancellationToken)
    {
        var numero = solicitud.Numero.Trim().ToLower();
        return _context.Facturas.AnyAsync(
            factura => factura.Id != id
                && factura.ProveedorId == solicitud.ProveedorId
                && factura.Numero.ToLower() == numero,
            cancellationToken);
    }

    private async Task<FacturaDto?> CargarAsync(int id, CancellationToken cancellationToken)
    {
        var factura = await _context.Facturas
            .AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.Proveedor)
            .Include(item => item.Detalles).ThenInclude(detalle => detalle.Construccion)
            .Include(item => item.Detalles).ThenInclude(detalle => detalle.Insumo)
            .Include(item => item.Detalles).ThenInclude(detalle => detalle.UnidadMedida)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (factura is null)
        {
            return null;
        }

        var detalles = factura.Detalles
            .OrderBy(detalle => detalle.Construccion.Descripcion)
            .ThenBy(detalle => detalle.Id)
            .Select(detalle => new LineaFacturaDto(
                detalle.Id,
                detalle.ConstruccionId,
                detalle.Construccion.Descripcion,
                detalle.InsumoId,
                detalle.Insumo.Codigo,
                detalle.Insumo.Descripcion,
                detalle.UnidadMedidaId,
                detalle.UnidadMedida?.Nombre ?? detalle.Insumo.UnidadCompra,
                detalle.Cantidad,
                detalle.Precio,
                detalle.Descripcion,
                detalle.Subtotal))
            .ToList();

        return new FacturaDto(
            factura.Id,
            factura.Numero,
            factura.Fecha,
            factura.ProveedorId,
            factura.Proveedor.Nombre,
            factura.OrdenCompra,
            factura.Observacion,
            factura.FechaRegistro,
            factura.UsuarioRegistroId,
            detalles.Sum(detalle => detalle.Subtotal),
            detalles,
            Convert.ToBase64String(factura.RowVersion ?? []));
    }

    private static FacturaDetalle CrearDetalle(LineaFacturaSolicitud linea, IReadOnlyDictionary<int, int?> unidades) =>
        new()
        {
            ConstruccionId = linea.ConstruccionId,
            InsumoId = linea.InsumoId,
            UnidadMedidaId = unidades.TryGetValue(linea.InsumoId, out var unidadId) ? unidadId : null,
            Cantidad = decimal.Round(linea.Cantidad, 4, MidpointRounding.AwayFromZero),
            Precio = decimal.Round(linea.Precio, 2, MidpointRounding.AwayFromZero),
            Descripcion = (linea.Descripcion ?? "").Trim()
        };

    private static string? ValidarForma(CrearFacturaSolicitud solicitud)
    {
        var resultado = new CrearFacturaValidador().Validate(solicitud);
        return resultado.IsValid ? null : resultado.Errors[0].ErrorMessage;
    }

    private static string? Texto(string? valor)
    {
        var limpio = valor?.Trim();
        return string.IsNullOrEmpty(limpio) ? null : limpio;
    }
}
