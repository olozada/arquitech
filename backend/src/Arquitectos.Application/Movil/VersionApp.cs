namespace Arquitectos.Application.Movil;

public static class VersionApp
{
    public static bool EsMenor(string? anunciada, string minima)
    {
        if (!Version.TryParse(Normalizar(anunciada), out var actual))
        {
            return true;
        }

        if (!Version.TryParse(Normalizar(minima), out var exigida))
        {
            return false;
        }

        return actual < exigida;
    }

    private static string Normalizar(string? valor)
    {
        var texto = (valor ?? "").Trim();
        var partes = texto.Split('.', StringSplitOptions.RemoveEmptyEntries);
        return partes.Length >= 2 ? texto : "";
    }
}
