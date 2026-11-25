using Arquitectos.Application.Actas;

namespace Arquitectos.Infrastructure.Actas;

internal static class CortesMaterial
{
    public readonly record struct Corte(ItemMaterialInformeDto? Linea, decimal Cantidad, decimal Valor, string Material);

    public static List<Corte> Armar(IReadOnlyList<ItemMaterialInformeDto> items, bool subtotales)
    {
        if (!subtotales)
        {
            return items.Select(item => new Corte(item, 0, 0, "")).ToList();
        }

        var grupos = new List<List<ItemMaterialInformeDto>>();
        var indice = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            var clave = $"{item.Material.Trim()}\u001f{item.Unidad.Trim()}";
            if (!indice.TryGetValue(clave, out var posicion))
            {
                posicion = grupos.Count;
                indice[clave] = posicion;
                grupos.Add([]);
            }

            grupos[posicion].Add(item);
        }

        var resultado = new List<Corte>();
        foreach (var grupo in grupos)
        {
            foreach (var linea in grupo)
            {
                resultado.Add(new Corte(linea, 0, 0, ""));
            }

            if (grupo.Count > 1)
            {
                resultado.Add(new Corte(
                    null,
                    grupo.Sum(linea => linea.Cantidad),
                    grupo.Sum(linea => linea.Subtotal),
                    grupo[0].Material));
            }
        }

        return resultado;
    }

    public static List<(string Nombre, List<ItemMaterialInformeDto> Items)> Casas(IReadOnlyList<ItemMaterialInformeDto> items)
    {
        var grupos = new List<(string Nombre, List<ItemMaterialInformeDto> Items)>();
        var indice = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            var nombre = item.Construccion ?? "";
            if (!indice.TryGetValue(nombre, out var posicion))
            {
                posicion = grupos.Count;
                indice[nombre] = posicion;
                grupos.Add((nombre, []));
            }

            grupos[posicion].Items.Add(item);
        }

        return grupos;
    }
}
