namespace Arquitectos.Application.Dashboard;

public sealed record FiltroDashboard
{
    public DateOnly Desde { get; init; }

    public DateOnly Hasta { get; init; }

    public int? ConstruccionId { get; init; }

    public int? ContratistaId { get; init; }

    public int? ArquitectoId { get; init; }

    public int? RevisorId { get; init; }

    /// <summary>Null incluye abiertas y cerradas.</summary>
    public bool? Cerrada { get; init; }

    public (DateOnly Desde, DateOnly Hasta) PeriodoAnterior()
    {
        var dias = Hasta.DayNumber - Desde.DayNumber + 1;
        var hastaAnterior = Desde.AddDays(-1);
        var desdeAnterior = hastaAnterior.AddDays(-(dias - 1));
        return (desdeAnterior, hastaAnterior);
    }

    public FiltroDashboard ConRango(DateOnly desde, DateOnly hasta) =>
        this with { Desde = desde, Hasta = hasta };
}
