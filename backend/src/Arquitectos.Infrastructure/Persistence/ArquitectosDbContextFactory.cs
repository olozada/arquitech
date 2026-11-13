using Arquitectos.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Arquitectos.Infrastructure.Persistence;

public sealed class ArquitectosDbContextFactory : IDesignTimeDbContextFactory<ArquitectosDbContext>
{
    public ArquitectosDbContext CreateDbContext(string[] args)
    {
        var directorioApi = BuscarDirectorioApi();
        var entorno = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var esDesarrollo = ArchivoEnv.EsDesarrollo(entorno);
        var previa = new ConfigurationBuilder()
            .SetBasePath(directorioApi)
            .AddJsonFile("appsettings.json", optional: false);
        AgregarJsonDeEntorno(previa, entorno, esDesarrollo);
        previa.AddEnvironmentVariables();
        var confiar = ArchivoEnv.ResolverConfianza(previa.Build(), esDesarrollo);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(directorioApi)
            .AddJsonFile("appsettings.json", optional: false);
        AgregarJsonDeEntorno(configuration, entorno, esDesarrollo);
        configuration
            .AddInMemoryCollection(ArchivoEnv.CrearValores(directorioApi, confiar))
            .AddUserSecrets("8f4c2a91-6d3e-4b70-9c15-a1e7d4b82f06")
            .AddEnvironmentVariables();
        var construida = configuration.Build();

        var connectionString = construida.GetConnectionString("Arquitectos");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // La comparación del modelo no abre la conexión. En CI no hay secretos.
            connectionString =
                "Server=127.0.0.1,9;Database=Arquitectos;User ID=ci;Password=ci;TrustServerCertificate=True;Connect Timeout=1";
        }
        else if (bool.TryParse(construida[ArchivoEnv.ClaveConfianza], out var confiarExplicita))
        {
            connectionString = ArchivoEnv.CadenaConConfianza(connectionString, confiarExplicita);
        }

        var options = new DbContextOptionsBuilder<ArquitectosDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new ArquitectosDbContext(options);
    }

    private static void AgregarJsonDeEntorno(IConfigurationBuilder configuration, string? entorno, bool esDesarrollo)
    {
        if (esDesarrollo)
        {
            configuration.AddJsonFile("appsettings.Development.json", optional: true);
            return;
        }

        if (!string.IsNullOrEmpty(entorno))
        {
            configuration.AddJsonFile($"appsettings.{entorno}.json", optional: true);
        }
    }

    private static string BuscarDirectorioApi()
    {
        foreach (var inicio in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directorio = new DirectoryInfo(inicio);
            while (directorio is not null)
            {
                var api = Path.Combine(directorio.FullName, "src", "Arquitectos.Api");
                if (File.Exists(Path.Combine(api, "appsettings.json")))
                {
                    return api;
                }

                if (directorio.Name == "Arquitectos.Api" && File.Exists(Path.Combine(directorio.FullName, "appsettings.json")))
                {
                    return directorio.FullName;
                }

                directorio = directorio.Parent;
            }
        }

        throw new InvalidOperationException("No se encontró el proyecto Arquitectos.Api para leer la configuración.");
    }
}
