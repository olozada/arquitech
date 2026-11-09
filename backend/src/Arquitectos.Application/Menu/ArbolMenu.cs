namespace Arquitectos.Application.Menu;

public static class ArbolMenu
{
    public static IReadOnlyList<OpcionMenuDto> Construir(IReadOnlyList<OpcionMenuDto> opciones)
    {
        var porId = opciones.ToDictionary(opcion => opcion.Id);
        var raices = new List<OpcionMenuDto>();

        foreach (var opcion in opciones)
        {
            if (opcion.PadreId is int padreId && porId.TryGetValue(padreId, out var padre))
            {
                padre.Hijos.Add(opcion);
            }
            else
            {
                raices.Add(opcion);
            }
        }

        return raices;
    }
}
