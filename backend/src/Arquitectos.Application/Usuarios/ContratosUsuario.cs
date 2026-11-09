namespace Arquitectos.Application.Usuarios;

public sealed record UsuarioDto(
    int Id,
    string Usuario,
    string Nombre,
    int TipoUsuarioId,
    string TipoUsuario,
    bool Activo);

public sealed record TipoUsuarioDto(int Id, string Nombre);

public sealed class CrearUsuarioSolicitud
{
    public string Usuario { get; set; } = "";

    public string Nombre { get; set; } = "";

    public string Password { get; set; } = "";

    public int TipoUsuarioId { get; set; }
}

public sealed class ActualizarUsuarioSolicitud
{
    public string Usuario { get; set; } = "";

    public string Nombre { get; set; } = "";

    public string? Password { get; set; }

    public int TipoUsuarioId { get; set; }

    public bool? Activo { get; set; }
}

public sealed class CambiarContrasenaSolicitud
{
    public string ContrasenaActual { get; set; } = "";

    public string ContrasenaNueva { get; set; } = "";
}

public enum EstadoUsuario
{
    Ok,
    NoEncontrado,
    Conflicto,
    Invalido
}

public sealed class ResultadoUsuario<T>
{
    public EstadoUsuario Estado { get; private init; }

    public string? Mensaje { get; private init; }

    public T? Valor { get; private init; }

    public static ResultadoUsuario<T> Ok(T valor) => new() { Estado = EstadoUsuario.Ok, Valor = valor };

    public static ResultadoUsuario<T> NoEncontrado(string mensaje) =>
        new() { Estado = EstadoUsuario.NoEncontrado, Mensaje = mensaje };

    public static ResultadoUsuario<T> Conflicto(string mensaje) =>
        new() { Estado = EstadoUsuario.Conflicto, Mensaje = mensaje };

    public static ResultadoUsuario<T> Invalido(string mensaje) =>
        new() { Estado = EstadoUsuario.Invalido, Mensaje = mensaje };
}

public static class MensajesUsuario
{
    public const int LongitudMinima = 8;

    public const string Usuario = "Debe ingresar el usuario.";

    public const string UsuarioLargo = "El usuario no puede superar 50 caracteres.";

    public const string Nombre = "Debe ingresar el nombre.";

    public const string NombreLargo = "El nombre no puede superar 150 caracteres.";

    public const string Password = "Debe ingresar la contraseña.";

    public const string PasswordCorta = "La contraseña debe tener al menos 8 caracteres.";

    public const string Tipo = "Debe seleccionar el tipo de usuario.";

    public const string Duplicado = "Ya existe un usuario con ese nombre de usuario.";

    public const string NoEncontrado = "No se encontró el usuario.";

    public const string Propio = "No puede desactivar su propio usuario.";

    public const string Actual = "Debe ingresar la contraseña actual.";

    public const string Nueva = "Debe ingresar la contraseña nueva.";

    public const string ActualIncorrecta = "La contraseña actual no es correcta.";

    public const string Distinta = "La contraseña nueva debe ser distinta de la actual.";
}
