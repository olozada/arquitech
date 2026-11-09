namespace Arquitectos.Application.Actas;

public interface IActaServicio
{
    Task<ResultadoActa<ActaCreadaDto>> CrearAsync(
        CrearActaSolicitud solicitud,
        int? usuarioId,
        CancellationToken cancellationToken);

    Task<ResultadoActa<ConsultaActasDto>> ConsultarAsync(
        DateOnly? desde,
        DateOnly? hasta,
        int? arquitectoId,
        CancellationToken cancellationToken);

    Task<ResultadoActa<ActaDto>> ObtenerAsync(int numeroActa, CancellationToken cancellationToken);

    Task<ResultadoActa<byte[]>> GenerarPdfAsync(int numeroActa, CancellationToken cancellationToken);

    Task<ResultadoActa<byte[]>> ExportarAsync(
        DateOnly? desde,
        DateOnly? hasta,
        int? arquitectoId,
        CancellationToken cancellationToken);
}
