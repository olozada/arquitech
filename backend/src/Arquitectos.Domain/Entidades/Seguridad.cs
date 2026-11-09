namespace Arquitectos.Domain.Entidades;

public class TipoUsuario
{
    public int Id { get; set; }

    public required string Nombre { get; set; }

    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();

    public ICollection<MenuOpcionTipoUsuario> OpcionesMenu { get; set; } = new List<MenuOpcionTipoUsuario>();
}

public class Usuario
{
    public int Id { get; set; }

    public required string NombreUsuario { get; set; }

    public required string Nombre { get; set; }

    public required string PasswordHash { get; set; }

    public int TipoUsuarioId { get; set; }

    public bool Activo { get; set; } = true;

    public TipoUsuario TipoUsuario { get; set; } = null!;
}

public class MenuOpcion
{
    public int Id { get; set; }

    public int? PadreId { get; set; }

    public required string Descripcion { get; set; }

    public string? Url { get; set; }

    public string? Icono { get; set; }

    public int Orden { get; set; }

    public MenuOpcion? Padre { get; set; }

    public ICollection<MenuOpcion> Hijos { get; set; } = new List<MenuOpcion>();

    public ICollection<MenuOpcionTipoUsuario> TiposUsuario { get; set; } = new List<MenuOpcionTipoUsuario>();
}

public class MenuOpcionTipoUsuario
{
    public int MenuOpcionId { get; set; }

    public int TipoUsuarioId { get; set; }

    public MenuOpcion MenuOpcion { get; set; } = null!;

    public TipoUsuario TipoUsuario { get; set; } = null!;
}
