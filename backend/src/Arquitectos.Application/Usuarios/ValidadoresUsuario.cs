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
            .MinimumLength(MensajesUsuario.LongitudMinima)
            .WithMessage(MensajesUsuario.PasswordCorta);

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
            .MinimumLength(MensajesUsuario.LongitudMinima)
            .When(solicitud => !string.IsNullOrWhiteSpace(solicitud.Password))
            .WithMessage(MensajesUsuario.PasswordCorta);

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
            .MinimumLength(MensajesUsuario.LongitudMinima)
            .WithMessage(MensajesUsuario.PasswordCorta);
    }
}
