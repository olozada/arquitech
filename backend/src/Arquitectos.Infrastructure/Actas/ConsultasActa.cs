using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Arquitectos.Application.Actas;
using Arquitectos.Application.Auditoria;
using Arquitectos.Application.Usuarios;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Auditoria;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Infrastructure.Actas;

public sealed partial class ActaServicio : IActaServicio
{
    public async Task<ResultadoActa<ConsultaActasDto>> ConsultarAsync(
        DateOnly? desde,
        DateOnly? hasta,
        int? arquitectoId,
        int? revisorId,
        int? contratistaId,
        int? construccionId,
        bool? cerrada,
        int usuarioId,
        CancellationToken cancellationToken,
        int pagina = 1,
        int tamano = Paginacion.TamanoDefecto,
        string? numero = null,
        string? nombreArquitecto = null,
        string? nombreRevisor = null,
        string? nombreContratista = null,
        string? orden = null,
        bool descendente = true)
    {
        var fechas = ValidarRango(desde, hasta);
        if (fechas is not null)
        {
            return ResultadoActa<ConsultaActasDto>.Invalido(fechas);
        }

        var actor = await CargarActorAsync(usuarioId, cancellationToken);
        if (actor is null)
        {
            return ResultadoActa<ConsultaActasDto>.Invalido("No se encontró el usuario.");
        }

        var (paginaOk, tamanoOk) = Paginacion.Normalizar(pagina, tamano);
        var consulta = Filtrar(
            _context.Actas.AsNoTracking(),
            desde,
            hasta,
            arquitectoId,
            revisorId,
            contratistaId,
            construccionId,
            cerrada,
            actor);
        consulta = FiltroNumero(consulta, numero);
        consulta = FiltroNombre(consulta, acta => acta.Arquitecto.Nombre, nombreArquitecto);
        consulta = FiltroNombre(consulta, acta => acta.Revisor.Nombre, nombreRevisor);
        consulta = FiltroNombre(consulta, acta => acta.Contratista.Nombre, nombreContratista);
        consulta = OrdenarConsulta(consulta, orden, descendente);

        var total = await consulta.CountAsync(cancellationToken);
        var filas = await consulta
            .Skip((paginaOk - 1) * tamanoOk)
            .Take(tamanoOk)
            .Select(acta => new FilaActa(
                acta.NumeroActa,
                acta.FechaInicial,
                acta.FechaFinal,
                acta.Contratista.Nombre,
                acta.Observacion,
                acta.Cerrada,
                acta.Arquitecto.Nombre,
                acta.Revisor.Nombre))
            .ToListAsync(cancellationToken);

        var ids = filas.Select(fila => fila.NumeroActa).ToList();
        var totales = ids.Count == 0
            ? new Dictionary<int, decimal>()
            : await _context.ActaDetalles
                .AsNoTracking()
                .Where(detalle => detalle.Aprobado && ids.Contains(detalle.NumeroActa))
                .GroupBy(detalle => detalle.NumeroActa)
                .Select(grupo => new { Numero = grupo.Key, Total = grupo.Sum(detalle => detalle.Subtotal) })
                .ToDictionaryAsync(grupo => grupo.Numero, grupo => grupo.Total, cancellationToken);

        var actas = filas
            .Select(fila => new ActaResumenDto(
                fila.NumeroActa,
                fila.FechaInicial,
                fila.FechaFinal,
                fila.Contratista,
                fila.Observacion,
                totales.GetValueOrDefault(fila.NumeroActa),
                fila.Cerrada,
                fila.Arquitecto,
                fila.Revisor))
            .ToList();

        return ResultadoActa<ConsultaActasDto>.Ok(
            new ConsultaActasDto(actas, total, paginaOk, tamanoOk, totales.Values.Sum()));
    }

    public async Task<ResultadoActa<ActaDto>> ObtenerAsync(
        int numeroActa,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        var actor = await CargarActorAsync(usuarioId, cancellationToken);
        if (actor is null)
        {
            return ResultadoActa<ActaDto>.Invalido("No se encontró el usuario.");
        }

        var acta = await CargarAsync(numeroActa, cancellationToken);
        return acta is null || !PuedeVer(acta, actor)
            ? ResultadoActa<ActaDto>.NoEncontrado("No se encontró el acta.")
            : ResultadoActa<ActaDto>.Ok(acta);
    }

