using System.Text.Json.Serialization;

namespace Arquitectos.Application.Movil;

public sealed class SincronizacionMovilSolicitud
{
    public Guid DispositivoId { get; set; }

    public int VersionEsquema { get; set; }

    public List<OperacionMovilSolicitud> Operaciones { get; set; } = [];
}

public sealed class OperacionMovilSolicitud
{
    public Guid IdOperacion { get; set; }

    public int Secuencia { get; set; }

    public string Tipo { get; set; } = "";

    public int NumeroActa { get; set; }

    public Guid? IdCliente { get; set; }

    public int? LineaId { get; set; }

    public string? RowVersionLinea { get; set; }

    public bool? Aprobado { get; set; }

    public LineaMovilSolicitud? Linea { get; set; }

    public DateTime CreadaEn { get; set; }
}

public sealed class LineaMovilSolicitud
{
    public int ConstruccionId { get; set; }

    public int ConceptoId { get; set; }

    public int? UnidadMedidaId { get; set; }

    public int? InsumoId { get; set; }

    public int? PresupuestoItemId { get; set; }

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal Cantidad { get; set; }

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal Precio { get; set; }

    public string Descripcion { get; set; } = "";
}

public sealed record LineaServidorMovil(
    int Id,
    Guid? IdCliente,
    int NumeroActa,
    int ConstruccionId,
    int ConceptoId,
    int? UnidadMedidaId,
    int? InsumoId,
    int? PresupuestoItemId,
    string Cantidad,
    string Precio,
    string Subtotal,
    string Descripcion,
    int TipoUsuarioId,
    bool Aprobado,
    string RowVersion);

public sealed record ResultadoLineaMovil(
    Guid IdOperacion,
    string Estado,
    int? LineaId,
    Guid? IdCliente,
    string? RowVersion,
    string? Mensaje,
    LineaServidorMovil? LineaServidor);

public sealed record SincronizacionMovilRespuesta(
    IReadOnlyList<ResultadoLineaMovil> Resultados,
    bool RequiereNuevoPaquete);

public enum EstadoSincronizacion
{
    Ok,
    SolicitudInvalida,
    NoAutorizado,
    Prohibido,
    DispositivoRevocado
}

public sealed class ResultadoSincronizacion
{
    public EstadoSincronizacion Estado { get; private init; }

    public string? Mensaje { get; private init; }

    public SincronizacionMovilRespuesta? Valor { get; private init; }

    public static ResultadoSincronizacion Ok(SincronizacionMovilRespuesta valor) =>
        new() { Estado = EstadoSincronizacion.Ok, Valor = valor };

    public static ResultadoSincronizacion SolicitudInvalida(string mensaje) =>
        new() { Estado = EstadoSincronizacion.SolicitudInvalida, Mensaje = mensaje };

    public static ResultadoSincronizacion NoAutorizado(string mensaje) =>
        new() { Estado = EstadoSincronizacion.NoAutorizado, Mensaje = mensaje };

    public static ResultadoSincronizacion Prohibido(string mensaje) =>
        new() { Estado = EstadoSincronizacion.Prohibido, Mensaje = mensaje };

    public static ResultadoSincronizacion DispositivoRevocado(string mensaje) =>
        new() { Estado = EstadoSincronizacion.DispositivoRevocado, Mensaje = mensaje };
}

public interface ISincronizadorMovil
{
    Task<ResultadoSincronizacion> SincronizarAsync(
        int usuarioId,
        long? sesionId,
        SincronizacionMovilSolicitud solicitud,
        CancellationToken cancellationToken);
}
