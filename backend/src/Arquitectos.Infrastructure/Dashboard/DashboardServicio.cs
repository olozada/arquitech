using Arquitectos.Application.Dashboard;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Persistence;
using Arquitectos.Infrastructure.Presupuestos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Arquitectos.Infrastructure.Dashboard;

public sealed partial class DashboardServicio : IDashboardServicio
{
    private const int TamanoPagina = 20;

    private static readonly string[] Severidades = ["Alta", "Media", "Baja"];

    private readonly ArquitectosDbContext _context;
    private readonly OpcionesDashboard _opciones;
    private readonly IMemoryCache _cache;
    private readonly TimeZoneInfo _zona;

    /// <summary>Solo para pruebas. En la aplicación se usa el reloj UTC.</summary>
    internal DateTime? InstanteUtc { get; set; }

    public DashboardServicio(
        ArquitectosDbContext context,
        IOptions<OpcionesDashboard> opciones,
        IMemoryCache cache)
    {
        _context = context;
        _opciones = opciones.Value;
        _cache = cache;
        _zona = ZonaHorariaDashboard.Resolver(_opciones.ZonaHoraria);
    }

}
