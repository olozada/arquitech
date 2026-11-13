using Arquitectos.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arquitectos.Infrastructure.Persistence;

public sealed class DispositivoConfiguracion : IEntityTypeConfiguration<Dispositivo>
{
    public void Configure(EntityTypeBuilder<Dispositivo> builder)
    {
        builder.ToTable("Dispositivos");
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.Nombre).HasMaxLength(100).IsRequired();
        builder.Property(d => d.Plataforma).HasMaxLength(20).IsRequired();
        builder.Property(d => d.VersionApp).HasMaxLength(20).IsRequired();
        builder.Property(d => d.RegistradoEn).HasColumnType("datetime2");
        builder.Property(d => d.UltimaSincronizacion).HasColumnType("datetime2");
        builder.Property(d => d.RevocadoEn).HasColumnType("datetime2");
        builder.HasIndex(d => d.UsuarioId);
        builder.HasOne(d => d.Usuario)
            .WithMany()
            .HasForeignKey(d => d.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class OperacionMovilConfiguracion : IEntityTypeConfiguration<OperacionMovil>
{
    public void Configure(EntityTypeBuilder<OperacionMovil> builder)
    {
        builder.ToTable("OperacionesMovil");
        builder.HasKey(o => o.IdOperacion);
        builder.Property(o => o.IdOperacion).ValueGeneratedNever();
        builder.Property(o => o.Tipo).HasMaxLength(30).IsRequired();
        builder.Property(o => o.Estado).HasMaxLength(20).IsRequired();
        builder.Property(o => o.RecibidaEn).HasColumnType("datetime2");
        builder.Property(o => o.CreadaEnDispositivo).HasColumnType("datetime2");
        builder.HasIndex(o => new { o.UsuarioId, o.RecibidaEn });
        builder.HasOne(o => o.Dispositivo)
            .WithMany()
            .HasForeignKey(o => o.DispositivoId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.Usuario)
            .WithMany()
            .HasForeignKey(o => o.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
