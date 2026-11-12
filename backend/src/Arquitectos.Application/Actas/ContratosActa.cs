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

    public string? RowVersion { get; set; }
}

public sealed class LineaActaSolicitud
{
    public int? Id { get; set; }

    public int ConstruccionId { get; set; }

    public int ConceptoId { get; set; }

    public int? UnidadMedidaId { get; set; }

    public int? InsumoId { get; set; }

    public decimal Cantidad { get; set; }

    public decimal Precio { get; set; }

    public string Descripcion { get; set; } = "";

    public int TipoUsuarioId { get; set; }

    public bool Aprobado { get; set; } = true;

    public int? PresupuestoItemId { get; set; }
}

public sealed record ActaCreadaDto(int NumeroActa);

public sealed record ActaResumenDto(
    int NumeroActa,
    DateOnly FechaInicial,
    DateOnly FechaFinal,
    string Contratista,
    string? Observacion,
    decimal Total,
    bool Cerrada,
    string Arquitecto,
    string Revisor);

public sealed record ConsultaActasDto(IReadOnlyList<ActaResumenDto> Actas, decimal TotalGeneral);

public sealed record ItemInformeDto(
    int? Id,
    int? NumeroActa,
    string Construccion,
    string Concepto,
    string Material,
    string Descripcion,
    string Unidad,
    decimal Cantidad,
    decimal Precio,
    decimal Subtotal,
    bool Aprobado);

public sealed record ItemMaterialInformeDto(
    string Codigo,
    string Material,
    string Unidad,
    decimal Cantidad,
    decimal Precio,
    decimal Subtotal,
    bool Aprobado,
    int? NumeroActa = null);

public sealed record InformeMaterialesDto(
    DateOnly? Desde,
    DateOnly? Hasta,
    IReadOnlyList<ItemMaterialInformeDto> Items,
    decimal Total,
    IReadOnlyList<string>? Encabezado = null,
    bool Agrupado = true);

public sealed record InformeItemsDto(
    bool Agrupado,
    DateOnly? Desde,
    DateOnly? Hasta,
    IReadOnlyList<ItemInformeDto> Items,
    decimal Total,
    bool RomperPorActa = false,
    bool OcultarConstruccion = false,
    IReadOnlyList<string>? Encabezado = null);

public sealed record LineaActaDto(
    int Id,
    int ConstruccionId,
    string Construccion,
    int ConceptoId,
    string Concepto,
    int? UnidadMedidaId,
    string UnidadMedida,
    int? InsumoId,
    string? Insumo,
    decimal Cantidad,
    decimal Precio,
    string Descripcion,
    decimal Subtotal,
    int TipoUsuarioId,
    string TipoUsuario,
    bool Aprobado,
    int? PresupuestoItemId = null,
    string? ItemPresupuesto = null);

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
    IReadOnlyList<LineaActaDto> Detalles,
    bool Cerrada,
    string RowVersion);

public sealed class CierreActaSolicitud
{
    public bool Cerrada { get; set; }

    public string? RowVersion { get; set; }
}

public enum EstadoActa
{
    Ok,
    NoEncontrado,
    Invalido,
    Conflicto
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

    public static ResultadoActa<T> Conflicto(string mensaje) =>
        new() { Estado = EstadoActa.Conflicto, Mensaje = mensaje };
}
