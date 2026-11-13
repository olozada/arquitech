using System.Text.Json;

namespace Arquitectos.Infrastructure.Movil;

internal static class JsonPaquete
{
    public static readonly JsonSerializerOptions Opciones = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
