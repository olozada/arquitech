using System.ComponentModel.DataAnnotations;

namespace Arquitectos.Application.Presupuestos;

public sealed class OpcionesPresupuesto
{
    public const string Seccion = "Presupuesto";

    [Range(0, 1)]
    public decimal PctImprevistos { get; set; } = 0.05m;

    [Range(0, 1)]
    public decimal PctIncremento { get; set; } = 0.07m;

    [Range(0, 1)]
    public decimal PctAdministracion { get; set; }

    [Range(0, 1)]
    public decimal PctUtilidad { get; set; }

    [Range(0, 1)]
    public decimal PctIvaUtilidad { get; set; } = 0.19m;

    [Range(100, 60000)]
    public int AutoguardadoMs { get; set; } = 600;

    [Range(typeof(long), "1", "10000000")]
    public long TamanoMaximoImportacion { get; set; } = 10_000_000;

    [Range(1, 20_000)]
    public int FilasMaximas { get; set; } = 20_000;
}
