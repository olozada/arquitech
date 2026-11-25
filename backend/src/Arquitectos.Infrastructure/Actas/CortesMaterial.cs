using Arquitectos.Application.Actas;

namespace Arquitectos.Infrastructure.Actas;

internal static class CortesMaterial
{
    public readonly record struct Corte(
        ItemMaterialInformeDto? Linea,
        decimal Cantidad,
        decimal Valor,
        string Material,
        bool Encabezado = false,
        bool OcultarIdentidad = false,
        string Codigo = "",
        string Unidad = "");

    public static List<Corte> Armar(IReadOnlyList<ItemMaterialInformeDto> items, bool subtotales, bool incluirUnicos = false)
    {
        if (!subtotales)
        {
            return items.Select(item => new Corte(item, 0, 0, "")).ToList();
        }

        var grupos = new List<List<ItemMaterialInformeDto>>();
        var indice = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            var clave = $"{item.Codigo.Trim()}\u001f{item.Material.Trim()}\u001f{item.Unidad.Trim()}";
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
            var agrupar = grupo.Count > 1 || incluirUnicos;
            if (agrupar)
            {
                resultado.Add(new Corte(
                    null,
                    0,
                    0,
                    grupo[0].Material,
                    Encabezado: true,
                    Codigo: grupo[0].Codigo,
                    Unidad: grupo[0].Unidad));
            }

            foreach (var linea in grupo)
            {
                resultado.Add(new Corte(linea, 0, 0, "", OcultarIdentidad: agrupar));
            }

            if (agrupar)
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
