namespace Arquitectos.Application.Catalogos;

public interface IConId
{
    int Id { get; }
}

public sealed record CatalogoNombreDto(int Id, string Nombre, bool Activo) : IConId;

public sealed class CatalogoNombreSolicitud
{
    public string Nombre { get; set; } = "";

    public bool? Activo { get; set; }
}

public sealed record ConstruccionDto(int Id, string Descripcion, string? Propietario, bool Activo) : IConId;

public sealed class ConstruccionSolicitud
{
    public string Descripcion { get; set; } = "";

    public string? Propietario { get; set; }

    public bool? Activo { get; set; }
}

public sealed record DescripcionUnidadDto(int Id, string Descripcion, string? Unidad, bool Activo) : IConId;

public sealed class DescripcionUnidadSolicitud
{
    public string Descripcion { get; set; } = "";

    public string? Unidad { get; set; }

    public bool? Activo { get; set; }
}

public sealed record UnidadMedidaDto(int Id, string Nombre, string? Descripcion, bool Activo) : IConId;

public sealed class UnidadMedidaSolicitud
{
    public string Nombre { get; set; } = "";

    public string? Descripcion { get; set; }

    public bool? Activo { get; set; }
}
