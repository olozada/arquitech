using Arquitectos.Application.Presupuestos;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Infrastructure.Presupuestos;

public sealed class InsumoServicio : IInsumoServicio
{
    private readonly ArquitectosDbContext _context;

    public InsumoServicio(ArquitectosDbContext context)
    {
        _context = context;
    }

    public async Task<PaginaDto<InsumoDto>> ListarAsync(
        string? tipo,
        bool incluirInactivos,
        CancellationToken cancellationToken,
        int pagina = 1,
        int tamano = Paginacion.TamanoDefecto,
        string? texto = null)
    {
        var (paginaOk, tamanoOk) = Paginacion.Normalizar(pagina, tamano);
        var consulta = _context.Insumos.AsNoTracking();
        if (!incluirInactivos)
        {
            consulta = consulta.Where(insumo => insumo.Activo);
        }

        if (TextoPresupuesto.TryTipoInsumo(tipo, out var tipoInsumo) && !string.IsNullOrWhiteSpace(tipo))
        {
            consulta = consulta.Where(insumo => insumo.Tipo == tipoInsumo);
        }

        if (!string.IsNullOrWhiteSpace(texto))
        {
            var filtro = texto.Trim().ToLower();
            consulta = consulta.Where(insumo =>
                insumo.Descripcion.ToLower().Contains(filtro) || (insumo.Codigo != null && insumo.Codigo.ToLower().Contains(filtro)));
        }

        var total = await consulta.CountAsync(cancellationToken);
        var items = await consulta
            .OrderBy(insumo => insumo.Descripcion)
            .Skip((paginaOk - 1) * tamanoOk)
            .Take(tamanoOk)
            .ToListAsync(cancellationToken);
        return new PaginaDto<InsumoDto>(items.Select(Mapa).ToList(), total, paginaOk, tamanoOk);
    }

    public async Task<InsumoDto?> ObtenerAsync(int id, CancellationToken cancellationToken)
    {
        var insumo = await _context.Insumos.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        return insumo is null ? null : Mapa(insumo);
    }

