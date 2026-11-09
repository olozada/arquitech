using FluentValidation;

namespace Arquitectos.Application.Catalogos;

public sealed class CatalogoNombreValidador : AbstractValidator<CatalogoNombreSolicitud>
{
    public CatalogoNombreValidador()
    {
        RuleFor(solicitud => solicitud.Nombre)
            .NotEmpty()
            .WithMessage("Debe ingresar el nombre.")
            .MaximumLength(150)
            .WithMessage("El nombre no puede superar 150 caracteres.");
    }
}

public sealed class ConstruccionValidador : AbstractValidator<ConstruccionSolicitud>
{
    public ConstruccionValidador()
    {
        RuleFor(solicitud => solicitud.Descripcion)
            .NotEmpty()
            .WithMessage("Debe ingresar la descripción.")
            .MaximumLength(200)
            .WithMessage("La descripción no puede superar 200 caracteres.");

        RuleFor(solicitud => solicitud.Propietario)
            .MaximumLength(150)
            .WithMessage("El propietario no puede superar 150 caracteres.");
    }
}

public sealed class DescripcionUnidadValidador : AbstractValidator<DescripcionUnidadSolicitud>
{
    public DescripcionUnidadValidador()
    {
        RuleFor(solicitud => solicitud.Descripcion)
            .NotEmpty()
            .WithMessage("Debe ingresar la descripción.")
            .MaximumLength(200)
            .WithMessage("La descripción no puede superar 200 caracteres.");

        RuleFor(solicitud => solicitud.Unidad)
            .MaximumLength(20)
            .WithMessage("La unidad no puede superar 20 caracteres.");
    }
}

public sealed class UnidadMedidaValidador : AbstractValidator<UnidadMedidaSolicitud>
{
    public UnidadMedidaValidador()
    {
        RuleFor(solicitud => solicitud.Nombre)
            .NotEmpty()
            .WithMessage("Debe ingresar el nombre.")
            .MaximumLength(20)
            .WithMessage("El nombre no puede superar 20 caracteres.");

        RuleFor(solicitud => solicitud.Descripcion)
            .MaximumLength(100)
            .WithMessage("La descripción no puede superar 100 caracteres.");
    }
}
