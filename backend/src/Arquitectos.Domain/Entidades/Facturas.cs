namespace Arquitectos.Domain.Entidades;

public class Factura
{
    public int Id { get; set; }

    public string Numero { get; set; } = "";

    public DateOnly Fecha { get; set; }

    public int ProveedorId { get; set; }

    public string? OrdenCompra { get; set; }

    public string? Observacion { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public DateTime? ModificadaEn { get; set; }

    public DateTime FechaRegistro { get; set; }

    public int? UsuarioRegistroId { get; set; }

    public Proveedor Proveedor { get; set; } = null!;

    public Usuario? UsuarioRegistro { get; set; }

    public ICollection<FacturaDetalle> Detalles { get; set; } = new List<FacturaDetalle>();
}

public class FacturaDetalle
{
    public int Id { get; set; }

    public int FacturaId { get; set; }

    public int ConstruccionId { get; set; }

    public int InsumoId { get; set; }

    public int? UnidadMedidaId { get; set; }

    public decimal Cantidad { get; set; }

    public decimal Precio { get; set; }

    public string Descripcion { get; set; } = "";

    public decimal Subtotal { get; set; }

    public Factura Factura { get; set; } = null!;

    public Construccion Construccion { get; set; } = null!;

    public Insumo Insumo { get; set; } = null!;

    public UnidadMedida? UnidadMedida { get; set; }
}
