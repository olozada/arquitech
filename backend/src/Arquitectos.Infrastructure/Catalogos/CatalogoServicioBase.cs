using Arquitectos.Application.Catalogos;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Infrastructure.Catalogos;

public abstract class CatalogoServicioBase<TEntity, TDto, TSolicitud> : ICatalogoServicio<TEntity, TDto, TSolicitud>
    where TEntity : class, ICatalogo
{
    private readonly ArquitectosDbContext _context;
    private readonly string _articuloYNombre;
    private readonly string _campo;

    protected CatalogoServicioBase(ArquitectosDbContext context, string articuloYNombre, string campo)
    {
        _context = context;
        _articuloYNombre = articuloYNombre;
        _campo = campo;
    }

    protected ArquitectosDbContext Context => _context;

    protected string ArticuloYNombre => _articuloYNombre;

    protected string MensajeDuplicado =>
        $"Ya existe {_articuloYNombre} con {(_campo == "nombre" ? "ese" : "esa")} {_campo}.";

    public async Task<IReadOnlyList<TDto>> ListarAsync(bool incluirInactivos, CancellationToken cancellationToken)
    {
        var consulta = _context.Set<TEntity>().AsNoTracking();
        if (!incluirInactivos)
        {
            consulta = consulta.Where(entidad => entidad.Activo);
        }

        var entidades = await Ordenar(consulta).ToListAsync(cancellationToken);
        return entidades.Select(Mapear).ToList();
    }

    public async Task<TDto?> ObtenerAsync(int id, CancellationToken cancellationToken)
    {
        var entidad = await _context.Set<TEntity>().AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return entidad is null ? default : Mapear(entidad);
    }

    public async Task<ResultadoCatalogo<TDto>> CrearAsync(TSolicitud solicitud, CancellationToken cancellationToken)
    {
        var valor = ValorUnico(solicitud);
        var inactivo = await BuscarInactivoAsync(valor, cancellationToken);
        if (inactivo is not null)
        {
            var conflictoInactivo = await ConflictoAdicionalAsync(solicitud, inactivo.Id, cancellationToken);
            if (conflictoInactivo is not null)
            {
                return ResultadoCatalogo<TDto>.Conflicto(conflictoInactivo);
            }

            inactivo.Activo = true;
            Aplicar(inactivo, solicitud, esNuevo: true);
            if (!await GuardarAsync(cancellationToken))
            {
                return ResultadoCatalogo<TDto>.Conflicto(MensajeDuplicado);
            }

            return ResultadoCatalogo<TDto>.Ok(Mapear(inactivo));
        }

        if (await ExisteDuplicadoAsync(valor, 0, cancellationToken))
        {
            return ResultadoCatalogo<TDto>.Conflicto(MensajeDuplicado);
        }

        var conflicto = await ConflictoAdicionalAsync(solicitud, 0, cancellationToken);
        if (conflicto is not null)
        {
            return ResultadoCatalogo<TDto>.Conflicto(conflicto);
        }

        var entidad = Activator.CreateInstance<TEntity>();
        entidad.Activo = true;
        Aplicar(entidad, solicitud, esNuevo: true);
        _context.Add(entidad);

        if (!await GuardarAsync(cancellationToken))
        {
            return ResultadoCatalogo<TDto>.Conflicto(MensajeDuplicado);
        }

        return ResultadoCatalogo<TDto>.Ok(Mapear(entidad));
    }

    public async Task<ResultadoCatalogo<TDto>> ActualizarAsync(int id, TSolicitud solicitud, CancellationToken cancellationToken)
    {
        var entidad = await _context.Set<TEntity>().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entidad is null)
        {
            return ResultadoCatalogo<TDto>.NoEncontrado($"No se encontró {_articuloYNombre}.");
        }

        var valor = ValorUnico(solicitud);
        if (await ExisteDuplicadoAsync(valor, id, cancellationToken))
        {
            return ResultadoCatalogo<TDto>.Conflicto(MensajeDuplicado);
        }

        var conflicto = await ConflictoAdicionalAsync(solicitud, id, cancellationToken);
        if (conflicto is not null)
        {
            return ResultadoCatalogo<TDto>.Conflicto(conflicto);
        }

        Aplicar(entidad, solicitud, esNuevo: false);
        if (!await GuardarAsync(cancellationToken))
        {
            return ResultadoCatalogo<TDto>.Conflicto(MensajeDuplicado);
        }

        return ResultadoCatalogo<TDto>.Ok(Mapear(entidad));
    }

    public async Task<ResultadoCatalogo<bool>> EliminarAsync(int id, CancellationToken cancellationToken)
    {
        var entidad = await _context.Set<TEntity>().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entidad is null || !entidad.Activo)
        {
            return ResultadoCatalogo<bool>.NoEncontrado($"No se encontró {_articuloYNombre}.");
        }

        entidad.Activo = false;
        await _context.SaveChangesAsync(cancellationToken);
        return ResultadoCatalogo<bool>.Ok(true);
    }

    protected static string? TextoOpcional(string? valor)
    {
        var texto = valor?.Trim();
        return string.IsNullOrEmpty(texto) ? null : texto;
    }

    protected static string? Documento(string? valor)
    {
        var texto = TextoOpcional(valor);
        if (texto is null)
        {
            return null;
        }

        var limpio = new string(texto.Where(caracter => caracter is not ' ' and not '.').ToArray());
        return limpio.Length == 0 ? null : limpio;
    }

    protected abstract IOrderedQueryable<TEntity> Ordenar(IQueryable<TEntity> consulta);

    protected abstract string ValorUnico(TSolicitud solicitud);

    protected abstract Task<bool> ExisteDuplicadoAsync(string valor, int exceptoId, CancellationToken cancellationToken);

    protected abstract Task<TEntity?> BuscarInactivoAsync(string valor, CancellationToken cancellationToken);

    protected abstract TDto Mapear(TEntity entidad);

    protected abstract void Aplicar(TEntity entidad, TSolicitud solicitud, bool esNuevo);

    protected virtual Task<string?> ConflictoAdicionalAsync(
        TSolicitud solicitud,
        int exceptoId,
        CancellationToken cancellationToken)
    {
        _ = solicitud;
        _ = exceptoId;
        _ = cancellationToken;
        return Task.FromResult<string?>(null);
    }

    private async Task<bool> GuardarAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException excepcion) when (EsViolacionUnica(excepcion))
        {
            return false;
        }
    }

    private static bool EsViolacionUnica(DbUpdateException excepcion)
    {
        for (var actual = excepcion.InnerException; actual is not null; actual = actual.InnerException)
        {
            if (actual is SqlException sql && sql.Number is 2601 or 2627)
            {
                return true;
            }
        }

        return false;
    }
}
