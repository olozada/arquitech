namespace Arquitectos.Application.Actas;

public interface IOperacionesLineaActa
{
    Task<ResultadoActa<LineaAplicadaDto>> AgregarAsync(
        int numeroActa,
        Guid idCliente,
        LineaActaSolicitud linea,
        ActorActa actor,
        CancellationToken cancellationToken);

    Task<ResultadoActa<LineaAplicadaDto>> EditarAsync(
        int numeroActa,
        int lineaId,
        string rowVersionLinea,
        LineaActaSolicitud linea,
        ActorActa actor,
        CancellationToken cancellationToken);

    Task<ResultadoActa<LineaAplicadaDto>> EliminarAsync(
        int numeroActa,
        int lineaId,
        string rowVersionLinea,
        ActorActa actor,
        CancellationToken cancellationToken);

    Task<ResultadoActa<LineaAplicadaDto>> RevisarAsync(
        int numeroActa,
        int lineaId,
        string rowVersionLinea,
        bool aprobado,
        ActorActa actor,
        CancellationToken cancellationToken);
}

public sealed record LineaAplicadaDto(int Id, Guid? IdCliente, string RowVersion, decimal Subtotal, bool Aprobado);
