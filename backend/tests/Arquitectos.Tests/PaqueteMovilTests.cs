using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Arquitectos.Application.Presupuestos;
using Arquitectos.Application.Usuarios;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Movil;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Arquitectos.Tests;

public sealed class EsquemaMovilTests
{
    [Fact]
    public void El_esquema_del_cliente_es_identico_al_recurso_del_backend()
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (directorio is not null && !File.Exists(Path.Combine(directorio.FullName, "backend", "Arquitectos.sln")))
        {
            directorio = directorio.Parent;
        }

        Assert.NotNull(directorio);
        var asset = File.ReadAllText(Path.Combine(directorio!.FullName, "movil", "assets", "esquema_movil_v1.sql"));
        Assert.Equal(EsquemaPaqueteMovil.Leer(), asset);
    }
}

[Collection("AnfitrionApi")]
public sealed class PaqueteMovilTests : IAsyncLifetime
{
    private const string Clave = "Clave12345";

    private readonly SqliteConnection _conexion = new("Data Source=paquete-movil;Mode=Memory;Cache=Shared");
    private WebApplicationFactory<Program> _fabrica = null!;
    private HttpClient _cliente = null!;
    private int _arquitectoId;
    private int _presupuestoPropioId;
    private int _presupuestoAjenoId;
    private string _hashArquitecto = "";

