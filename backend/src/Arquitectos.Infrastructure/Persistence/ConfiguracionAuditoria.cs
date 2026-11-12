using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RegistroAuditoria = Arquitectos.Domain.Entidades.Auditoria;

namespace Arquitectos.Infrastructure.Persistence;

public sealed class AuditoriaConfiguracion : IEntityTypeConfiguration<RegistroAuditoria>
{
    public void Configure(EntityTypeBuilder<RegistroAuditoria> builder)
    {
        builder.ToTable("Auditoria");
        builder.Property(a => a.Fecha).HasColumnType("datetime2");
        builder.Property(a => a.Entidad).HasMaxLength(64).IsRequired();
        builder.Property(a => a.EntidadId).HasMaxLength(64).IsRequired();
        builder.Property(a => a.Accion).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.Ip).HasMaxLength(64);
        builder.HasIndex(a => new { a.Entidad, a.EntidadId });
        builder.HasIndex(a => a.Fecha);
    }
}
