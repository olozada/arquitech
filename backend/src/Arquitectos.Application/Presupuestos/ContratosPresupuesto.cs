namespace Arquitectos.Application.Presupuestos;

public enum EstadoOperacion
{
    Ok,
    NoEncontrado,
    Prohibido,
    Validacion,
    Conflicto
}

public sealed record ResultadoOperacion<T>(EstadoOperacion Estado, T? Valor, string? Mensaje)
{
    public static ResultadoOperacion<T> Ok(T valor) => new(EstadoOperacion.Ok, valor, null);

    public static ResultadoOperacion<T> NoEncontrado(string mensaje) => new(EstadoOperacion.NoEncontrado, default, mensaje);

    public static ResultadoOperacion<T> Prohibido(string mensaje) => new(EstadoOperacion.Prohibido, default, mensaje);

    public static ResultadoOperacion<T> Validacion(string mensaje) => new(EstadoOperacion.Validacion, default, mensaje);

    public static ResultadoOperacion<T> Conflicto(string mensaje) => new(EstadoOperacion.Conflicto, default, mensaje);

    public static ResultadoOperacion<T> MismoEstado<TOtro>(ResultadoOperacion<TOtro> origen) =>
        origen.Estado switch
        {
            EstadoOperacion.NoEncontrado => NoEncontrado(origen.Mensaje ?? "No encontrado."),
            EstadoOperacion.Prohibido => Prohibido(origen.Mensaje ?? "No autorizado."),
            EstadoOperacion.Conflicto => Conflicto(origen.Mensaje ?? "El presupuesto fue modificado por otro usuario."),
            EstadoOperacion.Ok => throw new InvalidOperationException("No se traslada un resultado exitoso."),
            _ => Validacion(origen.Mensaje ?? "Solicitud inválida.")
        };
}

public sealed record InsumoDto(
    int Id,
    string? Codigo,
    string Descripcion,
    string Tipo,
    string UnidadCompra,
    decimal PrecioBase,
    decimal PorcentajeIva,
    decimal CantidadPorUnidad,
    string UnidadUso,
    decimal PrecioUnitario,
    DateTime FechaPrecio,
    bool Activo);

public sealed class InsumoSolicitud
{
    public string? Codigo { get; set; }

    public string Descripcion { get; set; } = "";

    public string Tipo { get; set; } = "Material";

    public string UnidadCompra { get; set; } = "";

    public decimal PrecioBase { get; set; }

    public decimal PorcentajeIva { get; set; }

    public decimal CantidadPorUnidad { get; set; } = 1;

    public string UnidadUso { get; set; } = "";

    public bool Activo { get; set; } = true;
}

public sealed record HistorialPrecioDto(int Id, decimal PrecioBase, decimal PorcentajeIva, DateTime Fecha, int? UsuarioId, string? Usuario);

public sealed record OfertaProveedorDto(decimal Precio, string Proveedor, int InsumoId);

public sealed record ParametrosManoObraDto(int DiasMes, bool IncluirDiasNoHabiles, int DiasHabilesAnio, decimal FactorPrestacional);

public sealed class ParametrosManoObraSolicitud
{
    public int DiasMes { get; set; } = 30;

    public bool IncluirDiasNoHabiles { get; set; }

    public int DiasHabilesAnio { get; set; } = 296;
}

public sealed record PrestacionDto(int Id, string Concepto, decimal Porcentaje, int Orden);

public sealed class PrestacionSolicitud
{
    public string Concepto { get; set; } = "";

    public decimal Porcentaje { get; set; }

    public int Orden { get; set; }
}

public sealed record CargoDto(int Id, string Nombre, decimal SalarioMensual, decimal Jornal, bool Activo);

public sealed class CargoSolicitud
{
    public string Nombre { get; set; } = "";

    public decimal SalarioMensual { get; set; }

    public bool Activo { get; set; } = true;
}

public sealed record IntegranteDto(int CargoId, string Cargo, decimal Cantidad, decimal Jornal);

public sealed record CuadrillaDto(
    int Id,
    string Nombre,
    string? Descripcion,
    bool Activo,
    decimal CostoDia,
    IReadOnlyList<IntegranteDto> Integrantes);

public sealed class IntegranteSolicitud
{
    public int CargoId { get; set; }

    public decimal Cantidad { get; set; }
}

public sealed class CuadrillaSolicitud
{
    public string Nombre { get; set; } = "";

    public string? Descripcion { get; set; }

    public bool Activo { get; set; } = true;

    public List<IntegranteSolicitud> Integrantes { get; set; } = [];
}

