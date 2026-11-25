namespace Arquitectos.Application.Facturas;

public interface IFacturaServicio
{
    Task<ResultadoFactura<ConsultaFacturasDto>> ConsultarAsync(
        DateOnly? desde,
        DateOnly? hasta,
        int? proveedorId,
        int? construccionId,
        string? numero,
        string? nombreProveedor,
        int pagina,
        int tamano,
        CancellationToken cancellationToken);

    Task<ResultadoFactura<FacturaDto>> ObtenerAsync(int id, CancellationToken cancellationToken);

    Task<ResultadoFactura<FacturaDto>> CrearAsync(
        CrearFacturaSolicitud solicitud,
        int usuarioId,
        CancellationToken cancellationToken);

    Task<ResultadoFactura<FacturaDto>> ActualizarAsync(
        int id,
        CrearFacturaSolicitud solicitud,
        CancellationToken cancellationToken);

    Task<ResultadoFactura<bool>> EliminarAsync(int id, CancellationToken cancellationToken);

    Task<ResultadoFactura<byte[]>> PdfAsync(int id, CancellationToken cancellationToken);

    Task<ResultadoFactura<InformeFacturasDto>> ConsultarItemsAsync(
        DateOnly? desde,
        DateOnly? hasta,
        int? proveedorId,
        int? construccionId,
        bool agrupado,
        int pagina,
        int tamano,
        bool todo,
        CancellationToken cancellationToken);

    Task<ResultadoFactura<byte[]>> ExcelItemsAsync(
        DateOnly? desde,
        DateOnly? hasta,
        int? proveedorId,
        int? construccionId,
        bool agrupado,
        CancellationToken cancellationToken);

    Task<ResultadoFactura<byte[]>> PdfItemsAsync(
        DateOnly? desde,
        DateOnly? hasta,
        int? proveedorId,
        int? construccionId,
        bool agrupado,
        CancellationToken cancellationToken);
}