    public async Task<ResultadoActa<byte[]>> GenerarPdfAsync(
        int numeroActa,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        var acta = await ObtenerAsync(numeroActa, usuarioId, cancellationToken);
        return acta.Estado != EstadoActa.Ok
            ? acta.Estado == EstadoActa.NoEncontrado
                ? ResultadoActa<byte[]>.NoEncontrado(acta.Mensaje ?? "No se encontró el acta.")
                : ResultadoActa<byte[]>.Invalido(acta.Mensaje ?? "No se encontró el usuario.")
            : ResultadoActa<byte[]>.Ok(GeneradorPdfActa.Generar(acta.Valor!));
    }

    public async Task<ResultadoActa<byte[]>> ExportarAsync(
        DateOnly? desde,
        DateOnly? hasta,
        int? arquitectoId,
        int? revisorId,
        int? contratistaId,
        int? construccionId,
        bool? cerrada,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        var acumulado = new List<ActaResumenDto>();
        ConsultaActasDto? primera = null;
        for (var pagina = 1; ; pagina++)
        {
            var consulta = await ConsultarAsync(
                desde, hasta, arquitectoId, revisorId, contratistaId, construccionId, cerrada, usuarioId, cancellationToken,
                pagina, Paginacion.TamanoMaximo);
            if (consulta.Estado != EstadoActa.Ok)
            {
                return ResultadoActa<byte[]>.Invalido(consulta.Mensaje ?? CrearActaValidador.MensajeFechas);
            }

            primera ??= consulta.Valor;
            acumulado.AddRange(consulta.Valor!.Items);
            if (acumulado.Count >= consulta.Valor.Total || consulta.Valor.Items.Count == 0)
            {
                break;
            }
        }

        return ResultadoActa<byte[]>.Ok(GeneradorExcelActas.Generar(
            primera! with { Items = acumulado, TotalGeneral = acumulado.Sum(acta => acta.Total) }));
    }


    private async Task<ActorActa?> CargarActorAsync(int usuarioId, CancellationToken cancellationToken)
    {
        return await _context.Usuarios
            .AsNoTracking()
            .Where(usuario => usuario.Id == usuarioId && usuario.Activo)
            .Select(usuario => new ActorActa(
                usuario.Id,
                usuario.TipoUsuarioId,
                usuario.ArquitectoId,
                usuario.ContratistaId,
                usuario.RevisorId))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static bool PuedeVer(Acta acta, ActorActa actor) =>
        PuedeVer(acta.ArquitectoId, acta.ContratistaId, acta.RevisorId, actor);

    private static bool PuedeVer(ActaDto acta, ActorActa actor) =>
        PuedeVer(acta.ArquitectoId, acta.ContratistaId, acta.RevisorId, actor);

    private static bool PuedeVer(int arquitectoId, int contratistaId, int revisorId, ActorActa actor) =>
        PermisosActa.PuedeVer(actor.TipoUsuarioId, actor.ArquitectoId, actor.ContratistaId, actor.RevisorId, arquitectoId, contratistaId, revisorId);

    private IQueryable<Acta> Filtrar(
        IQueryable<Acta> consulta,
        DateOnly? desde,
        DateOnly? hasta,
        int? arquitectoId,
        int? revisorId,
        int? contratistaId,
        int? construccionId,
        bool? cerrada,
        ActorActa actor)
    {
        consulta = actor.TipoUsuarioId switch
        {
            PerfilesUsuario.Arquitecto => consulta.Where(acta => acta.ArquitectoId == actor.ArquitectoId),
            PerfilesUsuario.Contratista => consulta.Where(acta => acta.ContratistaId == actor.ContratistaId),
            PerfilesUsuario.Revisor => consulta.Where(acta => acta.RevisorId == actor.RevisorId),
            PerfilesUsuario.Administrador => consulta,
            _ => consulta.Where(acta => false)
        };

        if (desde is DateOnly inicio)
        {
            consulta = consulta.Where(acta => acta.FechaFinal >= inicio);
        }

        if (hasta is DateOnly fin)
        {
            consulta = consulta.Where(acta => acta.FechaInicial <= fin);
        }

        if (arquitectoId is > 0)
        {
            consulta = consulta.Where(acta => acta.ArquitectoId == arquitectoId);
        }

        if (revisorId is > 0)
        {
            consulta = consulta.Where(acta => acta.RevisorId == revisorId);
        }

        if (contratistaId is > 0)
        {
            consulta = consulta.Where(acta => acta.ContratistaId == contratistaId);
        }

        if (construccionId is > 0)
        {
            consulta = consulta.Where(acta => acta.Detalles.Any(detalle => detalle.ConstruccionId == construccionId));
        }

        if (cerrada is bool estado)
        {
            consulta = consulta.Where(acta => acta.Cerrada == estado);
        }

        return consulta;
    }

    private async Task<ActaDto?> CargarAsync(int numeroActa, CancellationToken cancellationToken)
    {
        var acta = await _context.Actas
            .AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.Arquitecto)
            .Include(item => item.Revisor)
            .Include(item => item.Contratista)
            .Include(item => item.Detalles).ThenInclude(detalle => detalle.Construccion)
            .Include(item => item.Detalles).ThenInclude(detalle => detalle.Concepto)
            .Include(item => item.Detalles).ThenInclude(detalle => detalle.UnidadMedida)
            .Include(item => item.Detalles).ThenInclude(detalle => detalle.Insumo)
            .Include(item => item.Detalles).ThenInclude(detalle => detalle.TipoUsuario)
            .Include(item => item.Detalles).ThenInclude(detalle => detalle.PresupuestoItem)
            .SingleOrDefaultAsync(item => item.NumeroActa == numeroActa, cancellationToken);

        if (acta is null)
        {
            return null;
        }

        var detalles = acta.Detalles
            .OrderBy(detalle => detalle.Construccion.Descripcion)
            .ThenBy(detalle => detalle.Id)
            .Select(detalle => new LineaActaDto(
                detalle.Id,
                detalle.ConstruccionId,
                detalle.Construccion.Descripcion,
                detalle.ConceptoId,
                detalle.Concepto.Descripcion,
                detalle.UnidadMedidaId,
                detalle.UnidadMedida?.Nombre ?? "",
                detalle.InsumoId,
                detalle.Insumo?.Descripcion,
                detalle.Cantidad,
                detalle.Precio,
                detalle.Descripcion,
                detalle.Aprobado ? detalle.Subtotal : 0,
                detalle.TipoUsuarioId,
                detalle.TipoUsuario.Nombre,
                detalle.Aprobado,
                detalle.PresupuestoItemId,
                detalle.PresupuestoItem is null ? null : detalle.PresupuestoItem.Codigo + " " + detalle.PresupuestoItem.Descripcion))
            .ToList();

        return new ActaDto(
            acta.NumeroActa,
            acta.FechaInicial,
            acta.FechaFinal,
            acta.ArquitectoId,
            acta.Arquitecto.Nombre,
            acta.RevisorId,
            acta.Revisor.Nombre,
            acta.ContratistaId,
            acta.Contratista.Nombre,
            acta.Observacion,
            acta.FechaRegistro,
            acta.UsuarioRegistroId,
            TotalActa(detalles.Select(detalle => (detalle.Subtotal, detalle.Aprobado))),
            detalles,
            acta.Cerrada,
            Convert.ToBase64String(acta.RowVersion ?? []));
    }


