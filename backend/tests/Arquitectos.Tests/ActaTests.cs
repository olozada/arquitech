using Arquitectos.Application.Actas;

namespace Arquitectos.Tests;

public class ActaTests
{
    private readonly CrearActaValidador _validador = new();

    [Fact]
    public void Rechaza_el_acta_sin_personas_ni_detalle()
    {
        var resultado = _validador.Validate(new CrearActaSolicitud());

        Assert.False(resultado.IsValid);
        Assert.Equal(CrearActaValidador.MensajeEncabezado, resultado.Errors[0].ErrorMessage);
    }

    [Fact]
    public void Rechaza_la_fecha_final_anterior_a_la_inicial()
    {
        var solicitud = SolicitudValida();
        solicitud.FechaFinal = solicitud.FechaInicial.AddDays(-1);

        var resultado = _validador.Validate(solicitud);

        Assert.Contains(resultado.Errors, error => error.ErrorMessage == CrearActaValidador.MensajeFechas);
    }

    [Fact]
    public void Rechaza_el_detalle_vacio()
    {
        var solicitud = SolicitudValida();
        solicitud.Detalles = [];

        var resultado = _validador.Validate(solicitud);

        Assert.Contains(resultado.Errors, error => error.ErrorMessage == CrearActaValidador.MensajeDetalle);
    }

    [Fact]
    public void Acepta_un_acta_con_una_linea_completa()
    {
        var resultado = _validador.Validate(SolicitudValida());

        Assert.True(resultado.IsValid);
    }

    private static CrearActaSolicitud SolicitudValida() => new()
    {
        FechaInicial = new DateOnly(2026, 11, 1),
        FechaFinal = new DateOnly(2026, 11, 9),
        ArquitectoId = 1,
        RevisorId = 1,
        ContratistaId = 1,
        Observacion = "Observación",
        Detalles =
        [
            new LineaActaSolicitud
            {
                ConstruccionId = 1,
                ConceptoId = 1,
                UnidadMedidaId = 1,
                Cantidad = 2,
                Precio = 100,
                Descripcion = "Muro"
            }
        ]
    };
}
