using FluentValidation;

namespace Arquitectos.Application.Actas;

public sealed class CrearActaValidador : AbstractValidator<CrearActaSolicitud>
{
    public const string MensajeEncabezado = "Debe seleccionar toda la información.";

    public const string MensajeDetalle = "Debe ingresar toda la información.";

    public const string MensajeFechas = "La fecha final debe ser mayor o igual que la fecha inicial.";

    public CrearActaValidador()
    {
        RuleFor(acta => acta.ArquitectoId).GreaterThan(0).WithMessage(MensajeEncabezado);
        RuleFor(acta => acta.RevisorId).GreaterThan(0).WithMessage(MensajeEncabezado);
        RuleFor(acta => acta.ContratistaId).GreaterThan(0).WithMessage(MensajeEncabezado);

        RuleFor(acta => acta.FechaFinal)
            .GreaterThanOrEqualTo(acta => acta.FechaInicial)
            .WithMessage(MensajeFechas);

        RuleFor(acta => acta.Observacion)
            .MaximumLength(1000)
            .WithMessage("La observación no puede superar 1000 caracteres.");

        RuleFor(acta => acta.Detalles)
            .NotEmpty()
            .WithMessage(MensajeDetalle);

        RuleForEach(acta => acta.Detalles).ChildRules(linea =>
        {
            linea.RuleFor(item => item.ConstruccionId).GreaterThan(0).WithMessage(MensajeDetalle);
            linea.RuleFor(item => item.ConceptoId).GreaterThan(0).WithMessage(MensajeDetalle);
            linea.RuleFor(item => item.UnidadMedidaId).GreaterThan(0).WithMessage(MensajeDetalle);
            linea.RuleFor(item => item.MaterialId)
                .Must(id => id is null or > 0)
                .WithMessage(MensajeDetalle);
            linea.RuleFor(item => item.Cantidad).GreaterThan(0).WithMessage(MensajeDetalle);
            linea.RuleFor(item => item.Precio).GreaterThanOrEqualTo(0).WithMessage(MensajeDetalle);
            linea.RuleFor(item => item.Descripcion)
                .NotEmpty()
                .WithMessage(MensajeDetalle)
                .MaximumLength(500)
                .WithMessage("La descripción no puede superar 500 caracteres.");
        });
    }
}
