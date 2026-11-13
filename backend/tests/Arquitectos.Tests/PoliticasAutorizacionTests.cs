using System.Reflection;
using System.Security.Claims;
using Arquitectos.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arquitectos.Tests;

public sealed class PoliticasAutorizacionTests
{
    [Fact]
    public async Task Cada_accion_declara_autorizacion_y_las_politicas_existen()
    {
        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AddAuthorization(Politicas.Registrar);
        await using var proveedor = servicios.BuildServiceProvider();
        var politicas = proveedor.GetRequiredService<IAuthorizationPolicyProvider>();
        var autorizacion = proveedor.GetRequiredService<IAuthorizationService>();

        foreach (var nombre in Nombres())
        {
            Assert.NotNull(await politicas.GetPolicyAsync(nombre));
        }

        var administrador = Principal(Perfiles.Administrador);
        var arquitecto = Principal(Perfiles.Arquitecto);
        var contratista = Principal(Perfiles.Contratista);
        var revisor = Principal(Perfiles.Revisor);

        Assert.True((await autorizacion.AuthorizeAsync(administrador, Politicas.SoloAdministrador)).Succeeded);
        Assert.False((await autorizacion.AuthorizeAsync(arquitecto, Politicas.SoloAdministrador)).Succeeded);
        Assert.True((await autorizacion.AuthorizeAsync(arquitecto, Politicas.GestionPresupuesto)).Succeeded);
        Assert.False((await autorizacion.AuthorizeAsync(contratista, Politicas.GestionPresupuesto)).Succeeded);
        Assert.True((await autorizacion.AuthorizeAsync(contratista, Politicas.TomaActas)).Succeeded);
        Assert.False((await autorizacion.AuthorizeAsync(revisor, Politicas.TomaActas)).Succeeded);
        Assert.True((await autorizacion.AuthorizeAsync(revisor, Politicas.RevisionActas)).Succeeded);
        Assert.False((await autorizacion.AuthorizeAsync(arquitecto, Politicas.RevisionActas)).Succeeded);
        Assert.True((await autorizacion.AuthorizeAsync(administrador, Politicas.Tablero)).Succeeded);
        Assert.False((await autorizacion.AuthorizeAsync(contratista, Politicas.Tablero)).Succeeded);

        var acciones = typeof(Program).Assembly.GetTypes()
            .Where(tipo => tipo.IsClass && !tipo.IsAbstract && typeof(ControllerBase).IsAssignableFrom(tipo))
            .SelectMany(tipo => Acciones(tipo).Select(metodo => (Controlador: tipo, Metodo: metodo)))
            .ToList();
        Assert.NotEmpty(acciones);

        foreach (var (controlador, metodo) in acciones)
        {
            var atributos = Autorizacion(controlador, metodo);
            Assert.True(
                atributos.Count > 0,
                $"{controlador.Name}.{metodo.Name} no declara [Authorize] ni [AllowAnonymous].");

            foreach (var autoriza in atributos.OfType<AuthorizeAttribute>())
            {
                Assert.True(string.IsNullOrEmpty(autoriza.Roles), $"{controlador.Name}.{metodo.Name} todavía usa Roles.");
                if (!string.IsNullOrEmpty(autoriza.Policy))
                {
                    Assert.NotNull(await politicas.GetPolicyAsync(autoriza.Policy));
                }
            }
        }
    }

    private static IEnumerable<string> Nombres() =>
        typeof(Politicas)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(campo => campo.FieldType == typeof(string))
            .Select(campo => (string)campo.GetValue(null)!);

    private static IEnumerable<MethodInfo> Acciones(Type tipo)
    {
        for (var actual = tipo;
             actual is not null && actual != typeof(ControllerBase) && actual != typeof(Controller) && actual != typeof(object);
             actual = actual.BaseType)
        {
            foreach (var metodo in actual.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                if (metodo.IsDefined(typeof(NonActionAttribute), inherit: true))
                {
                    continue;
                }

                if (metodo.GetCustomAttributes(true).OfType<IActionHttpMethodProvider>().Any())
                {
                    yield return metodo;
                }
            }
        }
    }

    private static List<Attribute> Autorizacion(Type controlador, MethodInfo metodo)
    {
        var atributos = new List<Attribute>();
        atributos.AddRange(metodo.GetCustomAttributes(true).OfType<Attribute>().Where(EsAutorizacion));
        for (var actual = controlador;
             actual is not null && actual != typeof(ControllerBase) && actual != typeof(object);
             actual = actual.BaseType)
        {
            atributos.AddRange(actual.GetCustomAttributes(inherit: false).OfType<Attribute>().Where(EsAutorizacion));
        }

        return atributos;
    }

    private static bool EsAutorizacion(Attribute atributo) =>
        atributo is AuthorizeAttribute or AllowAnonymousAttribute;

    private static ClaimsPrincipal Principal(string perfil) =>
        new(new ClaimsIdentity(
            [new Claim(ClaimsArquitectos.TipoUsuario, perfil)],
            authenticationType: "prueba",
            nameType: ClaimsArquitectos.Nombre,
            roleType: ClaimsArquitectos.TipoUsuario));
}
