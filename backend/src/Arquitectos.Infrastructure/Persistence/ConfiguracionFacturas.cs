using Arquitectos.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arquitectos.Infrastructure.Persistence;

public sealed class FacturaConfiguracion : IEntityTypeConfiguration<Factura>
{
    public void Configure(EntityTypeBuilder<Factura> builder)
    {
        builder.ToTable("Facturas");
        builder.Property(f => f.Numero).HasMaxLength(30).IsRequired();
        builder.Property(f => f.Fecha).HasColumnType("date");
        builder.Property(f => f.OrdenCompra).HasMaxLength(50);
        builder.Property(f => f.Observacion).HasMaxLength(1000);
        builder.Property(f => f.RowVersion).IsRowVersion();
        builder.Property(f => f.ModificadaEn).HasColumnType("datetime2");
        builder.Property(f => f.FechaRegistro)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(f => new { f.ProveedorId, f.Numero })
            .IsUnique()
            .HasDatabaseName("IX_Facturas_Proveedor_Numero");
        builder.HasIndex(f => new { f.Fecha, f.Id });

        builder.HasOne(f => f.Proveedor)
            .WithMany()
            .HasForeignKey(f => f.ProveedorId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(f => f.UsuarioRegistro)
            .WithMany()
            .HasForeignKey(f => f.UsuarioRegistroId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class FacturaDetalleConfiguracion : IEntityTypeConfiguration<FacturaDetalle>
{
    public void Configure(EntityTypeBuilder<FacturaDetalle> builder)
    {
        builder.ToTable("FacturaDetalles", tabla =>
        {
            tabla.HasCheckConstraint("CK_FacturaDetalles_Cantidad", "[Cantidad] > 0");
            tabla.HasCheckConstraint("CK_FacturaDetalles_Precio", "[Precio] >= 0");
        });
        builder.Property(d => d.Cantidad).HasPrecision(18, 4);
        builder.Property(d => d.Precio).HasPrecision(18, 2);
        builder.Property(d => d.Descripcion).HasMaxLength(500).IsRequired();
        builder.Property(d => d.Subtotal)
            .HasPrecision(37, 6)
            .HasComputedColumnSql("[Cantidad] * [Precio]", stored: true);
        builder.HasIndex(d => new { d.ConstruccionId, d.InsumoId });
        builder.HasIndex(d => d.FacturaId);

        builder.HasOne(d => d.Factura)
            .WithMany(f => f.Detalles)
            .HasForeignKey(d => d.FacturaId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(d => d.Construccion)
            .WithMany()
            .HasForeignKey(d => d.ConstruccionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(d => d.Insumo)
            .WithMany()
            .HasForeignKey(d => d.InsumoId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(d => d.UnidadMedida)
            .WithMany()
            .HasForeignKey(d => d.UnidadMedidaId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