public sealed record ApuResumenDto(int Id, string? Codigo, string Descripcion, string Unidad, bool EsSubanalisis, decimal CostoTotal, bool Activo, int Usos = 0);

public sealed record LineaApuDto(
    int Id,
    int Orden,
    int? InsumoId,
    int? CuadrillaId,
    int? SubApuId,
    string Descripcion,
    string Unidad,
    decimal Cantidad,
    decimal? PrecioManual,
    string Modo,
    decimal Factor,
    decimal Precio,
    decimal Valor);

public sealed record GrupoApuDto(int Id, string Grupo, decimal Divisor, decimal Subtotal, IReadOnlyList<LineaApuDto> Lineas);

public sealed record ApuDetalleDto(
    int Id,
    string? Codigo,
    string Descripcion,
    string Unidad,
    int? UnidadMedidaId,
    bool EsSubanalisis,
    decimal DivisorGeneral,
    int? ConceptoId,
    bool Activo,
    decimal CostoTotal,
    IReadOnlyDictionary<string, decimal> Costos,
    IReadOnlyList<GrupoApuDto> Grupos);

public sealed class LineaApuSolicitud
{
    public int? InsumoId { get; set; }

    public int? CuadrillaId { get; set; }

    public int? SubApuId { get; set; }

    public string Descripcion { get; set; } = "";

    public string Unidad { get; set; } = "";

    public decimal Cantidad { get; set; } = 1;

    public decimal? PrecioManual { get; set; }

    public string Modo { get; set; } = "Desperdicio";

    public decimal Factor { get; set; }
}

public sealed class GrupoApuSolicitud
{
    public string Grupo { get; set; } = "Material";

    public decimal Divisor { get; set; } = 1;

    public List<LineaApuSolicitud> Lineas { get; set; } = [];
}

public sealed class ApuSolicitud
{
    public string? Codigo { get; set; }

    public string Descripcion { get; set; } = "";

    public string Unidad { get; set; } = "";

    public int? UnidadMedidaId { get; set; }

    public bool EsSubanalisis { get; set; }

    public decimal DivisorGeneral { get; set; } = 1;

    public int? ConceptoId { get; set; }

    public bool Activo { get; set; } = true;

    public List<GrupoApuSolicitud> Grupos { get; set; } = [];
}

public sealed record UsoApuDto(string Tipo, int Id, string Nombre, bool Aprobado = false);

public sealed record PresupuestoResumenDto(
    int Id,
    int ConstruccionId,
    string Construccion,
    string Nombre,
    int Version,
    string Estado,
    decimal Total,
    decimal? ValorPorM2,
    decimal AvanceFinanciero);

public sealed record MedicionDto(int Id, string Ubicacion, decimal Veces, decimal? Largo, decimal? Ancho, decimal? Alto, decimal Parcial);

public sealed record ItemPresupuestoDto(
    int Id,
    string Codigo,
    string Descripcion,
    string Unidad,
    int? UnidadMedidaId,
    int? ApuId,
    string? Apu,
    string? UnidadApu,
    decimal FactorConversionApu,
    bool AvisoUnidad,
    int? ConceptoId,
    decimal Cantidad,
    int? CantidadOrigenItemId,
    decimal CantidadOrigenFactor,
    decimal ValorMaterialUnitario,
    decimal ValorManoObraUnitario,
    decimal ValorUnitario,
    decimal Total,
    bool DesdeApu,
    IReadOnlyList<MedicionDto> Mediciones);

public sealed record CapituloPresupuestoDto(
    int Id,
    int Numero,
    string Nombre,
    decimal Total,
    decimal Incidencia,
    IReadOnlyList<ItemPresupuestoDto> Items);

public sealed record CostoPresupuestoDto(
    decimal Subtotal,
    decimal TotalMaterial,
    decimal TotalManoObra,
    decimal Imprevistos,
    decimal Incremento,
    decimal Administracion,
    decimal Utilidad,
    decimal IvaUtilidad,
    decimal Total,
    decimal? ValorPorM2);

public sealed record PresupuestoDetalleDto(
    int Id,
    int ConstruccionId,
    string Construccion,
    string Nombre,
    int Version,
    string Estado,
    DateOnly Fecha,
    int MesesProyecto,
    decimal AreaConstruidaM2,
    decimal PctImprevistos,
    decimal PctIncremento,
    decimal PctAdministracion,
    decimal PctUtilidad,
    decimal PctIvaUtilidad,
    DateTime? FechaAprobacion,
    CostoPresupuestoDto Resumen,
    IReadOnlyList<CapituloPresupuestoDto> Capitulos,
    string RowVersion,
    bool EsPlantilla);

