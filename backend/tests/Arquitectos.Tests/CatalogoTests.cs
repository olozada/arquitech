using Arquitectos.Application.Catalogos;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Catalogos;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Tests;

public class CatalogoTests
{
    [Fact]
    public void El_nombre_vacio_no_pasa_la_validacion()
    {
        var resultado = new CatalogoNombreValidador().Validate(new CatalogoNombreSolicitud { Nombre = "   " });

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, error => error.ErrorMessage == "Debe ingresar el nombre.");
    }

    [Fact]
    public void La_unidad_de_medida_rechaza_un_nombre_demasiado_largo()
    {
        var resultado = new UnidadMedidaValidador().Validate(new UnidadMedidaSolicitud
        {
            Nombre = new string('A', 21)
        });

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public void La_construccion_exige_descripcion()
    {
        var resultado = new ConstruccionValidador().Validate(new ConstruccionSolicitud());

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, error => error.PropertyName == nameof(ConstruccionSolicitud.Descripcion));
    }

    [Fact]
    public async Task Eliminar_y_volver_a_crear_reactiva_el_mismo_registro()
    {
        await using var conexion = new SqliteConnection("Data Source=:memory:");
        await conexion.OpenAsync();
        var opciones = new DbContextOptionsBuilder<ArquitectosDbContext>()
            .UseSqlite(conexion)
            .Options;
        await using var context = new ArquitectosDbContext(opciones);
        await context.Database.EnsureCreatedAsync();
        var servicio = new CatalogoNombreServicio<Arquitecto>(context, "un arquitecto");

        var creado = await servicio.CrearAsync(new CatalogoNombreSolicitud { Nombre = "Juan" }, CancellationToken.None);
        var duplicado = await servicio.CrearAsync(new CatalogoNombreSolicitud { Nombre = "Juan" }, CancellationToken.None);
        var eliminado = await servicio.EliminarAsync(creado.Valor!.Id, CancellationToken.None);
        var recreado = await servicio.CrearAsync(new CatalogoNombreSolicitud { Nombre = "Juan" }, CancellationToken.None);
        var listado = await servicio.ListarAsync(incluirInactivos: false, CancellationToken.None);

        Assert.False(creado.Duplicado);
        Assert.True(duplicado.Duplicado);
        Assert.Equal("Ya existe un arquitecto con ese nombre.", duplicado.Mensaje);
        Assert.True(eliminado.Encontrado);
        Assert.False(recreado.Duplicado);
        Assert.Equal(creado.Valor.Id, recreado.Valor!.Id);
        Assert.True(recreado.Valor.Activo);
        Assert.Equal("Juan", Assert.Single(listado).Nombre);
        Assert.Equal(1, await context.Arquitectos.CountAsync());
    }

    [Fact]
    public async Task La_cedula_se_guarda_y_no_se_repite()
    {
        await using var conexion = new SqliteConnection("Data Source=:memory:");
        await conexion.OpenAsync();
        var opciones = new DbContextOptionsBuilder<ArquitectosDbContext>()
            .UseSqlite(conexion)
            .Options;
        await using var context = new ArquitectosDbContext(opciones);
        await context.Database.EnsureCreatedAsync();
        var servicio = new CatalogoPersonaServicio<Arquitecto>(context, "un arquitecto");

        var creado = await servicio.CrearAsync(
            new CatalogoPersonaSolicitud { Nombre = "Ana", Cedula = " 1094123456 " },
            CancellationToken.None);
        var repetida = await servicio.CrearAsync(
            new CatalogoPersonaSolicitud { Nombre = "Otra", Cedula = "1094123456" },
            CancellationToken.None);
        var sinCedula = await servicio.CrearAsync(
            new CatalogoPersonaSolicitud { Nombre = "Luis", Cedula = "  " },
            CancellationToken.None);
        var tambienSinCedula = await servicio.CrearAsync(
            new CatalogoPersonaSolicitud { Nombre = "Marta" },
            CancellationToken.None);

        Assert.Equal("1094123456", creado.Valor!.Cedula);
        Assert.True(repetida.Duplicado);
        Assert.Equal("Ya existe un arquitecto con esa cédula.", repetida.Mensaje);
        Assert.Null(sinCedula.Valor!.Cedula);
        Assert.False(tambienSinCedula.Duplicado);

        var conPuntos = await servicio.CrearAsync(
            new CatalogoPersonaSolicitud { Nombre = "Otra más", Cedula = "1094.123.456" },
            CancellationToken.None);
        await servicio.ActualizarAsync(
            creado.Valor.Id,
            new CatalogoPersonaSolicitud { Nombre = "Ana", Cedula = "1094.123.456" },
            CancellationToken.None);
        var revisores = new CatalogoPersonaServicio<Revisor>(context, "un revisor");
        var enOtroCatalogo = await revisores.CrearAsync(
            new CatalogoPersonaSolicitud { Nombre = "Ing. Ana", Cedula = "1094123456" },
            CancellationToken.None);

        Assert.True(conPuntos.Duplicado);
        Assert.True(enOtroCatalogo.Duplicado);
        Assert.Equal("Ya existe un arquitecto con esa cédula.", enOtroCatalogo.Mensaje);
    }

    [Fact]
    public async Task El_nit_del_proveedor_se_guarda_y_no_se_repite()
    {
        await using var conexion = new SqliteConnection("Data Source=:memory:");
        await conexion.OpenAsync();
        var opciones = new DbContextOptionsBuilder<ArquitectosDbContext>()
            .UseSqlite(conexion)
            .Options;
        await using var context = new ArquitectosDbContext(opciones);
        await context.Database.EnsureCreatedAsync();
        var servicio = new ProveedorServicio(context);

        var creado = await servicio.CrearAsync(
            new ProveedorSolicitud { Nombre = "Ferretería", Nit = " 890123456-1 " },
            CancellationToken.None);
        var repetido = await servicio.CrearAsync(
            new ProveedorSolicitud { Nombre = "Otra", Nit = "890123456-1" },
            CancellationToken.None);

        Assert.Equal("890123456-1", creado.Valor!.Nit);
        Assert.True(repetido.Duplicado);
        Assert.Equal("Ya existe un proveedor con ese NIT.", repetido.Mensaje);

        var conPuntos = await servicio.CrearAsync(
            new ProveedorSolicitud { Nombre = "Tercera", Nit = "890.123.456-1" },
            CancellationToken.None);
        Assert.True(conPuntos.Duplicado);
    }

    [Fact]
    public void La_cedula_larga_no_pasa_la_validacion()
    {
        var resultado = new CatalogoPersonaValidador().Validate(new CatalogoPersonaSolicitud
        {
            Nombre = "Ana",
            Cedula = new string('1', 21)
        });

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, error => error.ErrorMessage == CatalogoPersonaValidador.MensajeCedula);
    }
}
