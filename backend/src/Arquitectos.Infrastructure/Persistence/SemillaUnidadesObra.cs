using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Presupuestos;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Infrastructure.Persistence;

public static class SemillaUnidadesObra
{
    public static async Task AplicarAsync(ArquitectosDbContext context, CancellationToken cancellationToken = default)
    {
        var existentes = await context.UnidadesMedida.AsNoTracking()
            .Select(unidad => unidad.Nombre)
            .ToListAsync(cancellationToken);
        var indice = existentes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var fila in CatalogoUnidadesObra.Filas)
        {
            if (indice.Contains(fila.Nombre))
            {
                continue;
            }

            context.UnidadesMedida.Add(new UnidadMedida
            {
                Nombre = fila.Nombre,
                Descripcion = fila.Descripcion,
                Activo = true,
            });
            indice.Add(fila.Nombre);
        }

        if (context.ChangeTracker.HasChanges())
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}