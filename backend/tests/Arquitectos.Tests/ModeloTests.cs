using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

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
            "Materiales",
            "ProveedorMateriales",
            "Actas",
            "ActaDetalles"
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
        Assert.NotNull(Propiedad<Material>(context, nameof(Material.Activo)));
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
        Assert.Equal(17, menu.Count);
        Assert.Contains(menu, fila => Equals(fila[nameof(MenuOpcion.Url)], "/"));
        Assert.Contains(menu, fila => Equals(fila[nameof(MenuOpcion.Url)], "/admin/usuarios"));
        Assert.Contains(menu, fila => Equals(fila[nameof(MenuOpcion.Url)], "/cuenta/contrasena"));
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
