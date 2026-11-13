using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Arquitectos.Application.Actas;
using Arquitectos.Application.Movil;
using Arquitectos.Application.Presupuestos;
using Arquitectos.Application.Usuarios;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arquitectos.Infrastructure.Movil;

public sealed class GeneradorPaqueteMovil : IGeneradorPaqueteMovil
{
    private const string VersionApi = "v1";

    private readonly ArquitectosDbContext _context;
    private readonly IPresupuestoServicio _presupuestos;
    private readonly OpcionesMovil _opciones;
    private readonly TimeProvider _reloj;
    private readonly CupoGeneracionMovil _cupo;
    private readonly ILogger<GeneradorPaqueteMovil> _logger;

    public GeneradorPaqueteMovil(
        ArquitectosDbContext context,
        IPresupuestoServicio presupuestos,
        IOptions<OpcionesMovil> opciones,
        TimeProvider reloj,
        CupoGeneracionMovil cupo,
        ILogger<GeneradorPaqueteMovil> logger)
    {
        _context = context;
        _presupuestos = presupuestos;
        _opciones = opciones.Value;
        _reloj = reloj;
        _cupo = cupo;
        _logger = logger;
    }

    public async Task<ResultadoPaquete> GenerarAsync(int usuarioId, long? sesionId, CancellationToken cancellationToken)
    {
        await _cupo.EsperarAsync(cancellationToken);
        var cronometro = Stopwatch.StartNew();
        var sqlite = Path.Combine(ArchivosPaqueteMovil.Directorio(), $"{Guid.NewGuid():N}.sqlite");
        var comprimido = sqlite + ".gz";
        FileStream? entrega = null;
        try
        {
            var generado = _reloj.GetUtcNow().UtcDateTime;
            var datos = await CargarAsync(usuarioId, generado, cancellationToken);
            if (datos.Error is ResultadoPaquete error)
            {
                return error;
            }

            await EscribirAsync(sqlite, datos, cancellationToken);
            var bytes = new FileInfo(sqlite).Length;
            var maximo = (long)_opciones.TamanoMaximoMb * 1024 * 1024;
            if (bytes > maximo)
            {
                _logger.LogWarning(
                    "Paquete móvil descartado para el usuario {UsuarioId}: {Bytes} bytes superan el máximo.",
                    usuarioId,
                    bytes);
                return ResultadoPaquete.DemasiadoGrande("El paquete supera el tamaño máximo.");
            }

            var sha = await HuellaAsync(sqlite, cancellationToken);
            await ComprimirAsync(sqlite, comprimido, cancellationToken);
            await MarcarDispositivoAsync(usuarioId, sesionId, generado, cancellationToken);
            entrega = new FileStream(
                comprimido,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                81920,
                FileOptions.Asynchronous | FileOptions.DeleteOnClose);
            cronometro.Stop();
            _logger.LogInformation(
                "Paquete móvil generado para el usuario {UsuarioId}. Filas {@Filas}. Bytes {Bytes}. Duración {DuracionMs} ms.",
                usuarioId,
                datos.Filas,
                bytes,
                cronometro.ElapsedMilliseconds);
            return ResultadoPaquete.Ok(entrega, sha, generado, _opciones.VersionEsquema);
        }
        finally
        {
            ArchivosPaqueteMovil.Borrar(sqlite);
            if (entrega is null)
            {
                ArchivosPaqueteMovil.Borrar(comprimido);
            }

            _cupo.Liberar();
        }
    }

