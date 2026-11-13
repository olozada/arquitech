namespace Arquitectos.Application.Actas;

public interface IActaServicio
{
    Task<ResultadoActa<ActaCreadaDto>> CrearAsync(
        CrearActaSolicitud solicitud,
        int? usuarioId,
        CancellationToken cancellationToken);

    Task<ResultadoActa<ActaDto>> ActualizarAsync(
        int numeroActa,
        CrearActaSolicitud solicitud,
        int usuarioId,
        CancellationToken cancellationToken);

    Task<ResultadoActa<ActaDto>> CambiarCierreAsync(
        int numeroActa,
        bool cerrada,
        int usuarioId,
        CancellationToken cancellationToken,
        string? rowVersion = null);

    Task<ResultadoActa<ConsultaActasDto>> ConsultarAsync(
        DateOnly? desde,
        DateOnly? hasta,
        int? arquitectoId,
        int? revisorId,
        int? contratistaId,
        int? construccionId,
        bool? cerrada,
        int usuarioId,
        CancellationToken cancellationToken,
        int pagina = 1,
        int tamano = Paginacion.TamanoDefecto,
        string? numero = null,
        string? nombreArquitecto = null,
        string? nombreRevisor = null,
        string? nombreContratista = null,
        string? orden = null,
        bool descendente = true);

    Task<ResultadoActa<ActaDto>> ObtenerAsync(int numeroActa, int usuarioId, CancellationToken cancellationToken);

    Task<ResultadoActa<byte[]>> GenerarPdfAsync(int numeroActa, int usuarioId, CancellationToken cancellationToken);

    Task<ResultadoActa<InformeItemsDto>> ConsultarItemsAsync(
        DateOnly? desde,
        DateOnly? hasta,
        int? arquitectoId,
        int? revisorId,
        int? contratistaId,
        int? construccionId,
        bool? cerrada,
        bool agrupado,
        int usuarioId,
        string? orden,
        bool descendente,
        bool romperPorActa,
        CancellationToken cancellationToken,
        int pagina = 0,
        int tamano = 0);

    Task<ResultadoActa<InformeMaterialesDto>> ConsultarMaterialesAsync(
        DateOnly? desde,
        DateOnly? hasta,
        int? arquitectoId,
        int? revisorId,
        int? contratistaId,
        int? construccionId,
        bool? cerrada,
        int? proveedorId,
        int usuarioId,
        CancellationToken cancellationToken,
        string? orden = null,
        bool descendente = false,
        bool agrupado = true,
        int pagina = 0,
        int tamano = 0);

    Task<ResultadoActa<byte[]>> ExportarMaterialesAsync(
        DateOnly? desde,
        DateOnly? hasta,
        int? arquitectoId,
        int? revisorId,
        int? contratistaId,
        int? construccionId,
        bool? cerrada,
        int? proveedorId,
        int usuarioId,
        CancellationToken cancellationToken,
        bool agrupado = true);

    Task<ResultadoActa<byte[]>> ImprimirMaterialesAsync(
        DateOnly? desde,
        DateOnly? hasta,
        int? arquitectoId,
        int? revisorId,
        int? contratistaId,
        int? construccionId,
        bool? cerrada,
        int? proveedorId,
        int usuarioId,
        string? orden,
        bool descendente,
        CancellationToken cancellationToken,
        bool agrupado = true);

    Task<ResultadoActa<byte[]>> ImprimirItemsAsync(
        DateOnly? desde,
        DateOnly? hasta,
        int? arquitectoId,
        int? revisorId,
        int? contratistaId,
        int? construccionId,
        bool? cerrada,
        bool agrupado,
        int usuarioId,
        string? orden,
        bool descendente,
        bool romperPorActa,
        CancellationToken cancellationToken);

    Task<ResultadoActa<byte[]>> ExportarAsync(
        DateOnly? desde,
        DateOnly? hasta,
        int? arquitectoId,
        int? revisorId,
        int? contratistaId,
        int? construccionId,
        bool? cerrada,
        int usuarioId,
        CancellationToken cancellationToken);
}
