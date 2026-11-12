using Arquitectos.Application.Presupuestos;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Infrastructure.Presupuestos;

public sealed class ManoObraServicio : IManoObraServicio
{
    private readonly ArquitectosDbContext _context;

    public ManoObraServicio(ArquitectosDbContext context)
    {
        _context = context;
    }

    public async Task<ParametrosManoObraDto> ObtenerParametrosAsync(CancellationToken cancellationToken)
    {
        var (parametros, factor) = await FactorAsync(cancellationToken);
        return new ParametrosManoObraDto(parametros.DiasMes, parametros.IncluirDiasNoHabiles, parametros.DiasHabilesAnio, factor);
    }

    public async Task<ParametrosManoObraDto> GuardarParametrosAsync(ParametrosManoObraSolicitud solicitud, CancellationToken cancellationToken)
    {
        var parametros = await _context.ParametrosManoObra.FirstOrDefaultAsync(cancellationToken);
        if (parametros is null)
        {
            parametros = new ParametrosManoObra();
            _context.ParametrosManoObra.Add(parametros);
        }

        parametros.DiasMes = solicitud.DiasMes <= 0 ? 30 : solicitud.DiasMes;
        parametros.IncluirDiasNoHabiles = solicitud.IncluirDiasNoHabiles;
        parametros.DiasHabilesAnio = solicitud.DiasHabilesAnio <= 0 ? 296 : solicitud.DiasHabilesAnio;
        await _context.SaveChangesAsync(cancellationToken);
        return await ObtenerParametrosAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PrestacionDto>> ListarPrestacionesAsync(CancellationToken cancellationToken)
    {
        return await _context.PrestacionesSociales.AsNoTracking()
            .OrderBy(prestacion => prestacion.Orden)
            .Select(prestacion => new PrestacionDto(prestacion.Id, prestacion.Concepto, prestacion.Porcentaje, prestacion.Orden))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PrestacionDto>> GuardarPrestacionesAsync(
        IReadOnlyList<PrestacionSolicitud> prestaciones,
        CancellationToken cancellationToken)
    {
        var actuales = await _context.PrestacionesSociales.ToListAsync(cancellationToken);
        _context.PrestacionesSociales.RemoveRange(actuales);
        var orden = 1;
        foreach (var prestacion in prestaciones.Where(item => !string.IsNullOrWhiteSpace(item.Concepto)))
        {
            _context.PrestacionesSociales.Add(new PrestacionSocial
            {
                Concepto = TextoPresupuesto.Recortar(prestacion.Concepto, 120),
                Porcentaje = prestacion.Porcentaje,
                Orden = prestacion.Orden > 0 ? prestacion.Orden : orden
            });
            orden++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return await ListarPrestacionesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CargoDto>> ListarCargosAsync(bool incluirInactivos, CancellationToken cancellationToken)
    {
        var (parametros, factor) = await FactorAsync(cancellationToken);
        var consulta = _context.Cargos.AsNoTracking();
        if (!incluirInactivos)
        {
            consulta = consulta.Where(cargo => cargo.Activo);
        }

        var cargos = await consulta.OrderBy(cargo => cargo.Nombre).ToListAsync(cancellationToken);
        return cargos.Select(cargo => Mapa(cargo, parametros, factor)).ToList();
    }

    public async Task<ResultadoOperacion<CargoDto>> GuardarCargoAsync(int? id, CargoSolicitud solicitud, CancellationToken cancellationToken)
    {
        var nombre = TextoPresupuesto.Recortar(solicitud.Nombre, 80);
        if (nombre.Length == 0)
        {
            return ResultadoOperacion<CargoDto>.Validacion("El nombre del cargo es obligatorio.");
        }

        var duplicado = await _context.Cargos.AnyAsync(cargo => cargo.Id != (id ?? 0) && cargo.Nombre == nombre, cancellationToken);
        if (duplicado)
        {
            return ResultadoOperacion<CargoDto>.Conflicto("Ya existe un cargo con ese nombre.");
        }

        Cargo entidad;
        if (id is null)
        {
            entidad = new Cargo { Nombre = nombre };
            _context.Cargos.Add(entidad);
        }
        else
        {
            var existente = await _context.Cargos.FirstOrDefaultAsync(cargo => cargo.Id == id, cancellationToken);
            if (existente is null)
            {
                return ResultadoOperacion<CargoDto>.NoEncontrado("No se encontró el cargo.");
            }

            entidad = existente;
        }

        entidad.Nombre = nombre;
        entidad.SalarioMensual = solicitud.SalarioMensual;
        entidad.Activo = solicitud.Activo;
        await _context.SaveChangesAsync(cancellationToken);
        var (parametros, factor) = await FactorAsync(cancellationToken);
        return ResultadoOperacion<CargoDto>.Ok(Mapa(entidad, parametros, factor));
    }

    public async Task<ResultadoOperacion<bool>> EliminarCargoAsync(int id, CancellationToken cancellationToken)
    {
        var cargo = await _context.Cargos.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (cargo is null)
        {
            return ResultadoOperacion<bool>.NoEncontrado("No se encontró el cargo.");
        }

        if (await _context.Set<CuadrillaCargo>().AnyAsync(integrante => integrante.CargoId == id, cancellationToken))
        {
            cargo.Activo = false;
        }
        else
        {
            _context.Cargos.Remove(cargo);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return ResultadoOperacion<bool>.Ok(true);
    }

    public async Task<IReadOnlyList<CuadrillaDto>> ListarCuadrillasAsync(bool incluirInactivos, CancellationToken cancellationToken)
    {
        var consulta = Consulta();
        if (!incluirInactivos)
        {
            consulta = consulta.Where(cuadrilla => cuadrilla.Activo);
        }

        var cuadrillas = await consulta.OrderBy(cuadrilla => cuadrilla.Nombre).ToListAsync(cancellationToken);
        var (parametros, factor) = await FactorAsync(cancellationToken);
        return cuadrillas.Select(cuadrilla => Mapa(cuadrilla, parametros, factor)).ToList();
    }

    public async Task<CuadrillaDto?> ObtenerCuadrillaAsync(int id, CancellationToken cancellationToken)
    {
        var cuadrilla = await Consulta().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (cuadrilla is null)
        {
            return null;
        }

        var (parametros, factor) = await FactorAsync(cancellationToken);
        return Mapa(cuadrilla, parametros, factor);
    }

    public async Task<ResultadoOperacion<CuadrillaDto>> GuardarCuadrillaAsync(
        int? id,
        CuadrillaSolicitud solicitud,
        CancellationToken cancellationToken)
    {
        var nombre = TextoPresupuesto.Recortar(solicitud.Nombre, 80);
        if (nombre.Length == 0)
        {
            return ResultadoOperacion<CuadrillaDto>.Validacion("El nombre de la cuadrilla es obligatorio.");
        }

        Cuadrilla entidad;
        if (id is null)
        {
            entidad = new Cuadrilla { Nombre = nombre };
            _context.Cuadrillas.Add(entidad);
        }
        else
        {
            var existente = await _context.Cuadrillas.Include(cuadrilla => cuadrilla.Integrantes)
                .FirstOrDefaultAsync(cuadrilla => cuadrilla.Id == id, cancellationToken);
            if (existente is null)
            {
                return ResultadoOperacion<CuadrillaDto>.NoEncontrado("No se encontró la cuadrilla.");
            }

            entidad = existente;
            _context.Set<CuadrillaCargo>().RemoveRange(entidad.Integrantes);
        }

        entidad.Nombre = nombre;
        entidad.Descripcion = TextoPresupuesto.Recortar(solicitud.Descripcion, 200);
        entidad.Activo = solicitud.Activo;
        foreach (var integrante in solicitud.Integrantes.Where(item => item.CargoId > 0 && item.Cantidad > 0))
        {
            entidad.Integrantes.Add(new CuadrillaCargo { CargoId = integrante.CargoId, Cantidad = integrante.Cantidad });
        }

        await _context.SaveChangesAsync(cancellationToken);
        var guardada = await ObtenerCuadrillaAsync(entidad.Id, cancellationToken);
        return ResultadoOperacion<CuadrillaDto>.Ok(guardada!);
    }

    public async Task<ResultadoOperacion<bool>> EliminarCuadrillaAsync(int id, CancellationToken cancellationToken)
    {
        var cuadrilla = await _context.Cuadrillas.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (cuadrilla is null)
        {
            return ResultadoOperacion<bool>.NoEncontrado("No se encontró la cuadrilla.");
        }

        if (await _context.Set<ApuLinea>().AnyAsync(linea => linea.CuadrillaId == id, cancellationToken))
        {
            cuadrilla.Activo = false;
        }
        else
        {
            _context.Cuadrillas.Remove(cuadrilla);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return ResultadoOperacion<bool>.Ok(true);
    }

    private IQueryable<Cuadrilla> Consulta() =>
        _context.Cuadrillas.AsNoTracking()
            .Include(cuadrilla => cuadrilla.Integrantes)
            .ThenInclude(integrante => integrante.Cargo);

    private async Task<(ParametrosManoObra Parametros, decimal Factor)> FactorAsync(CancellationToken cancellationToken)
    {
        var parametros = await _context.ParametrosManoObra.AsNoTracking().FirstOrDefaultAsync(cancellationToken)
            ?? new ParametrosManoObra { DiasMes = 30, DiasHabilesAnio = 296 };
        var porcentajes = await _context.PrestacionesSociales.AsNoTracking().Select(prestacion => prestacion.Porcentaje).ToListAsync(cancellationToken);
        return (parametros, CalculadoraApu.FactorPrestacional(porcentajes, parametros.IncluirDiasNoHabiles, parametros.DiasHabilesAnio));
    }

    private static CargoDto Mapa(Cargo cargo, ParametrosManoObra parametros, decimal factor) => new(
        cargo.Id,
        cargo.Nombre,
        cargo.SalarioMensual,
        CalculadoraApu.Jornal(cargo.SalarioMensual, parametros.DiasMes, factor),
        cargo.Activo);

    private static CuadrillaDto Mapa(Cuadrilla cuadrilla, ParametrosManoObra parametros, decimal factor)
    {
        var integrantes = cuadrilla.Integrantes
            .OrderBy(integrante => integrante.Cargo.Nombre)
            .Select(integrante => new IntegranteDto(
                integrante.CargoId,
                integrante.Cargo.Nombre,
                integrante.Cantidad,
                CalculadoraApu.Jornal(integrante.Cargo.SalarioMensual, parametros.DiasMes, factor)))
            .ToList();
        var costo = CalculadoraApu.CostoDiaCuadrilla(
            cuadrilla.Integrantes.Select(integrante => (integrante.Cargo.SalarioMensual, integrante.Cantidad)),
            parametros.DiasMes,
            factor);
        return new CuadrillaDto(cuadrilla.Id, cuadrilla.Nombre, cuadrilla.Descripcion, cuadrilla.Activo, costo, integrantes);
    }
}
