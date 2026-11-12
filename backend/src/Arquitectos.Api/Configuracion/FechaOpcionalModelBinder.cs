using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Arquitectos.Api.Configuracion;

public sealed class FechaOpcionalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var valor = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valor == ValueProviderResult.None || string.IsNullOrWhiteSpace(valor.FirstValue))
        {
            bindingContext.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        if (DateOnly.TryParse(valor.FirstValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
        {
            bindingContext.Result = ModelBindingResult.Success(fecha);
            return Task.CompletedTask;
        }

        bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "La fecha no es válida.");
        return Task.CompletedTask;
    }
}

public sealed class FechaOpcionalModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        return context.Metadata.ModelType == typeof(DateOnly?) ? new FechaOpcionalModelBinder() : null;
    }
}
