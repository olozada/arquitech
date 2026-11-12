namespace Arquitectos.Domain.Entidades;

public interface ICatalogo
{
    int Id { get; set; }

    bool Activo { get; set; }
}

public interface IConNombre : ICatalogo
{
    string Nombre { get; set; }
}

public interface IConCedula : IConNombre
{
    string? Cedula { get; set; }
}

public interface IConDescripcionPropietario : ICatalogo
{
    string Descripcion { get; set; }

    string? Propietario { get; set; }
}

public interface IConNombreDescripcion : ICatalogo
{
    string Nombre { get; set; }

    string? Descripcion { get; set; }
}

public interface IConDescripcionUnidad : ICatalogo
{
    string Descripcion { get; set; }

    string? Unidad { get; set; }
}

public class Arquitecto : IConCedula
{
    public int Id { get; set; }

    public required string Nombre { get; set; }

    public string? Cedula { get; set; }

    public bool Activo { get; set; } = true;
}

public class Revisor : IConCedula
{
    public int Id { get; set; }

    public required string Nombre { get; set; }

    public string? Cedula { get; set; }

    public bool Activo { get; set; } = true;
}

public class Contratista : IConCedula
{
    public int Id { get; set; }

    public required string Nombre { get; set; }

    public string? Cedula { get; set; }

    public bool Activo { get; set; } = true;
}

public class Proveedor : IConNombre
{
    public int Id { get; set; }

    public required string Nombre { get; set; }

    public string? Nit { get; set; }

    public bool Activo { get; set; } = true;

    public ICollection<ProveedorMaterial> Materiales { get; set; } = new List<ProveedorMaterial>();
}

public class Construccion : IConDescripcionPropietario
{
    public int Id { get; set; }

    public required string Descripcion { get; set; }

    public string? Propietario { get; set; }

    public bool Activo { get; set; } = true;
}

public class UnidadMedida : IConNombreDescripcion
{
    public int Id { get; set; }

    public required string Nombre { get; set; }

    public string? Descripcion { get; set; }

    public bool Activo { get; set; } = true;
}

public class Concepto : IConDescripcionUnidad
{
    public int Id { get; set; }

    public string? Codigo { get; set; }

    public required string Descripcion { get; set; }

    public string? Unidad { get; set; }

    public bool Activo { get; set; } = true;
}

public class ProveedorMaterial
{
    public int ProveedorId { get; set; }

    public int InsumoId { get; set; }

    public decimal Precio { get; set; }

    public Proveedor Proveedor { get; set; } = null!;

    public Insumo Insumo { get; set; } = null!;
}
