namespace Arquitectos.Application.ProveedorMateriales;

public interface IProveedorMaterialServicio
{
    Task<ResultadoProveedorMaterial<IReadOnlyList<MaterialAgregadoDto>>> ListarAgregadosAsync(
        int proveedorId,
        CancellationToken cancellationToken);

    Task<ResultadoProveedorMaterial<IReadOnlyList<MaterialDisponibleDto>>> ListarDisponiblesAsync(
        int proveedorId,
        CancellationToken cancellationToken);

    Task<ResultadoProveedorMaterial<bool>> AgregarAsync(
        int proveedorId,
        IReadOnlyList<AgregarMaterialSolicitud> materiales,
        CancellationToken cancellationToken);

    Task<ResultadoProveedorMaterial<bool>> QuitarAsync(
        int proveedorId,
        IReadOnlyList<int> materialIds,
        CancellationToken cancellationToken);
}
