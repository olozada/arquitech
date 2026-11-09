using Arquitectos.Application.Menu;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Infrastructure.Seguridad;

public sealed class MenuServicio : IMenuServicio
{
    private readonly ArquitectosDbContext _context;

    public MenuServicio(ArquitectosDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<OpcionMenuDto>> ObtenerArbol(int tipoUsuarioId, CancellationToken cancellationToken)
    {
        var opciones = await _context.MenuOpciones
            .AsNoTracking()
            .Where(opcion => opcion.TiposUsuario.Any(tipo => tipo.TipoUsuarioId == tipoUsuarioId))
            .OrderBy(opcion => opcion.Orden)
            .ThenBy(opcion => opcion.Descripcion)
            .Select(opcion => new OpcionMenuPlana
            {
                Id = opcion.Id,
                PadreId = opcion.PadreId,
                Descripcion = opcion.Descripcion,
                Url = opcion.Url,
                Icono = opcion.Icono,
                Orden = opcion.Orden
            })
            .ToListAsync(cancellationToken);

        return ArbolMenu.Construir(opciones.Select(opcion => new OpcionMenuDto
        {
            Id = opcion.Id,
            PadreId = opcion.PadreId,
            Descripcion = opcion.Descripcion,
            Url = opcion.Url,
            Icono = opcion.Icono,
            Orden = opcion.Orden
        }).ToList());
    }

    private sealed class OpcionMenuPlana
    {
        public int Id { get; set; }

        public int? PadreId { get; set; }

        public string Descripcion { get; set; } = "";

        public string? Url { get; set; }

        public string? Icono { get; set; }

        public int Orden { get; set; }
    }
}
