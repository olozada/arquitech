using Arquitectos.Application.ProveedorMateriales;

namespace Arquitectos.Tests;

public class ProveedorMaterialTests
{
    private readonly AgregarMaterialesValidador _agregar = new();
    private readonly QuitarMaterialesValidador _quitar = new();

    [Fact]
    public void Agregar_sin_seleccion_pide_materiales()
    {
        var resultado = _agregar.Validate([]);

        Assert.False(resultado.IsValid);
        Assert.Equal(AgregarMaterialesValidador.MensajeSeleccion, resultado.Errors[0].ErrorMessage);
    }

    [Fact]
    public void Agregar_con_precio_cero_rechaza_la_seleccion()
    {
        var resultado = _agregar.Validate(
        [
            new AgregarMaterialSolicitud { MaterialId = 1, Precio = 0 }
        ]);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, error => error.ErrorMessage == AgregarMaterialesValidador.MensajePrecio);
    }

    [Fact]
    public void Agregar_con_precio_positivo_es_valido()
    {
        var resultado = _agregar.Validate(
        [
            new AgregarMaterialSolicitud { MaterialId = 1, Precio = 1500.5m }
        ]);

        Assert.True(resultado.IsValid);
    }

    [Fact]
    public void Quitar_sin_seleccion_pide_materiales()
    {
        var resultado = _quitar.Validate([]);

        Assert.False(resultado.IsValid);
        Assert.Equal(AgregarMaterialesValidador.MensajeSeleccion, resultado.Errors[0].ErrorMessage);
    }
}
