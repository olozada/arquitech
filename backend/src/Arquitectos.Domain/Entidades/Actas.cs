namespace Arquitectos.Domain.Entidades;

public class Acta
{
    public int NumeroActa { get; set; }

    public DateOnly FechaInicial { get; set; }

    public DateOnly FechaFinal { get; set; }

    public int ArquitectoId { get; set; }

    public int RevisorId { get; set; }

    public int ContratistaId { get; set; }

    public string? Observacion { get; set; }

    public bool Cerrada { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public DateTime FechaRegistro { get; set; }

    public int? UsuarioRegistroId { get; set; }

    public Arquitecto Arquitecto { get; set; } = null!;

    public Revisor Revisor { get; set; } = null!;

    public Contratista Contratista { get; set; } = null!;

    public Usuario? UsuarioRegistro { get; set; }

    public ICollection<ActaDetalle> Detalles { get; set; } = new List<ActaDetalle>();
}

public class ActaDetalle
{
    public int Id { get; set; }

    public int NumeroActa { get; set; }

    public int ConstruccionId { get; set; }

    public int ConceptoId { get; set; }

    public int? UnidadMedidaId { get; set; }

    public int? InsumoId { get; set; }

    public decimal Cantidad { get; set; }

    public decimal Precio { get; set; }

    public string Descripcion { get; set; } = "";

    public decimal Subtotal { get; set; }

    public int TipoUsuarioId { get; set; }

    public bool Aprobado { get; set; } = true;

    public int? PresupuestoItemId { get; set; }

    public Acta Acta { get; set; } = null!;

    public TipoUsuario TipoUsuario { get; set; } = null!;

    public Construccion Construccion { get; set; } = null!;

    public Concepto Concepto { get; set; } = null!;

    public UnidadMedida? UnidadMedida { get; set; }

    public Insumo? Insumo { get; set; }

    public PresupuestoItem? PresupuestoItem { get; set; }
}
