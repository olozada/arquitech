using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Infrastructure.Presupuestos;

public sealed class MotorCostos
{
    private readonly ArquitectosDbContext _context;

    public MotorCostos(ArquitectosDbContext context)
    {
        _context = context;
    }

    public async Task<ContextoCosto> CargarAsync(CancellationToken cancellationToken)
    {
        var parametros = await _context.ParametrosManoObra.AsNoTracking().FirstOrDefaultAsync(cancellationToken)
            ?? new ParametrosManoObra { DiasMes = 30, DiasHabilesAnio = 296 };
        var porcentajes = await _context.PrestacionesSociales.AsNoTracking()
            .Select(prestacion => prestacion.Porcentaje)
            .ToListAsync(cancellationToken);
        var factor = CalculadoraApu.FactorPrestacional(porcentajes, parametros.IncluirDiasNoHabiles, parametros.DiasHabilesAnio);
        var cuadrillas = await _context.Cuadrillas.AsNoTracking()
            .Include(cuadrilla => cuadrilla.Integrantes)
            .ThenInclude(integrante => integrante.Cargo)
            .ToListAsync(cancellationToken);
        var costos = cuadrillas.ToDictionary(
            cuadrilla => cuadrilla.Id,
            cuadrilla => CalculadoraApu.CostoDiaCuadrilla(
                cuadrilla.Integrantes.Select(integrante => (integrante.Cargo.SalarioMensual, integrante.Cantidad)),
                parametros.DiasMes,
                factor));
        var apus = await _context.Apus.AsNoTracking()
            .Include(apu => apu.Grupos)
            .ThenInclude(grupo => grupo.Lineas)
            .ThenInclude(linea => linea.Insumo)
            .ToListAsync(cancellationToken);
        var calculos = apus.ToDictionary(
            apu => apu.Id,
            apu => new ApuCalculo(
                apu.Id,
                apu.DivisorGeneral == 0 ? 1 : apu.DivisorGeneral,
                apu.Grupos
                    .OrderBy(grupo => grupo.Orden)
                    .Select(grupo => new GrupoCalculo(
                        grupo.Grupo.ToString(),
                        grupo.Divisor == 0 ? 1 : grupo.Divisor,
                        grupo.Lineas.OrderBy(linea => linea.Orden).Select(linea => Linea(linea, costos)).ToList()))
                    .ToList()));

        return new ContextoCosto(parametros, factor, costos, calculos);
    }

    public static ResultadoApu? Calcular(int apuId, ContextoCosto contexto)
    {
        try
        {
            return CalculadoraApu.Calcular(apuId, contexto.Apus);
        }
        catch (CicloApuException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static LineaCalculo Linea(ApuLinea linea, IReadOnlyDictionary<int, decimal> costos)
    {
        var herramienta = linea.Modo == ModoLineaApu.PorcentajeManoObra;
        decimal precio = 0;
        if (!herramienta && linea.SubApuId is null)
        {
            if (linea.Insumo is not null)
            {
                precio = linea.Insumo.PrecioUnitario;
            }
            else if (linea.CuadrillaId is int cuadrilla && costos.TryGetValue(cuadrilla, out var costo))
            {
                precio = costo;
            }
            else
            {
                precio = linea.PrecioManual ?? 0;
            }
        }

        var modo = linea.Modo switch
        {
            ModoLineaApu.Rendimiento => ModoCalculo.Rendimiento,
            ModoLineaApu.Divisor => ModoCalculo.Divisor,
            _ => ModoCalculo.Desperdicio
        };
        return new LineaCalculo(modo, linea.Cantidad, linea.Factor, precio, linea.SubApuId, herramienta);
    }
}

public sealed record ContextoCosto(
    ParametrosManoObra Parametros,
    decimal FactorPrestacional,
    IReadOnlyDictionary<int, decimal> CostoDia,
    IReadOnlyDictionary<int, ApuCalculo> Apus);