    private const string Colacion = "Latin1_General_CI_AI";

    private static readonly MethodInfo LikeMetodo = typeof(DbFunctionsExtensions).GetMethod(
        nameof(DbFunctionsExtensions.Like),
        [typeof(DbFunctions), typeof(string), typeof(string), typeof(string)])!;

    private static readonly MethodInfo ColacionMetodo = typeof(RelationalDbFunctionsExtensions)
        .GetMethods()
        .Single(metodo => metodo.Name == nameof(RelationalDbFunctionsExtensions.Collate)
            && metodo.IsGenericMethodDefinition
            && metodo.GetParameters().Length == 3)
        .MakeGenericMethod(typeof(string));

    private static readonly MethodInfo ReemplazarMetodo = typeof(string).GetMethod(
        nameof(string.Replace),
        [typeof(string), typeof(string)])!;

    private static readonly MethodInfo MinusculasMetodo = typeof(string).GetMethod(
        nameof(string.ToLower),
        Type.EmptyTypes)!;

    private static readonly (string Origen, string Destino)[] Reemplazos =
    [
        ("á", "a"), ("Á", "a"), ("é", "e"), ("É", "e"), ("í", "i"), ("Í", "i"),
        ("ó", "o"), ("Ó", "o"), ("ú", "u"), ("Ú", "u"), ("ü", "u"), ("Ü", "u"),
        ("ñ", "n"), ("Ñ", "n")
    ];

    private IQueryable<Acta> FiltroNumero(IQueryable<Acta> consulta, string? texto)
    {
        var limpio = Normalizar(texto);
        if (limpio.Length == 0)
        {
            return consulta;
        }

        var patron = "%" + Escapar(limpio) + "%";
        return consulta.Where(acta => EF.Functions.Like(acta.NumeroActa.ToString(), patron, "\\"));
    }

