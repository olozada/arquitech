using Arquitectos.Application;
using Arquitectos.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Arquitectos.Tests;

public class ComposicionTests
{
    [Fact]
    public void AddApplication_registra_el_contenedor()
    {
        var services = new ServiceCollection();

        var resultado = services.AddApplication();

        Assert.Same(services, resultado);
    }

    [Fact]
    public void AddInfrastructure_sin_cadena_de_conexion_falla()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var excepcion = Assert.Throws<InvalidOperationException>(
            () => services.AddInfrastructure(configuration));

        Assert.Contains("ConnectionStrings:Arquitectos", excepcion.Message);
    }

    [Fact]
    public void AddInfrastructure_con_cadena_de_conexion_completa_el_registro()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .Add(new FuenteMemoria(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Arquitectos"] = "Server=localhost;Database=Arquitectos;Trusted_Connection=True;TrustServerCertificate=True"
            }))
            .Build();

        var resultado = services.AddInfrastructure(configuration);

        Assert.Same(services, resultado);
    }

    private sealed class FuenteMemoria : IConfigurationSource
    {
        private readonly IEnumerable<KeyValuePair<string, string?>> _datos;

        public FuenteMemoria(IEnumerable<KeyValuePair<string, string?>> datos) => _datos = datos;

        public IConfigurationProvider Build(IConfigurationBuilder builder) => new ProveedorMemoria(_datos);
    }

    private sealed class ProveedorMemoria : ConfigurationProvider
    {
        public ProveedorMemoria(IEnumerable<KeyValuePair<string, string?>> datos)
        {
            foreach (var dato in datos)
            {
                Data[dato.Key] = dato.Value;
            }
        }
    }
}
