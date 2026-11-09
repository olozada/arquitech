using System.ComponentModel.DataAnnotations;

namespace Arquitectos.Application.Dashboard;

public sealed class OpcionesDashboard
{
    public const string Seccion = "Dashboard";

    [Range(0, 500)]
    public decimal UmbralDesvioPrecioPct { get; set; } = 15;

    [Range(1, 3650)]
    public int VentanaReferenciaDias { get; set; } = 180;

    [Range(1, 1000)]
    public int MinObservacionesReferencia { get; set; } = 3;

    [Range(1, 3650)]
    public int DiasSinActividad { get; set; } = 30;

    [Range(1, 3650)]
    public int DiasRegistroTardio { get; set; } = 15;

    [Required]
    public string ZonaHoraria { get; set; } = "America/Bogota";

    [Range(1, 1000)]
    public int MaxAlertas { get; set; } = 100;

    public decimal UmbralFraccion => UmbralDesvioPrecioPct / 100m;
}
