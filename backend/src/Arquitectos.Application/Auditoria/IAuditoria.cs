using Arquitectos.Domain.Entidades;

namespace Arquitectos.Application.Auditoria;

public interface IAuditoria
{
    Task RegistrarAsync(
        string entidad,
        string entidadId,
        AccionAuditoria accion,
        int? usuarioId,
        string? cambiosJson,
        CancellationToken cancellationToken);
}

public interface IConsultaAuditoria
{
    Task<PaginaAuditoriaDto> ConsultarAsync(
        string? entidad,
        string? entidadId,
        DateOnly? desde,
        DateOnly? hasta,
        int? usuarioId,
        int pagina,
        CancellationToken cancellationToken);
}

public sealed record AuditoriaDto(
    long Id,
    DateTime Fecha,
    int? UsuarioId,
    string? Usuario,
    string Entidad,
    string EntidadId,
    string Accion,
    string? CambiosJson,
    string? Ip);

public sealed record PaginaAuditoriaDto(IReadOnlyList<AuditoriaDto> Registros, int Total, int Pagina, int Tamano);
