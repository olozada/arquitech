using System.ComponentModel.DataAnnotations;

namespace Arquitectos.Application.Movil;

public sealed class OpcionesMovil
{
    public const string Seccion = "Movil";

    [Range(1, 100)]
    public int VersionEsquema { get; set; } = 1;

    [Required]
    public string VersionMinimaApp { get; set; } = "1.0.0";

    [Range(1, 120)]
    public int MesesHistorial { get; set; } = 12;

    [Range(1, 500)]
    public int MaxOperacionesLote { get; set; } = 500;

    [Range(1, 16)]
    public int GeneracionesSimultaneas { get; set; } = 4;

    [Range(1, 200)]
    public int TamanoMaximoMb { get; set; } = 50;

    [Range(1, 3650)]
    public int DiasRetencionOperaciones { get; set; } = 90;
}
