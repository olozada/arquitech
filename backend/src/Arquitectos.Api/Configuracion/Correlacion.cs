namespace Arquitectos.Api.Configuracion;

public static class Correlacion
{
    public const string Encabezado = "X-Correlation-Id";

    public static string Asegurar(HttpContext contexto)
    {
        var recibido = contexto.Request.Headers[Encabezado].ToString();
        var id = IdValido(recibido) ? recibido : Guid.NewGuid().ToString("N");
        contexto.TraceIdentifier = id;
        contexto.Request.Headers[Encabezado] = id;
        contexto.Response.Headers[Encabezado] = id;
        return id;
    }

    public static string Leer(HttpContext contexto)
    {
        var respuesta = contexto.Response.Headers[Encabezado].ToString();
        if (IdValido(respuesta))
        {
            return respuesta;
        }

        var solicitud = contexto.Request.Headers[Encabezado].ToString();
        if (IdValido(solicitud))
        {
            return solicitud;
        }

        return string.IsNullOrWhiteSpace(contexto.TraceIdentifier)
            ? Guid.NewGuid().ToString("N")
            : contexto.TraceIdentifier;
    }

    private static bool IdValido(string id) =>
        id.Length is > 0 and <= 64 && id.All(caracter => char.IsAsciiLetterOrDigit(caracter) || caracter is '-' or '_' or '.');
}
