namespace Arquitectos.Application.Facturas;

public sealed class CrearFacturaSolicitud
{
    public string Numero { get; set; } = "";

    public DateOnly Fecha { get; set; }

    public int ProveedorId { get; set; }

    public string? OrdenCompra { get; set; }

    public string? Observacion { get; set; }

    public List<LineaFacturaSolicitud> Detalles { get; set; } = [];

    public string? RowVersion { get; set; }
}

public sealed class LineaFacturaSolicitud
{
    public int ConstruccionId { get; set; }

    public int InsumoId { get; set; }

    public int? UnidadMedidaId { get; set; }

    public decimal Cantidad { get; set; }

    public decimal Precio { get; set; }

    public string Descripcion { get; set; } = "";
}

public sealed record FacturaResumenDto(
    int Id,
    string Numero,
    DateOnly Fecha,
    string Proveedor,
    string? OrdenCompra,
    string? Observacion,
    decimal Total);

public sealed record ConsultaFacturasDto(
    IReadOnlyList<FacturaResumenDto> Items,
    int Total,
    int Pagina,
    int Tamano,
    decimal TotalGeneral);

public sealed record LineaFacturaDto(
    int Id,
    int ConstruccionId,
    string Construccion,
    int InsumoId,
    string? CodigoInsumo,
    string Insumo,
    int? UnidadMedidaId,
    string UnidadMedida,
    decimal Cantidad,
    decimal Precio,
    string Descripcion,
    decimal Subtotal);

public sealed record FacturaDto(
    int Id,
    string Numero,
    DateOnly Fecha,
    int ProveedorId,
    string Proveedor,
    string? OrdenCompra,
    string? Observacion,
    DateTime FechaRegistro,
    int? UsuarioRegistroId,
    decimal Total,
    IReadOnlyList<LineaFacturaDto> Detalles,
    string RowVersion);

public sealed record ItemFacturaInformeDto(
    int? FacturaId,
    string? Numero,
    DateOnly? Fecha,
    string Construccion,
    string Codigo,
    string Insumo,
    string Descripcion,
    string Unidad,
    decimal Cantidad,
    decimal Precio,
    decimal Subtotal);

public sealed record InformeFacturasDto(
    bool Agrupado,
    DateOnly? Desde,
    DateOnly? Hasta,
    IReadOnlyList<ItemFacturaInformeDto> Items,
    decimal Total,
    IReadOnlyList<string> Encabezado,
    int Cantidad,
    int Pagina,
    int Tamano);

public enum EstadoFactura
{
    Ok,
    NoEncontrado,
    Invalido,
    Conflicto
}

public sealed class ResultadoFactura<T>
{
    public EstadoFactura Estado { get; private init; }

    public string? Mensaje { get; private init; }

    public T? Valor { get; private init; }

    public static ResultadoFactura<T> Ok(T valor) => new() { Estado = EstadoFactura.Ok, Valor = valor };

    public static ResultadoFactura<T> NoEncontrado(string mensaje) =>
        new() { Estado = EstadoFactura.NoEncontrado, Mensaje = mensaje };

    public static ResultadoFactura<T> Invalido(string mensaje) =>
        new() { Estado = EstadoFactura.Invalido, Mensaje = mensaje };

    public static ResultadoFactura<T> Conflicto(string mensaje) =>
        new() { Estado = EstadoFactura.Conflicto, Mensaje = mensaje };
}
