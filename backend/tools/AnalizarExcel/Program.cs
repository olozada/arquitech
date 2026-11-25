using System.Text;
using System.Text.Json;
using Arquitectos.Infrastructure.Presupuestos;
using ExcelDataReader;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var raiz = args.Length > 0 ? args[0] : @"C:\Arquitectos";
var archivos = new[]
{
    "Presupuesto Preliminar Casa Murillo.xlsx",
    "Presupuesto Preliminar Edfificio FIKUS Ene 2020.xls",
    "Presupuesto preliminar Edificio Clinifracturas.xls",
    "Presupuesto Preliminar LA RESERVA Casa 33 Jun 22.xls",
};

var esperadas = new[]
{
    "Presupuesto", "PRECIOS MATERIALES", "PRECIO HERRAMIENTA", "ALQULER EQUIPO",
    "LABORATORIO", "PRECIOS M.O.", "P.M.O.", "A.P.U."
};

var todasUnidades = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
var conceptosGlobal = new Dictionary<string, (string Descripcion, HashSet<string> Unidades)>(StringComparer.OrdinalIgnoreCase);
var resumen = new List<object>();

foreach (var nombre in archivos)
{
    var ruta = Path.Combine(raiz, nombre);
    if (!File.Exists(ruta))
    {
        Console.WriteLine($"NO ENCONTRADO: {ruta}");
        continue;
    }

    Console.WriteLine($"\n======== {nombre} ========");
    var hojas = LeerHojas(ruta);
    Console.WriteLine($"Hojas ({hojas.Count}): {string.Join(" | ", hojas.Keys)}");

    foreach (var exp in esperadas)
    {
        var ok = hojas.ContainsKey(exp);
        if (!ok)
        {
            var sim = hojas.Keys.Where(k => k.Contains(exp.AsSpan(0, Math.Min(5, exp.Length)), StringComparison.OrdinalIgnoreCase)).ToList();
            Console.WriteLine($"  Falta hoja estándar: {exp}{(sim.Count > 0 ? $" (similares: {string.Join(", ", sim)})" : "")}");
        }
    }

    var porLibro = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
    {
        ["Presupuesto_items"] = UnidadesPresupuesto(hojas),
        ["Insumos_compra"] = UnidadesHojaPrecios(hojas, "PRECIOS MATERIALES", 4, 6),
        ["Insumos_uso"] = UnidadesHojaPrecios(hojas, "PRECIOS MATERIALES", 8, 6),
        ["Herramienta"] = UnidadesHojaPrecios(hojas, "PRECIO HERRAMIENTA", 4, 6),
        ["Equipo"] = UnidadesHojaPrecios(hojas, "ALQULER EQUIPO", 4, 6),
        ["Laboratorio"] = UnidadesHojaPrecios(hojas, "LABORATORIO", 4, 5),
        ["M.O._precios"] = UnidadesColumna(hojas, "PRECIOS M.O.", 3, 5),
        ["A.P.U._cabecera"] = UnidadesApuCabecera(hojas),
        ["A.P.U._lineas"] = UnidadesApuLineas(hojas),
        ["P.M.O."] = UnidadesPmo(hojas),
    };

    foreach (var (origen, set) in porLibro)
    {
        foreach (var u in set)
        {
            var can = TextoPresupuesto.CanonicaUnidad(u);
            if (!todasUnidades.TryGetValue(can, out var ejemplos))
            {
                ejemplos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                todasUnidades[can] = ejemplos;
            }

            ejemplos.Add(u.Trim());
        }
    }

    foreach (var (descripcion, unidad) in ItemsPresupuesto(hojas))
    {
        var clave = TextoPresupuesto.Normalizar(descripcion);
        if (clave.Length == 0 || string.IsNullOrWhiteSpace(unidad))
        {
            continue;
        }

        var can = CatalogoUnidadesObra.ResolverNombre(unidad) ?? TextoPresupuesto.CanonicaUnidad(unidad);
        if (!conceptosGlobal.TryGetValue(clave, out var fila))
        {
            fila = (descripcion, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            conceptosGlobal[clave] = fila;
        }

        fila.Unidades.Add(can);
    }

    var unicasLibro = porLibro.Values.SelectMany(s => s).Select(u => TextoPresupuesto.CanonicaUnidad(u)).Distinct().OrderBy(x => x).ToList();
    Console.WriteLine($"Unidades canónicas distintas en libro: {unicasLibro.Count}");
    Console.WriteLine(string.Join(", ", unicasLibro));

    ModeloImportado? modelo = null;
    InvalidOperationException? errorLector = null;
    if (ruta.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
    {
        try
        {
            modelo = LectorExcel.Leer(ruta);
            var informe = LectorModelo.Evaluar(modelo);
            Console.WriteLine($"LectorExcel OK: {informe.ApusRevisados} APUs, {informe.UnidadesDistintas} ítems con unidad ≠ APU");
            if (informe.UnidadesQueNoCoinciden.Count > 0)
            {
                Console.WriteLine("  Ejemplos discrepancia presupuesto/APU:");
                foreach (var linea in informe.UnidadesQueNoCoinciden.Take(5))
                {
                    Console.WriteLine("    " + linea);
                }
            }
        }
        catch (Exception ex)
        {
            errorLector = ex is InvalidOperationException ioe ? ioe : new InvalidOperationException(ex.Message, ex);
            Console.WriteLine($"LectorExcel: {errorLector.Message}");
        }
    }

    resumen.Add(new
    {
        archivo = nombre,
        hojas = hojas.Keys.OrderBy(x => x).ToList(),
        unidadesCanonicas = unicasLibro,
        lectorExcel = modelo is not null ? "ok" : errorLector?.Message
    });
}

Console.WriteLine("\n======== Catálogo unificado (canónico → variantes) ========");
foreach (var par in todasUnidades.OrderBy(p => p.Key))
{
    Console.WriteLine($"{par.Key,-8} ← {string.Join(" | ", par.Value.OrderBy(x => x))}");
}

var conflictoConceptos = conceptosGlobal.Values
    .Where(fila => fila.Unidades.Count > 1)
    .Select(fila => new { descripcion = fila.Descripcion, unidades = fila.Unidades.OrderBy(x => x).ToList() })
    .OrderBy(fila => fila.descripcion)
    .ToList();
Console.WriteLine($"\nConceptos con más de una unidad entre libros: {conflictoConceptos.Count}");
foreach (var fila in conflictoConceptos.Take(8))
{
    Console.WriteLine($"  · {fila.descripcion} → {string.Join(", ", fila.unidades)}");
}

var salidaJson = Path.Combine(raiz, "presupuesto_demo", "analisis_unidades_excel.json");
var salidaConceptos = Path.Combine(raiz, "presupuesto_demo", "conceptos_unidades_excel.json");
var payload = new
{
    generado = DateTime.UtcNow,
    libros = resumen,
    catalogo = todasUnidades.ToDictionary(p => p.Key, p => p.Value.OrderBy(x => x).ToList()),
    conceptos = conceptosGlobal.Values.Select(fila => new { fila.Descripcion, unidades = fila.Unidades.OrderBy(x => x).ToList() }).OrderBy(fila => fila.Descripcion).ToList(),
    conflictos = conflictoConceptos,
    catalogoOficial = CatalogoUnidadesObra.Filas.Select(fila => new { fila.Nombre, fila.Descripcion }).ToList()
};
var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
Directory.CreateDirectory(Path.GetDirectoryName(salidaJson)!);
File.WriteAllText(salidaJson, json);
File.WriteAllText(salidaConceptos, json);
Console.WriteLine($"\nJSON: {salidaJson}");
Console.WriteLine($"JSON conceptos: {salidaConceptos}");

static Dictionary<string, object?[][]> LeerHojas(string ruta)
{
    using var stream = File.Open(ruta, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    using var reader = ExcelReaderFactory.CreateReader(stream);
    var dataSet = reader.AsDataSet();
    var hojas = new Dictionary<string, object?[][]>(StringComparer.OrdinalIgnoreCase);
    foreach (System.Data.DataTable table in dataSet.Tables)
    {
        var filas = new List<object?[]>();
        foreach (System.Data.DataRow row in table.Rows)
        {
            filas.Add(row.ItemArray);
        }

        hojas[table.TableName] = filas.ToArray();
    }

    return hojas;
}

static string Celda(object?[][] filas, int fila, int col)
{
    fila--;
    col--;
    if (fila < 0 || fila >= filas.Length)
    {
        return "";
    }

    var arr = filas[fila];
    if (col < 0 || col >= arr.Length || arr[col] is null)
    {
        return "";
    }

    return arr[col]!.ToString()?.Trim() ?? "";
}

static bool EsUnidadCandidata(string texto)
{
    if (string.IsNullOrWhiteSpace(texto) || texto.Length > 25)
    {
        return false;
    }

    if (decimal.TryParse(texto.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _))
    {
        return false;
    }

    if (texto.StartsWith('=') || texto.Contains('(') && texto.Length > 15)
    {
        return false;
    }

    var upper = texto.ToUpperInvariant();
    if (upper is "DESCRIPCION" or "UNIDAD" or "CANTIDAD" or "P.U." or "VALOR UNITARIO" or "VALOR TOTAL")
    {
        return false;
    }

    return true;
}

static void Agregar(HashSet<string> set, string texto)
{
    if (EsUnidadCandidata(texto))
    {
        set.Add(texto.Trim());
    }
}

static IEnumerable<(string Descripcion, string Unidad)> ItemsPresupuesto(Dictionary<string, object?[][]> hojas)
{
    if (!hojas.TryGetValue("Presupuesto", out var filas))
    {
        var alterna = hojas.Keys.FirstOrDefault(nombre =>
            nombre.Contains("edificio", StringComparison.OrdinalIgnoreCase)
            || nombre.Contains("presupuesto", StringComparison.OrdinalIgnoreCase));
        if (alterna is null || !hojas.TryGetValue(alterna, out filas))
        {
            yield break;
        }
    }

    for (var fila = 9; fila <= Math.Min(188, filas.Length); fila++)
    {
        var codigo = Celda(filas, fila, 1);
        var descripcion = Celda(filas, fila, 2);
        var unidad = Celda(filas, fila, 3);
        if (codigo.Length > 0 && codigo.Any(char.IsDigit) && descripcion.Length > 0 && unidad.Length > 0)
        {
            yield return (descripcion, unidad);
        }
    }
}

static HashSet<string> UnidadesPresupuesto(Dictionary<string, object?[][]> hojas)
{
    var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    if (!hojas.TryGetValue("Presupuesto", out var filas))
    {
        return set;
    }

    for (var fila = 9; fila <= Math.Min(188, filas.Length); fila++)
    {
        var codigo = Celda(filas, fila, 1);
        var u = Celda(filas, fila, 3);
        if (codigo.Length > 0 && codigo.Any(c => char.IsDigit(c)))
        {
            Agregar(set, u);
        }
    }

    return set;
}

static HashSet<string> UnidadesHojaPrecios(Dictionary<string, object?[][]> hojas, string nombre, int colUnidad, int filaInicio)
{
    var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    if (!hojas.TryGetValue(nombre, out var filas))
    {
        return set;
    }

    for (var fila = filaInicio + 1; fila <= filas.Length; fila++)
    {
        var desc = Celda(filas, fila, 3);
        if (desc.Length == 0)
        {
            continue;
        }

        Agregar(set, Celda(filas, fila, colUnidad));
    }

    return set;
}

static HashSet<string> UnidadesColumna(Dictionary<string, object?[][]> hojas, string nombre, int col, int filaInicio)
{
    var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    if (!hojas.TryGetValue(nombre, out var filas))
    {
        return set;
    }

    for (var fila = filaInicio; fila <= filas.Length; fila++)
    {
        Agregar(set, Celda(filas, fila, col));
    }

    return set;
}

static HashSet<string> UnidadesApuCabecera(Dictionary<string, object?[][]> hojas)
{
    var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    if (!hojas.TryGetValue("A.P.U.", out var filas))
    {
        return set;
    }

    for (var fila = 1; fila < filas.Length; fila++)
    {
        var desc = Celda(filas, fila, 3);
        var next = Celda(filas, fila + 2, 3);
        if (desc.Length > 0 && next.Equals("DESCRIPCION", StringComparison.OrdinalIgnoreCase))
        {
            Agregar(set, Celda(filas, fila, 4));
        }
    }

    return set;
}

static HashSet<string> UnidadesApuLineas(Dictionary<string, object?[][]> hojas)
{
    var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    if (!hojas.TryGetValue("A.P.U.", out var filas))
    {
        return set;
    }

    for (var fila = 1; fila <= filas.Length; fila++)
    {
        var desc = Celda(filas, fila, 3);
        if (desc.Length == 0)
        {
            continue;
        }

        var titulo = desc.ToUpperInvariant();
        if (titulo.StartsWith("VALOR ", StringComparison.Ordinal))
        {
            continue;
        }

        Agregar(set, Celda(filas, fila, 4));
    }

    return set;
}

static HashSet<string> UnidadesPmo(Dictionary<string, object?[][]> hojas)
{
    var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    if (!hojas.TryGetValue("P.M.O.", out var filas))
    {
        return set;
    }

    foreach (var fila in filas)
    {
        if (fila.Length < 4)
        {
            continue;
        }

        Agregar(set, fila[2]?.ToString() ?? "");
    }

    return set;
}