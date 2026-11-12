namespace Arquitectos.Domain.Entidades;

public enum AccionAuditoria
{
    Crear,
    Modificar,
    Eliminar,
    Aprobar,
    Rechazar,
    Cerrar,
    Reabrir,
    Importar
}

public class Auditoria
{
    public long Id { get; set; }

    public DateTime Fecha { get; set; }

    public int? UsuarioId { get; set; }

    public required string Entidad { get; set; }

    public required string EntidadId { get; set; }

    public AccionAuditoria Accion { get; set; }

    public string? CambiosJson { get; set; }

    public string? Ip { get; set; }
}
