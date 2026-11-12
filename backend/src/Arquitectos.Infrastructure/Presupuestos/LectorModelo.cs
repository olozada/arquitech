using System.Text.Json;
using System.Text.Json.Serialization;

namespace Arquitectos.Infrastructure.Presupuestos;

public sealed class ModeloImportado
{
    public string? Proyecto { get; set; }

    public int MesesProyecto { get; set; }

    public List<InsumoImportado> Insumos { get; set; } = [];

    public ManoObraImportada ManoObra { get; set; } = new();

    public List<ApuImportado> Apus { get; set; } = [];

    public PresupuestoImportado Presupuesto { get; set; } = new();
}

public sealed class InsumoImportado
{
    public string? Codigo { get; set; }

    public string Tipo { get; set; } = "";

    public string Descripcion { get; set; } = "";

    public string? UnidadCompra { get; set; }

    public decimal PrecioBase { get; set; }

    public decimal Iva { get; set; }

    public decimal CantidadPorUnidad { get; set; } = 1;

    public string? UnidadUso { get; set; }

    public decimal PrecioUnitario { get; set; }
}

public sealed class ManoObraImportada
{
    public List<PrestacionImportada> Prestaciones { get; set; } = [];

    public List<CargoImportado> Cargos { get; set; } = [];

    public List<CuadrillaImportada> Cuadrillas { get; set; } = [];
}

public sealed class PrestacionImportada
{
    public string Concepto { get; set; } = "";

    public decimal Porcentaje { get; set; }
}

public sealed class CargoImportado
{
    public string Cargo { get; set; } = "";

    public decimal SalarioMensual { get; set; }
}

public sealed class CuadrillaImportada
{
    public string? Nombre { get; set; }

    public decimal Maestros { get; set; }

    public decimal Oficiales { get; set; }

    public decimal Ayudantes { get; set; }

    public decimal? CostoDia { get; set; }
}

public sealed class ApuImportado
{
    public int Id { get; set; }

    public int Fila { get; set; }

    public int? FilaPresupuesto { get; set; }

    public string Descripcion { get; set; } = "";

    public string? Unidad { get; set; }

    public bool EsSubanalisis { get; set; }

    public decimal? TotalExcel { get; set; }

    public decimal Divisor { get; set; } = 1;

    public List<GrupoImportado> Grupos { get; set; } = [];
}

public sealed class GrupoImportado
{
    public string Tipo { get; set; } = "";

    public decimal Divisor { get; set; } = 1;

    public List<LineaImportada> Lineas { get; set; } = [];
}

public sealed class LineaImportada
{
    public string? CodigoInsumo { get; set; }

    public string Descripcion { get; set; } = "";

    [JsonConverter(typeof(UnidadFlexible))]
    public string? Unidad { get; set; }

    public decimal Cantidad { get; set; }

    public decimal Precio { get; set; }

    public decimal Factor { get; set; }

    public string Modo { get; set; } = "desperdicio";

    public string? Tipo { get; set; }

    public decimal? PorcentajeMO { get; set; }

    public int? SubApuId { get; set; }
}

public sealed class PresupuestoImportado
{
    public List<CapituloImportado> Capitulos { get; set; } = [];

    public decimal PorcentajeImprevistos { get; set; }

    public decimal PorcentajeIncremento { get; set; }

    public decimal AreaM2 { get; set; }
}

public sealed class CapituloImportado
{
    public int Numero { get; set; }

    public string Nombre { get; set; } = "";

    public List<ItemImportado> Items { get; set; } = [];
}

public sealed class ItemImportado
{
    public string Codigo { get; set; } = "";

    public int Fila { get; set; }

    public string Descripcion { get; set; } = "";

    public string? Unidad { get; set; }

    public decimal Cantidad { get; set; }

    public decimal ValorMaterial { get; set; }

    public decimal ValorManoObra { get; set; }

    public int? ApuId { get; set; }
}

public sealed class InformeImportacion
{
    public int ApusReproducidos { get; set; }

    public int ApusRevisados { get; set; }

    public int UnidadesDistintas { get; set; }

    public int CodigosDuplicados { get; set; }

    public int ItemsSinApu { get; set; }

    public decimal Subtotal { get; set; }

    public decimal TotalMaterial { get; set; }

    public decimal TotalManoObra { get; set; }

    public decimal Imprevistos { get; set; }

    public decimal Incremento { get; set; }

    public decimal Total { get; set; }

    public decimal ValorPorM2 { get; set; }

    public List<string> UnidadesQueNoCoinciden { get; set; } = [];

    public List<string> ApusDistintos { get; set; } = [];
}

public static class LectorModelo
{
    private static readonly JsonSerializerOptions Opciones = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static ModeloImportado LeerJson(string json) =>
        JsonSerializer.Deserialize<ModeloImportado>(json, Opciones) ?? new ModeloImportado();

