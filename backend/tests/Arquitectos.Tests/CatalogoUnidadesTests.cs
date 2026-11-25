using Arquitectos.Infrastructure.Presupuestos;

namespace Arquitectos.Tests;

public class CatalogoUnidadesTests
{
    [Theory]
    [InlineData("M2", "m2")]
    [InlineData("UND.", "und")]
    [InlineData("GLOBAL", "glb")]
    [InlineData("Lb", "lb")]
    [InlineData("1: 2 : 3", "mez")]
    public void ResolverNombre_unifica_variantes_excel(string entrada, string esperado)
    {
        Assert.Equal(esperado, CatalogoUnidadesObra.ResolverNombre(entrada));
    }

    [Fact]
    public void ResolverNombre_rechaza_texto_libre()
    {
        Assert.Null(CatalogoUnidadesObra.ResolverNombre("pokemon"));
    }
}