    public async Task InitializeAsync()
    {
        await _conexion.OpenAsync();
        Environment.SetEnvironmentVariable("Jwt__Key", new string('k', 40));
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__Arquitectos",
            "Server=127.0.0.1;Database=Arquitectos;User ID=ci;Password=ci;TrustServerCertificate=True");
        _fabrica = AuthMovilTests.Fabrica(_conexion, limiteLogin: 100);
        using var alcance = _fabrica.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<ArquitectosDbContext>();
        await contexto.Database.EnsureCreatedAsync();
        await SembrarAsync(contexto);
        _cliente = _fabrica.CreateClient();
    }

    [Fact]
    public async Task Paquete_con_version_semver_mas_build_no_responde_426()
    {
        var dispositivo = Guid.NewGuid();
        var ingreso = await LoginAsync("arq.paquete", dispositivo);
        Assert.Equal(HttpStatusCode.OK, ingreso.StatusCode);
        using var json = await JsonDocument.ParseAsync(await ingreso.Content.ReadAsStreamAsync());
        var token = json.RootElement.GetProperty("accessToken").GetString();
        var peticion = new HttpRequestMessage(HttpMethod.Get, "/api/movil/v1/paquete");
        peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        peticion.Headers.TryAddWithoutValidation("X-Version-App", "1.0.0+1");
        peticion.Headers.TryAddWithoutValidation("X-Requested-With", "arquitectos");
        var respuesta = await _cliente.SendAsync(peticion);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        respuesta.Dispose();
        ingreso.Dispose();
    }

    [Fact]
    public async Task Cada_perfil_recibe_solo_sus_actas_y_el_arquitecto_sus_presupuestos()
    {
        var carpeta = Path.Combine(Path.GetTempPath(), "arquitectos-movil");
        var antes = Directory.Exists(carpeta) ? Directory.GetFiles(carpeta).ToHashSet(StringComparer.OrdinalIgnoreCase) : [];
        var dispositivoArquitecto = Guid.NewGuid();
        var dispositivoAjeno = Guid.NewGuid();
        var dispositivoContratista = Guid.NewGuid();
        var dispositivoRevisor = Guid.NewGuid();
        var dispositivoSinPaquete = Guid.NewGuid();

        var arquitecto = await DescargarAsync("arq.paquete", dispositivoArquitecto);
        var ajeno = await DescargarAsync("arq.ajeno", dispositivoAjeno);
        var contratista = await DescargarAsync("con.paquete", dispositivoContratista);
        var revisor = await DescargarAsync("rev.paquete", dispositivoRevisor);
        var ingresoSinPaquete = await LoginAsync("con.ajeno", dispositivoSinPaquete);
        Assert.Equal(HttpStatusCode.OK, ingresoSinPaquete.StatusCode);
        var vieja = await PedirAsync("arq.paquete", dispositivoArquitecto, "0.9.0");

        await using (var propio = await AbrirAsync(arquitecto.Sqlite))
        {
            Assert.Equal(1L, await EscalarAsync(propio, "PRAGMA user_version"));
            Assert.Equal(Canonicas(), await TablasAsync(propio));
            Assert.DoesNotContain(await ColumnasAsync(propio), nombre => nombre.Contains("hash", StringComparison.OrdinalIgnoreCase) || nombre.Contains("password", StringComparison.OrdinalIgnoreCase) || nombre.Contains("contrasena", StringComparison.OrdinalIgnoreCase));
            Assert.Equal(["Linea propia", "Linea reciente"], await TextosAsync(propio, "SELECT descripcion FROM acta_detalles ORDER BY descripcion"));
            Assert.Equal([_presupuestoPropioId], await EnterosAsync(propio, "SELECT id FROM presupuestos ORDER BY id"));
            Assert.Contains("Concepto usado", await TextosAsync(propio, "SELECT descripcion FROM conceptos"));
            Assert.DoesNotContain("Concepto libre inactivo", await TextosAsync(propio, "SELECT descripcion FROM conceptos"));
            Assert.Contains("Concepto activo", await TextosAsync(propio, "SELECT descripcion FROM conceptos"));
            Assert.DoesNotContain("Arquitecto retirado", await TextosAsync(propio, "SELECT nombre FROM arquitectos"));
            Assert.Equal(["Item de otra obra", "Item visible"], await TextosAsync(propio, "SELECT descripcion FROM items_presupuesto_acta ORDER BY descripcion"));
            Assert.Empty(await TextosAsync(propio, "SELECT descripcion FROM items_presupuesto_acta WHERE precio IS NOT NULL"));
            Assert.Equal(0L, await EscalarAsync(propio, "SELECT COUNT(*) FROM outbox"));
            var local = (string)(await EscalarTextoAsync(propio, "SELECT local_id FROM acta_detalles WHERE descripcion = 'Linea propia'"))!;
            Assert.Equal(local, await EscalarTextoAsync(propio, "SELECT id_cliente FROM acta_detalles WHERE descripcion = 'Linea propia'"));
            await IgualQueLaWebAsync(propio, _presupuestoPropioId, _arquitectoId);
        }

        await using (var otro = await AbrirAsync(ajeno.Sqlite))
        {
            Assert.Equal(["Linea ajena"], await TextosAsync(otro, "SELECT descripcion FROM acta_detalles"));
            Assert.Equal([_presupuestoAjenoId], await EnterosAsync(otro, "SELECT id FROM presupuestos"));
        }

        await using (var visita = await AbrirAsync(contratista.Sqlite))
        {
            Assert.Equal(["Linea propia", "Linea reciente"], await TextosAsync(visita, "SELECT descripcion FROM acta_detalles ORDER BY descripcion"));
            Assert.Equal(0L, await EscalarAsync(visita, "SELECT COUNT(*) FROM presupuestos"));
        }

        await using (var visita = await AbrirAsync(revisor.Sqlite))
        {
            Assert.Equal(["Linea propia", "Linea reciente"], await TextosAsync(visita, "SELECT descripcion FROM acta_detalles ORDER BY descripcion"));
            Assert.Equal(0L, await EscalarAsync(visita, "SELECT COUNT(*) FROM presupuestos"));
        }

        Assert.DoesNotContain(_hashArquitecto, Encoding.UTF8.GetString(arquitecto.Sqlite), StringComparison.Ordinal);
        Assert.DoesNotContain("ProveedorQueNoDebeSalir", Encoding.UTF8.GetString(arquitecto.Sqlite), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.UpgradeRequired, vieja.StatusCode);
        vieja.Dispose();

        using var alcance = _fabrica.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<ArquitectosDbContext>();
        var sincronizados = await contexto.Dispositivos.AsNoTracking().Where(item => item.UltimaSincronizacion != null).Select(item => item.Id).ToListAsync();
        Assert.Contains(dispositivoArquitecto, sincronizados);
        Assert.Contains(dispositivoAjeno, sincronizados);
        Assert.Contains(dispositivoContratista, sincronizados);
        Assert.Contains(dispositivoRevisor, sincronizados);
        Assert.DoesNotContain(dispositivoSinPaquete, sincronizados);

        var sobrantes = EsperarArchivosNuevos(carpeta, antes);
        Assert.Empty(sobrantes);
    }

    public async Task DisposeAsync()
    {
        _cliente.Dispose();
        await _fabrica.DisposeAsync();
        await _conexion.DisposeAsync();
        var lectura = Path.Combine(Path.GetTempPath(), "arquitectos-movil-test");
        if (Directory.Exists(lectura))
        {
            Directory.Delete(lectura, true);
        }
    }

    private async Task SembrarAsync(ArquitectosDbContext contexto)
    {
        var arquitecto = new Arquitecto { Nombre = "Arquitecto uno" };
        var otro = new Arquitecto { Nombre = "Arquitecto dos" };
        var retirado = new Arquitecto { Nombre = "Arquitecto retirado", Activo = false };
        var revisor = new Revisor { Nombre = "Revisor uno" };
        var otroRevisor = new Revisor { Nombre = "Revisor dos" };
        var contratista = new Contratista { Nombre = "Contratista uno" };
        var otroContratista = new Contratista { Nombre = "Contratista dos" };
        var obra = new Construccion { Descripcion = "Obra uno" };
        var otraObra = new Construccion { Descripcion = "Obra dos" };
        var obraInactiva = new Construccion { Descripcion = "Obra inactiva", Activo = false };
        var concepto = new Concepto { Descripcion = "Concepto activo", Unidad = "m2" };
        var conceptoUsado = new Concepto { Descripcion = "Concepto usado", Activo = false };
        var conceptoLibre = new Concepto { Descripcion = "Concepto libre inactivo", Activo = false };
        var unidad = new UnidadMedida { Nombre = "m2", Descripcion = "Metro cuadrado" };
        var insumo = new Insumo
        {
            Codigo = "CEM",
            Descripcion = "Cemento",
            Tipo = TipoInsumo.Material,
            UnidadUso = "kg",
            PrecioBase = 1,
            PrecioUnitario = 1,
            FechaPrecio = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        contexto.AddRange(arquitecto, otro, retirado, revisor, otroRevisor, contratista, otroContratista, obra, otraObra, obraInactiva, concepto, conceptoUsado, conceptoLibre, unidad, insumo);
        await contexto.SaveChangesAsync();
        var proveedor = new Proveedor { Nombre = "ProveedorQueNoDebeSalir" };
        contexto.Proveedores.Add(proveedor);
        await contexto.SaveChangesAsync();
        contexto.ProveedorMateriales.Add(new ProveedorMaterial { ProveedorId = proveedor.Id, InsumoId = insumo.Id, Precio = 99 });
        contexto.ParametrosManoObra.Add(new ParametrosManoObra());

        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var cliente = Guid.NewGuid();
        contexto.Actas.AddRange(
            Acta(arquitecto.Id, revisor.Id, contratista.Id, hoy, hoy, false, obra.Id, conceptoUsado.Id, unidad.Id, insumo.Id, "Linea propia", cliente),
            Acta(otro.Id, otroRevisor.Id, otroContratista.Id, hoy, hoy, false, otraObra.Id, concepto.Id, unidad.Id, null, "Linea ajena", null),
            Acta(arquitecto.Id, revisor.Id, contratista.Id, hoy.AddMonths(-2), hoy.AddMonths(-1), true, obra.Id, concepto.Id, unidad.Id, null, "Linea reciente", null),
            Acta(arquitecto.Id, revisor.Id, contratista.Id, hoy.AddMonths(-14), hoy.AddMonths(-13), true, obra.Id, concepto.Id, unidad.Id, null, "Linea vieja", null));
        var propio = PresupuestoDe(obra.Id, "Presupuesto visible", false, EstadoPresupuesto.Aprobado, concepto.Id, unidad.Id, "Item visible");
        var ajenoPresupuesto = PresupuestoDe(otraObra.Id, "Presupuesto ajeno", false, EstadoPresupuesto.Aprobado, concepto.Id, unidad.Id, "Item de otra obra");
        var plantilla = PresupuestoDe(obra.Id, "Plantilla", true, EstadoPresupuesto.Borrador, concepto.Id, unidad.Id, "Item de plantilla");
        var inactivo = PresupuestoDe(obraInactiva.Id, "Presupuesto inactivo", false, EstadoPresupuesto.Aprobado, concepto.Id, unidad.Id, "Item de obra inactiva");
        contexto.Presupuestos.AddRange(propio, ajenoPresupuesto, plantilla, inactivo);
        await contexto.SaveChangesAsync();

        _arquitectoId = await UsuarioAsync("arq.paquete", SemillaInicial.TipoArquitectoId, arquitecto.Id);
        await UsuarioAsync("arq.ajeno", SemillaInicial.TipoArquitectoId, otro.Id);
        await UsuarioAsync("con.paquete", SemillaInicial.TipoContratistaId, contratista.Id);
        await UsuarioAsync("rev.paquete", SemillaInicial.TipoRevisorId, revisor.Id);
        await UsuarioAsync("con.ajeno", SemillaInicial.TipoContratistaId, otroContratista.Id);
        _presupuestoPropioId = propio.Id;
        _presupuestoAjenoId = ajenoPresupuesto.Id;
        _hashArquitecto = (await contexto.Usuarios.AsNoTracking().SingleAsync(item => item.Id == _arquitectoId)).PasswordHash;
    }

    private async Task<int> UsuarioAsync(string nombre, int tipo, int perfilId)
    {
        using var alcance = _fabrica.Services.CreateScope();
        var servicio = alcance.ServiceProvider.GetRequiredService<IUsuarioServicio>();
        var creado = await servicio.CrearAsync(new CrearUsuarioSolicitud
        {
            Usuario = nombre,
            Nombre = nombre,
            Password = Clave,
            TipoUsuarioId = tipo,
            PerfilId = perfilId
        }, CancellationToken.None);
        Assert.Equal(EstadoUsuario.Ok, creado.Estado);
        return creado.Valor!.Id;
    }

    private static Acta Acta(
        int arquitectoId,
        int revisorId,
        int contratistaId,
        DateOnly inicio,
        DateOnly fin,
        bool cerrada,
        int construccionId,
        int conceptoId,
        int unidadId,
        int? insumoId,
        string descripcion,
        Guid? idCliente) =>
        new()
        {
            FechaInicial = inicio,
            FechaFinal = fin,
            Cerrada = cerrada,
            ArquitectoId = arquitectoId,
            RevisorId = revisorId,
            ContratistaId = contratistaId,
            Observacion = "Observación",
            FechaRegistro = new DateTime(fin.Year, fin.Month, fin.Day, 15, 0, 0, DateTimeKind.Utc),
            Detalles =
            [
                new ActaDetalle
                {
                    ConstruccionId = construccionId,
                    ConceptoId = conceptoId,
                    UnidadMedidaId = unidadId,
                    InsumoId = insumoId,
                    Cantidad = 2.5m,
                    Precio = 10.5m,
                    Subtotal = 26.25m,
                    Descripcion = descripcion,
                    TipoUsuarioId = SemillaInicial.TipoArquitectoId,
                    Aprobado = true,
                    IdCliente = idCliente
                }
            ]
        };

    private static Presupuesto PresupuestoDe(int construccionId, string nombre, bool plantilla, EstadoPresupuesto estado, int conceptoId, int unidadId, string item) =>
        new()
        {
            ConstruccionId = construccionId,
            Nombre = nombre,
            EsPlantilla = plantilla,
            Estado = estado,
            Fecha = new DateOnly(2026, 3, 1),
            MesesProyecto = 4,
            AreaConstruidaM2 = 80,
            PctImprevistos = 0.05m,
            PctIncremento = 0.07m,
            PctAdministracion = 0.1m,
            PctUtilidad = 0.05m,
            PctIvaUtilidad = 0.19m,
            Capitulos =
            [
                new PresupuestoCapitulo
                {
                    Numero = 1,
                    Nombre = "Preliminares",
                    Orden = 1,
                    Items =
                    [
                        new PresupuestoItem
                        {
                            Codigo = "01",
                            Descripcion = item,
                            Unidad = "m2",
                            ConceptoId = conceptoId,
                            UnidadMedidaId = unidadId,
                            Cantidad = 3,
                            ValorMaterialUnitario = 100,
                            ValorManoObraUnitario = 40,
                            Orden = 1
                        }
                    ]
                }
            ]
        };

    private async Task<Paquete> DescargarAsync(string usuario, Guid dispositivo)
    {
        var respuesta = await PedirAsync(usuario, dispositivo, "1.0.0");
        var cuerpo = respuesta.StatusCode == HttpStatusCode.OK ? "" : await respuesta.Content.ReadAsStringAsync();
        Assert.True(respuesta.StatusCode == HttpStatusCode.OK, cuerpo);
        Assert.Equal("application/gzip", respuesta.Content.Headers.ContentType?.MediaType);
        Assert.Equal("1", respuesta.Headers.GetValues("X-Paquete-Esquema").Single());
        var sha = respuesta.Headers.GetValues("X-Paquete-Sha256").Single();
        var bytes = await respuesta.Content.ReadAsByteArrayAsync();
        Assert.Equal(bytes.Length, respuesta.Content.Headers.ContentLength);
        respuesta.Dispose();
        await using var gzip = new GZipStream(new MemoryStream(bytes), CompressionMode.Decompress);
        using var plano = new MemoryStream();
        await gzip.CopyToAsync(plano);
        var sqlite = plano.ToArray();
        Assert.Equal(sha, Convert.ToHexString(SHA256.HashData(sqlite)).ToLowerInvariant());
        return new Paquete(sqlite);
    }

    private async Task<HttpResponseMessage> PedirAsync(string usuario, Guid dispositivo, string version)
    {
        var ingreso = await LoginAsync(usuario, dispositivo);
        Assert.Equal(HttpStatusCode.OK, ingreso.StatusCode);
        using var json = await JsonDocument.ParseAsync(await ingreso.Content.ReadAsStreamAsync());
        var token = json.RootElement.GetProperty("accessToken").GetString();
        var peticion = new HttpRequestMessage(HttpMethod.Get, "/api/movil/v1/paquete");
        peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        peticion.Headers.TryAddWithoutValidation("X-Version-App", version);
        peticion.Headers.TryAddWithoutValidation("X-Requested-With", "arquitectos");
        return await _cliente.SendAsync(peticion);
    }

    private Task<HttpResponseMessage> LoginAsync(string usuario, Guid dispositivo)
    {
        var peticion = new HttpRequestMessage(HttpMethod.Post, "/api/movil/v1/auth/login")
        {
            Content = JsonContent.Create(new
            {
                usuario,
                contrasena = Clave,
                dispositivo = new { id = dispositivo, nombre = "Teléfono de obra", plataforma = "android", versionApp = "1.0.0" }
            })
        };
        peticion.Headers.TryAddWithoutValidation("X-Requested-With", "arquitectos");
        peticion.Headers.TryAddWithoutValidation("X-Version-App", "1.0.0");
        return _cliente.SendAsync(peticion);
    }

    private async Task IgualQueLaWebAsync(SqliteConnection conexion, int presupuestoId, int usuarioId)
    {
        using var alcance = _fabrica.Services.CreateScope();
        var servicio = alcance.ServiceProvider.GetRequiredService<IPresupuestoServicio>();
        var detalle = await servicio.ObtenerAsync(presupuestoId, usuarioId, CancellationToken.None);
        var validacion = await servicio.ValidarAsync(presupuestoId, usuarioId, CancellationToken.None);
        var seguimiento = await servicio.SeguimientoAsync(presupuestoId, usuarioId, CancellationToken.None);
        Assert.Equal(EstadoOperacion.Ok, detalle.Estado);
        Assert.Equal(JsonSerializer.Serialize(detalle.Valor, JsonPaquete.Opciones), await EscalarTextoAsync(conexion, "SELECT detalle_json FROM presupuestos WHERE id = " + presupuestoId));
        Assert.Equal(JsonSerializer.Serialize(validacion.Valor, JsonPaquete.Opciones), await EscalarTextoAsync(conexion, "SELECT validacion_json FROM presupuestos WHERE id = " + presupuestoId));
        Assert.Equal(JsonSerializer.Serialize(seguimiento.Valor, JsonPaquete.Opciones), await EscalarTextoAsync(conexion, "SELECT seguimiento_json FROM presupuestos WHERE id = " + presupuestoId));
    }

    private static async Task<SqliteConnection> AbrirAsync(byte[] sqlite)
    {
        var ruta = Path.Combine(Path.GetTempPath(), "arquitectos-movil-test", Guid.NewGuid().ToString("N") + ".sqlite");
        Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
        await File.WriteAllBytesAsync(ruta, sqlite);
        var conexion = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = ruta, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
        await conexion.OpenAsync();
        return conexion;
    }

    private static async Task<long> EscalarAsync(SqliteConnection conexion, string sql)
    {
        await using var comando = conexion.CreateCommand();
        comando.CommandText = sql;
        return (long)(await comando.ExecuteScalarAsync())!;
    }

    private static async Task<string?> EscalarTextoAsync(SqliteConnection conexion, string sql)
    {
        await using var comando = conexion.CreateCommand();
        comando.CommandText = sql;
        var valor = await comando.ExecuteScalarAsync();
        return valor as string;
    }

    private static async Task<List<string>> TextosAsync(SqliteConnection conexion, string sql)
    {
        await using var comando = conexion.CreateCommand();
        comando.CommandText = sql;
        await using var lector = await comando.ExecuteReaderAsync();
        var filas = new List<string>();
        while (await lector.ReadAsync())
        {
            filas.Add(lector.GetString(0));
        }

        return filas;
    }

    private static async Task<List<int>> EnterosAsync(SqliteConnection conexion, string sql)
    {
        await using var comando = conexion.CreateCommand();
        comando.CommandText = sql;
        await using var lector = await comando.ExecuteReaderAsync();
        var filas = new List<int>();
        while (await lector.ReadAsync())
        {
            filas.Add(lector.GetInt32(0));
        }

        return filas;
    }

    private static async Task<List<string>> TablasAsync(SqliteConnection conexion) =>
        await TextosAsync(conexion, "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name");

    private static async Task<List<string>> ColumnasAsync(SqliteConnection conexion)
    {
        var tablas = await TablasAsync(conexion);
        var columnas = new List<string>();
        foreach (var tabla in tablas)
        {
            columnas.AddRange(await TextosAsync(conexion, "SELECT name FROM pragma_table_info('" + tabla + "')"));
        }

        return columnas;
    }

    private static string[] Canonicas() =>
    [
        "acta_detalles",
        "actas",
        "arquitectos",
        "conceptos",
        "construcciones",
        "contratistas",
        "insumos",
        "items_presupuesto_acta",
        "meta",
        "outbox",
        "presupuestos",
        "revisores",
        "tipos_usuario",
        "unidades_medida",
        "usuario"
    ];

    private static string[] EsperarArchivosNuevos(string carpeta, HashSet<string> antes)
    {
        var nuevos = Array.Empty<string>();
        for (var intento = 0; intento < 20; intento++)
        {
            var actuales = Directory.Exists(carpeta) ? Directory.GetFiles(carpeta) : [];
            nuevos = actuales.Where(archivo => !antes.Contains(archivo)).ToArray();
            if (nuevos.Length == 0)
            {
                return nuevos;
            }

            Thread.Sleep(50);
        }

        return nuevos;
    }

    private sealed record Paquete(byte[] Sqlite);
}
