namespace Arquitectos.Application.Usuarios;

public static class PerfilesUsuario
{
    public const int Administrador = 1;

    public const int Arquitecto = 2;

    public const int Contratista = 3;

    public const int Revisor = 4;

    public static bool EsOperativo(int tipoUsuarioId) =>
        tipoUsuarioId is Arquitecto or Contratista or Revisor;
}
