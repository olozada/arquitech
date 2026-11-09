using FluentValidation;

namespace Arquitectos.Application.Auth;

public sealed class LoginSolicitudValidador : AbstractValidator<LoginSolicitud>
{
    public LoginSolicitudValidador()
    {
        RuleFor(solicitud => solicitud.Usuario)
            .NotEmpty()
            .WithMessage("Debe ingresar el usuario.");

        RuleFor(solicitud => solicitud.Password)
            .NotEmpty()
            .WithMessage("Debe ingresar la contraseña.");
    }
}
