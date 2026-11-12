using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Presupuestos;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Infrastructure.Persistence;

public static class SemillaPlantilla
{
    public const string Nombre = "Vivienda unifamiliar";

    public static async Task<string> AplicarAsync(ArquitectosDbContext contexto, CancellationToken cancellationToken = default)
    {
        if (await contexto.Presupuestos.AnyAsync(presupuesto => presupuesto.EsPlantilla && presupuesto.Nombre == Nombre, cancellationToken))
        {
            return "La plantilla Vivienda unifamiliar ya existe.";
        }

        var ruta = BuscarJson();
        if (ruta is null)
        {
            throw new InvalidOperationException("No se encontró presupuesto_demo/presupuesto_casa_murillo.json.");
        }

        var modelo = LectorModelo.LeerJson(await File.ReadAllTextAsync(ruta, cancellationToken));
        var plantilla = new Presupuesto
        {
            EsPlantilla = true,
            Nombre = Nombre,
            Version = 1,
            Estado = EstadoPresupuesto.Borrador,
            Fecha = DateOnly.FromDateTime(DateTime.UtcNow),
            MesesProyecto = modelo.MesesProyecto,
            AreaConstruidaM2 = modelo.Presupuesto.AreaM2,
            PctImprevistos = modelo.Presupuesto.PorcentajeImprevistos,
            PctIncremento = modelo.Presupuesto.PorcentajeIncremento
        };
        var ordenCapitulo = 1;
        foreach (var capitulo in modelo.Presupuesto.Capitulos)
        {
            var nuevo = new PresupuestoCapitulo
            {
                Numero = capitulo.Numero == 0 ? ordenCapitulo : capitulo.Numero,
                Nombre = TextoPresupuesto.Recortar(capitulo.Nombre, 200),
                Orden = ordenCapitulo
            };
            var orden = 1;
            foreach (var item in capitulo.Items)
            {
                nuevo.Items.Add(new PresupuestoItem
                {
                    Codigo = TextoPresupuesto.Recortar(item.Codigo, 20),
                    Descripcion = TextoPresupuesto.Recortar(item.Descripcion, 500),
                    Unidad = TextoPresupuesto.Recortar(item.Unidad ?? "", 20),
                    Cantidad = 0,
                    FactorConversionApu = 1,
                    CantidadOrigenFactor = 1,
                    ValorMaterialUnitario = item.ValorMaterial,
                    ValorManoObraUnitario = item.ValorManoObra,
                    Orden = orden++
                });
            }

            plantilla.Capitulos.Add(nuevo);
            ordenCapitulo++;
        }

        contexto.Presupuestos.Add(plantilla);
        await contexto.SaveChangesAsync(cancellationToken);
        var items = plantilla.Capitulos.Sum(capitulo => capitulo.Items.Count);
        return $"Se creó la plantilla Vivienda unifamiliar con {plantilla.Capitulos.Count} capítulos y {items} ítems.";
    }

    public static string? BuscarJson()
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (directorio is not null)
        {
            var candidato = Path.Combine(directorio.FullName, "presupuesto_demo", "presupuesto_casa_murillo.json");
            if (File.Exists(candidato))
            {
                return candidato;
            }

            directorio = directorio.Parent;
        }

        return null;
    }
}
