namespace Arquitectos.Application.Actas;

public sealed class CrearActaSolicitud
{
    public DateOnly FechaInicial { get; set; }

    public DateOnly FechaFinal { get; set; }

    public int ArquitectoId { get; set; }

    public int RevisorId { get; set; }

    public int ContratistaId { get; set; }

    public string? Observacion { get; set; }

    public List<LineaActaSolicitud> Detalles { get; set; } = [];
}

public sealed class LineaActaSolicitud
{
    public int ConstruccionId { get; set; }

    public int ConceptoId { get; set; }

    public int UnidadMedidaId { get; set; }

    public int? MaterialId { get; set; }

    public decimal Cantidad { get; set; }

    public decimal Precio { get; set; }

    public string Descripcion { get; set; } = "";
}

public sealed record ActaCreadaDto(int NumeroActa);

public sealed record ActaResumenDto(
    int NumeroActa,
    DateOnly FechaInicial,
    DateOnly FechaFinal,
    string Contratista,
    string? Observacion,
    decimal Total);

public sealed record ConsultaActasDto(IReadOnlyList<ActaResumenDto> Actas, decimal TotalGeneral);

public sealed record LineaActaDto(
    int Id,
    int ConstruccionId,
    string Construccion,
    int ConceptoId,
    string Concepto,
    int UnidadMedidaId,
    string UnidadMedida,
    int? MaterialId,
    string? Material,
    decimal Cantidad,
    decimal Precio,
    string Descripcion,
    decimal Subtotal);

public sealed record ActaDto(
    int NumeroActa,
    DateOnly FechaInicial,
    DateOnly FechaFinal,
    int ArquitectoId,
    string Arquitecto,
    int RevisorId,
    string Revisor,
    int ContratistaId,
    string Contratista,
    string? Observacion,
    DateTime FechaRegistro,
    int? UsuarioRegistroId,
    decimal Total,
    IReadOnlyList<LineaActaDto> Detalles);

public enum EstadoActa
{
    Ok,
    NoEncontrado,
    Invalido
}

public sealed class ResultadoActa<T>
{
    public EstadoActa Estado { get; private init; }

    public string? Mensaje { get; private init; }

    public T? Valor { get; private init; }

    public static ResultadoActa<T> Ok(T valor) => new() { Estado = EstadoActa.Ok, Valor = valor };

    public static ResultadoActa<T> NoEncontrado(string mensaje) =>
        new() { Estado = EstadoActa.NoEncontrado, Mensaje = mensaje };

    public static ResultadoActa<T> Invalido(string mensaje) =>
        new() { Estado = EstadoActa.Invalido, Mensaje = mensaje };
}
