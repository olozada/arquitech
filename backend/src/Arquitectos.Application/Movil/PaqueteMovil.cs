namespace Arquitectos.Application.Movil;

public enum EstadoPaquete
{
    Ok,
    NoAutorizado,
    Prohibido,
    DemasiadoGrande
}

public sealed class ResultadoPaquete
{
    public EstadoPaquete Estado { get; private init; }

    public string? Mensaje { get; private init; }

    public Stream? Contenido { get; private init; }

    public string? Sha256 { get; private init; }

    public DateTime GeneradoEn { get; private init; }

    public int Esquema { get; private init; }

    public static ResultadoPaquete Ok(Stream contenido, string sha256, DateTime generadoEn, int esquema) =>
        new()
        {
            Estado = EstadoPaquete.Ok,
            Contenido = contenido,
            Sha256 = sha256,
            GeneradoEn = generadoEn,
            Esquema = esquema
        };

    public static ResultadoPaquete NoAutorizado(string mensaje) =>
        new() { Estado = EstadoPaquete.NoAutorizado, Mensaje = mensaje };

    public static ResultadoPaquete Prohibido(string mensaje) =>
        new() { Estado = EstadoPaquete.Prohibido, Mensaje = mensaje };

    public static ResultadoPaquete DemasiadoGrande(string mensaje) =>
        new() { Estado = EstadoPaquete.DemasiadoGrande, Mensaje = mensaje };
}

public interface IGeneradorPaqueteMovil
{
    Task<ResultadoPaquete> GenerarAsync(int usuarioId, long? sesionId, CancellationToken cancellationToken);
}
