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
        var configuration = new ConfigurationBuilder()
            .SetBasePath(directorioApi)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets("8f4c2a91-6d3e-4b70-9c15-a1e7d4b82f06")
            .AddInMemoryCollection(ArchivoEnv.CrearValores(directorioApi))
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Arquitectos");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Arquitectos no está configurada. Defínela en el archivo .env o con dotnet user-secrets.");
        }

        var options = new DbContextOptionsBuilder<ArquitectosDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new ArquitectosDbContext(options);
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
