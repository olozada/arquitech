using Arquitectos.Application.Presupuestos;
using Arquitectos.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Infrastructure.Presupuestos;

public sealed partial class PresupuestoServicio
{
    public async Task<ResultadoOperacion<IReadOnlyList<ValidacionPresupuestoDto>>> ValidarAsync(int id, int usuarioId, CancellationToken cancellationToken)
    {
        var presupuesto = await CargarAsync(id, cancellationToken);
        if (presupuesto is null)
        {
            return ResultadoOperacion<IReadOnlyList<ValidacionPresupuestoDto>>.NoEncontrado("No se encontró el presupuesto.");
        }

        if (!await PuedeVerAsync(usuarioId, presupuesto.ConstruccionId, cancellationToken))
        {
            return ResultadoOperacion<IReadOnlyList<ValidacionPresupuestoDto>>.Prohibido("No tiene acceso a este presupuesto.");
        }

        return ResultadoOperacion<IReadOnlyList<ValidacionPresupuestoDto>>.Ok(await ValidarInternoAsync(presupuesto, cancellationToken));
    }

    private async Task<IReadOnlyList<ValidacionPresupuestoDto>> ValidarInternoAsync(Presupuesto presupuesto, CancellationToken cancellationToken)
    {
        var unidades = await _context.Apus.AsNoTracking()
            .Select(apu => new { apu.Id, apu.Unidad })
            .ToDictionaryAsync(apu => apu.Id, apu => apu.Unidad, cancellationToken);
        var avisos = new List<ValidacionPresupuestoDto>();
        var codigos = presupuesto.Capitulos.SelectMany(capitulo => capitulo.Items)
            .Select(item => item.Codigo.Trim())
            .Where(codigo => codigo.Length > 0)
            .GroupBy(codigo => codigo)
            .Where(grupo => grupo.Count() > 1)
            .Select(grupo => grupo.Key)
            .ToHashSet();
        foreach (var item in presupuesto.Capitulos.SelectMany(capitulo => capitulo.Items))
        {
            if (codigos.Contains(item.Codigo.Trim()))
            {
                avisos.Add(new ValidacionPresupuestoDto(item.Id, item.Codigo, "CodigoRepetido", $"El código {item.Codigo} está repetido."));
            }

            if (item.Cantidad == 0)
            {
                avisos.Add(new ValidacionPresupuestoDto(item.Id, item.Codigo, "CantidadCero", $"El ítem {item.Codigo} tiene cantidad cero."));
            }

            if (item.ApuId is null && item.ValorMaterialUnitario == 0 && item.ValorManoObraUnitario == 0)
            {
                avisos.Add(new ValidacionPresupuestoDto(item.Id, item.Codigo, "SinApuNiValores", $"El ítem {item.Codigo} no tiene análisis ni valores."));
            }

            if (item.ApuId is int apuId && item.FactorConversionApu == 1 && unidades.TryGetValue(apuId, out var unidad) && !TextoPresupuesto.MismaUnidad(item.Unidad, unidad))
            {
                avisos.Add(new ValidacionPresupuestoDto(item.Id, item.Codigo, "UnidadDistintaApu", $"La unidad de {item.Codigo} no coincide con el análisis de precios."));
            }

            if (item.ConceptoId is not > 0)
            {
                avisos.Add(new ValidacionPresupuestoDto(item.Id, item.Codigo, "SinConcepto", $"El ítem {item.Codigo} no tiene concepto."));
            }
        }

        return avisos;
    }

}
