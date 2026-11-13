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

    public int? ArquitectoId { get; set; }

    public int? ContratistaId { get; set; }

    public int? RevisorId { get; set; }

    public bool Activo { get; set; } = true;

    public int IntentosFallidos { get; set; }

    public DateTime? BloqueadoHasta { get; set; }

    public bool DebeCambiarContrasena { get; set; }

    public TipoUsuario TipoUsuario { get; set; } = null!;

    public Arquitecto? Arquitecto { get; set; }

    public Contratista? Contratista { get; set; }

    public Revisor? Revisor { get; set; }

    public ICollection<SesionUsuario> Sesiones { get; set; } = new List<SesionUsuario>();
}

public class SesionUsuario
{
    public long Id { get; set; }

    public int UsuarioId { get; set; }

    public required string HashToken { get; set; }

    public DateTime Expira { get; set; }

    public DateTime CreadoEn { get; set; }

    public DateTime? RevocadoEn { get; set; }

    public string? Ip { get; set; }

    public string? UserAgent { get; set; }

    public Guid? DispositivoId { get; set; }

    public Usuario Usuario { get; set; } = null!;

    public Dispositivo? Dispositivo { get; set; }
}

public class Dispositivo
{
    public Guid Id { get; set; }

    public int UsuarioId { get; set; }

    public required string Nombre { get; set; }

    public required string Plataforma { get; set; }

    public required string VersionApp { get; set; }

    public DateTime RegistradoEn { get; set; }

    public DateTime? UltimaSincronizacion { get; set; }

    public DateTime? RevocadoEn { get; set; }

    public Usuario Usuario { get; set; } = null!;

    public ICollection<SesionUsuario> Sesiones { get; set; } = new List<SesionUsuario>();
}

public class OperacionMovil
{
    public Guid IdOperacion { get; set; }

    public Guid DispositivoId { get; set; }

    public int UsuarioId { get; set; }

    public required string Tipo { get; set; }

    public int NumeroActa { get; set; }

    public required string Estado { get; set; }

    public string? ResultadoJson { get; set; }

    public DateTime RecibidaEn { get; set; }

    public DateTime CreadaEnDispositivo { get; set; }

    public Dispositivo Dispositivo { get; set; } = null!;

    public Usuario Usuario { get; set; } = null!;
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