    private async Task<DatosPaquete> CargarAsync(int usuarioId, DateTime generado, CancellationToken cancellationToken)
    {
        var usuario = await _context.Usuarios.AsNoTracking()
            .Where(item => item.Id == usuarioId)
            .Select(item => new UsuarioPaquete(
                item.Id,
                item.Nombre,
                item.TipoUsuarioId,
                item.ArquitectoId,
                item.ContratistaId,
                item.RevisorId,
                item.Activo))
            .FirstOrDefaultAsync(cancellationToken);
        if (usuario is null || !usuario.Activo)
        {
            return DatosPaquete.Fallo(ResultadoPaquete.NoAutorizado("La sesión no es válida."));
        }

        if (!PerfilesUsuario.EsOperativo(usuario.TipoUsuarioId))
        {
            return DatosPaquete.Fallo(ResultadoPaquete.Prohibido("La aplicación móvil no está disponible para el administrador."));
        }

        var desde = DateOnly.FromDateTime(generado).AddMonths(-_opciones.MesesHistorial);
        var consulta = _context.Actas.AsNoTracking()
            .Where(acta => !acta.Cerrada || acta.FechaFinal >= desde);
        consulta = usuario.TipoUsuarioId switch
        {
            PerfilesUsuario.Arquitecto => consulta.Where(acta => acta.ArquitectoId == usuario.ArquitectoId),
            PerfilesUsuario.Contratista => consulta.Where(acta => acta.ContratistaId == usuario.ContratistaId),
            PerfilesUsuario.Revisor => consulta.Where(acta => acta.RevisorId == usuario.RevisorId),
            _ => consulta.Where(_ => false)
        };
        var actas = await consulta
            .Include(acta => acta.Detalles)
            .OrderBy(acta => acta.NumeroActa)
            .ToListAsync(cancellationToken);
        actas = actas
            .Where(acta => PermisosActa.PuedeVer(
                usuario.TipoUsuarioId,
                usuario.ArquitectoId,
                usuario.ContratistaId,
                usuario.RevisorId,
                acta.ArquitectoId,
                acta.ContratistaId,
                acta.RevisorId))
            .ToList();

        var datos = new DatosPaquete { Usuario = usuario, Generado = generado, Actas = actas };
        datos.Tipos = await _context.TiposUsuario.AsNoTracking().OrderBy(item => item.Id)
            .Select(item => new FilaIdNombre(item.Id, item.Nombre))
            .ToListAsync(cancellationToken);
        datos.Arquitectos = await PersonasAsync(_context.Arquitectos, actas.Select(acta => acta.ArquitectoId), cancellationToken);
        datos.Revisores = await PersonasAsync(_context.Revisores, actas.Select(acta => acta.RevisorId), cancellationToken);
        datos.Contratistas = await PersonasAsync(_context.Contratistas, actas.Select(acta => acta.ContratistaId), cancellationToken);
        datos.Construcciones = await ConstruccionesAsync(Ids(actas.SelectMany(acta => acta.Detalles.Select(detalle => detalle.ConstruccionId))), cancellationToken);
        datos.Conceptos = await ConceptosAsync(Ids(actas.SelectMany(acta => acta.Detalles.Select(detalle => detalle.ConceptoId))), cancellationToken);
        datos.Unidades = await UnidadesAsync(Ids(actas.SelectMany(acta => acta.Detalles.Where(detalle => detalle.UnidadMedidaId != null).Select(detalle => detalle.UnidadMedidaId!.Value))), cancellationToken);
        datos.Insumos = await InsumosAsync(Ids(actas.SelectMany(acta => acta.Detalles.Where(detalle => detalle.InsumoId != null).Select(detalle => detalle.InsumoId!.Value))), cancellationToken);
        datos.Items = await ItemsActaAsync(cancellationToken);
        if (usuario.TipoUsuarioId == PerfilesUsuario.Arquitecto)
        {
            datos.Presupuestos = await PresupuestosAsync(usuarioId, cancellationToken);
        }

        return datos;
    }

