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
}
