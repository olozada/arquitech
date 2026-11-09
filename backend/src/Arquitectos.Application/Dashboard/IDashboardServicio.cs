namespace Arquitectos.Application.Dashboard;

public interface IDashboardServicio
{
    Task<DashboardDto> ObtenerAsync(FiltroDashboard filtro, CancellationToken cancellationToken);

    Task<PaginaAlertasDto> AlertasAsync(
        FiltroDashboard filtro,
        string? tipo,
        string? severidad,
        int pagina,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PuntoPrecioDto>> PrecioUnitarioAsync(
        DateOnly desde,
        DateOnly hasta,
        int? conceptoId,
        int? materialId,
        CancellationToken cancellationToken);

    Task<byte[]> ExportarAsync(FiltroDashboard filtro, CancellationToken cancellationToken);
}
