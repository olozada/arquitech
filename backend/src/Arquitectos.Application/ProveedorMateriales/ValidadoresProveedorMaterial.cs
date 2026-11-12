using FluentValidation;

namespace Arquitectos.Application.ProveedorMateriales;

public sealed class AgregarMaterialesValidador : AbstractValidator<List<AgregarMaterialSolicitud>>
{
    public const string MensajeSeleccion = "Debe seleccionar materiales.";

    public const string MensajePrecio = "Debe seleccionar valores superiores a cero.";

    public AgregarMaterialesValidador()
    {
        RuleFor(materiales => materiales)
            .NotEmpty()
            .WithMessage(MensajeSeleccion);

        RuleForEach(materiales => materiales).ChildRules(material =>
        {
            material.RuleFor(item => item.InsumoId)
                .GreaterThan(0)
                .WithMessage(MensajeSeleccion);

            material.RuleFor(item => item.Precio)
                .GreaterThan(0)
                .WithMessage(MensajePrecio);
        });
    }
}

public sealed class QuitarMaterialesValidador : AbstractValidator<List<int>>
{
    public QuitarMaterialesValidador()
    {
        RuleFor(materiales => materiales)
            .NotEmpty()
            .WithMessage(AgregarMaterialesValidador.MensajeSeleccion);

        RuleForEach(materiales => materiales)
            .GreaterThan(0)
            .WithMessage(AgregarMaterialesValidador.MensajeSeleccion);
    }
}
