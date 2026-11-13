using FluentValidation;

namespace Arquitectos.Application.Movil;

public sealed class SincronizacionMovilValidador : AbstractValidator<SincronizacionMovilSolicitud>
{
    public static readonly string[] Tipos = ["AgregarLinea", "EditarLinea", "EliminarLinea", "RevisarLinea"];

    public SincronizacionMovilValidador()
    {
        RuleFor(solicitud => solicitud.DispositivoId)
            .NotEmpty()
            .WithMessage("Debe indicar el dispositivo.");
        RuleFor(solicitud => solicitud.VersionEsquema)
            .GreaterThan(0)
            .WithMessage("Debe indicar la versión del esquema.");
        RuleFor(solicitud => solicitud.Operaciones)
            .NotNull()
            .WithMessage("Debe indicar las operaciones.");
        RuleForEach(solicitud => solicitud.Operaciones).ChildRules(operacion =>
        {
            operacion.RuleFor(item => item.IdOperacion)
                .NotEmpty()
                .WithMessage("Debe indicar el identificador de la operación.");
            operacion.RuleFor(item => item.Secuencia)
                .GreaterThan(0)
                .WithMessage("La secuencia debe ser mayor que cero.");
            operacion.RuleFor(item => item.Tipo)
                .Must(tipo => Tipos.Contains(tipo))
                .WithMessage("El tipo de operación no es válido.");
            operacion.RuleFor(item => item.NumeroActa)
                .GreaterThan(0)
                .WithMessage("Debe indicar el acta.");
            operacion.When(item => item.Tipo == "AgregarLinea", () =>
            {
                operacion.RuleFor(item => item.IdCliente)
                    .NotEmpty()
                    .WithMessage("Debe indicar el identificador de la línea.");
                operacion.RuleFor(item => item.Linea)
                    .NotNull()
                    .WithMessage("Debe indicar la línea.");
            });
            operacion.When(item => item.Tipo == "EditarLinea", () =>
            {
                operacion.RuleFor(item => item.Linea)
                    .NotNull()
                    .WithMessage("Debe indicar la línea.");
                operacion.RuleFor(item => item)
                    .Must(item => item.LineaId is > 0 || item.IdCliente is not null)
                    .WithMessage("Debe indicar la línea.");
            });
            operacion.When(item => item.Tipo is "EliminarLinea" or "RevisarLinea", () =>
            {
                operacion.RuleFor(item => item)
                    .Must(item => item.LineaId is > 0 || item.IdCliente is not null)
                    .WithMessage("Debe indicar la línea.");
            });
            operacion.When(item => item.Tipo == "RevisarLinea", () =>
            {
                operacion.RuleFor(item => item.Aprobado)
                    .NotNull()
                    .WithMessage("Debe indicar si la línea queda aprobada.");
            });
        });
    }
}
