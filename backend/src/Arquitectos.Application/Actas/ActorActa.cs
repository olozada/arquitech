namespace Arquitectos.Application.Actas;

public sealed record ActorActa(
    int UsuarioId,
    int TipoUsuarioId,
    int? ArquitectoId,
    int? ContratistaId,
    int? RevisorId);
