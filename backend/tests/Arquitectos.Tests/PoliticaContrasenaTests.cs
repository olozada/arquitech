using Arquitectos.Application.Usuarios;

namespace Arquitectos.Tests;

public class PoliticaContrasenaTests
{
    [Fact]
    public void Rechaza_corta_sin_letras_igual_al_usuario_y_comun()
    {
        Assert.Equal(MensajesUsuario.PasswordCorta, PoliticaContrasena.Validar("Clave123", "ana"));
        Assert.Equal(MensajesUsuario.PasswordComposicion, PoliticaContrasena.Validar("sololetras", "ana"));
        Assert.Equal(MensajesUsuario.PasswordIgualUsuario, PoliticaContrasena.Validar("AnaMaria12", "anamaria12"));
        Assert.Equal(MensajesUsuario.PasswordComun, PoliticaContrasena.Validar("password123", "ana"));
        Assert.Null(PoliticaContrasena.Validar("Clave12345", "ana"));
    }
}
