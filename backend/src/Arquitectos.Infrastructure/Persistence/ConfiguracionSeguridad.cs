using Arquitectos.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arquitectos.Infrastructure.Persistence;

public sealed class TipoUsuarioConfiguracion : IEntityTypeConfiguration<TipoUsuario>
{
    public void Configure(EntityTypeBuilder<TipoUsuario> builder)
    {
        builder.ToTable("TiposUsuario");
        builder.Property(t => t.Nombre).HasMaxLength(50).IsRequired();
        builder.HasIndex(t => t.Nombre).IsUnique();
    }
}

public sealed class UsuarioConfiguracion : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");
        builder.Property(u => u.NombreUsuario).HasColumnName("Usuario").HasMaxLength(50).IsRequired();
        builder.Property(u => u.Nombre).HasMaxLength(150).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(200).IsRequired();
        builder.Property(u => u.Activo).HasDefaultValue(true);
        builder.HasIndex(u => u.NombreUsuario).IsUnique();
        builder.HasOne(u => u.TipoUsuario)
            .WithMany(t => t.Usuarios)
            .HasForeignKey(u => u.TipoUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class MenuOpcionConfiguracion : IEntityTypeConfiguration<MenuOpcion>
{
    public void Configure(EntityTypeBuilder<MenuOpcion> builder)
    {
        builder.ToTable("MenuOpciones");
        builder.Property(m => m.Descripcion).HasMaxLength(100).IsRequired();
        builder.Property(m => m.Url).HasMaxLength(200);
        builder.Property(m => m.Icono).HasMaxLength(50);
        builder.Property(m => m.Orden).HasDefaultValue(0);
        builder.HasOne(m => m.Padre)
            .WithMany(m => m.Hijos)
            .HasForeignKey(m => m.PadreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class MenuOpcionTipoUsuarioConfiguracion : IEntityTypeConfiguration<MenuOpcionTipoUsuario>
{
    public void Configure(EntityTypeBuilder<MenuOpcionTipoUsuario> builder)
    {
        builder.ToTable("MenuOpcionTipoUsuario");
        builder.HasKey(m => new { m.MenuOpcionId, m.TipoUsuarioId });
        builder.HasOne(m => m.MenuOpcion)
            .WithMany(o => o.TiposUsuario)
            .HasForeignKey(m => m.MenuOpcionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(m => m.TipoUsuario)
            .WithMany(t => t.OpcionesMenu)
            .HasForeignKey(m => m.TipoUsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
