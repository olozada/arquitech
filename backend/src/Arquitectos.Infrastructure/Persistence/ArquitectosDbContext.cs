using Arquitectos.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Arquitectos.Infrastructure.Persistence;

public class ArquitectosDbContext : DbContext
{
    public ArquitectosDbContext(DbContextOptions<ArquitectosDbContext> options)
        : base(options)
    {
    }

    public DbSet<TipoUsuario> TiposUsuario => Set<TipoUsuario>();

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<MenuOpcion> MenuOpciones => Set<MenuOpcion>();

    public DbSet<MenuOpcionTipoUsuario> MenuOpcionTipoUsuario => Set<MenuOpcionTipoUsuario>();

    public DbSet<Arquitecto> Arquitectos => Set<Arquitecto>();

    public DbSet<Revisor> Revisores => Set<Revisor>();

    public DbSet<Contratista> Contratistas => Set<Contratista>();

    public DbSet<Proveedor> Proveedores => Set<Proveedor>();

    public DbSet<Construccion> Construcciones => Set<Construccion>();

    public DbSet<UnidadMedida> UnidadesMedida => Set<UnidadMedida>();

    public DbSet<Concepto> Conceptos => Set<Concepto>();

    public DbSet<Material> Materiales => Set<Material>();

    public DbSet<ProveedorMaterial> ProveedorMateriales => Set<ProveedorMaterial>();

    public DbSet<Acta> Actas => Set<Acta>();

    public DbSet<ActaDetalle> ActaDetalles => Set<ActaDetalle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ArquitectosDbContext).Assembly);
        SemillaInicial.Aplicar(modelBuilder);

        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            foreach (var propiedad in modelBuilder.Model.GetEntityTypes().SelectMany(entidad => entidad.GetProperties()))
            {
                if (propiedad.FindAnnotation(RelationalAnnotationNames.ColumnType)?.Value is "date" or "datetime2")
                {
                    propiedad.SetColumnType(null);
                }
            }
        }
    }
}