public sealed class MedicionSolicitud
{
    public string Ubicacion { get; set; } = "";

    public decimal Veces { get; set; } = 1;

    public decimal? Largo { get; set; }

    public decimal? Ancho { get; set; }

    public decimal? Alto { get; set; }
}

public sealed class ItemPresupuestoSolicitud
{
    public int? Id { get; set; }

    public string Codigo { get; set; } = "";

    public string Descripcion { get; set; } = "";

    public string Unidad { get; set; } = "";

    public int? UnidadMedidaId { get; set; }

    public int? ApuId { get; set; }

    public decimal FactorConversionApu { get; set; } = 1;

    public int? ConceptoId { get; set; }

    public decimal Cantidad { get; set; }

    public int? CantidadOrigenItemId { get; set; }

    public decimal CantidadOrigenFactor { get; set; } = 1;

    public decimal ValorMaterialUnitario { get; set; }

    public decimal ValorManoObraUnitario { get; set; }

    public List<MedicionSolicitud> Mediciones { get; set; } = [];
}

public sealed class CapituloPresupuestoSolicitud
{
    public int? Id { get; set; }

    public int Numero { get; set; }

    public string Nombre { get; set; } = "";

    public List<ItemPresupuestoSolicitud> Items { get; set; } = [];
}

public sealed class PresupuestoSolicitud
{
    public int ConstruccionId { get; set; }

    public string Nombre { get; set; } = "";

    public DateOnly Fecha { get; set; }

    public int MesesProyecto { get; set; }

    public decimal AreaConstruidaM2 { get; set; }

    public decimal PctImprevistos { get; set; } = 0.05m;

    public decimal PctIncremento { get; set; } = 0.07m;

    public decimal PctAdministracion { get; set; }

    public decimal PctUtilidad { get; set; }

    public decimal PctIvaUtilidad { get; set; }

    public List<CapituloPresupuestoSolicitud> Capitulos { get; set; } = [];

    public string? RowVersion { get; set; }
}

public sealed record DiferenciaItemDto(
    string Codigo,
    string Descripcion,
    decimal MaterialActual,
    decimal MaterialNuevo,
    decimal ManoObraActual,
    decimal ManoObraNueva,
    decimal Diferencia);

public sealed record RecalculoDto(CostoPresupuestoDto Actual, CostoPresupuestoDto Propuesto, IReadOnlyList<DiferenciaItemDto> Diferencias, bool Aplicado);

public sealed record SeguimientoFilaDto(
    int? ItemId,
    int? CapituloId,
    string Capitulo,
    string Codigo,
    string Descripcion,
    string Unidad,
    decimal CantidadPresupuestada,
    decimal CantidadEjecutada,
    decimal AvanceFisico,
    decimal ValorPresupuestado,
    decimal ValorEjecutado,
    decimal AvanceFinanciero,
    decimal Saldo,
    decimal PrecioPresupuestado,
    decimal PrecioActas,
    decimal ProyeccionCierre);

public sealed record PuntoEjecucionDto(int Anio, int Mes, decimal Ejecutado, decimal Acumulado);

public sealed record SeguimientoDto(
    int PresupuestoId,
    string Construccion,
    CostoPresupuestoDto Resumen,
    decimal Ejecutado,
    decimal Proyeccion,
    decimal NoPresupuestado,
    IReadOnlyList<SeguimientoFilaDto> Capitulos,
    IReadOnlyList<SeguimientoFilaDto> Items,
    IReadOnlyList<SeguimientoFilaDto> NoPresupuestados,
    IReadOnlyList<PuntoEjecucionDto> AcumuladoMensual);

public sealed record ItemActaDto(
    int Id,
    string Codigo,
    string Descripcion,
    string Unidad,
    int? ConceptoId,
    int? UnidadMedidaId,
    decimal? Precio);

public sealed record InformeImportacionDto(
    int PresupuestoId,
    int ApusRevisados,
    int ApusReproducidos,
    int UnidadesDistintas,
    int CodigosDuplicados,
    int ItemsSinApu,
    decimal Subtotal,
    decimal TotalMaterial,
    decimal TotalManoObra,
    decimal Imprevistos,
    decimal Incremento,
    decimal Total,
    decimal ValorPorM2,
    IReadOnlyList<string> UnidadesQueNoCoinciden,
    IReadOnlyList<string> ApusDistintos);

public interface IInsumoServicio
{
    Task<PaginaDto<InsumoDto>> ListarAsync(string? tipo, bool incluirInactivos, CancellationToken cancellationToken, int pagina = 1, int tamano = Paginacion.TamanoDefecto, string? texto = null);

