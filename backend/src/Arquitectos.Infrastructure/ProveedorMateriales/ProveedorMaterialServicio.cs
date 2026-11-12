using Arquitectos.Application.ProveedorMateriales;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Infrastructure.ProveedorMateriales;

public sealed class ProveedorMaterialServicio : IProveedorMaterialServicio
{
    private const string MensajeProveedor = "No se encontró el proveedor.";
    private const string MensajeMateriales = "Hay insumos que no existen o están inactivos.";
    private const string MensajeDuplicado = "El insumo ya está agregado al proveedor.";
    private const string MensajeNoAgregados = "No se encontraron los insumos seleccionados.";

    private readonly ArquitectosDbContext _context;

    public ProveedorMaterialServicio(ArquitectosDbContext context)
    {
        _context = context;
    }

    public async Task<ResultadoProveedorMaterial<IReadOnlyList<MaterialAgregadoDto>>> ListarAgregadosAsync(
        int proveedorId,
        CancellationToken cancellationToken)
    {
        if (!await ExisteProveedorAsync(proveedorId, cancellationToken))
        {
            return ResultadoProveedorMaterial<IReadOnlyList<MaterialAgregadoDto>>.NoEncontrado(MensajeProveedor);
        }

        var filas = await _context.ProveedorMateriales
            .AsNoTracking()
            .Where(item => item.ProveedorId == proveedorId)
            .OrderBy(item => item.Insumo.Descripcion)
            .Select(item => new { item.InsumoId, item.Insumo.Descripcion, item.Precio })
            .ToListAsync(cancellationToken);

        IReadOnlyList<MaterialAgregadoDto> materiales = filas
            .Select(item => new MaterialAgregadoDto(item.InsumoId, item.Descripcion, item.Precio))
            .ToList();

        return ResultadoProveedorMaterial<IReadOnlyList<MaterialAgregadoDto>>.Ok(materiales);
    }

    public async Task<ResultadoProveedorMaterial<IReadOnlyList<MaterialDisponibleDto>>> ListarDisponiblesAsync(
        int proveedorId,
        CancellationToken cancellationToken)
    {
        if (!await ExisteProveedorAsync(proveedorId, cancellationToken))
        {
            return ResultadoProveedorMaterial<IReadOnlyList<MaterialDisponibleDto>>.NoEncontrado(MensajeProveedor);
        }

        var asignados = _context.ProveedorMateriales
            .Where(item => item.ProveedorId == proveedorId)
            .Select(item => item.InsumoId);

        var filas = await _context.Insumos
            .AsNoTracking()
            .Where(insumo => insumo.Activo && !asignados.Contains(insumo.Id))
            .OrderBy(insumo => insumo.Descripcion)
            .Select(insumo => new { insumo.Id, insumo.Descripcion })
            .ToListAsync(cancellationToken);

        IReadOnlyList<MaterialDisponibleDto> materiales = filas
            .Select(material => new MaterialDisponibleDto(material.Id, material.Descripcion))
            .ToList();

        return ResultadoProveedorMaterial<IReadOnlyList<MaterialDisponibleDto>>.Ok(materiales);
    }

    public async Task<ResultadoProveedorMaterial<bool>> AgregarAsync(
        int proveedorId,
        IReadOnlyList<AgregarMaterialSolicitud> materiales,
        CancellationToken cancellationToken)
    {
        if (!await ExisteProveedorAsync(proveedorId, cancellationToken))
        {
            return ResultadoProveedorMaterial<bool>.NoEncontrado(MensajeProveedor);
        }

        var ids = materiales.Select(item => item.InsumoId).ToList();
        if (ids.Distinct().Count() != ids.Count)
        {
            return ResultadoProveedorMaterial<bool>.Invalido("Hay insumos repetidos en la selección.");
        }

        if (materiales.Any(item => decimal.Round(item.Precio, 2, MidpointRounding.AwayFromZero) <= 0))
        {
            return ResultadoProveedorMaterial<bool>.Invalido(AgregarMaterialesValidador.MensajePrecio);
        }

        var existentes = await _context.Insumos
            .AsNoTracking()
            .Where(insumo => ids.Contains(insumo.Id))
            .Select(insumo => new { insumo.Id, insumo.Activo })
            .ToListAsync(cancellationToken);

        if (existentes.Count != ids.Count || existentes.Any(material => !material.Activo))
        {
            return ResultadoProveedorMaterial<bool>.Invalido(MensajeMateriales);
        }

        var yaAgregados = await _context.ProveedorMateriales
            .Where(item => item.ProveedorId == proveedorId && ids.Contains(item.InsumoId))
            .Select(item => item.InsumoId)
            .ToListAsync(cancellationToken);

        if (yaAgregados.Count > 0)
        {
            return ResultadoProveedorMaterial<bool>.Duplicado(MensajeDuplicado);
        }

        foreach (var material in materiales)
        {
            _context.ProveedorMateriales.Add(new ProveedorMaterial
            {
                ProveedorId = proveedorId,
                InsumoId = material.InsumoId,
                Precio = decimal.Round(material.Precio, 2, MidpointRounding.AwayFromZero)
            });
        }

        try
        {
            await using var transaccion = await _context.Database.BeginTransactionAsync(cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException excepcion) when (EsViolacionUnica(excepcion))
        {
            return ResultadoProveedorMaterial<bool>.Duplicado(MensajeDuplicado);
        }

        return ResultadoProveedorMaterial<bool>.Ok(true);
    }

    public async Task<ResultadoProveedorMaterial<bool>> QuitarAsync(
        int proveedorId,
        IReadOnlyList<int> materialIds,
        CancellationToken cancellationToken)
    {
        if (!await ExisteProveedorAsync(proveedorId, cancellationToken))
        {
            return ResultadoProveedorMaterial<bool>.NoEncontrado(MensajeProveedor);
        }

        var ids = materialIds.Distinct().ToList();
        var agregados = await _context.ProveedorMateriales
            .Where(item => item.ProveedorId == proveedorId && ids.Contains(item.InsumoId))
            .ToListAsync(cancellationToken);

        if (agregados.Count == 0)
        {
            return ResultadoProveedorMaterial<bool>.NoEncontrado(MensajeNoAgregados);
        }

        _context.ProveedorMateriales.RemoveRange(agregados);
        await _context.SaveChangesAsync(cancellationToken);
        return ResultadoProveedorMaterial<bool>.Ok(true);
    }

    private Task<bool> ExisteProveedorAsync(int proveedorId, CancellationToken cancellationToken) =>
        _context.Proveedores.AnyAsync(proveedor => proveedor.Id == proveedorId, cancellationToken);

    private static bool EsViolacionUnica(DbUpdateException excepcion)
    {
        for (var actual = excepcion.InnerException; actual is not null; actual = actual.InnerException)
        {
            if (actual is SqlException sql && sql.Number is 2601 or 2627)
            {
                return true;
            }
        }

        return false;
    }
}