    private async Task<List<FilaIdNombre>> PersonasAsync<T>(DbSet<T> origen, IEnumerable<int> referenciados, CancellationToken cancellationToken)
        where T : class, IConNombre
    {
        var ids = Ids(referenciados);
        var consulta = origen.AsNoTracking().Where(item => item.Activo || ids.Contains(item.Id));
        return await consulta.OrderBy(item => item.Id)
            .Select(item => new FilaIdNombre(item.Id, item.Nombre))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<FilaIdNombre>> ConstruccionesAsync(HashSet<int> referenciados, CancellationToken cancellationToken)
    {
        var consulta = _context.Construcciones.AsNoTracking()
            .Where(item => item.Activo || referenciados.Contains(item.Id));
        return await consulta.OrderBy(item => item.Id)
            .Select(item => new FilaIdNombre(item.Id, item.Descripcion))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<FilaConcepto>> ConceptosAsync(HashSet<int> referenciados, CancellationToken cancellationToken) =>
        await _context.Conceptos.AsNoTracking()
            .Where(item => item.Activo || referenciados.Contains(item.Id))
            .OrderBy(item => item.Id)
            .Select(item => new FilaConcepto(item.Id, item.Codigo, item.Descripcion, item.Unidad))
            .ToListAsync(cancellationToken);

    private async Task<List<FilaIdNombre>> UnidadesAsync(HashSet<int> referenciados, CancellationToken cancellationToken)
    {
        var filas = await _context.UnidadesMedida.AsNoTracking()
            .Where(item => item.Activo || referenciados.Contains(item.Id))
            .OrderBy(item => item.Id)
            .Select(item => new { item.Id, item.Nombre, item.Descripcion })
            .ToListAsync(cancellationToken);
        return filas
            .Select(item => new FilaIdNombre(item.Id, string.IsNullOrWhiteSpace(item.Descripcion) ? item.Nombre : item.Descripcion))
            .ToList();
    }

    private async Task<List<FilaInsumo>> InsumosAsync(HashSet<int> referenciados, CancellationToken cancellationToken)
    {
        var filas = await _context.Insumos.AsNoTracking()
            .Where(item => item.Activo || referenciados.Contains(item.Id))
            .OrderBy(item => item.Id)
            .Select(item => new { item.Id, item.Codigo, item.Descripcion, item.UnidadUso })
            .ToListAsync(cancellationToken);
        return filas
            .Select(item => new FilaInsumo(item.Id, item.Codigo, item.Descripcion, string.IsNullOrWhiteSpace(item.UnidadUso) ? null : item.UnidadUso))
            .ToList();
    }

    private async Task<List<FilaItemActa>> ItemsActaAsync(CancellationToken cancellationToken)
    {
        var obras = await _context.Construcciones.AsNoTracking()
            .Where(item => item.Activo)
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);
        var items = new List<FilaItemActa>();
        foreach (var obra in obras)
        {
            foreach (var item in await _presupuestos.ItemsParaActaAsync(obra, false, cancellationToken))
            {
                items.Add(new FilaItemActa(item.Id, obra, item.Codigo, item.Descripcion, item.Unidad, item.ConceptoId, item.UnidadMedidaId, item.Precio));
            }
        }

        return items;
    }

    private async Task<List<FilaPresupuesto>> PresupuestosAsync(int usuarioId, CancellationToken cancellationToken)
    {
        var lista = await _presupuestos.ListarAsync(null, usuarioId, cancellationToken);
        if (lista.Estado != EstadoOperacion.Ok || lista.Valor is null)
        {
            return [];
        }

        var filas = new List<FilaPresupuesto>();
        foreach (var resumen in lista.Valor)
        {
            var detalle = await _presupuestos.ObtenerAsync(resumen.Id, usuarioId, cancellationToken);
            if (detalle.Estado != EstadoOperacion.Ok || detalle.Valor is null)
            {
                throw new InvalidOperationException($"No se pudo armar el presupuesto {resumen.Id}.");
            }

            var validacion = await _presupuestos.ValidarAsync(resumen.Id, usuarioId, cancellationToken);
            var seguimiento = await _presupuestos.SeguimientoAsync(resumen.Id, usuarioId, cancellationToken);
            filas.Add(new FilaPresupuesto(
                resumen.Id,
                resumen.ConstruccionId,
                resumen.Construccion,
                resumen.Nombre,
                resumen.Version,
                resumen.Estado,
                Texto(resumen.Total),
                JsonSerializer.Serialize(detalle.Valor, JsonPaquete.Opciones),
                validacion.Estado == EstadoOperacion.Ok ? JsonSerializer.Serialize(validacion.Valor, JsonPaquete.Opciones) : null,
                seguimiento.Estado == EstadoOperacion.Ok ? JsonSerializer.Serialize(seguimiento.Valor, JsonPaquete.Opciones) : null));
        }

        return filas;
    }

    private async Task EscribirAsync(string ruta, DatosPaquete datos, CancellationToken cancellationToken)
    {
        var cadena = new SqliteConnectionStringBuilder
        {
            DataSource = ruta,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Private,
            Pooling = false
        }.ToString();
        await using var conexion = new SqliteConnection(cadena);
        await conexion.OpenAsync(cancellationToken);
        await EjecutarAsync(conexion, null, EsquemaPaqueteMovil.Leer(), cancellationToken);
        await EjecutarAsync(conexion, null, "PRAGMA foreign_keys = ON;", cancellationToken);
        await using var transaccion = (SqliteTransaction)await conexion.BeginTransactionAsync(cancellationToken);
        var usuario = datos.Usuario;
        await InsertarAsync(conexion, transaccion, "INSERT INTO meta (clave, valor) VALUES ($p0, $p1)",
        [
            ["version_esquema", _opciones.VersionEsquema.ToString(CultureInfo.InvariantCulture)],
            ["generado_en", Instante(datos.Generado)],
            ["usuario_id", usuario.Id.ToString(CultureInfo.InvariantCulture)],
            ["tipo_usuario_id", usuario.TipoUsuarioId.ToString(CultureInfo.InvariantCulture)],
            ["meses_historial", _opciones.MesesHistorial.ToString(CultureInfo.InvariantCulture)],
            ["version_api", VersionApi]
        ], cancellationToken);
        await InsertarAsync(conexion, transaccion,
            "INSERT INTO usuario (id, nombre, tipo_usuario_id, arquitecto_id, contratista_id, revisor_id) VALUES ($p0, $p1, $p2, $p3, $p4, $p5)",
            [[usuario.Id, usuario.Nombre, usuario.TipoUsuarioId, usuario.ArquitectoId, usuario.ContratistaId, usuario.RevisorId]],
            cancellationToken);
        await InsertarAsync(conexion, transaccion, "INSERT INTO tipos_usuario (id, nombre) VALUES ($p0, $p1)", Filas(datos.Tipos), cancellationToken);
        await InsertarAsync(conexion, transaccion, "INSERT INTO arquitectos (id, nombre) VALUES ($p0, $p1)", Filas(datos.Arquitectos), cancellationToken);
        await InsertarAsync(conexion, transaccion, "INSERT INTO revisores (id, nombre) VALUES ($p0, $p1)", Filas(datos.Revisores), cancellationToken);
        await InsertarAsync(conexion, transaccion, "INSERT INTO contratistas (id, nombre) VALUES ($p0, $p1)", Filas(datos.Contratistas), cancellationToken);
        await InsertarAsync(conexion, transaccion, "INSERT INTO construcciones (id, descripcion) VALUES ($p0, $p1)", Filas(datos.Construcciones), cancellationToken);
        await InsertarAsync(conexion, transaccion,
            "INSERT INTO conceptos (id, codigo, descripcion, unidad) VALUES ($p0, $p1, $p2, $p3)",
            datos.Conceptos.Select(item => new object?[] { item.Id, Vacio(item.Codigo), item.Descripcion, Vacio(item.Unidad) }).ToList(),
            cancellationToken);
        await InsertarAsync(conexion, transaccion, "INSERT INTO unidades_medida (id, descripcion) VALUES ($p0, $p1)", Filas(datos.Unidades), cancellationToken);
        await InsertarAsync(conexion, transaccion,
            "INSERT INTO insumos (id, codigo, descripcion, unidad) VALUES ($p0, $p1, $p2, $p3)",
            datos.Insumos.Select(item => new object?[] { item.Id, Vacio(item.Codigo), item.Descripcion, Vacio(item.Unidad) }).ToList(),
            cancellationToken);
        await InsertarAsync(conexion, transaccion,
            "INSERT INTO items_presupuesto_acta (id, construccion_id, codigo, descripcion, unidad, concepto_id, unidad_medida_id, precio) VALUES ($p0, $p1, $p2, $p3, $p4, $p5, $p6, $p7)",
            datos.Items.Select(item => new object?[]
            {
                item.Id, item.ConstruccionId, Vacio(item.Codigo), item.Descripcion, Vacio(item.Unidad), item.ConceptoId, item.UnidadMedidaId, Texto(item.Precio)
            }).ToList(),
            cancellationToken);
        await InsertarAsync(conexion, transaccion,
            "INSERT INTO actas (numero_acta, fecha_inicial, fecha_final, arquitecto_id, revisor_id, contratista_id, observacion, cerrada, fecha_registro, row_version) VALUES ($p0, $p1, $p2, $p3, $p4, $p5, $p6, $p7, $p8, $p9)",
            datos.Actas.Select(acta => new object?[]
            {
                acta.NumeroActa,
                Fecha(acta.FechaInicial),
                Fecha(acta.FechaFinal),
                acta.ArquitectoId,
                acta.RevisorId,
                acta.ContratistaId,
                acta.Observacion,
                acta.Cerrada ? 1 : 0,
                Instante(acta.FechaRegistro),
                Convert.ToBase64String(acta.RowVersion)
            }).ToList(),
            cancellationToken);
        await InsertarAsync(conexion, transaccion,
            """
            INSERT INTO acta_detalles (
              local_id, id, id_cliente, numero_acta, construccion_id, concepto_id, unidad_medida_id, insumo_id, presupuesto_item_id,
              cantidad, precio, subtotal, descripcion, tipo_usuario_id, aprobado, row_version, estado_local)
            VALUES ($p0, $p1, $p2, $p3, $p4, $p5, $p6, $p7, $p8, $p9, $p10, $p11, $p12, $p13, $p14, $p15, $p16)
            """,
            datos.Actas.SelectMany(acta => acta.Detalles.OrderBy(detalle => detalle.Id)).Select(detalle => new object?[]
            {
                detalle.IdCliente is Guid cliente ? cliente.ToString("D") : "s" + detalle.Id.ToString(CultureInfo.InvariantCulture),
                detalle.Id,
                detalle.IdCliente?.ToString("D"),
                detalle.NumeroActa,
                detalle.ConstruccionId,
                detalle.ConceptoId,
                detalle.UnidadMedidaId,
                detalle.InsumoId,
                detalle.PresupuestoItemId,
                Texto(detalle.Cantidad),
                Texto(detalle.Precio),
                Texto(detalle.Subtotal),
                detalle.Descripcion,
                detalle.TipoUsuarioId,
                detalle.Aprobado ? 1 : 0,
                detalle.RowVersion.Length == 0 ? null : Convert.ToBase64String(detalle.RowVersion),
                "sincronizado"
            }).ToList(),
            cancellationToken);
        await InsertarAsync(conexion, transaccion,
            """
            INSERT INTO presupuestos (
              id, construccion_id, construccion, nombre, version, estado, total, actualizado_en, detalle_json, validacion_json, seguimiento_json)
            VALUES ($p0, $p1, $p2, $p3, $p4, $p5, $p6, $p7, $p8, $p9, $p10)
            """,
            datos.Presupuestos.Select(item => new object?[]
            {
                item.Id, item.ConstruccionId, item.Construccion, item.Nombre, item.Version, item.Estado, item.Total, null, item.DetalleJson, item.ValidacionJson, item.SeguimientoJson
            }).ToList(),
            cancellationToken);
        await transaccion.CommitAsync(cancellationToken);
        await EjecutarAsync(conexion, null, $"PRAGMA user_version = {_opciones.VersionEsquema.ToString(CultureInfo.InvariantCulture)};", cancellationToken);
        await EjecutarAsync(conexion, null, "VACUUM;", cancellationToken);
        datos.Filas["meta"] = 6;
        datos.Filas["usuario"] = 1;
        datos.Filas["tipos_usuario"] = datos.Tipos.Count;
        datos.Filas["arquitectos"] = datos.Arquitectos.Count;
        datos.Filas["revisores"] = datos.Revisores.Count;
        datos.Filas["contratistas"] = datos.Contratistas.Count;
        datos.Filas["construcciones"] = datos.Construcciones.Count;
        datos.Filas["conceptos"] = datos.Conceptos.Count;
        datos.Filas["unidades_medida"] = datos.Unidades.Count;
        datos.Filas["insumos"] = datos.Insumos.Count;
        datos.Filas["items_presupuesto_acta"] = datos.Items.Count;
        datos.Filas["actas"] = datos.Actas.Count;
        datos.Filas["acta_detalles"] = datos.Actas.Sum(acta => acta.Detalles.Count);
        datos.Filas["presupuestos"] = datos.Presupuestos.Count;
        datos.Filas["outbox"] = 0;
    }

    private async Task MarcarDispositivoAsync(int usuarioId, long? sesionId, DateTime generado, CancellationToken cancellationToken)
    {
        if (sesionId is not long id)
        {
            return;
        }

        var dispositivoId = await _context.SesionesUsuario.AsNoTracking()
            .Where(sesion => sesion.Id == id && sesion.UsuarioId == usuarioId)
            .Select(sesion => sesion.DispositivoId)
            .FirstOrDefaultAsync(cancellationToken);
        if (dispositivoId is not Guid dispositivo)
        {
            return;
        }

        await _context.Dispositivos
            .Where(item => item.Id == dispositivo && item.UsuarioId == usuarioId && item.RevocadoEn == null)
            .ExecuteUpdateAsync(llamadas => llamadas.SetProperty(item => item.UltimaSincronizacion, generado), cancellationToken);
    }

    private static async Task ComprimirAsync(string origen, string destino, CancellationToken cancellationToken)
    {
        await using var entrada = new FileStream(origen, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using var salida = new FileStream(destino, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
        await using var gzip = new GZipStream(salida, CompressionLevel.SmallestSize);
        await entrada.CopyToAsync(gzip, cancellationToken);
    }

    private static async Task<string> HuellaAsync(string ruta, CancellationToken cancellationToken)
    {
        await using var entrada = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        var hash = await SHA256.HashDataAsync(entrada, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static async Task EjecutarAsync(SqliteConnection conexion, SqliteTransaction? transaccion, string sql, CancellationToken cancellationToken)
    {
        await using var comando = conexion.CreateCommand();
        comando.Transaction = transaccion;
        comando.CommandText = sql;
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertarAsync(
        SqliteConnection conexion,
        SqliteTransaction transaccion,
        string sql,
        IReadOnlyList<object?[]> filas,
        CancellationToken cancellationToken)
    {
        if (filas.Count == 0)
        {
            return;
        }

        await using var comando = conexion.CreateCommand();
        comando.Transaction = transaccion;
        comando.CommandText = sql;
        var parametros = new SqliteParameter[filas[0].Length];
        for (var indice = 0; indice < parametros.Length; indice++)
        {
            parametros[indice] = comando.CreateParameter();
            parametros[indice].ParameterName = "$p" + indice.ToString(CultureInfo.InvariantCulture);
            comando.Parameters.Add(parametros[indice]);
        }

        comando.Prepare();
        foreach (var fila in filas)
        {
            for (var indice = 0; indice < parametros.Length; indice++)
            {
                parametros[indice].Value = fila[indice] ?? DBNull.Value;
            }

            await comando.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static List<object?[]> Filas(IEnumerable<FilaIdNombre> filas) =>
        filas.Select(item => new object?[] { item.Id, item.Nombre }).ToList();

    private static HashSet<int> Ids(IEnumerable<int> origen) => origen.ToHashSet();

    private static string Fecha(DateOnly valor) => valor.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string Instante(DateTime valor)
    {
        var utc = valor.Kind == DateTimeKind.Local ? valor.ToUniversalTime() : DateTime.SpecifyKind(valor, DateTimeKind.Utc);
        return utc.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'", CultureInfo.InvariantCulture);
    }

    private static string Texto(decimal valor) => valor.ToString(CultureInfo.InvariantCulture);

    private static object? Texto(decimal? valor) => valor is decimal numero ? Texto(numero) : null;

    private static string? Vacio(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor;

    private sealed class DatosPaquete
    {
        public ResultadoPaquete? Error { get; private init; }

        public UsuarioPaquete Usuario { get; init; } = null!;

        public DateTime Generado { get; init; }

        public List<Acta> Actas { get; init; } = [];

        public List<FilaIdNombre> Tipos { get; set; } = [];

        public List<FilaIdNombre> Arquitectos { get; set; } = [];

        public List<FilaIdNombre> Revisores { get; set; } = [];

        public List<FilaIdNombre> Contratistas { get; set; } = [];

        public List<FilaIdNombre> Construcciones { get; set; } = [];

        public List<FilaConcepto> Conceptos { get; set; } = [];

        public List<FilaIdNombre> Unidades { get; set; } = [];

        public List<FilaInsumo> Insumos { get; set; } = [];

        public List<FilaItemActa> Items { get; set; } = [];

        public List<FilaPresupuesto> Presupuestos { get; set; } = [];

        public Dictionary<string, int> Filas { get; } = [];

        public static DatosPaquete Fallo(ResultadoPaquete error) => new() { Error = error };
    }

    private sealed record UsuarioPaquete(int Id, string Nombre, int TipoUsuarioId, int? ArquitectoId, int? ContratistaId, int? RevisorId, bool Activo);

    private sealed record FilaIdNombre(int Id, string Nombre);

    private sealed record FilaConcepto(int Id, string? Codigo, string Descripcion, string? Unidad);

    private sealed record FilaInsumo(int Id, string? Codigo, string Descripcion, string? Unidad);

    private sealed record FilaItemActa(int Id, int ConstruccionId, string Codigo, string Descripcion, string Unidad, int? ConceptoId, int? UnidadMedidaId, decimal? Precio);

    private sealed record FilaPresupuesto(
        int Id,
        int ConstruccionId,
        string Construccion,
        string Nombre,
        int Version,
        string Estado,
        string Total,
        string DetalleJson,
        string? ValidacionJson,
        string? SeguimientoJson);
}
