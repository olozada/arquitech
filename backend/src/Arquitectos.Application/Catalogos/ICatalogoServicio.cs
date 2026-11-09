namespace Arquitectos.Application.Catalogos;

public interface ICatalogoServicio<TEntidad, TDto, TSolicitud>
{
    Task<IReadOnlyList<TDto>> ListarAsync(bool incluirInactivos, CancellationToken cancellationToken);

    Task<TDto?> ObtenerAsync(int id, CancellationToken cancellationToken);

    Task<ResultadoCatalogo<TDto>> CrearAsync(TSolicitud solicitud, CancellationToken cancellationToken);

    Task<ResultadoCatalogo<TDto>> ActualizarAsync(int id, TSolicitud solicitud, CancellationToken cancellationToken);

    Task<ResultadoCatalogo<bool>> EliminarAsync(int id, CancellationToken cancellationToken);
}
