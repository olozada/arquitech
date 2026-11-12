namespace Arquitectos.Domain.Entidades;

public enum TipoInsumo
{
    Material = 1,
    Equipo = 2,
    Herramienta = 3,
    Laboratorio = 4,
    Transporte = 5,
    ManoObraDestajo = 6,
    Otro = 7
}

public enum GrupoApu
{
    Material = 1,
    ManoObra = 2,
    Equipo = 3,
    Transporte = 4,
    Otros = 5
}

public enum ModoLineaApu
{
    Desperdicio = 1,
    Rendimiento = 2,
    Divisor = 3,
    PorcentajeManoObra = 4
}

public enum EstadoPresupuesto
{
    Borrador = 1,
    Aprobado = 2,
    Cerrado = 3
}

public class Insumo : ICatalogo
{
    public int Id { get; set; }

    public string? Codigo { get; set; }

    public required string Descripcion { get; set; }

    public TipoInsumo Tipo { get; set; }

    public string UnidadCompra { get; set; } = "";

    public decimal PrecioBase { get; set; }

    public decimal PorcentajeIva { get; set; }

    public decimal CantidadPorUnidad { get; set; } = 1;

    public string UnidadUso { get; set; } = "";

    public decimal PrecioUnitario { get; set; }

    public DateTime FechaPrecio { get; set; }

    public bool Activo { get; set; } = true;

    public ICollection<InsumoPrecioHistorial> Historial { get; set; } = [];
}

public class InsumoPrecioHistorial
{
    public int Id { get; set; }

    public int InsumoId { get; set; }

    public decimal PrecioBase { get; set; }

    public decimal PorcentajeIva { get; set; }

    public DateTime Fecha { get; set; }

    public int? UsuarioId { get; set; }

    public Insumo Insumo { get; set; } = null!;
}

public class PrestacionSocial
{
    public int Id { get; set; }

    public required string Concepto { get; set; }

    public decimal Porcentaje { get; set; }

    public int Orden { get; set; }
}

public class ParametrosManoObra
{
    public int Id { get; set; }

    public int DiasMes { get; set; } = 30;

    public bool IncluirDiasNoHabiles { get; set; }

    public int DiasHabilesAnio { get; set; } = 296;
}

public class Cargo
{
    public int Id { get; set; }

    public required string Nombre { get; set; }

    public decimal SalarioMensual { get; set; }

    public bool Activo { get; set; } = true;

    public ICollection<CuadrillaCargo> Cuadrillas { get; set; } = [];
}

public class Cuadrilla
{
    public int Id { get; set; }

    public required string Nombre { get; set; }

    public string? Descripcion { get; set; }

    public bool Activo { get; set; } = true;

    public ICollection<CuadrillaCargo> Integrantes { get; set; } = [];
}

public class CuadrillaCargo
{
    public int CuadrillaId { get; set; }

    public int CargoId { get; set; }

    public decimal Cantidad { get; set; }

    public Cuadrilla Cuadrilla { get; set; } = null!;

    public Cargo Cargo { get; set; } = null!;
}

public class Apu
{
    public int Id { get; set; }

    public string? Codigo { get; set; }

    public required string Descripcion { get; set; }

    public int? UnidadMedidaId { get; set; }

    public string Unidad { get; set; } = "";

    public bool EsSubanalisis { get; set; }

    public decimal DivisorGeneral { get; set; } = 1;

    public int? ConceptoId { get; set; }

    public bool Activo { get; set; } = true;

    public UnidadMedida? UnidadMedida { get; set; }

    public Concepto? Concepto { get; set; }

    public ICollection<ApuGrupo> Grupos { get; set; } = [];
}

public class ApuGrupo
{
    public int Id { get; set; }

    public int ApuId { get; set; }

    public GrupoApu Grupo { get; set; }

    public decimal Divisor { get; set; } = 1;

    public int Orden { get; set; }

    public Apu Apu { get; set; } = null!;

    public ICollection<ApuLinea> Lineas { get; set; } = [];
}

public class ApuLinea
{
    public int Id { get; set; }

    public int ApuGrupoId { get; set; }

    public int Orden { get; set; }

    public int? InsumoId { get; set; }

    public int? CuadrillaId { get; set; }

    public int? SubApuId { get; set; }

    public string Descripcion { get; set; } = "";

    public string Unidad { get; set; } = "";

    public decimal Cantidad { get; set; } = 1;

    public decimal? PrecioManual { get; set; }

    public ModoLineaApu Modo { get; set; }

    public decimal Factor { get; set; }

    public ApuGrupo Grupo { get; set; } = null!;

    public Insumo? Insumo { get; set; }

    public Cuadrilla? Cuadrilla { get; set; }

    public Apu? SubApu { get; set; }
}

public class Presupuesto
{
    public int Id { get; set; }

    public int? ConstruccionId { get; set; }

    public bool EsPlantilla { get; set; }

    public int? PresupuestoOrigenId { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public required string Nombre { get; set; }

    public int Version { get; set; } = 1;

    public EstadoPresupuesto Estado { get; set; } = EstadoPresupuesto.Borrador;

    public DateOnly Fecha { get; set; }

    public int MesesProyecto { get; set; }

    public decimal AreaConstruidaM2 { get; set; }

    public decimal PctImprevistos { get; set; } = 0.05m;

    public decimal PctIncremento { get; set; } = 0.07m;

    public decimal PctAdministracion { get; set; }

    public decimal PctUtilidad { get; set; }

    public decimal PctIvaUtilidad { get; set; }

    public DateTime? FechaAprobacion { get; set; }

    public int? UsuarioAprobacionId { get; set; }

    public Construccion? Construccion { get; set; }

    public ICollection<PresupuestoCapitulo> Capitulos { get; set; } = [];
}

public class PresupuestoCapitulo
{
    public int Id { get; set; }

    public int PresupuestoId { get; set; }

    public int Numero { get; set; }

    public required string Nombre { get; set; }

    public int Orden { get; set; }

    public Presupuesto Presupuesto { get; set; } = null!;

    public ICollection<PresupuestoItem> Items { get; set; } = [];
}

public class PresupuestoItem
{
    public int Id { get; set; }

    public int CapituloId { get; set; }

    public required string Codigo { get; set; }

    public required string Descripcion { get; set; }

    public int? UnidadMedidaId { get; set; }

    public string Unidad { get; set; } = "";

    public int? ApuId { get; set; }

    public decimal FactorConversionApu { get; set; } = 1;

    public int? ConceptoId { get; set; }

    public decimal Cantidad { get; set; }

    public int? CantidadOrigenItemId { get; set; }

    public decimal CantidadOrigenFactor { get; set; } = 1;

    public decimal ValorMaterialUnitario { get; set; }

    public decimal ValorManoObraUnitario { get; set; }

    public string? DesgloseApuJson { get; set; }

    public int Orden { get; set; }

    public PresupuestoCapitulo Capitulo { get; set; } = null!;

    public UnidadMedida? UnidadMedida { get; set; }

    public Apu? Apu { get; set; }

    public Concepto? Concepto { get; set; }

    public ICollection<PresupuestoMedicion> Mediciones { get; set; } = [];
}

public class PresupuestoMedicion
{
    public int Id { get; set; }

    public int PresupuestoItemId { get; set; }

    public string Ubicacion { get; set; } = "";

    public decimal Veces { get; set; } = 1;

    public decimal? Largo { get; set; }

    public decimal? Ancho { get; set; }

    public decimal? Alto { get; set; }

    public decimal Parcial { get; set; }

    public PresupuestoItem Item { get; set; } = null!;
}
