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
    public async Task<ResultadoOperacion<RecalculoDto>> RecalcularAsync(int id, bool aplicar, int usuarioId, CancellationToken cancellationToken, string? rowVersion = null)
    {
        var presupuesto = await CargarAsync(id, cancellationToken);
        if (presupuesto is null)
        {
            return ResultadoOperacion<RecalculoDto>.NoEncontrado("No se encontró el presupuesto.");
        }

        if (!await PuedeVerAsync(usuarioId, presupuesto.ConstruccionId, cancellationToken))
        {
            return ResultadoOperacion<RecalculoDto>.Prohibido("No tiene acceso a este presupuesto.");
        }

        if (aplicar && presupuesto.Estado != EstadoPresupuesto.Borrador)
        {
            return ResultadoOperacion<RecalculoDto>.Validacion("Solo un borrador puede guardar el recálculo.");
        }

        if (aplicar && !await EsAdministradorAsync(usuarioId, cancellationToken))
        {
            return ResultadoOperacion<RecalculoDto>.Prohibido("Solo el administrador puede guardar el recálculo.");
        }

        if (aplicar && VersionDistinta(presupuesto, rowVersion))
        {
            return ResultadoOperacion<RecalculoDto>.Conflicto("El presupuesto fue modificado por otro usuario.");
        }

        var actual = CostoDe(presupuesto);
        var propuesta = await ProponerAsync(presupuesto, cancellationToken);
        if (aplicar)
        {
            await AplicarCalculoAsync(presupuesto, true, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return ResultadoOperacion<RecalculoDto>.Ok(new RecalculoDto(actual, propuesta.Costo, propuesta.Diferencias, aplicar));
    }


    private async Task AplicarCalculoAsync(Presupuesto presupuesto, bool escribirPrecios, CancellationToken cancellationToken)
    {
        var items = presupuesto.Capitulos.SelectMany(capitulo => capitulo.Items).ToList();
        foreach (var item in items)
        {
            if (item.Mediciones.Count > 0 && item.CantidadOrigenItemId is null)
            {
                item.Cantidad = item.Mediciones.Sum(medicion => medicion.Parcial);
            }
        }

        for (var paso = 0; paso < items.Count; paso++)
        {
            var cambio = false;
            foreach (var item in items)
            {
                if (item.CantidadOrigenItemId is not int origenId)
                {
                    continue;
                }

                var origen = items.FirstOrDefault(candidato => candidato.Id == origenId);
                if (origen is null)
                {
                    continue;
                }

                var cantidad = origen.Cantidad * (item.CantidadOrigenFactor == 0 ? 1 : item.CantidadOrigenFactor);
                if (cantidad != item.Cantidad)
                {
                    item.Cantidad = cantidad;
                    cambio = true;
                }
            }

            if (!cambio)
            {
                break;
            }
        }

        if (!escribirPrecios)
        {
            return;
        }

        var contexto = await _motor.CargarAsync(cancellationToken);
        foreach (var item in items.Where(fila => fila.ApuId is not null))
        {
            if (MotorCostos.Calcular(item.ApuId!.Value, contexto) is not ResultadoApu resultado)
            {
                continue;
            }

            var (material, manoObra) = CalculadoraPresupuesto.UnitariosDesdeApu(resultado.Grupos, item.FactorConversionApu);
            item.ValorMaterialUnitario = material;
            item.ValorManoObraUnitario = manoObra;
        }
    }

    private async Task<(CostoPresupuestoDto Costo, List<DiferenciaItemDto> Diferencias)> ProponerAsync(
        Presupuesto presupuesto,
        CancellationToken cancellationToken)
    {
        var contexto = await _motor.CargarAsync(cancellationToken);
        decimal material = 0;
        decimal manoObra = 0;
        var diferencias = new List<DiferenciaItemDto>();
        foreach (var item in presupuesto.Capitulos.SelectMany(capitulo => capitulo.Items))
        {
            var nuevoMaterial = item.ValorMaterialUnitario;
            var nuevaManoObra = item.ValorManoObraUnitario;
            if (item.ApuId is int apuId && MotorCostos.Calcular(apuId, contexto) is ResultadoApu resultado)
            {
                (nuevoMaterial, nuevaManoObra) = CalculadoraPresupuesto.UnitariosDesdeApu(resultado.Grupos, item.FactorConversionApu);
            }

            material += item.Cantidad * nuevoMaterial;
            manoObra += item.Cantidad * nuevaManoObra;
            var delta = item.Cantidad * ((nuevoMaterial + nuevaManoObra) - (item.ValorMaterialUnitario + item.ValorManoObraUnitario));
            if (Math.Abs(delta) > 0.5m)
            {
                diferencias.Add(new DiferenciaItemDto(item.Codigo, item.Descripcion, item.ValorMaterialUnitario, nuevoMaterial, item.ValorManoObraUnitario, nuevaManoObra, delta));
            }
        }

        return (Resumen(presupuesto, material + manoObra, material, manoObra), diferencias);
    }

}
