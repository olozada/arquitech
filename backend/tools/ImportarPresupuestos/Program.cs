using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Configuration;
using Arquitectos.Infrastructure.Persistence;
using Arquitectos.Infrastructure.Presupuestos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var raiz = BuscarRaiz();
var importaciones = new (string Archivo, string Obra)[]
{
    ("Presupuesto Preliminar Casa Murillo.xlsx", "Casa Murillo (Excel)"),
    ("Presupuesto Preliminar Edfificio FIKUS Ene 2020.xls", "Edificio FIKUS"),
    ("Presupuesto preliminar Edificio Clinifracturas.xls", "Edificio Clinifracturas"),
    ("Presupuesto Preliminar LA RESERVA Casa 33 Jun 22.xls", "LA RESERVA Casa 33"),
};

var configuracion = new ConfigurationManager();
configuracion
    .AddJsonFile(Path.Combine(raiz, "backend", "src", "Arquitectos.Api", "appsettings.json"), optional: false)
    .AddJsonFile(Path.Combine(raiz, "backend", "src", "Arquitectos.Api", "appsettings.Development.json"), optional: true)
    .AddEnvironmentVariables();
var delEnv = ArchivoEnv.CrearValores(raiz, confiarCertificado: true);
if (delEnv.Count > 0)
{
    configuracion.AddInMemoryCollection(delEnv);
}

ArchivoEnv.FijarConfianza(configuracion);

var cadena = configuracion.GetConnectionString("Arquitectos");
if (string.IsNullOrWhiteSpace(cadena))
{
    Console.Error.WriteLine("No hay cadena de conexión. Revise el .env.");
    return 1;
}

Console.WriteLine("Conexión configurada.");
await using var context = new ArquitectosDbContext(
    new DbContextOptionsBuilder<ArquitectosDbContext>().UseSqlServer(cadena).Options);

await context.Database.MigrateAsync();
await SemillaUnidadesObra.AplicarAsync(context);

var importador = new ImportadorPresupuesto(context);
foreach (var (archivo, nombreObra) in importaciones)
{
    var ruta = Path.Combine(raiz, archivo);
    if (!File.Exists(ruta))
    {
        Console.WriteLine($"OMITIDO (no existe): {archivo}");
        continue;
    }

    var obra = await context.Construcciones.FirstOrDefaultAsync(item => item.Descripcion == nombreObra);
    if (obra is null)
    {
        obra = new Construccion { Descripcion = nombreObra, Activo = true };
        context.Construcciones.Add(obra);
        await context.SaveChangesAsync();
        Console.WriteLine($"Construcción creada: {nombreObra} (Id {obra.Id})");
    }

    var xlsx = ConvertidorXlsAXlsx.AsegurarXlsx(ruta);
    try
    {
        Console.WriteLine($"\nImportando {archivo} → obra {obra.Id}…");
        var modelo = LectorExcel.Leer(xlsx);
        var informe = await importador.GuardarAsync(modelo, obra.Id, CancellationToken.None);
        Console.WriteLine($"  Presupuesto Id {informe.PresupuestoId}, total {informe.Total:N0}, APUs {informe.ApusReproducidos}/{informe.ApusRevisados}, sin APU {informe.ItemsSinApu}");
        if (informe.UnidadesQueNoCoinciden.Count > 0)
        {
            Console.WriteLine($"  Unidades ≠ APU: {informe.UnidadesQueNoCoinciden.Count}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ERROR: {ex.Message}");
        context.ChangeTracker.Clear();
    }
    finally
    {
        if (!string.Equals(xlsx, ruta, StringComparison.OrdinalIgnoreCase) && File.Exists(xlsx))
        {
            File.Delete(xlsx);
        }
    }
}

Console.WriteLine("\nImportación terminada.");
return 0;

static string BuscarRaiz()
{
    var directorio = new DirectoryInfo(AppContext.BaseDirectory);
    while (directorio is not null)
    {
        if (File.Exists(Path.Combine(directorio.FullName, "Presupuesto Preliminar Casa Murillo.xlsx")))
        {
            return directorio.FullName;
        }

        directorio = directorio.Parent;
    }

    return @"C:\Arquitectos";
}