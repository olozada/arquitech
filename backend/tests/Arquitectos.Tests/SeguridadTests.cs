using Arquitectos.Application.Auth;
using Arquitectos.Application.Menu;

namespace Arquitectos.Tests;

public class SeguridadTests
{
    private readonly LoginSolicitudValidador _validador = new();

    [Fact]
    public void Login_exige_usuario_y_contrasena()
    {
        var resultado = _validador.Validate(new LoginSolicitud());

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, error => error.PropertyName == nameof(LoginSolicitud.Usuario));
        Assert.Contains(resultado.Errors, error => error.PropertyName == nameof(LoginSolicitud.Password));
    }

    [Fact]
    public void Login_acepta_credenciales_informadas()
    {
        var resultado = _validador.Validate(new LoginSolicitud
        {
            Usuario = "admin",
            Password = "secreto"
        });

        Assert.True(resultado.IsValid);
    }

    [Fact]
    public void El_menu_arma_el_arbol_por_padre()
    {
        var opciones = new List<OpcionMenuDto>
        {
            new() { Id = 1, Descripcion = "Administración", Orden = 1 },
            new() { Id = 2, PadreId = 1, Descripcion = "Arquitectos", Url = "/admin/arquitectos", Orden = 1 },
            new() { Id = 11, Descripcion = "Actas", Orden = 2 },
            new() { Id = 12, PadreId = 11, Descripcion = "Toma de actas", Url = "/actas/nueva", Orden = 1 }
        };

        var arbol = ArbolMenu.Construir(opciones);

        Assert.Equal(2, arbol.Count);
        Assert.Equal("Arquitectos", arbol[0].Hijos[0].Descripcion);
        Assert.Equal("/actas/nueva", arbol[1].Hijos[0].Url);
    }

    [Fact]
    public void Una_opcion_sin_padre_visible_queda_en_la_raiz()
    {
        var arbol = ArbolMenu.Construir(
        [
            new OpcionMenuDto { Id = 4, PadreId = 99, Descripcion = "Suelta" }
        ]);

        Assert.Single(arbol);
        Assert.Equal("Suelta", arbol[0].Descripcion);
    }
}
