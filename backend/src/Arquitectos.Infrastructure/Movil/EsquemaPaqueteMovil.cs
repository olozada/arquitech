using System.Reflection;

namespace Arquitectos.Infrastructure.Movil;

internal static class EsquemaPaqueteMovil
{
    public const string Recurso = "Arquitectos.Infrastructure.Movil.esquema_movil_v1.sql";

    public static string Leer()
    {
        using var flujo = Assembly.GetExecutingAssembly().GetManifestResourceStream(Recurso)
            ?? throw new InvalidOperationException("No está embebido el esquema del paquete móvil.");
        using var lector = new StreamReader(flujo);
        return lector.ReadToEnd();
    }
}
