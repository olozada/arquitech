using FluentValidation;

namespace Arquitectos.Application.Actas;

public sealed class CrearActaValidador : AbstractValidator<CrearActaSolicitud>
{
    public const string MensajeArquitecto = "Debe seleccionar el arquitecto.";

    public const string MensajeRevisor = "Debe seleccionar el revisor.";

    public const string MensajeContratista = "Debe seleccionar el contratista.";

    public const string MensajeConstruccion = "Debe seleccionar la construcción del ítem.";

    public const string MensajeConcepto = "Debe seleccionar el concepto del ítem.";

    public const string MensajeUnidad = "Debe seleccionar la unidad de medida del ítem.";

    public const string MensajeCantidad = "Debe indicar la cantidad del ítem.";

    public const string MensajePrecio = "Debe indicar el precio del ítem.";

    public const string MensajeDescripcion = "Debe indicar la descripción del ítem.";

    public const string MensajeFechas = "La fecha final debe ser mayor o igual que la fecha inicial.";

    public const string MensajeRevisorAgrega = "El revisor no puede agregar ítems.";

    public const string MensajeCerrada = "El acta ya está cerrada. No se pueden agregar, modificar ni eliminar ítems.";

    public CrearActaValidador()
    {
        RuleFor(acta => acta.ArquitectoId).GreaterThan(0).WithMessage(MensajeArquitecto);
        RuleFor(acta => acta.RevisorId).GreaterThan(0).WithMessage(MensajeRevisor);
        RuleFor(acta => acta.ContratistaId).GreaterThan(0).WithMessage(MensajeContratista);

        RuleFor(acta => acta.FechaFinal)
            .GreaterThanOrEqualTo(acta => acta.FechaInicial)
            .WithMessage(MensajeFechas);

        RuleFor(acta => acta.Observacion)
            .MaximumLength(1000)
            .WithMessage("La observación no puede superar 1000 caracteres.");

        RuleForEach(acta => acta.Detalles).ChildRules(linea =>
        {
            linea.RuleFor(item => item.ConstruccionId).GreaterThan(0).WithMessage(MensajeConstruccion);
            linea.RuleFor(item => item.ConceptoId).GreaterThan(0).WithMessage(MensajeConcepto);
            linea.RuleFor(item => item.UnidadMedidaId)
                .Must(id => id is null or > 0)
                .WithMessage(MensajeUnidad);
            linea.RuleFor(item => item.InsumoId)
                .Must(id => id is null or > 0)
                .WithMessage("Debe seleccionar un insumo válido.");
            linea.RuleFor(item => item.Cantidad).GreaterThan(0).WithMessage(MensajeCantidad);
            linea.RuleFor(item => item.Precio).GreaterThanOrEqualTo(0).WithMessage(MensajePrecio);
            linea.RuleFor(item => item.Descripcion)
                .MaximumLength(500)
                .WithMessage("La descripción no puede superar 500 caracteres.");
        });
    }
}
