using Arquitectos.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arquitectos.Infrastructure.Persistence;

public sealed class ArquitectoConfiguracion : IEntityTypeConfiguration<Arquitecto>
{
    public void Configure(EntityTypeBuilder<Arquitecto> builder)
    {
        builder.ToTable("Arquitectos");
        builder.Property(a => a.Nombre).HasMaxLength(150).IsRequired();
        builder.Property(a => a.Activo).HasDefaultValue(true);
        builder.HasIndex(a => a.Nombre).IsUnique();
    }
}

public sealed class RevisorConfiguracion : IEntityTypeConfiguration<Revisor>
{
    public void Configure(EntityTypeBuilder<Revisor> builder)
    {
        builder.ToTable("Revisores");
        builder.Property(r => r.Nombre).HasMaxLength(150).IsRequired();
        builder.Property(r => r.Activo).HasDefaultValue(true);
        builder.HasIndex(r => r.Nombre).IsUnique();
    }
}

public sealed class ContratistaConfiguracion : IEntityTypeConfiguration<Contratista>
{
    public void Configure(EntityTypeBuilder<Contratista> builder)
    {
        builder.ToTable("Contratistas");
        builder.Property(c => c.Nombre).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Activo).HasDefaultValue(true);
        builder.HasIndex(c => c.Nombre).IsUnique();
    }
}

public sealed class ProveedorConfiguracion : IEntityTypeConfiguration<Proveedor>
{
    public void Configure(EntityTypeBuilder<Proveedor> builder)
    {
        builder.ToTable("Proveedores");
        builder.Property(p => p.Nombre).HasMaxLength(150).IsRequired();
        builder.Property(p => p.Activo).HasDefaultValue(true);
        builder.HasIndex(p => p.Nombre).IsUnique();
    }
}

public sealed class ConstruccionConfiguracion : IEntityTypeConfiguration<Construccion>
{
    public void Configure(EntityTypeBuilder<Construccion> builder)
    {
        builder.ToTable("Construcciones");
        builder.Property(c => c.Descripcion).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Propietario).HasMaxLength(150);
        builder.Property(c => c.Activo).HasDefaultValue(true);
        builder.HasIndex(c => c.Descripcion).IsUnique();
    }
}

public sealed class UnidadMedidaConfiguracion : IEntityTypeConfiguration<UnidadMedida>
{
    public void Configure(EntityTypeBuilder<UnidadMedida> builder)
    {
        builder.ToTable("UnidadesMedida");
        builder.Property(u => u.Nombre).HasMaxLength(20).IsRequired();
        builder.Property(u => u.Descripcion).HasMaxLength(100);
        builder.Property(u => u.Activo).HasDefaultValue(true);
        builder.HasIndex(u => u.Nombre).IsUnique();
    }
}

public sealed class ConceptoConfiguracion : IEntityTypeConfiguration<Concepto>
{
    public void Configure(EntityTypeBuilder<Concepto> builder)
    {
        builder.ToTable("Conceptos");
        builder.Property(c => c.Codigo).HasMaxLength(30);
        builder.Property(c => c.Descripcion).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Unidad).HasMaxLength(20);
        builder.Property(c => c.Activo).HasDefaultValue(true);
        builder.HasIndex(c => c.Descripcion).IsUnique();
    }
}

public sealed class MaterialConfiguracion : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> builder)
    {
        builder.ToTable("Materiales");
        builder.Property(m => m.Codigo).HasMaxLength(30);
        builder.Property(m => m.Descripcion).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Unidad).HasMaxLength(20);
        builder.Property(m => m.Activo).HasDefaultValue(true);
        builder.HasIndex(m => m.Descripcion).IsUnique();
    }
}

public sealed class ProveedorMaterialConfiguracion : IEntityTypeConfiguration<ProveedorMaterial>
{
    public void Configure(EntityTypeBuilder<ProveedorMaterial> builder)
    {
        builder.ToTable("ProveedorMateriales", tabla =>
        {
            tabla.HasCheckConstraint("CK_ProveedorMateriales_Precio", "[Precio] > 0");
        });
        builder.HasKey(p => new { p.ProveedorId, p.MaterialId });
        builder.Property(p => p.Precio).HasPrecision(18, 2);
        builder.HasOne(p => p.Proveedor)
            .WithMany(p => p.Materiales)
            .HasForeignKey(p => p.ProveedorId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Material)
            .WithMany()
            .HasForeignKey(p => p.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
