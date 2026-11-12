using System.Text.Json;
using Arquitectos.Application.Auditoria;
using Arquitectos.Application.Presupuestos;
using Arquitectos.Application.Usuarios;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Auditoria;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Arquitectos.Infrastructure.Presupuestos;

public sealed partial class PresupuestoServicio
{
    private static string? CicloCantidades(Presupuesto presupuesto) =>
        ReglasPresupuesto.CicloCantidades(presupuesto);

    private async Task AsegurarConceptosAsync(Presupuesto presupuesto, CancellationToken cancellationToken)
    {
        var conceptos = await _context.Conceptos.ToListAsync(cancellationToken);
        var indice = new Dictionary<string, Concepto>();
        foreach (var concepto in conceptos)
        {
            indice.TryAdd(TextoPresupuesto.Normalizar(concepto.Descripcion), concepto);
        }

        foreach (var item in presupuesto.Capitulos.SelectMany(capitulo => capitulo.Items).Where(item => item.ConceptoId is not > 0 && !string.IsNullOrWhiteSpace(item.Descripcion)))
        {
            var concepto = ImportadorPresupuesto.AsegurarConcepto(_context, indice, item.Descripcion, item.Unidad);
            item.Concepto = concepto;
            if (concepto.Id > 0)
            {
                item.ConceptoId = concepto.Id;
            }
        }
    }

    private static string? Codigos(PresupuestoSolicitud solicitud) =>
        ReglasPresupuesto.CodigoRepetido(solicitud);

}