    public static Dictionary<int, ApuCalculo> Mapa(ModeloImportado modelo) =>
        modelo.Apus.ToDictionary(
            apu => apu.Id,
            apu => new ApuCalculo(
                apu.Id,
                apu.Divisor == 0 ? 1 : apu.Divisor,
                apu.Grupos.Select(grupo => new GrupoCalculo(
                    grupo.Tipo,
                    grupo.Divisor == 0 ? 1 : grupo.Divisor,
                    grupo.Lineas.Select(linea => new LineaCalculo(
                        Modo(linea.Modo),
                        linea.Cantidad,
                        linea.PorcentajeMO ?? linea.Factor,
                        linea.Precio,
                        linea.SubApuId,
                        string.Equals(linea.Tipo, "HerramientaMenor", StringComparison.OrdinalIgnoreCase))).ToList())).ToList()));

    public static InformeImportacion Evaluar(ModeloImportado modelo)
    {
        var apus = Mapa(modelo);

        var informe = new InformeImportacion();
        foreach (var apu in modelo.Apus)
        {
            informe.ApusRevisados++;
            try
            {
                var total = CalculadoraApu.Calcular(apu.Id, apus).Total;
                var excel = apu.TotalExcel ?? 0;
                if (Math.Abs(total - excel) <= Math.Max(1m, Math.Abs(excel) * 0.001m))
                {
                    informe.ApusReproducidos++;
                }
                else
                {
                    informe.ApusDistintos.Add(apu.Descripcion);
                }
            }
            catch (CicloApuException)
            {
                informe.ApusDistintos.Add(apu.Descripcion);
            }
        }

        var items = modelo.Presupuesto.Capitulos.SelectMany(capitulo => capitulo.Items).ToList();
        informe.CodigosDuplicados = items.GroupBy(item => item.Codigo).Count(grupo => grupo.Count() > 1);
        informe.ItemsSinApu = items.Count(item => item.ApuId is not > 0);
        var porApu = modelo.Apus.ToDictionary(apu => apu.Id);
        informe.UnidadesDistintas = 0;
        foreach (var item in items)
        {
            if (item.ApuId is not int id || !porApu.TryGetValue(id, out var apu))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(apu.Unidad) || MismaUnidad(item.Unidad, apu.Unidad))
            {
                continue;
            }

            informe.UnidadesDistintas++;
            informe.UnidadesQueNoCoinciden.Add($"{item.Codigo} {item.Unidad}≠{apu.Unidad}");
        }

        informe.TotalMaterial = items.Sum(item => item.Cantidad * item.ValorMaterial);
        informe.TotalManoObra = items.Sum(item => item.Cantidad * item.ValorManoObra);
        var resumen = CalculadoraPresupuesto.Resumir(
            informe.TotalMaterial + informe.TotalManoObra,
            informe.TotalMaterial,
            informe.TotalManoObra,
            modelo.Presupuesto.PorcentajeImprevistos,
            modelo.Presupuesto.PorcentajeIncremento,
            0,
            0,
            0,
            modelo.Presupuesto.AreaM2);
        informe.Subtotal = resumen.Subtotal;
        informe.Imprevistos = resumen.Imprevistos;
        informe.Incremento = resumen.Incremento;
        informe.Total = resumen.Total;
        informe.ValorPorM2 = resumen.ValorPorM2 ?? 0;
        return informe;
    }

    private static ModoCalculo Modo(string modo) => modo.ToLowerInvariant() switch
    {
        "rendimiento" => ModoCalculo.Rendimiento,
        "divisor" => ModoCalculo.Divisor,
        _ => ModoCalculo.Desperdicio
    };

    private static bool MismaUnidad(string? izquierda, string? derecha) =>
        Canonica(izquierda) == Canonica(derecha);

    private static string Canonica(string? texto)
    {
        var valor = (texto ?? "").Trim().ToUpperInvariant()
            .Replace(" ", "", StringComparison.Ordinal)
            .Replace(".", "", StringComparison.Ordinal)
            .Replace("²", "2", StringComparison.Ordinal)
            .Replace("³", "3", StringComparison.Ordinal);
        return valor switch
        {
            "MTS2" or "MT2" or "M²" => "M2",
            "MTS3" or "MT3" => "M3",
            "ML" or "MLINEAL" => "ML",
            "UND" or "UN" or "UNID" or "UNIDAD" => "UND",
            "GLB" or "GLOBAL" => "GLB",
            _ => valor
        };
    }
}

public sealed class UnidadFlexible : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.GetDecimal().ToString(System.Globalization.CultureInfo.InvariantCulture),
            JsonTokenType.Null => null,
            _ => null
        };

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value);
    }
}
