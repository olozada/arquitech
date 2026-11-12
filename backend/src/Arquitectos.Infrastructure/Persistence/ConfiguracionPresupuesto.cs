using Arquitectos.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arquitectos.Infrastructure.Persistence;

public sealed class InsumoConfiguracion : IEntityTypeConfiguration<Insumo>
{
    public void Configure(EntityTypeBuilder<Insumo> builder)
    {
        builder.ToTable("Insumos");
        builder.Property(i => i.Codigo).HasMaxLength(30);
        builder.Property(i => i.Descripcion).HasMaxLength(500).IsRequired();
        builder.Property(i => i.UnidadCompra).HasMaxLength(30);
        builder.Property(i => i.UnidadUso).HasMaxLength(30);
        builder.Property(i => i.PrecioBase).HasPrecision(19, 6);
        builder.Property(i => i.PorcentajeIva).HasPrecision(9, 6);
        builder.Property(i => i.CantidadPorUnidad).HasPrecision(19, 6);
        builder.Property(i => i.PrecioUnitario).HasPrecision(19, 6);
        builder.Property(i => i.Activo).HasDefaultValue(true);
        builder.HasIndex(i => i.Codigo).IsUnique().HasFilter("[Codigo] IS NOT NULL");
    }
}

public sealed class InsumoPrecioHistorialConfiguracion : IEntityTypeConfiguration<InsumoPrecioHistorial>
{
    public void Configure(EntityTypeBuilder<InsumoPrecioHistorial> builder)
    {
        builder.ToTable("InsumoPreciosHistorial");
        builder.Property(h => h.PrecioBase).HasPrecision(19, 6);
        builder.Property(h => h.PorcentajeIva).HasPrecision(9, 6);
        builder.HasOne(h => h.Insumo).WithMany(i => i.Historial).HasForeignKey(h => h.InsumoId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PrestacionSocialConfiguracion : IEntityTypeConfiguration<PrestacionSocial>
{
    public void Configure(EntityTypeBuilder<PrestacionSocial> builder)
    {
        builder.ToTable("PrestacionesSociales");
        builder.Property(p => p.Concepto).HasMaxLength(120).IsRequired();
        builder.Property(p => p.Porcentaje).HasPrecision(9, 6);
    }
}

public sealed class ParametrosManoObraConfiguracion : IEntityTypeConfiguration<ParametrosManoObra>
{
    public void Configure(EntityTypeBuilder<ParametrosManoObra> builder)
    {
        builder.ToTable("ParametrosManoObra");
        builder.HasData(new ParametrosManoObra { Id = 1, DiasMes = 30, IncluirDiasNoHabiles = false, DiasHabilesAnio = 296 });
    }
}

public sealed class CargoConfiguracion : IEntityTypeConfiguration<Cargo>
{
    public void Configure(EntityTypeBuilder<Cargo> builder)
    {
        builder.ToTable("Cargos");
        builder.Property(c => c.Nombre).HasMaxLength(80).IsRequired();
        builder.Property(c => c.SalarioMensual).HasPrecision(19, 2);
        builder.Property(c => c.Activo).HasDefaultValue(true);
        builder.HasIndex(c => c.Nombre).IsUnique();
    }
}

public sealed class CuadrillaConfiguracion : IEntityTypeConfiguration<Cuadrilla>
{
    public void Configure(EntityTypeBuilder<Cuadrilla> builder)
    {
        builder.ToTable("Cuadrillas");
        builder.Property(c => c.Nombre).HasMaxLength(80).IsRequired();
        builder.Property(c => c.Descripcion).HasMaxLength(200);
        builder.Property(c => c.Activo).HasDefaultValue(true);
    }
}

public sealed class CuadrillaCargoConfiguracion : IEntityTypeConfiguration<CuadrillaCargo>
{
    public void Configure(EntityTypeBuilder<CuadrillaCargo> builder)
    {
        builder.ToTable("CuadrillaCargos");
        builder.HasKey(c => new { c.CuadrillaId, c.CargoId });
        builder.Property(c => c.Cantidad).HasPrecision(9, 4);
        builder.HasOne(c => c.Cuadrilla).WithMany(q => q.Integrantes).HasForeignKey(c => c.CuadrillaId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(c => c.Cargo).WithMany(c => c.Cuadrillas).HasForeignKey(c => c.CargoId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ApuConfiguracion : IEntityTypeConfiguration<Apu>
{
    public void Configure(EntityTypeBuilder<Apu> builder)
    {
        builder.ToTable("Apus");
        builder.Property(a => a.Codigo).HasMaxLength(30);
        builder.Property(a => a.Descripcion).HasMaxLength(500).IsRequired();
        builder.Property(a => a.Unidad).HasMaxLength(20);
        builder.Property(a => a.DivisorGeneral).HasPrecision(19, 6);
        builder.Property(a => a.Activo).HasDefaultValue(true);
        builder.HasOne(a => a.UnidadMedida).WithMany().HasForeignKey(a => a.UnidadMedidaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Concepto).WithMany().HasForeignKey(a => a.ConceptoId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ApuGrupoConfiguracion : IEntityTypeConfiguration<ApuGrupo>
{
    public void Configure(EntityTypeBuilder<ApuGrupo> builder)
    {
        builder.ToTable("ApuGrupos");
        builder.Property(g => g.Divisor).HasPrecision(19, 6);
        builder.HasOne(g => g.Apu).WithMany(a => a.Grupos).HasForeignKey(g => g.ApuId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ApuLineaConfiguracion : IEntityTypeConfiguration<ApuLinea>
{
    public void Configure(EntityTypeBuilder<ApuLinea> builder)
    {
        builder.ToTable("ApuLineas");
        builder.Property(l => l.Descripcion).HasMaxLength(500);
        builder.Property(l => l.Unidad).HasMaxLength(20);
        builder.Property(l => l.Cantidad).HasPrecision(19, 6);
        builder.Property(l => l.PrecioManual).HasPrecision(19, 6);
        builder.Property(l => l.Factor).HasPrecision(19, 8);
        builder.HasOne(l => l.Grupo).WithMany(g => g.Lineas).HasForeignKey(l => l.ApuGrupoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(l => l.Insumo).WithMany().HasForeignKey(l => l.InsumoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(l => l.Cuadrilla).WithMany().HasForeignKey(l => l.CuadrillaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(l => l.SubApu).WithMany().HasForeignKey(l => l.SubApuId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PresupuestoConfiguracion : IEntityTypeConfiguration<Presupuesto>
{
    public void Configure(EntityTypeBuilder<Presupuesto> builder)
    {
        builder.ToTable("Presupuestos", tabla => tabla.HasCheckConstraint("CK_Presupuestos_Plantilla", "[EsPlantilla] = 1 OR [ConstruccionId] IS NOT NULL"));
        builder.Property(p => p.Nombre).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Fecha).HasColumnType("date");
        builder.Property(p => p.AreaConstruidaM2).HasPrecision(19, 4);
        builder.Property(p => p.PctImprevistos).HasPrecision(9, 6);
        builder.Property(p => p.PctIncremento).HasPrecision(9, 6);
        builder.Property(p => p.PctAdministracion).HasPrecision(9, 6);
        builder.Property(p => p.PctUtilidad).HasPrecision(9, 6);
        builder.Property(p => p.PctIvaUtilidad).HasPrecision(9, 6);
        builder.Property(p => p.RowVersion).IsRowVersion();
        builder.HasIndex(p => p.ConstruccionId).IsUnique().HasFilter("[Estado] = 2 AND [EsPlantilla] = 0").HasDatabaseName("IX_Presupuestos_Aprobado");
        builder.HasOne(p => p.Construccion).WithMany().HasForeignKey(p => p.ConstruccionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Presupuesto>().WithMany().HasForeignKey(p => p.PresupuestoOrigenId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PresupuestoCapituloConfiguracion : IEntityTypeConfiguration<PresupuestoCapitulo>
{
    public void Configure(EntityTypeBuilder<PresupuestoCapitulo> builder)
    {
        builder.ToTable("PresupuestoCapitulos");
        builder.Property(c => c.Nombre).HasMaxLength(200).IsRequired();
        builder.HasOne(c => c.Presupuesto).WithMany(p => p.Capitulos).HasForeignKey(c => c.PresupuestoId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PresupuestoItemConfiguracion : IEntityTypeConfiguration<PresupuestoItem>
{
    public void Configure(EntityTypeBuilder<PresupuestoItem> builder)
    {
        builder.ToTable("PresupuestoItems");
        builder.Property(i => i.Codigo).HasMaxLength(20).IsRequired();
        builder.Property(i => i.Descripcion).HasMaxLength(500).IsRequired();
        builder.Property(i => i.Unidad).HasMaxLength(20);
        builder.Property(i => i.Cantidad).HasPrecision(19, 4);
        builder.Property(i => i.FactorConversionApu).HasPrecision(19, 6);
        builder.Property(i => i.CantidadOrigenFactor).HasPrecision(19, 6);
        builder.Property(i => i.ValorMaterialUnitario).HasPrecision(19, 6);
        builder.Property(i => i.ValorManoObraUnitario).HasPrecision(19, 6);
        builder.HasOne(i => i.Capitulo).WithMany(c => c.Items).HasForeignKey(i => i.CapituloId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(i => i.Apu).WithMany().HasForeignKey(i => i.ApuId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Concepto).WithMany().HasForeignKey(i => i.ConceptoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.UnidadMedida).WithMany().HasForeignKey(i => i.UnidadMedidaId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PresupuestoMedicionConfiguracion : IEntityTypeConfiguration<PresupuestoMedicion>
{
    public void Configure(EntityTypeBuilder<PresupuestoMedicion> builder)
    {
        builder.ToTable("PresupuestoMediciones");
        builder.Property(m => m.Ubicacion).HasMaxLength(200);
        builder.Property(m => m.Veces).HasPrecision(19, 4);
        builder.Property(m => m.Largo).HasPrecision(19, 4);
        builder.Property(m => m.Ancho).HasPrecision(19, 4);
        builder.Property(m => m.Alto).HasPrecision(19, 4);
        builder.Property(m => m.Parcial).HasPrecision(19, 4);
        builder.HasOne(m => m.Item).WithMany(i => i.Mediciones).HasForeignKey(m => m.PresupuestoItemId).OnDelete(DeleteBehavior.Cascade);
    }
}
