using FluentValidation;

namespace Arquitectos.Application.Dashboard;

public sealed class FiltroDashboardValidador : AbstractValidator<FiltroDashboard>
{
    public const string MensajeFechas = "La fecha final debe ser mayor o igual que la fecha inicial.";

    public const string MensajeRango = "El rango no puede superar 36 meses.";

    public FiltroDashboardValidador()
    {
        RuleFor(filtro => filtro.Desde)
            .Must(fecha => fecha.Year >= 2000)
            .WithMessage("Debe ingresar la fecha inicial.");

        RuleFor(filtro => filtro.Hasta)
            .Must(fecha => fecha.Year >= 2000)
            .WithMessage("Debe ingresar la fecha final.");

        RuleFor(filtro => filtro.Hasta)
            .GreaterThanOrEqualTo(filtro => filtro.Desde)
            .WithMessage(MensajeFechas);

        RuleFor(filtro => filtro)
            .Must(filtro => filtro.Hasta <= filtro.Desde.AddMonths(36))
            .WithMessage(MensajeRango);
    }
}