    public async Task<ResultadoOperacion<InsumoDto>> GuardarAsync(
        int? id,
        InsumoSolicitud solicitud,
        int? usuarioId,
        CancellationToken cancellationToken)
    {
        if (!TextoPresupuesto.TryTipoInsumo(solicitud.Tipo, out var tipo))
        {
            return ResultadoOperacion<InsumoDto>.Validacion("El tipo de insumo no es válido.");
        }

        var descripcion = TextoPresupuesto.Recortar(solicitud.Descripcion, 500);
        if (descripcion.Length == 0)
        {
            return ResultadoOperacion<InsumoDto>.Validacion("La descripción es obligatoria.");
        }

        if (solicitud.CantidadPorUnidad <= 0)
        {
            return ResultadoOperacion<InsumoDto>.Validacion("La cantidad por unidad debe ser mayor que cero.");
        }

        var codigo = string.IsNullOrWhiteSpace(solicitud.Codigo) ? null : solicitud.Codigo.Trim();
        var duplicado = await _context.Insumos.AnyAsync(
            insumo => insumo.Id != (id ?? 0) && codigo != null && insumo.Codigo == codigo,
            cancellationToken);
        if (duplicado)
        {
            return ResultadoOperacion<InsumoDto>.Conflicto("Ya existe un insumo con ese código.");
        }

        Insumo entidad;
        if (id is null)
        {
            entidad = new Insumo { Descripcion = descripcion };
            _context.Insumos.Add(entidad);
        }
        else
        {
            var existente = await _context.Insumos.FirstOrDefaultAsync(insumo => insumo.Id == id, cancellationToken);
            if (existente is null)
            {
                return ResultadoOperacion<InsumoDto>.NoEncontrado("No se encontró el insumo.");
            }

            entidad = existente;
        }

        var precioCambio = id is null || entidad.PrecioBase != solicitud.PrecioBase || entidad.PorcentajeIva != solicitud.PorcentajeIva;
        entidad.Codigo = codigo;
        entidad.Descripcion = descripcion;
        entidad.Tipo = tipo;
        entidad.UnidadCompra = TextoPresupuesto.Recortar(solicitud.UnidadCompra, 30);
        entidad.PrecioBase = solicitud.PrecioBase;
        entidad.PorcentajeIva = solicitud.PorcentajeIva;
        entidad.CantidadPorUnidad = solicitud.CantidadPorUnidad;
        entidad.UnidadUso = TextoPresupuesto.Recortar(solicitud.UnidadUso, 30);
        entidad.PrecioUnitario = CalculadoraApu.PrecioUnitarioInsumo(solicitud.PrecioBase, solicitud.PorcentajeIva, solicitud.CantidadPorUnidad);
        entidad.Activo = solicitud.Activo;
        if (precioCambio)
        {
            entidad.FechaPrecio = DateTime.UtcNow;
            _context.InsumoPreciosHistorial.Add(new InsumoPrecioHistorial
            {
                Insumo = entidad,
                PrecioBase = entidad.PrecioBase,
                PorcentajeIva = entidad.PorcentajeIva,
                Fecha = entidad.FechaPrecio,
                UsuarioId = usuarioId
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        return ResultadoOperacion<InsumoDto>.Ok(Mapa(entidad));
    }

    public async Task<ResultadoOperacion<bool>> EliminarAsync(int id, bool confirmar, CancellationToken cancellationToken)
    {
        var insumo = await _context.Insumos.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (insumo is null)
        {
            return ResultadoOperacion<bool>.NoEncontrado("No se encontró el insumo.");
        }

        var usado = await _context.Set<ApuLinea>().AnyAsync(linea => linea.InsumoId == id, cancellationToken);
        if (usado && !confirmar)
        {
            return ResultadoOperacion<bool>.Validacion("El insumo se usa en análisis de precios. Confirme para desactivarlo.");
        }

        insumo.Activo = false;
        await _context.SaveChangesAsync(cancellationToken);
        return ResultadoOperacion<bool>.Ok(true);
    }

    public async Task<IReadOnlyList<HistorialPrecioDto>> HistorialAsync(int id, CancellationToken cancellationToken)
    {
        var filas = await _context.InsumoPreciosHistorial.AsNoTracking()
            .Where(historial => historial.InsumoId == id)
            .OrderByDescending(historial => historial.Fecha)
            .Select(historial => new { historial.Id, historial.PrecioBase, historial.PorcentajeIva, historial.Fecha, historial.UsuarioId })
            .ToListAsync(cancellationToken);
        var usuarios = filas.Where(fila => fila.UsuarioId != null).Select(fila => fila.UsuarioId!.Value).Distinct().ToList();
        var nombres = await _context.Usuarios.AsNoTracking()
            .Where(usuario => usuarios.Contains(usuario.Id))
            .ToDictionaryAsync(usuario => usuario.Id, usuario => usuario.Nombre, cancellationToken);
        return filas
            .Select(fila => new HistorialPrecioDto(
                fila.Id,
                fila.PrecioBase,
                fila.PorcentajeIva,
                fila.Fecha,
                fila.UsuarioId,
                fila.UsuarioId is int usuarioId && nombres.TryGetValue(usuarioId, out var nombre) ? nombre : null))
            .ToList();
    }

    public async Task<byte[]> ExcelAsync(string? tipo, CancellationToken cancellationToken)
    {
        var consulta = _context.Insumos.AsNoTracking().Where(insumo => insumo.Activo);
        if (!string.IsNullOrWhiteSpace(tipo) && Enum.TryParse<TipoInsumo>(tipo, true, out var tipoInsumo))
        {
            consulta = consulta.Where(insumo => insumo.Tipo == tipoInsumo);
        }

        var insumos = await consulta.OrderBy(insumo => insumo.Codigo).ThenBy(insumo => insumo.Descripcion).ToListAsync(cancellationToken);
        using var libro = new ClosedXML.Excel.XLWorkbook();
        var hoja = libro.AddWorksheet("Insumos");
        var encabezados = new[] { "Código", "Tipo", "Descripción", "Unidad compra", "Precio base", "IVA", "Cantidad", "Unidad uso", "Precio unitario" };
        for (var columna = 0; columna < encabezados.Length; columna++)
        {
            hoja.Cell(1, columna + 1).Value = encabezados[columna];
        }

        var fila = 2;
        foreach (var insumo in insumos)
        {
            hoja.Cell(fila, 1).Value = insumo.Codigo;
            hoja.Cell(fila, 2).Value = insumo.Tipo.ToString();
            hoja.Cell(fila, 3).Value = insumo.Descripcion;
            hoja.Cell(fila, 4).Value = insumo.UnidadCompra;
            hoja.Cell(fila, 5).Value = insumo.PrecioBase;
            hoja.Cell(fila, 6).Value = insumo.PorcentajeIva;
            hoja.Cell(fila, 7).Value = insumo.CantidadPorUnidad;
            hoja.Cell(fila, 8).Value = insumo.UnidadUso;
            hoja.Cell(fila, 9).Value = insumo.PrecioUnitario;
            fila++;
        }

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return memoria.ToArray();
    }

    public async Task<OfertaProveedorDto?> OfertaProveedorAsync(int id, CancellationToken cancellationToken)
    {
        var existe = await _context.Insumos.AsNoTracking().AnyAsync(insumo => insumo.Id == id, cancellationToken);
        if (!existe)
        {
            return null;
        }

        var ofertas = await _context.ProveedorMateriales.AsNoTracking()
            .Where(oferta => oferta.InsumoId == id)
            .Select(oferta => new { oferta.Precio, oferta.Proveedor.Nombre })
            .ToListAsync(cancellationToken);
        var minima = ofertas.OrderBy(oferta => oferta.Precio).ThenBy(oferta => oferta.Nombre).FirstOrDefault();
        return minima is null ? null : new OfertaProveedorDto(minima.Precio, minima.Nombre, id);
    }

    public async Task<ResultadoOperacion<InsumoDto>> TomarPrecioProveedorAsync(int id, int? usuarioId, CancellationToken cancellationToken)
    {
        var oferta = await OfertaProveedorAsync(id, cancellationToken);
        if (oferta is null)
        {
            return ResultadoOperacion<InsumoDto>.Validacion("El insumo no tiene un precio de proveedor.");
        }

        var insumo = await _context.Insumos.FirstAsync(item => item.Id == id, cancellationToken);
        return await GuardarAsync(id, new InsumoSolicitud
        {
            Codigo = insumo.Codigo,
            Descripcion = insumo.Descripcion,
            Tipo = insumo.Tipo.ToString(),
            UnidadCompra = insumo.UnidadCompra,
            PrecioBase = oferta.Precio,
            PorcentajeIva = insumo.PorcentajeIva,
            CantidadPorUnidad = insumo.CantidadPorUnidad,
            UnidadUso = insumo.UnidadUso,
            Activo = insumo.Activo
        }, usuarioId, cancellationToken);
    }

    private static InsumoDto Mapa(Insumo insumo) => new(
        insumo.Id,
        insumo.Codigo,
        insumo.Descripcion,
        insumo.Tipo.ToString(),
        insumo.UnidadCompra,
        insumo.PrecioBase,
        insumo.PorcentajeIva,
        insumo.CantidadPorUnidad,
        insumo.UnidadUso,
        insumo.PrecioUnitario,
        insumo.FechaPrecio,
        insumo.Activo);
}
