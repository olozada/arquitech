using System.Globalization;
using System.Text.Json;
using Arquitectos.Application.Presupuestos;
using Arquitectos.Application.ProveedorMateriales;
using Arquitectos.Domain.Entidades;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Infrastructure.ProveedorMateriales;

public sealed partial class ProveedorMaterialServicio
{
    private const int MaximoFilasCargue = 5000;

    private static readonly JsonSerializerOptions JsonMapeo = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<ResultadoProveedorMaterial<MapeoProductosDto>> ObtenerMapeoAsync(
        int proveedorId,
        CancellationToken cancellationToken)
    {
        var proveedor = await _context.Proveedores.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == proveedorId, cancellationToken);
        if (proveedor is null)
        {
            return ResultadoProveedorMaterial<MapeoProductosDto>.NoEncontrado(MensajeProveedor);
        }

        return ResultadoProveedorMaterial<MapeoProductosDto>.Ok(LeerMapeo(proveedor.MapeoProductos));
    }

    public async Task<ResultadoProveedorMaterial<VistaCargueDto>> PrevisualizarCargueAsync(
        int proveedorId,
        Stream archivo,
        string nombreArchivo,
        CancellationToken cancellationToken)
    {
        var proveedor = await _context.Proveedores.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == proveedorId, cancellationToken);
        if (proveedor is null)
        {
            return ResultadoProveedorMaterial<VistaCargueDto>.NoEncontrado(MensajeProveedor);
        }

        if (!TryLeerTabla(archivo, nombreArchivo, out var tabla, out var error))
        {
            return ResultadoProveedorMaterial<VistaCargueDto>.Invalido(error);
        }

        var guardado = LeerMapeo(proveedor.MapeoProductos);
        var sugerido = CompletarMapeo(guardado, tabla.Columnas);
        var muestra = tabla.Filas.Take(5).Select(fila => (IReadOnlyList<string>)fila).ToList();
        return ResultadoProveedorMaterial<VistaCargueDto>.Ok(new VistaCargueDto(tabla.Columnas, muestra, sugerido));
    }

    public async Task<ResultadoProveedorMaterial<ResultadoCargueDto>> CargarProductosAsync(
        int proveedorId,
        Stream archivo,
        string nombreArchivo,
        MapeoProductosDto mapeo,
        CancellationToken cancellationToken)
    {
        var proveedor = await _context.Proveedores.FirstOrDefaultAsync(item => item.Id == proveedorId, cancellationToken);
        if (proveedor is null)
        {
            return ResultadoProveedorMaterial<ResultadoCargueDto>.NoEncontrado(MensajeProveedor);
        }

        if (!TryLeerTabla(archivo, nombreArchivo, out var tabla, out var error))
        {
            return ResultadoProveedorMaterial<ResultadoCargueDto>.Invalido(error);
        }

        var columnas = Indices(tabla.Columnas, mapeo, out var mensaje);
        if (columnas is null)
        {
            return ResultadoProveedorMaterial<ResultadoCargueDto>.Invalido(mensaje);
        }

        var insumos = await _context.Insumos.ToListAsync(cancellationToken);
        var porCodigo = insumos
            .Where(insumo => !string.IsNullOrWhiteSpace(insumo.Codigo))
            .GroupBy(insumo => Clave(insumo.Codigo!))
            .ToDictionary(grupo => grupo.Key, grupo => grupo.First());
        var porDescripcion = insumos
            .GroupBy(insumo => Clave(insumo.Descripcion))
            .ToDictionary(grupo => grupo.Key, grupo => grupo.First());
        var ofertas = await _context.ProveedorMateriales
            .Where(item => item.ProveedorId == proveedorId)
            .ToDictionaryAsync(item => item.InsumoId, cancellationToken);

        var nuevos = 0;
        var preciosNuevos = 0;
        var preciosActualizados = 0;
        var omitidos = 0;
        var avisos = new List<string>();
        var vistos = new HashSet<string>();
        var pendientes = new List<(Insumo Insumo, decimal? Precio)>();

        foreach (var fila in tabla.Filas)
        {
            var codigo = Texto(fila, columnas.Codigo, 30);
            var descripcion = Texto(fila, columnas.Descripcion, 500);
            var unidad = Texto(fila, columnas.Unidad, 30);
            var precioTexto = Texto(fila, columnas.Precio, 40);
            if (codigo.Length == 0 && descripcion.Length == 0 && precioTexto.Length == 0)
            {
                continue;
            }

            var claveFila = codigo.Length > 0 ? "c:" + Clave(codigo) : "d:" + Clave(descripcion);
            if (!vistos.Add(claveFila))
            {
                omitidos++;
                continue;
            }

            decimal? precio = null;
            if (precioTexto.Length > 0)
            {
                if (!TryPrecio(precioTexto, out var valor))
                {
                    omitidos++;
                    Aviso(avisos, $"El precio \"{precioTexto}\" no es válido.");
                    continue;
                }

                precio = decimal.Round(valor, 2, MidpointRounding.AwayFromZero);
            }

            Insumo? insumo = null;
            if (codigo.Length > 0)
            {
                porCodigo.TryGetValue(Clave(codigo), out insumo);
            }

            if (insumo is null && descripcion.Length > 0)
            {
                porDescripcion.TryGetValue(Clave(descripcion), out insumo);
            }

            if (insumo is null)
            {
                if (descripcion.Length == 0)
                {
                    omitidos++;
                    Aviso(avisos, $"El código {codigo} no existe y el archivo no trae descripción.");
                    continue;
                }

                var unidadUso = unidad.Length == 0 ? "UND" : unidad;
                insumo = new Insumo
                {
                    Codigo = codigo.Length == 0 ? null : codigo,
                    Descripcion = descripcion,
                    Tipo = TipoInsumo.Material,
                    UnidadCompra = unidadUso,
                    UnidadUso = unidadUso,
                    PrecioBase = precio ?? 0,
                    CantidadPorUnidad = 1,
                    PrecioUnitario = CalculadoraApu.PrecioUnitarioInsumo(precio ?? 0, 0, 1),
                    FechaPrecio = DateTime.UtcNow,
                    Activo = true
                };
                _context.Insumos.Add(insumo);
                if (codigo.Length > 0)
                {
                    porCodigo[Clave(codigo)] = insumo;
                }

                porDescripcion[Clave(descripcion)] = insumo;
                nuevos++;
            }
            else if (!insumo.Activo)
            {
                insumo.Activo = true;
            }

            pendientes.Add((insumo, precio));
        }

        await _context.SaveChangesAsync(cancellationToken);
        foreach (var (insumo, precio) in pendientes)
        {
            if (precio is not decimal precioOferta)
            {
                continue;
            }

            if (ofertas.TryGetValue(insumo.Id, out var oferta))
            {
                if (oferta.Precio != precioOferta)
                {
                    oferta.Precio = precioOferta;
                    preciosActualizados++;
                }
            }
            else
            {
                var creada = new ProveedorMaterial
                {
                    ProveedorId = proveedorId,
                    InsumoId = insumo.Id,
                    Precio = precioOferta
                };
                _context.ProveedorMateriales.Add(creada);
                ofertas[insumo.Id] = creada;
                preciosNuevos++;
            }
        }

        proveedor.MapeoProductos = JsonSerializer.Serialize(new MapeoProductosDto
        {
            Codigo = mapeo.Codigo.Trim(),
            Descripcion = mapeo.Descripcion.Trim(),
            Unidad = mapeo.Unidad.Trim(),
            Precio = mapeo.Precio.Trim()
        }, JsonMapeo);
        await _context.SaveChangesAsync(cancellationToken);
        return ResultadoProveedorMaterial<ResultadoCargueDto>.Ok(
            new ResultadoCargueDto(nuevos, preciosNuevos, preciosActualizados, omitidos, avisos));
    }

    private static MapeoProductosDto LeerMapeo(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new MapeoProductosDto();
        }

        try
        {
            return JsonSerializer.Deserialize<MapeoProductosDto>(json, JsonMapeo) ?? new MapeoProductosDto();
        }
        catch (JsonException)
        {
            return new MapeoProductosDto();
        }
    }

    private static MapeoProductosDto CompletarMapeo(MapeoProductosDto guardado, IReadOnlyList<string> columnas)
    {
        bool Existe(string nombre) => columnas.Any(columna => string.Equals(columna, nombre, StringComparison.OrdinalIgnoreCase));
        string Elegir(string guardada, params string[] sinonimos)
        {
            if (guardada.Length > 0 && Existe(guardada))
            {
                return columnas.First(columna => string.Equals(columna, guardada, StringComparison.OrdinalIgnoreCase));
            }

            return columnas.FirstOrDefault(columna => sinonimos.Any(sinonimo => Clave(columna) == Clave(sinonimo))) ?? "";
        }

        return new MapeoProductosDto
        {
            Codigo = Elegir(guardado.Codigo, "codigo", "código", "cod", "sku", "referencia", "ref"),
            Descripcion = Elegir(guardado.Descripcion, "descripcion", "descripción", "producto", "nombre", "material", "item", "ítem"),
            Unidad = Elegir(guardado.Unidad, "unidad", "um", "und", "medida"),
            Precio = Elegir(guardado.Precio, "precio", "valor", "lista", "pvp", "costo")
        };
    }

    private static ColumnasCargue? Indices(IReadOnlyList<string> columnas, MapeoProductosDto mapeo, out string mensaje)
    {
        int Indice(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                return -1;
            }

            return columnas.ToList().FindIndex(columna => string.Equals(columna, nombre.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        var codigo = Indice(mapeo.Codigo);
        var descripcion = Indice(mapeo.Descripcion);
        var unidad = Indice(mapeo.Unidad);
        var precio = Indice(mapeo.Precio);
        if ((mapeo.Codigo.Trim().Length > 0 && codigo < 0) ||
            (mapeo.Descripcion.Trim().Length > 0 && descripcion < 0) ||
            (mapeo.Unidad.Trim().Length > 0 && unidad < 0) ||
            (mapeo.Precio.Trim().Length > 0 && precio < 0))
        {
            mensaje = "El archivo no tiene alguna de las columnas del mapeo guardado.";
            return null;
        }

        if (descripcion < 0 && codigo < 0)
        {
            mensaje = "Mapee la descripción o el código del producto.";
            return null;
        }

        mensaje = "";
        return new ColumnasCargue(codigo, descripcion, unidad, precio);
    }

    private static bool TryLeerTabla(Stream archivo, string nombreArchivo, out TablaCargue tabla, out string error)
    {
        tabla = new TablaCargue([], []);
        error = "";
        var extension = Path.GetExtension(nombreArchivo).ToLowerInvariant();
        try
        {
            tabla = extension is ".csv" or ".txt" ? LeerTexto(archivo) : LeerExcel(archivo);
        }
        catch (Exception)
        {
            error = "No se pudo leer el archivo. Use Excel, CSV o TXT.";
            return false;
        }

        if (tabla.Columnas.Count == 0)
        {
            error = "El archivo no tiene columnas.";
            return false;
        }

        if (tabla.Filas.Count > MaximoFilasCargue)
        {
            error = $"El archivo supera {MaximoFilasCargue} filas.";
            return false;
        }

        return true;
    }

    private static TablaCargue LeerExcel(Stream archivo)
    {
        using var libro = new XLWorkbook(archivo);
        var hoja = libro.Worksheets.First();
        var usada = hoja.RangeUsed();
        if (usada is null)
        {
            return new TablaCargue([], []);
        }

        var titulos = new List<string>();
        var vistos = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var columna = 1; columna <= usada.ColumnCount(); columna++)
        {
            var titulo = hoja.Cell(usada.FirstRow().RowNumber(), usada.FirstColumn().ColumnNumber() + columna - 1).GetString().Trim();
            if (titulo.Length == 0)
            {
                titulo = $"Columna {columna}";
            }

            vistos.TryGetValue(titulo, out var repetida);
            vistos[titulo] = repetida + 1;
            titulos.Add(repetida == 0 ? titulo : $"{titulo} ({repetida + 1})");
        }

        var filas = new List<IReadOnlyList<string>>();
        for (var fila = usada.FirstRow().RowNumber() + 1; fila <= usada.LastRow().RowNumber(); fila++)
        {
            var celdas = new List<string>();
            for (var columna = 0; columna < titulos.Count; columna++)
            {
                var celda = hoja.Cell(fila, usada.FirstColumn().ColumnNumber() + columna);
                celdas.Add(TextoCelda(celda));
            }

            if (celdas.Any(texto => texto.Length > 0))
            {
                filas.Add(celdas);
            }
        }

        return new TablaCargue(titulos, filas);
    }

    private static string TextoCelda(IXLCell celda)
    {
        if (celda.IsEmpty())
        {
            return "";
        }

        if (celda.DataType == XLDataType.Number)
        {
            return celda.GetDouble().ToString(CultureInfo.InvariantCulture);
        }

        return celda.GetString().Trim();
    }

    private static TablaCargue LeerTexto(Stream archivo)
    {
        using var lector = new StreamReader(archivo, detectEncodingFromByteOrderMarks: true);
        var contenido = lector.ReadToEnd();
        var lineas = contenido.Split(["\r\n", "\n"], StringSplitOptions.None)
            .Where(linea => linea.Trim().Length > 0)
            .ToList();
        if (lineas.Count == 0)
        {
            return new TablaCargue([], []);
        }

        var separador = lineas[0].Count(caracter => caracter == ';') >= lineas[0].Count(caracter => caracter == ',')
            && lineas[0].Contains(';')
            ? ';'
            : lineas[0].Contains('\t') ? '\t' : ',';
        var titulos = Partir(lineas[0], separador);
        var filas = lineas.Skip(1).Select(linea => (IReadOnlyList<string>)Partir(linea, separador)).ToList();
        return new TablaCargue(titulos, filas);
    }

    private static List<string> Partir(string linea, char separador) =>
        linea.Split(separador).Select(parte => parte.Trim().Trim('"')).ToList();

    private static string Texto(IReadOnlyList<string> fila, int indice, int maximo)
    {
        if (indice < 0 || indice >= fila.Count)
        {
            return "";
        }

        var texto = fila[indice].Trim();
        return texto.Length <= maximo ? texto : texto[..maximo];
    }

    private static string Clave(string texto) => texto.Trim().ToLowerInvariant();

    private static void Aviso(List<string> avisos, string texto)
    {
        if (avisos.Count < 20)
        {
            avisos.Add(texto);
        }
    }

    internal static bool TryPrecio(string texto, out decimal precio)
    {
        precio = 0;
        texto = texto.Trim().Replace("$", "", StringComparison.Ordinal).Replace(" ", "", StringComparison.Ordinal).Replace("\u00a0", "", StringComparison.Ordinal);
        if (texto.Length == 0)
        {
            return false;
        }

        if (texto.Count(caracter => caracter == '.') > 1 && !texto.Contains(','))
        {
            texto = texto.Replace(".", "", StringComparison.Ordinal);
        }

        if (texto.Count(caracter => caracter == ',') > 1 && !texto.Contains('.'))
        {
            texto = texto.Replace(",", "", StringComparison.Ordinal);
        }

        var coma = texto.LastIndexOf(',');
        var punto = texto.LastIndexOf('.');
        if (coma >= 0 && punto >= 0)
        {
            texto = coma > punto
                ? texto.Replace(".", "", StringComparison.Ordinal).Replace(',', '.')
                : texto.Replace(",", "", StringComparison.Ordinal);
        }
        else if (coma >= 0)
        {
            var decimales = texto.Length - coma - 1;
            texto = decimales == 3 ? texto.Replace(",", "", StringComparison.Ordinal) : texto.Replace(',', '.');
        }
        else if (punto >= 0 && texto.Length - punto - 1 == 3 && texto.Count(caracter => caracter == '.') == 1)
        {
            texto = texto.Replace(".", "", StringComparison.Ordinal);
        }

        return decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out precio) && precio > 0;
    }

    private sealed record ColumnasCargue(int Codigo, int Descripcion, int Unidad, int Precio);

    private sealed record TablaCargue(IReadOnlyList<string> Columnas, IReadOnlyList<IReadOnlyList<string>> Filas);
}
