using FluentValidation;

namespace Arquitectos.Application.Facturas;

public sealed class CrearFacturaValidador : AbstractValidator<CrearFacturaSolicitud>
{
    public const string MensajeNumero = "Debe indicar el número de factura.";

    public const string MensajeNumeroLargo = "El número de factura no puede superar 30 caracteres.";

    public const string MensajeFecha = "Debe indicar la fecha de la factura.";

    public const string MensajeProveedor = "Debe seleccionar el proveedor.";

    public const string MensajeOrden = "La orden de compra no puede superar 50 caracteres.";

    public const string MensajeObservacion = "La observación no puede superar 1000 caracteres.";

    public const string MensajeLineas = "Debe agregar al menos un material.";

    public const string MensajeConstruccion = "Debe seleccionar la construcción del ítem.";

    public const string MensajeInsumo = "Debe seleccionar el insumo del ítem.";

    public const string MensajeUnidad = "Debe seleccionar la unidad de medida del ítem.";

    public const string MensajeCantidad = "Debe indicar la cantidad del ítem.";

    public const string MensajePrecio = "Debe indicar el precio del ítem.";

    public const string MensajeDescripcion = "La descripción no puede superar 500 caracteres.";

    public const string MensajeDuplicada = "Ya existe una factura con ese número para este proveedor.";

    public const string MensajeNoDisponible = "Información factura no disponible.";

    public const string MensajeConflicto = "Otra persona modificó esta factura.";

    public CrearFacturaValidador()
    {
        RuleFor(factura => factura.Numero)
            .NotEmpty()
            .WithMessage(MensajeNumero)
            .MaximumLength(30)
            .WithMessage(MensajeNumeroLargo);
        RuleFor(factura => factura.Fecha)
            .Must(fecha => fecha != default)
            .WithMessage(MensajeFecha);
        RuleFor(factura => factura.ProveedorId).GreaterThan(0).WithMessage(MensajeProveedor);
        RuleFor(factura => factura.OrdenCompra)
            .MaximumLength(50)
            .WithMessage(MensajeOrden);
        RuleFor(factura => factura.Observacion)
            .MaximumLength(1000)
            .WithMessage(MensajeObservacion);
        RuleFor(factura => factura.Detalles)
            .NotEmpty()
            .WithMessage(MensajeLineas);
        RuleForEach(factura => factura.Detalles).ChildRules(linea =>
        {
            linea.RuleFor(item => item.ConstruccionId).GreaterThan(0).WithMessage(MensajeConstruccion);
            linea.RuleFor(item => item.InsumoId).GreaterThan(0).WithMessage(MensajeInsumo);
            linea.RuleFor(item => item.UnidadMedidaId)
                .Must(id => id is null or > 0)
                .WithMessage(MensajeUnidad);
            linea.RuleFor(item => item.Cantidad).GreaterThan(0).WithMessage(MensajeCantidad);
            linea.RuleFor(item => item.Precio).GreaterThanOrEqualTo(0).WithMessage(MensajePrecio);
            linea.RuleFor(item => item.Descripcion)
                .MaximumLength(500)
                .WithMessage(MensajeDescripcion);
        });
    }
}
