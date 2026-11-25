namespace Arquitectos.Infrastructure.Presupuestos;

/// <summary>
/// Unidades oficialmente admitidas en presupuesto, actas e importación Excel.
/// </summary>
public static class CatalogoUnidadesObra
{
    public sealed record Fila(string Nombre, string Descripcion);

    public static readonly IReadOnlyList<Fila> Filas =
    [
        new("m2", "Metro cuadrado"),
        new("m3", "Metro cúbico"),
        new("ml", "Metro lineal"),
        new("kg", "Kilogramo"),
        new("lb", "Libra"),
        new("und", "Unidad"),
        new("glb", "Global"),
        new("bulto", "Bulto"),
        new("bolsa", "Bolsa"),
        new("rollo", "Rollo"),
        new("jgo", "Juego"),
        new("botella", "Botella"),
        new("tarro", "Tarro"),
        new("l", "Litro"),
        new("gal", "Galón"),
        new("gl", "Galón (envase)"),
        new("jorn", "Jornal"),
        new("dia", "Día"),
        new("hora", "Hora"),
        new("mes", "Mes"),
        new("viaje", "Viaje"),
        new("cun", "Cuña"),
        new("cunete", "Cuñete"),
        new("pers", "Persona"),
        new("mez", "Mezcla / proporción"),
        new("%tub", "Porcentaje tubería"),
    ];

    private static readonly Dictionary<string, string> PorCanonica =
        Filas.ToDictionary(fila => TextoPresupuesto.CanonicaUnidad(fila.Nombre), fila => fila.Nombre, StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> Oficiales =
        Filas.Select(fila => TextoPresupuesto.Normalizar(fila.Nombre)).ToHashSet(StringComparer.OrdinalIgnoreCase);

    public static bool EsNombreOficial(string nombre) =>
        Oficiales.Contains(TextoPresupuesto.Normalizar(nombre));

    /// <summary>
    /// Devuelve el nombre de catálogo (máx. 20) o null si el texto no se reconoce.
    /// </summary>
    public static string? ResolverNombre(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        var canonica = TextoPresupuesto.CanonicaUnidad(texto);
        if (PorCanonica.TryGetValue(canonica, out var nombre))
        {
            return nombre;
        }

        return EsNombreOficial(texto) ? TextoPresupuesto.Recortar(texto.Trim(), 20) : null;
    }
}