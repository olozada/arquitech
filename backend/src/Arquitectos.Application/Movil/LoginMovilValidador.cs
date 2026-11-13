using FluentValidation;

namespace Arquitectos.Application.Movil;

public sealed class LoginMovilValidador : AbstractValidator<LoginMovilSolicitud>
{
    public LoginMovilValidador()
    {
        RuleFor(solicitud => solicitud.Usuario)
            .NotEmpty()
            .WithMessage("Debe ingresar el usuario.");

        RuleFor(solicitud => solicitud.Contrasena)
            .NotEmpty()
            .WithMessage("Debe ingresar la contraseña.");

        RuleFor(solicitud => solicitud.Dispositivo)
            .NotNull()
            .WithMessage("Debe indicar el dispositivo.");

        When(solicitud => solicitud.Dispositivo is not null, () =>
        {
            RuleFor(solicitud => solicitud.Dispositivo!.Id)
                .NotEmpty()
                .WithMessage("Debe indicar el dispositivo.");
            RuleFor(solicitud => solicitud.Dispositivo!.Nombre)
                .NotEmpty()
                .MaximumLength(100)
                .WithMessage("El nombre del dispositivo no puede superar 100 caracteres.");
            RuleFor(solicitud => solicitud.Dispositivo!.Plataforma)
                .NotEmpty()
                .MaximumLength(20)
                .WithMessage("La plataforma del dispositivo no puede superar 20 caracteres.");
            RuleFor(solicitud => solicitud.Dispositivo!.VersionApp)
                .NotEmpty()
                .MaximumLength(20)
                .WithMessage("La versión de la aplicación no puede superar 20 caracteres.");
        });
    }
}

public sealed class RenovarMovilValidador : AbstractValidator<RenovarMovilSolicitud>
{
    public RenovarMovilValidador()
    {
        RuleFor(solicitud => solicitud.RefreshToken)
            .NotEmpty()
            .WithMessage("Debe indicar el token de renovación.");

        RuleFor(solicitud => solicitud.DispositivoId)
            .NotEmpty()
            .WithMessage("Debe indicar el dispositivo.");
    }
}
