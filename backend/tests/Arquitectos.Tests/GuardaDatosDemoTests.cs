using Arquitectos.Infrastructure.Configuration;
using Microsoft.Data.SqlClient;

namespace Arquitectos.Tests;

public sealed class GuardaDatosDemoTests
{
    private const string Clave = "clave-que-no-debe-salir";

    [Fact]
    public void Reset_contra_ARQUITECTOS_se_rechaza_sin_abrir_la_base()
    {
        var resultado = GuardaDatosDemo.Evaluar(
            esDesarrollo: true,
            cadena: Cadena("ARQUITECTOS"),
            sufijos: null,
            reinicio: true,
            confirmar: true);

        Assert.False(resultado.Permitido);
        Assert.NotEqual(0, resultado.CodigoSalida);
        Assert.Equal("ARQUITECTOS", resultado.BaseDatos);
        Assert.Equal("servidor-prueba", resultado.Servidor);
        Assert.Contains("ARQUITECTOS", resultado.Mensaje, StringComparison.Ordinal);
        Assert.DoesNotContain(Clave, resultado.Mensaje, StringComparison.Ordinal);
        Assert.DoesNotContain("usuario-secreto", resultado.Mensaje, StringComparison.Ordinal);
    }

    [Fact]
    public void Reset_contra_ARQUITECTOS_DEV_procede_solo_con_confirmar()
    {
        var sinConfirmar = GuardaDatosDemo.Evaluar(true, Cadena("ARQUITECTOS_DEV"), null, reinicio: true, confirmar: false);
        Assert.False(sinConfirmar.Permitido);
        Assert.NotEqual(0, sinConfirmar.CodigoSalida);
        Assert.Contains("--confirmar", sinConfirmar.Mensaje, StringComparison.Ordinal);
        Assert.DoesNotContain(Clave, sinConfirmar.Mensaje, StringComparison.Ordinal);

        var conConfirmar = GuardaDatosDemo.Evaluar(true, Cadena("ARQUITECTOS_DEV"), null, reinicio: true, confirmar: true);
        Assert.True(conConfirmar.Permitido);
        Assert.Equal(0, conConfirmar.CodigoSalida);
        Assert.Equal("ARQUITECTOS_DEV", conConfirmar.BaseDatos);
        Assert.Equal("servidor-prueba", conConfirmar.Servidor);
        Assert.DoesNotContain(Clave, conConfirmar.Mensaje, StringComparison.Ordinal);
        Assert.DoesNotContain("usuario-secreto", conConfirmar.Mensaje, StringComparison.Ordinal);
    }

    [Fact]
    public void La_semilla_en_ARQUITECTOS_E2E_no_exige_confirmar_y_fuera_de_development_se_rechaza()
    {
        var semilla = GuardaDatosDemo.Evaluar(true, Cadena("ARQUITECTOS_E2E"), null, reinicio: false, confirmar: false);
        Assert.Equal(0, semilla.CodigoSalida);
        Assert.Equal("ARQUITECTOS_E2E", semilla.BaseDatos);

        var produccion = GuardaDatosDemo.Evaluar(false, Cadena("ARQUITECTOS_DEV"), null, reinicio: false, confirmar: true);
        Assert.NotEqual(0, produccion.CodigoSalida);
        Assert.Contains("Development", produccion.Mensaje, StringComparison.Ordinal);
        Assert.DoesNotContain(Clave, produccion.Mensaje, StringComparison.Ordinal);
    }

    [Fact]
    public void Los_sufijos_salen_de_la_configuracion()
    {
        var resultado = GuardaDatosDemo.Evaluar(
            true,
            Cadena("OBRA_LOCAL"),
            ["_LOCAL"],
            reinicio: false,
            confirmar: false);

        Assert.Equal(0, resultado.CodigoSalida);

        var rechazada = GuardaDatosDemo.Evaluar(true, Cadena("ARQUITECTOS_DEV"), ["_LOCAL"], reinicio: false, confirmar: false);
        Assert.NotEqual(0, rechazada.CodigoSalida);
        Assert.Contains("_LOCAL", rechazada.Mensaje, StringComparison.Ordinal);
    }

    private static string Cadena(string catalogo) =>
        new SqlConnectionStringBuilder
        {
            DataSource = "servidor-prueba",
            InitialCatalog = catalogo,
            UserID = "usuario-secreto",
            Password = Clave,
            Encrypt = true
        }.ConnectionString;
}
