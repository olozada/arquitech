namespace Arquitectos.Application.Dashboard;

public sealed record KpiDto(
    string Codigo,
    string Titulo,
    decimal Valor,
    decimal? ValorAnterior,
    decimal? VariacionPct,
    string Formato,
    decimal? Complemento = null);

public sealed record PuntoMensualDto(int Anio, int Mes, decimal Valor, int Actas, decimal? ValorAnioAnterior);

public sealed record PuntoPrecioDto(int Anio, int Mes, decimal Promedio, decimal Minimo, decimal Maximo, int Lineas);

public sealed record RankingDto(
    int Id,
    string Nombre,
    decimal Valor,
    int Actas,
    decimal Participacion,
    decimal ParticipacionAcumulada);

public sealed record AlertaDto(
    string Tipo,
    string Severidad,
    string Titulo,
    string Detalle,
    int? NumeroActa,
    int? ConstruccionId,
    string? Construccion,
    decimal? Impacto);

public sealed record CalidadDatosDto(
    decimal PctMaterialesConPrecio,
    int MaterialesUnProveedor,
    int LineasSinMaterial,
    int ActasSinObservacion);

public sealed record DashboardDto(
    FiltroDashboard Filtro,
    DateOnly DesdeAnterior,
    DateOnly HastaAnterior,
    IReadOnlyList<KpiDto> Kpis,
    IReadOnlyList<PuntoMensualDto> Tendencia,
    IReadOnlyList<RankingDto> TopConstrucciones,
    IReadOnlyList<RankingDto> Contratistas,
    IReadOnlyList<RankingDto> Conceptos,
    IReadOnlyList<RankingDto> Arquitectos,
    IReadOnlyList<RankingDto> Revisores,
    IReadOnlyDictionary<string, int> AlertasPorSeveridad,
    IReadOnlyList<AlertaDto> AlertasTop,
    CalidadDatosDto Calidad);

public sealed record PaginaAlertasDto(IReadOnlyList<AlertaDto> Alertas, int Total, int Pagina, int Tamano);

public sealed record LineaParaAlerta(
    int Id,
    int NumeroActa,
    DateOnly FechaInicial,
    DateOnly FechaFinal,
    int ContratistaId,
    string Contratista,
    int ConstruccionId,
    string Construccion,
    int ConceptoId,
    string Concepto,
    int UnidadMedidaId,
    int? MaterialId,
    string? Material,
    decimal Cantidad,
    decimal Precio,
    decimal Subtotal);

public sealed record PrecioProveedor(int MaterialId, decimal PrecioMinimo, string ProveedorMasBarato);

public sealed record ObraActividad(int Id, string Nombre, DateOnly? UltimaFechaFinal);

public sealed record ActaRegistro(
    int NumeroActa,
    DateOnly FechaFinal,
    DateOnly FechaRegistroLocal,
    string Contratista,
    int? ConstruccionId,
    string? Construccion);