    Task<InsumoDto?> ObtenerAsync(int id, CancellationToken cancellationToken);

    Task<ResultadoOperacion<InsumoDto>> GuardarAsync(int? id, InsumoSolicitud solicitud, int? usuarioId, CancellationToken cancellationToken);

    Task<ResultadoOperacion<bool>> EliminarAsync(int id, bool confirmar, CancellationToken cancellationToken);

    Task<IReadOnlyList<HistorialPrecioDto>> HistorialAsync(int id, CancellationToken cancellationToken);

    Task<byte[]> ExcelAsync(string? tipo, CancellationToken cancellationToken);

    Task<OfertaProveedorDto?> OfertaProveedorAsync(int id, CancellationToken cancellationToken);

    Task<ResultadoOperacion<InsumoDto>> TomarPrecioProveedorAsync(int id, int? usuarioId, CancellationToken cancellationToken);
}

public interface IManoObraServicio
{
    Task<ParametrosManoObraDto> ObtenerParametrosAsync(CancellationToken cancellationToken);

    Task<ParametrosManoObraDto> GuardarParametrosAsync(ParametrosManoObraSolicitud solicitud, CancellationToken cancellationToken);

    Task<IReadOnlyList<PrestacionDto>> ListarPrestacionesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<PrestacionDto>> GuardarPrestacionesAsync(IReadOnlyList<PrestacionSolicitud> prestaciones, CancellationToken cancellationToken);

    Task<IReadOnlyList<CargoDto>> ListarCargosAsync(bool incluirInactivos, CancellationToken cancellationToken);

    Task<ResultadoOperacion<CargoDto>> GuardarCargoAsync(int? id, CargoSolicitud solicitud, CancellationToken cancellationToken);

    Task<ResultadoOperacion<bool>> EliminarCargoAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<CuadrillaDto>> ListarCuadrillasAsync(bool incluirInactivos, CancellationToken cancellationToken);

    Task<CuadrillaDto?> ObtenerCuadrillaAsync(int id, CancellationToken cancellationToken);

    Task<ResultadoOperacion<CuadrillaDto>> GuardarCuadrillaAsync(int? id, CuadrillaSolicitud solicitud, CancellationToken cancellationToken);

    Task<ResultadoOperacion<bool>> EliminarCuadrillaAsync(int id, CancellationToken cancellationToken);
}

public sealed record PresupuestoCreadoDto(int Id);

public sealed record PlantillaResumenDto(int Id, string Nombre, int Capitulos, int Items, DateOnly Actualizado);

public sealed record ValidacionPresupuestoDto(int? ItemId, string Codigo, string Tipo, string Mensaje);

public sealed class CrearPresupuestoSolicitud
{
    public int? ConstruccionId { get; set; }

    public string Nombre { get; set; } = "";

    public DateOnly Fecha { get; set; }

    public int MesesProyecto { get; set; }

    public decimal? AreaConstruidaM2 { get; set; }

    public decimal? PctImprevistos { get; set; }

    public decimal? PctIncremento { get; set; }

    public decimal? PctAdministracion { get; set; }

    public decimal? PctUtilidad { get; set; }

    public decimal? PctIvaUtilidad { get; set; }

    public string Origen { get; set; } = "vacio";

    public int? OrigenPresupuestoId { get; set; }

    public bool ConservarCantidades { get; set; }
}

public sealed class NombrePlantillaSolicitud
{
    public string Nombre { get; set; } = "";
}

public sealed class ItemsDesdeApuSolicitud
{
    public List<int> ApuIds { get; set; } = [];

    public string? RowVersion { get; set; }
}

public sealed class OrdenItemSolicitud
{
    public int Id { get; set; }

    public int Orden { get; set; }

    public List<int> Items { get; set; } = [];
}

public sealed class OrdenPresupuestoSolicitud
{
    public List<OrdenItemSolicitud> Capitulos { get; set; } = [];

    public string? RowVersion { get; set; }
}

public sealed class MoverItemSolicitud
{
    public int CapituloDestinoId { get; set; }

    public string? RowVersion { get; set; }
}

public sealed class VersionSolicitud
{
    public string? RowVersion { get; set; }
}

public interface IApuServicio
{
    Task<PaginaDto<ApuResumenDto>> ListarAsync(string? texto, bool incluirInactivos, CancellationToken cancellationToken, int pagina = 1, int tamano = Paginacion.TamanoDefecto, bool? esSubanalisis = null);

    Task<IReadOnlyList<ApuResumenDto>> BuscarAsync(string? texto, int? unidadId, int top, CancellationToken cancellationToken);

