namespace Arquitectos.Application.Menu;

public interface IMenuServicio
{
    Task<IReadOnlyList<OpcionMenuDto>> ObtenerArbol(int tipoUsuarioId, CancellationToken cancellationToken);
}
