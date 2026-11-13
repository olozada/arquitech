using System.Collections;
using System.Text.Json;
using Arquitectos.Domain.Entidades;
using RegistroAuditoria = Arquitectos.Domain.Entidades.Auditoria;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Arquitectos.Infrastructure.Auditoria;

public sealed class AuditoriaInterceptor : SaveChangesInterceptor
{
    private static readonly HashSet<Type> Tipos =
    [
        typeof(Acta),
        typeof(ActaDetalle),
        typeof(Presupuesto),
        typeof(PresupuestoItem),
        typeof(Insumo),
        typeof(Usuario),
        typeof(ProveedorMaterial),
        typeof(Cuadrilla),
        typeof(Cargo)
    ];

    private readonly IHttpContextAccessor _acceso;
    private readonly List<(EntityEntry Entrada, Dictionary<string, object?[]> Cambios)> _creados = [];
    private int _profundidad;

    public AuditoriaInterceptor(IHttpContextAccessor acceso)
    {
        _acceso = acceso;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Registrar(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Registrar(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        _creados.Clear();
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        _creados.Clear();
        return Task.CompletedTask;
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        CompletarCreados(eventData.Context);
        return result;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await CompletarCreadosAsync(eventData.Context, cancellationToken);
        return result;
    }

    private void Registrar(DbContext? contexto)
    {
        if (contexto is null || _profundidad > 0)
        {
            return;
        }

        var usuarioId = UsuarioActual();
        var ip = _acceso.HttpContext?.Connection.RemoteIpAddress?.ToString();
        var origen = OrigenAuditoria.De(_acceso.HttpContext);
        var ahora = DateTime.UtcNow;
        foreach (var entrada in contexto.ChangeTracker.Entries().Where(Auditable).ToList())
        {
            if (entrada.State == EntityState.Added)
            {
                _creados.Add((entrada, Cambios(entrada)));
                continue;
            }

            var cambios = Cambios(entrada);
            if (entrada.State == EntityState.Modified && cambios.Count == 0)
            {
                continue;
            }

            contexto.Set<RegistroAuditoria>().Add(new RegistroAuditoria
            {
                Fecha = ahora,
                UsuarioId = usuarioId,
                Entidad = entrada.Metadata.ClrType.Name,
                EntidadId = Identificador(entrada),
                Accion = entrada.State switch
                {
                    EntityState.Added => AccionAuditoria.Crear,
                    EntityState.Deleted => AccionAuditoria.Eliminar,
                    _ => AccionAuditoria.Modificar
                },
                CambiosJson = cambios.Count == 0 ? null : JsonSerializer.Serialize(cambios),
                Ip = ip,
                Origen = origen
            });
        }
    }

    private void CompletarCreados(DbContext? contexto)
    {
        if (contexto is null || _creados.Count == 0)
        {
            return;
        }

        var creaciones = _creados.ToList();
        _creados.Clear();
        AgregarCreados(contexto, creaciones);
        _profundidad++;
        try
        {
            contexto.SaveChanges();
        }
        finally
        {
            _profundidad--;
        }
    }

    private async Task CompletarCreadosAsync(DbContext? contexto, CancellationToken cancellationToken)
    {
        if (contexto is null || _creados.Count == 0)
        {
            return;
        }

        var creaciones = _creados.ToList();
        _creados.Clear();
        AgregarCreados(contexto, creaciones);
        _profundidad++;
        try
        {
            await contexto.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            _profundidad--;
        }
    }

    private void AgregarCreados(
        DbContext contexto,
        IReadOnlyList<(EntityEntry Entrada, Dictionary<string, object?[]> Cambios)> creaciones)
    {
        var usuarioId = UsuarioActual();
        var ip = _acceso.HttpContext?.Connection.RemoteIpAddress?.ToString();
        var origen = OrigenAuditoria.De(_acceso.HttpContext);
        var ahora = DateTime.UtcNow;
        foreach (var (entrada, cambios) in creaciones)
        {
            if (entrada.State == EntityState.Detached)
            {
                continue;
            }

            contexto.Set<RegistroAuditoria>().Add(new RegistroAuditoria
            {
                Fecha = ahora,
                UsuarioId = usuarioId,
                Entidad = entrada.Metadata.ClrType.Name,
                EntidadId = Identificador(entrada),
                Accion = AccionAuditoria.Crear,
                CambiosJson = cambios.Count == 0 ? null : JsonSerializer.Serialize(cambios),
                Ip = ip,
                Origen = origen
            });
        }
    }

    private static bool Auditable(EntityEntry entrada) =>
        Tipos.Contains(entrada.Metadata.ClrType) &&
        entrada.State is EntityState.Added or EntityState.Modified or EntityState.Deleted;

    private static Dictionary<string, object?[]> Cambios(EntityEntry entrada)
    {
        var cambios = new Dictionary<string, object?[]>();
        foreach (var propiedad in entrada.Properties)
        {
            if (propiedad.Metadata.IsPrimaryKey() ||
                propiedad.Metadata.IsShadowProperty() ||
                propiedad.Metadata.IsConcurrencyToken ||
                Excluida(propiedad.Metadata.Name) ||
                propiedad.Metadata.ClrType == typeof(byte[]))
            {
                continue;
            }

            if (entrada.State == EntityState.Modified && !propiedad.IsModified)
            {
                continue;
            }

            var antes = Presentar(propiedad.OriginalValue);
            var despues = entrada.State == EntityState.Deleted ? null : Presentar(propiedad.CurrentValue);
            if (entrada.State != EntityState.Deleted && Iguales(antes, despues))
            {
                continue;
            }

            cambios[propiedad.Metadata.Name] = [antes, despues];
        }

        return cambios;
    }

    private static bool Excluida(string nombre) =>
        nombre.Equals("PasswordHash", StringComparison.OrdinalIgnoreCase) ||
        nombre.Equals("RowVersion", StringComparison.OrdinalIgnoreCase) ||
        nombre.Contains("Token", StringComparison.OrdinalIgnoreCase);

    private static object? Presentar(object? valor) => valor switch
    {
        null => null,
        DateTime fecha => fecha.ToUniversalTime().ToString("O"),
        DateOnly dia => dia.ToString("yyyy-MM-dd"),
        Enum enumerado => enumerado.ToString(),
        string texto => texto,
        IEnumerable => valor.ToString(),
        _ => valor
    };

    private static bool Iguales(object? antes, object? despues) =>
        Equals(antes, despues) || (antes is string textoAntes && despues is string textoDespues && textoAntes == textoDespues);

    private static string Identificador(EntityEntry entrada)
    {
        var clave = entrada.Metadata.FindPrimaryKey();
        if (clave is null)
        {
            return "";
        }

        return string.Join(':', clave.Properties.Select(propiedad =>
        {
            var valor = entrada.State == EntityState.Deleted
                ? entrada.Property(propiedad.Name).OriginalValue
                : entrada.Property(propiedad.Name).CurrentValue;
            return valor?.ToString() ?? "";
        }));
    }

    private int? UsuarioActual()
    {
        var principal = _acceso.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var sub = principal.FindFirst("sub")?.Value
            ?? principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(sub, out var id) ? id : null;
    }
}
