using Arquitectos.Domain.Entidades;
using RegistroAuditoria = Arquitectos.Domain.Entidades.Auditoria;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Arquitectos.Infrastructure.Persistence;

public class ArquitectosDbContext : DbContext
{
    public ArquitectosDbContext(DbContextOptions<ArquitectosDbContext> options)
        : base(options)
    {
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        VersionesSqlite();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        VersionesSqlite();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void VersionesSqlite()
    {
        ChangeTracker.DetectChanges();
        var tocados = new HashSet<Presupuesto>();
        foreach (var entrada in ChangeTracker.Entries<Presupuesto>())
        {
            if (entrada.State is EntityState.Added or EntityState.Modified)
            {
                tocados.Add(entrada.Entity);
            }
        }

        foreach (var entrada in ChangeTracker.Entries<PresupuestoCapitulo>())
        {
            if (entrada.State is EntityState.Added or EntityState.Modified or EntityState.Deleted && entrada.Entity.Presupuesto is not null)
            {
                tocados.Add(entrada.Entity.Presupuesto);
            }
        }

        foreach (var entrada in ChangeTracker.Entries<PresupuestoItem>())
        {
            if (entrada.State is EntityState.Added or EntityState.Modified or EntityState.Deleted && entrada.Entity.Capitulo?.Presupuesto is not null)
            {
                tocados.Add(entrada.Entity.Capitulo.Presupuesto);
            }
        }

        foreach (var entrada in ChangeTracker.Entries<PresupuestoMedicion>())
        {
            if (entrada.State is EntityState.Added or EntityState.Modified or EntityState.Deleted && entrada.Entity.Item?.Capitulo?.Presupuesto is not null)
            {
                tocados.Add(entrada.Entity.Item.Capitulo.Presupuesto);
            }
        }

        var sqlite = Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite";
        var actas = new HashSet<Acta>();
        foreach (var entrada in ChangeTracker.Entries<Acta>())
        {
            if (entrada.State is EntityState.Added or EntityState.Modified)
            {
                actas.Add(entrada.Entity);
            }
        }

        if (sqlite)
        {
            foreach (var detalle in ChangeTracker.Entries<ActaDetalle>())
            {
                if (detalle.State is EntityState.Added or EntityState.Modified)
                {
                    detalle.Entity.RowVersion = Guid.NewGuid().ToByteArray();
                }
            }
        }

        foreach (var detalle in ChangeTracker.Entries<ActaDetalle>())
        {
            if (detalle.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            var acta = detalle.Entity.Acta ?? ChangeTracker.Entries<Acta>()
                .Select(item => item.Entity)
                .FirstOrDefault(item => item.NumeroActa == detalle.Entity.NumeroActa && item.NumeroActa != 0);
            if (acta is not null)
            {
                actas.Add(acta);
            }
        }

        foreach (var acta in actas)
        {
            var entrada = Entry(acta);
            if (entrada.State == EntityState.Detached)
            {
                continue;
            }

            if (sqlite)
            {
                acta.RowVersion = Guid.NewGuid().ToByteArray();
            }
            else if (entrada.State != EntityState.Added)
            {
                entrada.Property(item => item.Observacion).IsModified = true;
            }
        }

        foreach (var presupuesto in tocados)
        {
            var entrada = Entry(presupuesto);
            if (entrada.State == EntityState.Detached)
            {
                continue;
            }

            if (sqlite)
            {
                presupuesto.RowVersion = Guid.NewGuid().ToByteArray();
            }
            else if (entrada.State != EntityState.Added)
            {
                entrada.Property(item => item.Nombre).IsModified = true;
            }
        }
    }

    public DbSet<TipoUsuario> TiposUsuario => Set<TipoUsuario>();

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<SesionUsuario> SesionesUsuario => Set<SesionUsuario>();

    public DbSet<RegistroAuditoria> Auditorias => Set<RegistroAuditoria>();

    public DbSet<MenuOpcion> MenuOpciones => Set<MenuOpcion>();

    public DbSet<MenuOpcionTipoUsuario> MenuOpcionTipoUsuario => Set<MenuOpcionTipoUsuario>();

    public DbSet<Arquitecto> Arquitectos => Set<Arquitecto>();

    public DbSet<Revisor> Revisores => Set<Revisor>();

    public DbSet<Contratista> Contratistas => Set<Contratista>();

    public DbSet<Proveedor> Proveedores => Set<Proveedor>();

    public DbSet<Construccion> Construcciones => Set<Construccion>();

    public DbSet<UnidadMedida> UnidadesMedida => Set<UnidadMedida>();

    public DbSet<Concepto> Conceptos => Set<Concepto>();

    public DbSet<ProveedorMaterial> ProveedorMateriales => Set<ProveedorMaterial>();

    public DbSet<Acta> Actas => Set<Acta>();

    public DbSet<ActaDetalle> ActaDetalles => Set<ActaDetalle>();

    public DbSet<Dispositivo> Dispositivos => Set<Dispositivo>();

    public DbSet<OperacionMovil> OperacionesMovil => Set<OperacionMovil>();

    public DbSet<Insumo> Insumos => Set<Insumo>();

    public DbSet<InsumoPrecioHistorial> InsumoPreciosHistorial => Set<InsumoPrecioHistorial>();

    public DbSet<PrestacionSocial> PrestacionesSociales => Set<PrestacionSocial>();

    public DbSet<ParametrosManoObra> ParametrosManoObra => Set<ParametrosManoObra>();

    public DbSet<Cargo> Cargos => Set<Cargo>();

    public DbSet<Cuadrilla> Cuadrillas => Set<Cuadrilla>();

    public DbSet<Apu> Apus => Set<Apu>();

    public DbSet<Presupuesto> Presupuestos => Set<Presupuesto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ArquitectosDbContext).Assembly);
        SemillaInicial.Aplicar(modelBuilder);

        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            foreach (var propiedad in modelBuilder.Model.GetEntityTypes().SelectMany(entidad => entidad.GetProperties()))
            {
                var tipo = propiedad.FindAnnotation(RelationalAnnotationNames.ColumnType)?.Value as string;
                if (tipo is "date" or "datetime2" or "rowversion")
                {
                    propiedad.SetColumnType(null);
                }

                if (propiedad.Name == "RowVersion")
                {
                    propiedad.ValueGenerated = ValueGenerated.Never;
                    propiedad.SetBeforeSaveBehavior(PropertySaveBehavior.Save);
                    propiedad.SetAfterSaveBehavior(PropertySaveBehavior.Save);
                    propiedad.SetColumnType("BLOB");
                }
            }
        }
    }
}
