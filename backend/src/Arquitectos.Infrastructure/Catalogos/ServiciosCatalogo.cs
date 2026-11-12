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

public sealed class CatalogoPersonaServicio<TEntity>
    : CatalogoServicioBase<TEntity, CatalogoPersonaDto, CatalogoPersonaSolicitud>
    where TEntity : class, IConCedula
{
    public CatalogoPersonaServicio(ArquitectosDbContext context, string articuloYNombre)
        : base(context, articuloYNombre, "nombre")
    {
    }

    protected override IOrderedQueryable<TEntity> Ordenar(IQueryable<TEntity> consulta) =>
        consulta.OrderBy(entidad => entidad.Nombre);

    protected override string ValorUnico(CatalogoPersonaSolicitud solicitud) => solicitud.Nombre.Trim();

    protected override Task<bool> ExisteDuplicadoAsync(string valor, int exceptoId, CancellationToken cancellationToken) =>
        Context.Set<TEntity>().AnyAsync(entidad => entidad.Nombre == valor && entidad.Id != exceptoId, cancellationToken);

    protected override Task<TEntity?> BuscarInactivoAsync(string valor, CancellationToken cancellationToken) =>
        Context.Set<TEntity>().FirstOrDefaultAsync(
            entidad => entidad.Nombre == valor && !entidad.Activo,
            cancellationToken);

    protected override CatalogoPersonaDto Mapear(TEntity entidad) =>
        new(entidad.Id, entidad.Nombre, entidad.Cedula, entidad.Activo);

    protected override void Aplicar(TEntity entidad, CatalogoPersonaSolicitud solicitud, bool esNuevo)
    {
        entidad.Nombre = solicitud.Nombre.Trim();
        entidad.Cedula = Documento(solicitud.Cedula);
        if (esNuevo)
        {
            entidad.Activo = true;
        }
        else if (solicitud.Activo is bool activo)
        {
            entidad.Activo = activo;
        }
    }

    protected override async Task<string?> ConflictoAdicionalAsync(
        CatalogoPersonaSolicitud solicitud,
        int exceptoId,
        CancellationToken cancellationToken)
    {
        var cedula = Documento(solicitud.Cedula);
        if (cedula is null)
        {
            return null;
        }

        var exceptoArquitecto = typeof(TEntity) == typeof(Arquitecto) ? exceptoId : 0;
        var exceptoRevisor = typeof(TEntity) == typeof(Revisor) ? exceptoId : 0;
        var exceptoContratista = typeof(TEntity) == typeof(Contratista) ? exceptoId : 0;
        if (await CedulaRepetidaAsync(Context.Arquitectos, cedula, exceptoArquitecto, cancellationToken))
        {
            return "Ya existe un arquitecto con esa cédula.";
        }

        if (await CedulaRepetidaAsync(Context.Revisores, cedula, exceptoRevisor, cancellationToken))
        {
            return "Ya existe un revisor con esa cédula.";
        }

        if (await CedulaRepetidaAsync(Context.Contratistas, cedula, exceptoContratista, cancellationToken))
        {
            return "Ya existe un contratista con esa cédula.";
        }

        return null;
    }

    private static async Task<bool> CedulaRepetidaAsync<TPersona>(
        IQueryable<TPersona> consulta,
        string cedula,
        int exceptoId,
        CancellationToken cancellationToken)
        where TPersona : class, IConCedula
    {
        var existentes = await consulta
            .Where(entidad => entidad.Cedula != null && entidad.Id != exceptoId)
            .Select(entidad => entidad.Cedula!)
            .ToListAsync(cancellationToken);
        return existentes.Any(existente => Documento(existente) == cedula);
    }
}

public sealed class ProveedorServicio
    : CatalogoServicioBase<Proveedor, ProveedorDto, ProveedorSolicitud>
{
    public ProveedorServicio(ArquitectosDbContext context)
        : base(context, "un proveedor", "nombre")
    {
    }

    protected override IOrderedQueryable<Proveedor> Ordenar(IQueryable<Proveedor> consulta) =>
        consulta.OrderBy(entidad => entidad.Nombre);

    protected override string ValorUnico(ProveedorSolicitud solicitud) => solicitud.Nombre.Trim();

    protected override Task<bool> ExisteDuplicadoAsync(string valor, int exceptoId, CancellationToken cancellationToken) =>
        Context.Proveedores.AnyAsync(entidad => entidad.Nombre == valor && entidad.Id != exceptoId, cancellationToken);

    protected override Task<Proveedor?> BuscarInactivoAsync(string valor, CancellationToken cancellationToken) =>
        Context.Proveedores.FirstOrDefaultAsync(
            entidad => entidad.Nombre == valor && !entidad.Activo,
            cancellationToken);

    protected override ProveedorDto Mapear(Proveedor entidad) =>
        new(entidad.Id, entidad.Nombre, entidad.Nit, entidad.Activo);

    protected override void Aplicar(Proveedor entidad, ProveedorSolicitud solicitud, bool esNuevo)
    {
        entidad.Nombre = solicitud.Nombre.Trim();
        entidad.Nit = Documento(solicitud.Nit);
        if (esNuevo)
        {
            entidad.Activo = true;
        }
        else if (solicitud.Activo is bool activo)
        {
            entidad.Activo = activo;
        }
    }

    protected override async Task<string?> ConflictoAdicionalAsync(
        ProveedorSolicitud solicitud,
        int exceptoId,
        CancellationToken cancellationToken)
    {
        var nit = Documento(solicitud.Nit);
        if (nit is null)
        {
            return null;
        }

        var existentes = await Context.Proveedores
            .Where(entidad => entidad.Nit != null && entidad.Id != exceptoId)
            .Select(entidad => entidad.Nit!)
            .ToListAsync(cancellationToken);
        return existentes.Any(existente => Documento(existente) == nit)
            ? "Ya existe un proveedor con ese NIT."
            : null;
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
