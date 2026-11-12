using Arquitectos.Infrastructure.Presupuestos;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Infrastructure.Persistence;

public static class SemillaCasaMurillo
{
    public static async Task<string> AplicarAsync(ArquitectosDbContext contexto, CancellationToken cancellationToken = default)
    {
        var obra = await contexto.Construcciones
            .FirstOrDefaultAsync(item => item.Descripcion.Contains("Casa campestre"), cancellationToken);
        if (obra is null)
        {
            throw new InvalidOperationException("No está la construcción Casa campestre. Recargue datos_demo.sql.");
        }

        if (!obra.Activo)
        {
            obra.Activo = true;
            await contexto.SaveChangesAsync(cancellationToken);
        }

        var existente = await contexto.Presupuestos
            .Where(item => item.ConstruccionId == obra.Id && !item.EsPlantilla)
            .Select(item => new { item.Id, item.Nombre })
            .FirstOrDefaultAsync(cancellationToken);
        if (existente is not null)
        {
            return $"La casa campestre ya tiene el presupuesto {existente.Id} ({existente.Nombre}).";
        }

        var ruta = BuscarJson();
        if (ruta is null)
        {
            throw new InvalidOperationException("No se encontró presupuesto_demo/presupuesto_casa_murillo.json.");
        }

        var modelo = LectorModelo.LeerJson(await File.ReadAllTextAsync(ruta, cancellationToken));
        var informe = await new ImportadorPresupuesto(contexto).GuardarAsync(modelo, obra.Id, cancellationToken);
        return $"Se creó el presupuesto {informe.PresupuestoId} «{modelo.Proyecto}» en {obra.Descripcion}. " +
               $"Capítulos del Excel, total {informe.Total:N0}, valor/m² {informe.ValorPorM2:N0}. " +
               $"APU reproducidos {informe.ApusReproducidos} de {informe.ApusRevisados}. Ítems sin APU: {informe.ItemsSinApu}.";
    }

    private static string? BuscarJson()
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