    Task<ApuDetalleDto?> ObtenerAsync(int id, CancellationToken cancellationToken);

    Task<ResultadoOperacion<ApuDetalleDto>> GuardarAsync(int? id, ApuSolicitud solicitud, CancellationToken cancellationToken);

    Task<ResultadoOperacion<bool>> EliminarAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<UsoApuDto>> UsosAsync(int id, CancellationToken cancellationToken);

    Task<ResultadoOperacion<ApuDetalleDto>> DuplicarAsync(int id, CancellationToken cancellationToken);
}

public interface IPresupuestoServicio
{
    Task<ResultadoOperacion<IReadOnlyList<PresupuestoResumenDto>>> ListarAsync(int? construccionId, int usuarioId, CancellationToken cancellationToken, string? estado = null, bool plantillas = false);

    Task<ResultadoOperacion<PresupuestoDetalleDto>> ObtenerAsync(int id, int usuarioId, CancellationToken cancellationToken);

    Task<ResultadoOperacion<PresupuestoCreadoDto>> CrearDesdeAsync(CrearPresupuestoSolicitud solicitud, int usuarioId, CancellationToken cancellationToken);

    Task<ResultadoOperacion<PresupuestoDetalleDto>> CrearAsync(PresupuestoSolicitud solicitud, int usuarioId, CancellationToken cancellationToken);

    Task<ResultadoOperacion<PresupuestoDetalleDto>> ActualizarAsync(int id, PresupuestoSolicitud solicitud, int usuarioId, CancellationToken cancellationToken);

    Task<ResultadoOperacion<bool>> EliminarAsync(int id, int usuarioId, CancellationToken cancellationToken, string? rowVersion = null);

    Task<ResultadoOperacion<IReadOnlyList<PlantillaResumenDto>>> PlantillasAsync(int usuarioId, CancellationToken cancellationToken);

    Task<ResultadoOperacion<PresupuestoCreadoDto>> GuardarComoPlantillaAsync(int id, string nombre, int usuarioId, CancellationToken cancellationToken);

    Task<ResultadoOperacion<PresupuestoDetalleDto>> AgregarDesdeApuAsync(int id, int capituloId, ItemsDesdeApuSolicitud solicitud, int usuarioId, CancellationToken cancellationToken);

    Task<ResultadoOperacion<PresupuestoDetalleDto>> RenumerarAsync(int id, string? rowVersion, int usuarioId, CancellationToken cancellationToken);

    Task<ResultadoOperacion<PresupuestoDetalleDto>> OrdenarAsync(int id, OrdenPresupuestoSolicitud solicitud, int usuarioId, CancellationToken cancellationToken);

    Task<ResultadoOperacion<PresupuestoDetalleDto>> DuplicarItemAsync(int id, int itemId, string? rowVersion, int usuarioId, CancellationToken cancellationToken);

    Task<ResultadoOperacion<PresupuestoDetalleDto>> MoverItemAsync(int id, int itemId, MoverItemSolicitud solicitud, int usuarioId, CancellationToken cancellationToken);

    Task<ResultadoOperacion<IReadOnlyList<ValidacionPresupuestoDto>>> ValidarAsync(int id, int usuarioId, CancellationToken cancellationToken);

    Task<ResultadoOperacion<RecalculoDto>> RecalcularAsync(int id, bool aplicar, int usuarioId, CancellationToken cancellationToken, string? rowVersion = null);

    Task<ResultadoOperacion<PresupuestoDetalleDto>> AprobarAsync(int id, int usuarioId, CancellationToken cancellationToken, bool confirmarCantidadesCero = false);

    Task<ResultadoOperacion<PresupuestoDetalleDto>> NuevaVersionAsync(int id, int usuarioId, CancellationToken cancellationToken);

    Task<ResultadoOperacion<PresupuestoDetalleDto>> CerrarAsync(int id, int usuarioId, CancellationToken cancellationToken);

    Task<ResultadoOperacion<SeguimientoDto>> SeguimientoAsync(int id, int usuarioId, CancellationToken cancellationToken);

    Task<ResultadoOperacion<byte[]>> ExcelAsync(int id, int usuarioId, CancellationToken cancellationToken);

    Task<ResultadoOperacion<byte[]>> PdfAsync(int id, int usuarioId, CancellationToken cancellationToken);

    Task<ResultadoOperacion<InformeImportacionDto>> ImportarAsync(Stream archivo, int construccionId, int usuarioId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ItemActaDto>> ItemsParaActaAsync(int construccionId, bool incluirPrecio, CancellationToken cancellationToken);
}
