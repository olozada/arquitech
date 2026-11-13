using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Arquitectos.Api.Configuracion;

/// <summary>
/// El JSON siempre incluye las propiedades no anulables. Sin esto, el esquema las deja opcionales
/// y los tipos generados no coinciden con lo que el frontend ya lee.
/// </summary>
public sealed class PropiedadesPresentes : ISchemaFilter
{
    public void Apply(IOpenApiSchema esquema, SchemaFilterContext contexto)
    {
        if (esquema is not OpenApiSchema concreto || concreto.Properties is not { Count: > 0 })
        {
            return;
        }

        concreto.Required ??= new HashSet<string>(StringComparer.Ordinal);
        foreach (var (nombre, propiedad) in concreto.Properties)
        {
            if (propiedad is OpenApiSchema detalle && detalle.Type is { } tipo && tipo.HasFlag(JsonSchemaType.Null))
            {
                continue;
            }

            concreto.Required.Add(nombre);
        }
    }
}
