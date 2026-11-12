using Arquitectos.Domain.Entidades;

namespace Arquitectos.Application.Presupuestos;

public static class ReglasPresupuesto
{
    public static string? CicloCantidades(Presupuesto presupuesto)
    {
        var porId = presupuesto.Capitulos.SelectMany(capitulo => capitulo.Items).Where(item => item.Id > 0).ToDictionary(item => item.Id);
        foreach (var inicio in porId.Values)
        {
            var visto = new HashSet<int>();
            var actual = inicio;
            while (actual.CantidadOrigenItemId is int origen && porId.TryGetValue(origen, out var siguiente))
            {
                if (!visto.Add(actual.Id))
                {
                    return "No se permite una referencia circular entre ítems.";
                }

                actual = siguiente;
            }
        }

        return null;
    }

    public static string? CodigoRepetido(PresupuestoSolicitud solicitud)
    {
        var repetido = solicitud.Capitulos.SelectMany(capitulo => capitulo.Items)
            .Select(item => item.Codigo.Trim())
            .Where(codigo => codigo.Length > 0)
            .GroupBy(codigo => codigo)
            .FirstOrDefault(grupo => grupo.Count() > 1);
        return repetido is null ? null : $"El código {repetido.Key} está repetido en el presupuesto.";
    }

    public static string SiguienteCodigo(Presupuesto presupuesto, int numeroCapitulo)
    {
        var prefijo = numeroCapitulo.ToString();
        var maximo = 0;
        foreach (var codigo in presupuesto.Capitulos.SelectMany(capitulo => capitulo.Items).Select(item => item.Codigo))
        {
            var partes = codigo.Split('.');
            if (partes.Length == 2 && partes[0] == prefijo && int.TryParse(partes[1], out var numero))
            {
                maximo = Math.Max(maximo, numero);
            }
        }

        var candidato = $"{prefijo}.{(maximo + 1):00}";
        var usados = presupuesto.Capitulos.SelectMany(capitulo => capitulo.Items).Select(item => item.Codigo).ToHashSet();
        while (usados.Contains(candidato))
        {
            maximo++;
            candidato = $"{prefijo}.{(maximo + 1):00}";
        }

        return candidato;
    }

    public static bool VersionDistinta(byte[]? actual, string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            return !(actual ?? []).SequenceEqual(Convert.FromBase64String(token));
        }
        catch (FormatException)
        {
            return true;
        }
    }
}