    private IQueryable<Acta> FiltroNombre(IQueryable<Acta> consulta, Expression<Func<Acta, string>> selector, string? texto)
    {
        var limpio = Normalizar(texto);
        if (limpio.Length == 0)
        {
            return consulta;
        }

        var patron = "%" + Escapar(limpio) + "%";
        var predicado = _context.Database.IsSqlServer()
            ? LikeColacion(selector, patron)
            : LikePlegado(selector, patron);
        return consulta.Where(predicado);
    }

    private static Expression<Func<Acta, bool>> LikeColacion(Expression<Func<Acta, string>> selector, string patron)
    {
        var valor = Expression.Call(ColacionMetodo, Funciones(), selector.Body, Expression.Constant(Colacion));
        return Expression.Lambda<Func<Acta, bool>>(Like(valor, patron), selector.Parameters);
    }

    private static Expression<Func<Acta, bool>> LikePlegado(Expression<Func<Acta, string>> selector, string patron)
    {
        Expression valor = selector.Body;
        foreach (var (origen, destino) in Reemplazos)
        {
            valor = Expression.Call(valor, ReemplazarMetodo, Expression.Constant(origen), Expression.Constant(destino));
        }

        valor = Expression.Call(valor, MinusculasMetodo);
        return Expression.Lambda<Func<Acta, bool>>(Like(valor, patron), selector.Parameters);
    }

    private static Expression Like(Expression valor, string patron) =>
        Expression.Call(LikeMetodo, Funciones(), valor, Expression.Constant(patron), Expression.Constant("\\"));

    private static Expression Funciones() =>
        Expression.Property(null, typeof(EF).GetProperty(nameof(EF.Functions))!);

    private static IQueryable<Acta> OrdenarConsulta(IQueryable<Acta> consulta, string? orden, bool descendente) =>
        orden switch
        {
            "fechaInicial" => descendente
                ? consulta.OrderByDescending(acta => acta.FechaInicial).ThenByDescending(acta => acta.NumeroActa)
                : consulta.OrderBy(acta => acta.FechaInicial).ThenBy(acta => acta.NumeroActa),
            "fechaFinal" => descendente
                ? consulta.OrderByDescending(acta => acta.FechaFinal).ThenByDescending(acta => acta.NumeroActa)
                : consulta.OrderBy(acta => acta.FechaFinal).ThenBy(acta => acta.NumeroActa),
            "arquitecto" => descendente
                ? consulta.OrderByDescending(acta => acta.Arquitecto.Nombre).ThenByDescending(acta => acta.NumeroActa)
                : consulta.OrderBy(acta => acta.Arquitecto.Nombre).ThenBy(acta => acta.NumeroActa),
            "revisor" => descendente
                ? consulta.OrderByDescending(acta => acta.Revisor.Nombre).ThenByDescending(acta => acta.NumeroActa)
                : consulta.OrderBy(acta => acta.Revisor.Nombre).ThenBy(acta => acta.NumeroActa),
            "contratista" => descendente
                ? consulta.OrderByDescending(acta => acta.Contratista.Nombre).ThenByDescending(acta => acta.NumeroActa)
                : consulta.OrderBy(acta => acta.Contratista.Nombre).ThenBy(acta => acta.NumeroActa),
            _ => descendente
                ? consulta.OrderByDescending(acta => acta.NumeroActa)
                : consulta.OrderBy(acta => acta.NumeroActa),
        };

    private static string Escapar(string texto) =>
        texto.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
            .Replace("[", "\\[", StringComparison.Ordinal);

    private static string Normalizar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return "";
        }

        var forma = valor.Trim().Normalize(NormalizationForm.FormD);
        return string.Concat(forma.Where(caracter => CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark))
            .ToLowerInvariant();
    }

    private sealed record FilaActa(
        int NumeroActa,
        DateOnly FechaInicial,
        DateOnly FechaFinal,
        string Contratista,
        string? Observacion,
        bool Cerrada,
        string Arquitecto,
        string Revisor);

    private static string? ValidarRango(DateOnly? desde, DateOnly? hasta) =>
        desde is DateOnly inicio && hasta is DateOnly fin && fin < inicio
            ? CrearActaValidador.MensajeFechas
            : null;

    private static string? Texto(string? valor)
    {
        var texto = valor?.Trim();
        return string.IsNullOrEmpty(texto) ? null : texto;
    }

    private static async Task<bool> ExisteActivo<TEntidad>(
        IQueryable<TEntidad> consulta,
        int id,
        CancellationToken cancellationToken)
        where TEntidad : class, ICatalogo =>
        await consulta.AnyAsync(entidad => entidad.Id == id && entidad.Activo, cancellationToken);

}
