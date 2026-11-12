using System.Text.Json;
using Arquitectos.Application.Auditoria;
using Arquitectos.Domain.Entidades;
using RegistroAuditoria = Arquitectos.Domain.Entidades.Auditoria;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Infrastructure.Auditoria;

public sealed class AuditoriaServicio : IAuditoria, IConsultaAuditoria
{

    private readonly ArquitectosDbContext _context;
    private readonly IHttpContextAccessor _acceso;

    public AuditoriaServicio(ArquitectosDbContext context, IHttpContextAccessor acceso)
    {
        _context = context;
        _acceso = acceso;
    }

    public Task RegistrarAsync(
        string entidad,
        string entidadId,
        AccionAuditoria accion,
        int? usuarioId,
        string? cambiosJson,
        CancellationToken cancellationToken)
    {
        _context.Auditorias.Add(new RegistroAuditoria
        {
            Fecha = DateTime.UtcNow,
            UsuarioId = usuarioId,
            Entidad = entidad,
            EntidadId = entidadId,
            Accion = accion,
            CambiosJson = cambiosJson,
            Ip = _acceso.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });
        return Task.CompletedTask;
    }

    public async Task<PaginaAuditoriaDto> ConsultarAsync(
        string? entidad,
        string? entidadId,
        DateOnly? desde,
        DateOnly? hasta,
        int? usuarioId,
        int pagina,
        int tamano,
        CancellationToken cancellationToken)
    {
        var (paginaOk, tamanoOk) = Paginacion.Normalizar(pagina, tamano);
        var consulta = _context.Auditorias.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(entidad) && !string.IsNullOrWhiteSpace(entidadId))
        {
            consulta = await IncluirRelacionados(consulta, entidad, entidadId, cancellationToken);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(entidad))
            {
                consulta = consulta.Where(registro => registro.Entidad == entidad);
            }

            if (!string.IsNullOrWhiteSpace(entidadId))
            {
                consulta = consulta.Where(registro => registro.EntidadId == entidadId);
            }
        }

        if (usuarioId is int id)
        {
            consulta = consulta.Where(registro => registro.UsuarioId == id);
        }

        if (desde is DateOnly inicio)
        {
            var desdeUtc = inicio.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            consulta = consulta.Where(registro => registro.Fecha >= desdeUtc);
        }

        if (hasta is DateOnly fin)
        {
            var hastaUtc = fin.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            consulta = consulta.Where(registro => registro.Fecha < hastaUtc);
        }

        var total = await consulta.CountAsync(cancellationToken);
        var filas = await consulta
            .OrderByDescending(registro => registro.Fecha)
            .ThenByDescending(registro => registro.Id)
            .Skip((paginaOk - 1) * tamanoOk)
            .Take(tamanoOk)
            .ToListAsync(cancellationToken);
        var usuarios = await NombresAsync(filas, cancellationToken);
        return new PaginaAuditoriaDto(
            filas.Select(registro => new AuditoriaDto(
                registro.Id,
                registro.Fecha,
                registro.UsuarioId,
                registro.UsuarioId is int usuario && usuarios.TryGetValue(usuario, out var nombre) ? nombre : null,
                registro.Entidad,
                registro.EntidadId,
                registro.Accion.ToString(),
                registro.CambiosJson,
                registro.Ip)).ToList(),
            total,
            paginaOk,
            tamanoOk);
    }

    private async Task<IQueryable<RegistroAuditoria>> IncluirRelacionados(
        IQueryable<RegistroAuditoria> consulta,
        string entidad,
        string entidadId,
        CancellationToken cancellationToken)
    {
        if (entidad == "Acta" && int.TryParse(entidadId, out var numero))
        {
            var detalles = await _context.ActaDetalles.AsNoTracking()
                .Where(detalle => detalle.NumeroActa == numero)
                .Select(detalle => detalle.Id.ToString())
                .ToListAsync(cancellationToken);
            return consulta.Where(registro =>
                (registro.Entidad == "Acta" && registro.EntidadId == entidadId) ||
                (registro.Entidad == "ActaDetalle" && detalles.Contains(registro.EntidadId)));
        }

        if (entidad == "Presupuesto" && int.TryParse(entidadId, out var presupuestoId))
        {
            var items = await _context.Set<PresupuestoItem>().AsNoTracking()
                .Where(item => item.Capitulo.PresupuestoId == presupuestoId)
                .Select(item => item.Id.ToString())
                .ToListAsync(cancellationToken);
            return consulta.Where(registro =>
                (registro.Entidad == "Presupuesto" && registro.EntidadId == entidadId) ||
                (registro.Entidad == "PresupuestoItem" && items.Contains(registro.EntidadId)));
        }

        return consulta.Where(registro => registro.Entidad == entidad && registro.EntidadId == entidadId);
    }

    private async Task<Dictionary<int, string>> NombresAsync(
        IReadOnlyList<RegistroAuditoria> filas,
        CancellationToken cancellationToken)
    {
        var ids = filas.Where(registro => registro.UsuarioId is int).Select(registro => registro.UsuarioId!.Value).Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        return await _context.Usuarios.AsNoTracking()
            .Where(usuario => ids.Contains(usuario.Id))
            .ToDictionaryAsync(usuario => usuario.Id, usuario => usuario.Nombre, cancellationToken);
    }
}

internal sealed class SinAuditoria : IAuditoria
{
    public static readonly SinAuditoria Instancia = new();

    public Task RegistrarAsync(
        string entidad,
        string entidadId,
        AccionAuditoria accion,
        int? usuarioId,
        string? cambiosJson,
        CancellationToken cancellationToken) => Task.CompletedTask;
}

internal static class CambiosAuditoria
{
    public static string Json(string campo, object? antes, object? despues) =>
        JsonSerializer.Serialize(new Dictionary<string, object?[]>
        {
            [campo] = [antes, despues]
        });
}
