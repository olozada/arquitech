using Arquitectos.Application.Movil;

namespace Arquitectos.Tests;

// Casos espejo de movil/test/version_app_movil_test.dart.
public sealed class VersionAppTests
{
    [Theory]
    [InlineData("1.0.0+1", "1.0.0", false)]
    [InlineData("1.2.0-beta", "1.1.0", false)]
    [InlineData("0.9.0", "1.0.0", true)]
    [InlineData("", "1.0.0", true)]
    [InlineData("abc", "1.0.0", true)]
    public void EsMenor_evalua_metadatos_semver(string anunciada, string minima, bool esperado)
    {
        Assert.Equal(esperado, VersionApp.EsMenor(anunciada, minima));
    }
}
