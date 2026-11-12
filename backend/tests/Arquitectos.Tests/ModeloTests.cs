using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Arquitectos.Tests;

public class ModeloTests
{
    [Fact]
    public void El_modelo_incluye_las_tablas_del_esquema()
    {
        using var context = CrearContexto();
        var tablas = context.Model.GetEntityTypes()
            .Select(entidad => entidad.GetTableName())
            .ToHashSet();

        string[] esperadas =
        [
            "TiposUsuario",
            "Usuarios",
            "MenuOpciones",
            "MenuOpcionTipoUsuario",
            "Arquitectos",
            "Revisores",
            "Contratistas",
            "Proveedores",
            "Construcciones",
            "UnidadesMedida",
            "Conceptos",
            "ProveedorMateriales",
            "Actas",
            "ActaDetalles",
            "Insumos",
            "InsumoPreciosHistorial",
            "PrestacionesSociales",
            "ParametrosManoObra",
            "Cargos",
            "Cuadrillas",
            "CuadrillaCargos",
            "Apus",
            "ApuGrupos",
            "ApuLineas",
            "Presupuestos",
            "PresupuestoCapitulos",
            "PresupuestoItems",
            "PresupuestoMediciones"
        ];

        foreach (var tabla in esperadas)
        {
            Assert.Contains(tabla, tablas);
        }
    }

    [Fact]
    public void Los_catalogos_tienen_activo_e_indice_unico()
    {
        using var context = CrearContexto();

        Assert.NotNull(Propiedad<Concepto>(context, nameof(Concepto.Activo)));
        Assert.NotNull(Propiedad<Insumo>(context, nameof(Insumo.Activo)));
        Assert.NotNull(Propiedad<UnidadMedida>(context, nameof(UnidadMedida.Activo)));

        Assert.Contains(
            context.Model.FindEntityType(typeof(Arquitecto))!.GetIndexes(),
            indice => indice.IsUnique && indice.Properties.Any(p => p.Name == nameof(Arquitecto.Nombre)));
        Assert.Contains(
            context.Model.FindEntityType(typeof(Concepto))!.GetIndexes(),
            indice => indice.IsUnique && indice.Properties.Any(p => p.Name == nameof(Concepto.Descripcion)));
    }

    [Fact]
    public void El_detalle_del_acta_se_borra_en_cascada_y_el_subtotal_es_calculado()
    {
        using var context = CrearContexto();
        var detalle = context.Model.FindEntityType(typeof(ActaDetalle))!;
        var acta = detalle.GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(Acta));
        var subtotal = detalle.FindProperty(nameof(ActaDetalle.Subtotal))!;

        Assert.Equal(DeleteBehavior.Cascade, acta.DeleteBehavior);
        Assert.Equal("[Cantidad] * [Precio]", subtotal.GetComputedColumnSql());
    }

    [Fact]
    public void La_semilla_crea_el_administrador_y_el_menu()
    {
        using var context = CrearContexto();
        var modelo = context.GetService<IDesignTimeModel>().Model;
        var usuarios = modelo.FindEntityType(typeof(Usuario))!.GetSeedData().ToList();
        var menu = modelo.FindEntityType(typeof(MenuOpcion))!.GetSeedData().ToList();

        Assert.Contains(usuarios, fila => Equals(fila[nameof(Usuario.NombreUsuario)], "admin"));
        // 22 opciones: la 12 se eliminó; 18 a 22 son el módulo de presupuesto y la 23 es el reporte por materiales.
        Assert.Equal(22, menu.Count);
        Assert.DoesNotContain(menu, fila => Equals(fila[nameof(MenuOpcion.Id)], 12));
        Assert.Contains(menu, fila => Equals(fila[nameof(MenuOpcion.Url)], "/actas"));
        Assert.Contains(menu, fila => Equals(fila[nameof(MenuOpcion.Url)], "/"));
        Assert.Contains(menu, fila => Equals(fila[nameof(MenuOpcion.Url)], "/admin/usuarios"));
        Assert.Contains(menu, fila => Equals(fila[nameof(MenuOpcion.Url)], "/cuenta/contrasena"));
        Assert.Contains(menu, fila => Equals(fila[nameof(MenuOpcion.Url)], "/presupuestos"));
        Assert.Contains(menu, fila => Equals(fila[nameof(MenuOpcion.Url)], "/presupuesto/apu"));
    }

    [Fact]
    public void El_snapshot_esta_sincronizado_con_el_modelo()
    {
        using var context = CrearContexto();
        var snapshot = context.GetService<IMigrationsAssembly>().ModelSnapshot!.Model;
        var inicializado = context.GetService<IModelRuntimeInitializer>().Initialize(snapshot);
        var diferencias = context.GetService<IMigrationsModelDiffer>().GetDifferences(
            inicializado.GetRelationalModel(),
            context.GetService<IDesignTimeModel>().Model.GetRelationalModel());

        Assert.Empty(diferencias);
    }

    private static IProperty Propiedad<T>(ArquitectosDbContext context, string nombre) =>
        context.Model.FindEntityType(typeof(T))!.FindProperty(nombre)!;

    private static ArquitectosDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<ArquitectosDbContext>()
            .UseSqlServer("Server=localhost;Database=Arquitectos;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new ArquitectosDbContext(options);
    }
}
