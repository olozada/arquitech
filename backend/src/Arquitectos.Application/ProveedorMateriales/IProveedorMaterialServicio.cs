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

    Task<ResultadoProveedorMaterial<MapeoProductosDto>> ObtenerMapeoAsync(
        int proveedorId,
        CancellationToken cancellationToken);

    Task<ResultadoProveedorMaterial<VistaCargueDto>> PrevisualizarCargueAsync(
        int proveedorId,
        Stream archivo,
        string nombreArchivo,
        CancellationToken cancellationToken);

    Task<ResultadoProveedorMaterial<ResultadoCargueDto>> CargarProductosAsync(
        int proveedorId,
        Stream archivo,
        string nombreArchivo,
        MapeoProductosDto mapeo,
        CancellationToken cancellationToken);
}
