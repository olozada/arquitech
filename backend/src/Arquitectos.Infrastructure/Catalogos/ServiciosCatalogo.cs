using Arquitectos.Application.Catalogos;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Infrastructure.Catalogos;

public sealed class CatalogoNombreServicio<TEntity>
    : CatalogoServicioBase<TEntity, CatalogoNombreDto, CatalogoNombreSolicitud>
    where TEntity : class, IConNombre
{
    public CatalogoNombreServicio(ArquitectosDbContext context, string articuloYNombre)
        : base(context, articuloYNombre, "nombre")
    {
    }

    protected override IOrderedQueryable<TEntity> Ordenar(IQueryable<TEntity> consulta) =>
        consulta.OrderBy(entidad => entidad.Nombre);

    protected override string ValorUnico(CatalogoNombreSolicitud solicitud) => solicitud.Nombre.Trim();

    protected override Task<bool> ExisteDuplicadoAsync(string valor, int exceptoId, CancellationToken cancellationToken) =>
        Context.Set<TEntity>().AnyAsync(entidad => entidad.Nombre == valor && entidad.Id != exceptoId, cancellationToken);

    protected override Task<TEntity?> BuscarInactivoAsync(string valor, CancellationToken cancellationToken) =>
        Context.Set<TEntity>().FirstOrDefaultAsync(
            entidad => entidad.Nombre == valor && !entidad.Activo,
            cancellationToken);

    protected override CatalogoNombreDto Mapear(TEntity entidad) =>
        new(entidad.Id, entidad.Nombre, entidad.Activo);

    protected override void Aplicar(TEntity entidad, CatalogoNombreSolicitud solicitud, bool esNuevo)
    {
        entidad.Nombre = solicitud.Nombre.Trim();
        if (esNuevo)
        {
            entidad.Activo = true;
        }
        else if (solicitud.Activo is bool activo)
        {
            entidad.Activo = activo;
        }
    }
}

public sealed class ConstruccionServicio
    : CatalogoServicioBase<Construccion, ConstruccionDto, ConstruccionSolicitud>
{
    public ConstruccionServicio(ArquitectosDbContext context)
        : base(context, "una construcción", "descripción")
    {
    }

    protected override IOrderedQueryable<Construccion> Ordenar(IQueryable<Construccion> consulta) =>
        consulta.OrderBy(entidad => entidad.Descripcion);

    protected override string ValorUnico(ConstruccionSolicitud solicitud) => solicitud.Descripcion.Trim();

    protected override Task<bool> ExisteDuplicadoAsync(string valor, int exceptoId, CancellationToken cancellationToken) =>
        Context.Construcciones.AnyAsync(entidad => entidad.Descripcion == valor && entidad.Id != exceptoId, cancellationToken);

    protected override Task<Construccion?> BuscarInactivoAsync(string valor, CancellationToken cancellationToken) =>
        Context.Construcciones.FirstOrDefaultAsync(
            entidad => entidad.Descripcion == valor && !entidad.Activo,
            cancellationToken);

    protected override ConstruccionDto Mapear(Construccion entidad) =>
        new(entidad.Id, entidad.Descripcion, entidad.Propietario, entidad.Activo);

    protected override void Aplicar(Construccion entidad, ConstruccionSolicitud solicitud, bool esNuevo)
    {
        entidad.Descripcion = solicitud.Descripcion.Trim();
        entidad.Propietario = TextoOpcional(solicitud.Propietario);
        if (esNuevo)
        {
            entidad.Activo = true;
        }
        else if (solicitud.Activo is bool activo)
        {
            entidad.Activo = activo;
        }
    }
}

public sealed class DescripcionUnidadServicio<TEntity>
    : CatalogoServicioBase<TEntity, DescripcionUnidadDto, DescripcionUnidadSolicitud>
    where TEntity : class, IConDescripcionUnidad
{
    public DescripcionUnidadServicio(ArquitectosDbContext context, string articuloYNombre)
        : base(context, articuloYNombre, "descripción")
    {
    }

    protected override IOrderedQueryable<TEntity> Ordenar(IQueryable<TEntity> consulta) =>
        consulta.OrderBy(entidad => entidad.Descripcion);

    protected override string ValorUnico(DescripcionUnidadSolicitud solicitud) => solicitud.Descripcion.Trim();

    protected override Task<bool> ExisteDuplicadoAsync(string valor, int exceptoId, CancellationToken cancellationToken) =>
        Context.Set<TEntity>().AnyAsync(entidad => entidad.Descripcion == valor && entidad.Id != exceptoId, cancellationToken);

    protected override Task<TEntity?> BuscarInactivoAsync(string valor, CancellationToken cancellationToken) =>
        Context.Set<TEntity>().FirstOrDefaultAsync(
            entidad => entidad.Descripcion == valor && !entidad.Activo,
            cancellationToken);

    protected override DescripcionUnidadDto Mapear(TEntity entidad) =>
        new(entidad.Id, entidad.Descripcion, entidad.Unidad, entidad.Activo);

    protected override void Aplicar(TEntity entidad, DescripcionUnidadSolicitud solicitud, bool esNuevo)
    {
        entidad.Descripcion = solicitud.Descripcion.Trim();
        entidad.Unidad = TextoOpcional(solicitud.Unidad);
        if (esNuevo)
        {
            entidad.Activo = true;
        }
        else if (solicitud.Activo is bool activo)
        {
            entidad.Activo = activo;
        }
    }
}

public sealed class UnidadMedidaServicio
    : CatalogoServicioBase<UnidadMedida, UnidadMedidaDto, UnidadMedidaSolicitud>
{
    public UnidadMedidaServicio(ArquitectosDbContext context)
        : base(context, "una unidad de medida", "nombre")
    {
    }

    protected override IOrderedQueryable<UnidadMedida> Ordenar(IQueryable<UnidadMedida> consulta) =>
        consulta.OrderBy(entidad => entidad.Nombre);

    protected override string ValorUnico(UnidadMedidaSolicitud solicitud) => solicitud.Nombre.Trim();

    protected override Task<bool> ExisteDuplicadoAsync(string valor, int exceptoId, CancellationToken cancellationToken) =>
        Context.UnidadesMedida.AnyAsync(entidad => entidad.Nombre == valor && entidad.Id != exceptoId, cancellationToken);

    protected override Task<UnidadMedida?> BuscarInactivoAsync(string valor, CancellationToken cancellationToken) =>
        Context.UnidadesMedida.FirstOrDefaultAsync(
            entidad => entidad.Nombre == valor && !entidad.Activo,
            cancellationToken);

    protected override UnidadMedidaDto Mapear(UnidadMedida entidad) =>
        new(entidad.Id, entidad.Nombre, entidad.Descripcion, entidad.Activo);

    protected override void Aplicar(UnidadMedida entidad, UnidadMedidaSolicitud solicitud, bool esNuevo)
    {
        entidad.Nombre = solicitud.Nombre.Trim();
        entidad.Descripcion = TextoOpcional(solicitud.Descripcion);
        if (esNuevo)
        {
            entidad.Activo = true;
        }
        else if (solicitud.Activo is bool activo)
        {
            entidad.Activo = activo;
        }
    }
}
