using FluentValidation;

namespace Arquitectos.Application.Usuarios;

public sealed class CrearUsuarioValidador : AbstractValidator<CrearUsuarioSolicitud>
{
    public CrearUsuarioValidador()
    {
        RuleFor(solicitud => solicitud.Usuario)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage(MensajesUsuario.Usuario)
            .MaximumLength(50)
            .WithMessage(MensajesUsuario.UsuarioLargo);

        RuleFor(solicitud => solicitud.Nombre)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage(MensajesUsuario.Nombre)
            .MaximumLength(150)
            .WithMessage(MensajesUsuario.NombreLargo);

        RuleFor(solicitud => solicitud.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage(MensajesUsuario.Password)
            .Must((solicitud, password) => PoliticaContrasena.Validar(password, solicitud.Usuario) is null)
            .WithMessage(solicitud => PoliticaContrasena.Validar(solicitud.Password, solicitud.Usuario)!);

        RuleFor(solicitud => solicitud.TipoUsuarioId)
            .GreaterThan(0)
            .WithMessage(MensajesUsuario.Tipo);
    }
}

public sealed class ActualizarUsuarioValidador : AbstractValidator<ActualizarUsuarioSolicitud>
{
    public ActualizarUsuarioValidador()
    {
        RuleFor(solicitud => solicitud.Usuario)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage(MensajesUsuario.Usuario)
            .MaximumLength(50)
            .WithMessage(MensajesUsuario.UsuarioLargo);

        RuleFor(solicitud => solicitud.Nombre)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage(MensajesUsuario.Nombre)
            .MaximumLength(150)
            .WithMessage(MensajesUsuario.NombreLargo);

        RuleFor(solicitud => solicitud.Password)
            .Must((solicitud, password) => PoliticaContrasena.Validar(password!, solicitud.Usuario) is null)
            .When(solicitud => !string.IsNullOrWhiteSpace(solicitud.Password))
            .WithMessage(solicitud => PoliticaContrasena.Validar(solicitud.Password!, solicitud.Usuario)!);

        RuleFor(solicitud => solicitud.TipoUsuarioId)
            .GreaterThan(0)
            .WithMessage(MensajesUsuario.Tipo);
    }
}

public sealed class CambiarContrasenaValidador : AbstractValidator<CambiarContrasenaSolicitud>
{
    public CambiarContrasenaValidador()
    {
        RuleFor(solicitud => solicitud.ContrasenaActual)
            .NotEmpty()
            .WithMessage(MensajesUsuario.Actual);

        RuleFor(solicitud => solicitud.ContrasenaNueva)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage(MensajesUsuario.Nueva)
            .Must(contrasena => PoliticaContrasena.Validar(contrasena, null) is null)
            .WithMessage(solicitud => PoliticaContrasena.Validar(solicitud.ContrasenaNueva, null)!);
    }
}
