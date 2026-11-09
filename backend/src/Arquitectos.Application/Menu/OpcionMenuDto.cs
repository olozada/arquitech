namespace Arquitectos.Application.Menu;

public sealed class OpcionMenuDto
{
    public int Id { get; set; }

    public int? PadreId { get; set; }

    public string Descripcion { get; set; } = "";

    public string? Url { get; set; }

    public string? Icono { get; set; }

    public int Orden { get; set; }

    public List<OpcionMenuDto> Hijos { get; set; } = [];
}
