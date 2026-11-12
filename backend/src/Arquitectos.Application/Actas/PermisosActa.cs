using Arquitectos.Application.Usuarios;

namespace Arquitectos.Application.Actas;

public static class PermisosActa
{
    public static bool PuedeVer(int tipoUsuarioId, int? arquitectoPropioId, int? contratistaPropioId, int? revisorPropioId, int arquitectoId, int contratistaId, int revisorId) =>
        tipoUsuarioId switch
        {
            PerfilesUsuario.Administrador => true,
            PerfilesUsuario.Arquitecto => arquitectoId == arquitectoPropioId,
            PerfilesUsuario.Contratista => contratistaId == contratistaPropioId,
            PerfilesUsuario.Revisor => revisorId == revisorPropioId,
            _ => false
        };
}
