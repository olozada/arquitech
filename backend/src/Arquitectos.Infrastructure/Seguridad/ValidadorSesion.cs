using Arquitectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Arquitectos.Infrastructure.Seguridad;

public sealed class ValidadorSesion
{
    private readonly ArquitectosDbContext _context;
    private readonly IMemoryCache _cache;

    public ValidadorSesion(ArquitectosDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<bool> VigenteAsync(int usuarioId, long sesionId, CancellationToken cancellationToken)
    {
        var generacion = Generacion(usuarioId);
        var clave = $"usuario-vigente:{usuarioId}:{sesionId}:{generacion}";
        if (_cache.TryGetValue(clave, out bool vigente))
        {
            return vigente;
        }

        var ahora = DateTime.UtcNow;
        var usuarioOk = await _context.Usuarios.AsNoTracking().AnyAsync(
            usuario => usuario.Id == usuarioId &&
                       usuario.Activo &&
                       (usuario.BloqueadoHasta == null || usuario.BloqueadoHasta <= ahora),
            cancellationToken);
        var sesionOk = usuarioOk && await _context.SesionesUsuario.AsNoTracking().AnyAsync(
            sesion => sesion.Id == sesionId &&
                      sesion.UsuarioId == usuarioId &&
                      sesion.RevocadoEn == null &&
                      sesion.Expira > ahora,
            cancellationToken);
        _cache.Set(clave, sesionOk, TimeSpan.FromSeconds(60));
        return sesionOk;
    }

    public void Invalidar(int usuarioId)
    {
        _cache.Set(ClaveGeneracion(usuarioId), Generacion(usuarioId) + 1);
    }

    private int Generacion(int usuarioId) =>
        _cache.GetOrCreate(ClaveGeneracion(usuarioId), entrada =>
        {
            entrada.Priority = CacheItemPriority.NeverRemove;
            return 0;
        });

    private static string ClaveGeneracion(int usuarioId) => $"usuario-generacion:{usuarioId}";
}
