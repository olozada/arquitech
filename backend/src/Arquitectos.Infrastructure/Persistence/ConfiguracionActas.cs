using Arquitectos.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arquitectos.Infrastructure.Persistence;

public sealed class ActaConfiguracion : IEntityTypeConfiguration<Acta>
{
    public void Configure(EntityTypeBuilder<Acta> builder)
    {
        builder.ToTable("Actas", tabla =>
        {
            tabla.HasCheckConstraint("CK_Actas_Fechas", "[FechaFinal] >= [FechaInicial]");
        });
        builder.HasKey(a => a.NumeroActa);
        builder.Property(a => a.NumeroActa).UseIdentityColumn();
        builder.Property(a => a.FechaInicial).HasColumnType("date");
        builder.Property(a => a.FechaFinal).HasColumnType("date");
        builder.Property(a => a.Observacion).HasMaxLength(1000);
        builder.Property(a => a.Cerrada).HasDefaultValue(false);
        builder.Property(a => a.RowVersion).IsRowVersion();
        builder.Property(a => a.FechaRegistro)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(a => new { a.FechaInicial, a.FechaFinal, a.ArquitectoId })
            .HasDatabaseName("IX_Actas_Fecha_Arq");
        builder.HasIndex(a => new { a.FechaFinal, a.ContratistaId });
        builder.HasIndex(a => new { a.ContratistaId, a.FechaInicial, a.FechaFinal });

        builder.HasOne(a => a.Arquitecto)
            .WithMany()
            .HasForeignKey(a => a.ArquitectoId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Revisor)
            .WithMany()
            .HasForeignKey(a => a.RevisorId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Contratista)
            .WithMany()
            .HasForeignKey(a => a.ContratistaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.UsuarioRegistro)
            .WithMany()
            .HasForeignKey(a => a.UsuarioRegistroId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ActaDetalleConfiguracion : IEntityTypeConfiguration<ActaDetalle>
{
    public void Configure(EntityTypeBuilder<ActaDetalle> builder)
    {
        builder.ToTable("ActaDetalles", tabla =>
        {
            tabla.HasCheckConstraint("CK_ActaDetalles_Cantidad", "[Cantidad] > 0");
            tabla.HasCheckConstraint("CK_ActaDetalles_Precio", "[Precio] >= 0");
        });
        builder.Property(d => d.Cantidad).HasPrecision(18, 4);
        builder.Property(d => d.Precio).HasPrecision(18, 2);
        builder.Property(d => d.Descripcion).HasMaxLength(500).IsRequired();
        builder.Property(d => d.Aprobado).HasDefaultValue(true);
        builder.Property(d => d.Subtotal)
            .HasPrecision(37, 6)
            .HasComputedColumnSql("[Cantidad] * [Precio]", stored: true);
        builder.HasIndex(d => new { d.ConstruccionId, d.ConceptoId });
        builder.HasIndex(d => new { d.ConceptoId, d.UnidadMedidaId, d.InsumoId });
        builder.HasIndex(d => new { d.NumeroActa, d.InsumoId });
        builder.HasIndex(d => new { d.PresupuestoItemId, d.Aprobado });

        builder.HasOne(d => d.Acta)
            .WithMany(a => a.Detalles)
            .HasForeignKey(d => d.NumeroActa)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(d => d.Construccion)
            .WithMany()
            .HasForeignKey(d => d.ConstruccionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(d => d.Concepto)
            .WithMany()
            .HasForeignKey(d => d.ConceptoId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(d => d.UnidadMedida)
            .WithMany()
            .HasForeignKey(d => d.UnidadMedidaId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(d => d.Insumo)
            .WithMany()
            .HasForeignKey(d => d.InsumoId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(d => d.TipoUsuario)
            .WithMany()
            .HasForeignKey(d => d.TipoUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(d => d.PresupuestoItem)
            .WithMany()
            .HasForeignKey(d => d.PresupuestoItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
