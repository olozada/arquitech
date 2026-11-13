using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Arquitectos.Application.Usuarios;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Movil;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Arquitectos.Tests;

[Collection("AnfitrionApi")]
public sealed class SincronizacionMovilTests : IAsyncLifetime
{
    private const string Clave = "Clave12345";

    private readonly SqliteConnection _conexion = new("Data Source=sincronizacion-movil;Mode=Memory;Cache=Shared");
    private WebApplicationFactory<Program> _fabrica = null!;
    private HttpClient _cliente = null!;
    private int _construccionId;
    private int _conceptoId;
    private int _actaAbierta;
    private int _actaCerrada;
    private Guid _dispositivo;

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
        var arquitecto = new Arquitecto { Nombre = "Arquitecto sync" };
        var revisor = new Revisor { Nombre = "Revisor sync" };
        var contratista = new Contratista { Nombre = "Contratista sync" };
        var obra = new Construccion { Descripcion = "Obra sync" };
        var concepto = new Concepto { Descripcion = "Concepto sync", Unidad = "m2" };
        contexto.AddRange(arquitecto, revisor, contratista, obra, concepto);
        await contexto.SaveChangesAsync();
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var abierta = new Acta
        {
            FechaInicial = hoy,
            FechaFinal = hoy,
            ArquitectoId = arquitecto.Id,
            RevisorId = revisor.Id,
            ContratistaId = contratista.Id,
            FechaRegistro = DateTime.UtcNow
        };
        var cerrada = new Acta
        {
            FechaInicial = hoy,
            FechaFinal = hoy,
            Cerrada = true,
            ArquitectoId = arquitecto.Id,
            RevisorId = revisor.Id,
            ContratistaId = contratista.Id,
            FechaRegistro = DateTime.UtcNow
        };
        contexto.Actas.AddRange(abierta, cerrada);
        await contexto.SaveChangesAsync();
        var servicio = alcance.ServiceProvider.GetRequiredService<IUsuarioServicio>();
        var creado = await servicio.CrearAsync(new CrearUsuarioSolicitud
        {
            Usuario = "arq.sync",
            Nombre = "Arquitecto sync",
            Password = Clave,
            TipoUsuarioId = SemillaInicial.TipoArquitectoId,
            PerfilId = arquitecto.Id
        }, CancellationToken.None);
        Assert.Equal(EstadoUsuario.Ok, creado.Estado);
        _construccionId = obra.Id;
        _conceptoId = concepto.Id;
        _actaAbierta = abierta.NumeroActa;
        _actaCerrada = cerrada.NumeroActa;
        _dispositivo = Guid.NewGuid();
        _cliente = _fabrica.CreateClient();
        var ingreso = await LoginAsync();
        Assert.Equal(HttpStatusCode.OK, ingreso.StatusCode);
        ingreso.Dispose();
    }

    [Fact]
    public async Task El_mismo_lote_dos_veces_devuelve_el_mismo_resultado_sin_duplicar()
    {
        var idOperacion = Guid.NewGuid();
        var idCliente = Guid.NewGuid();
        var cuerpo = Lote(Operacion("AgregarLinea", 1, idOperacion, _actaAbierta, idCliente, "Muro nuevo"));
        var primera = await EnviarAsync(cuerpo);
        var segunda = await EnviarAsync(cuerpo);
        var texto1 = await primera.Content.ReadAsStringAsync();
        var texto2 = await segunda.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, primera.StatusCode);
        Assert.Equal(HttpStatusCode.OK, segunda.StatusCode);
        Assert.Equal(texto1, texto2);
        using var json = JsonDocument.Parse(texto1);
        Assert.Equal("Aplicada", json.RootElement.GetProperty("resultados")[0].GetProperty("estado").GetString());
        Assert.True(json.RootElement.GetProperty("requiereNuevoPaquete").GetBoolean());
        using var alcance = _fabrica.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<ArquitectosDbContext>();
        Assert.Equal(1, await contexto.ActaDetalles.CountAsync(detalle => detalle.IdCliente == idCliente));
        Assert.Equal(1, await contexto.OperacionesMovil.CountAsync(item => item.IdOperacion == idOperacion));
    }

    [Fact]
    public async Task Una_edicion_dependiente_de_un_agregado_rechazado_no_se_aplica()
    {
        var cliente = Guid.NewGuid();
        var cuerpo = Lote(
            Operacion("AgregarLinea", 1, Guid.NewGuid(), _actaCerrada, cliente, "No entra"),
            Operacion("EditarLinea", 2, Guid.NewGuid(), _actaCerrada, cliente, "Tampoco"));
        var respuesta = await EnviarAsync(cuerpo);
        using var json = await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync());
        var resultados = json.RootElement.GetProperty("resultados");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("Rechazada", resultados[0].GetProperty("estado").GetString());
        Assert.Contains("cerrada", resultados[0].GetProperty("mensaje").GetString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Rechazada", resultados[1].GetProperty("estado").GetString());
        Assert.Equal(SincronizadorMovil.MensajeDependencia, resultados[1].GetProperty("mensaje").GetString());
        respuesta.Dispose();
    }

    [Fact]
    public async Task Una_version_distinta_responde_conflicto_con_la_linea_actual()
    {
        var cliente = Guid.NewGuid();
        var alta = await EnviarAsync(Lote(Operacion("AgregarLinea", 1, Guid.NewGuid(), _actaAbierta, cliente, "Original")));
        using var creada = await JsonDocument.ParseAsync(await alta.Content.ReadAsStreamAsync());
        var aplicada = creada.RootElement.GetProperty("resultados")[0];
        var version = aplicada.GetProperty("rowVersion").GetString();
        var lineaId = aplicada.GetProperty("lineaId").GetInt32();
        alta.Dispose();
        using (var alcance = _fabrica.Services.CreateScope())
        {
            var contexto = alcance.ServiceProvider.GetRequiredService<ArquitectosDbContext>();
            var detalle = await contexto.ActaDetalles.SingleAsync(item => item.Id == lineaId);
            detalle.Descripcion = "Cambiada en la web";
            await contexto.SaveChangesAsync();
        }

        var edicion = Operacion("EditarLinea", 1, Guid.NewGuid(), _actaAbierta, cliente, "Desde el móvil");
        edicion["rowVersionLinea"] = version;
        edicion["lineaId"] = lineaId;
        var respuesta = await EnviarAsync(Lote(edicion));
        using var json = await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync());
        var resultado = json.RootElement.GetProperty("resultados")[0];

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("Conflicto", resultado.GetProperty("estado").GetString());
        Assert.Equal("Otra persona modificó esta línea.", resultado.GetProperty("mensaje").GetString());
        Assert.Equal("Cambiada en la web", resultado.GetProperty("lineaServidor").GetProperty("descripcion").GetString());
        respuesta.Dispose();
    }

    [Fact]
    public async Task Editar_y_eliminar_por_idCliente_encuentran_la_linea_de_un_lote_anterior()
    {
        var cliente = Guid.NewGuid();
        var alta = await EnviarAsync(Lote(Operacion("AgregarLinea", 1, Guid.NewGuid(), _actaAbierta, cliente, "Por cliente")));
        using var creada = await JsonDocument.ParseAsync(await alta.Content.ReadAsStreamAsync());
        var version = creada.RootElement.GetProperty("resultados")[0].GetProperty("rowVersion").GetString();
        alta.Dispose();

        var edicion = Operacion("EditarLinea", 1, Guid.NewGuid(), _actaAbierta, cliente, "Editada por cliente");
        edicion["rowVersionLinea"] = version;
        var editada = await EnviarAsync(Lote(edicion));
        using var jsonEdicion = await JsonDocument.ParseAsync(await editada.Content.ReadAsStreamAsync());
        var resultadoEdicion = jsonEdicion.RootElement.GetProperty("resultados")[0];
        var versionNueva = resultadoEdicion.GetProperty("rowVersion").GetString();
        editada.Dispose();

        var baja = Operacion("EliminarLinea", 1, Guid.NewGuid(), _actaAbierta, cliente, null);
        baja.Remove("linea");
        baja["rowVersionLinea"] = versionNueva;
        var eliminada = await EnviarAsync(Lote(baja));
        using var jsonBaja = await JsonDocument.ParseAsync(await eliminada.Content.ReadAsStreamAsync());

        Assert.Equal("Aplicada", resultadoEdicion.GetProperty("estado").GetString());
        Assert.True(resultadoEdicion.GetProperty("lineaId").GetInt32() > 0);
        Assert.Equal("Aplicada", jsonBaja.RootElement.GetProperty("resultados")[0].GetProperty("estado").GetString());
        using var alcance = _fabrica.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<ArquitectosDbContext>();
        Assert.Equal(0, await contexto.ActaDetalles.CountAsync(detalle => detalle.IdCliente == cliente));
        eliminada.Dispose();
    }

    [Fact]
    public async Task Un_lote_con_mas_de_500_operaciones_responde_400()
    {
        var operaciones = Enumerable.Range(1, 501)
            .Select(secuencia => Operacion("AgregarLinea", secuencia, Guid.NewGuid(), _actaAbierta, Guid.NewGuid(), "De más"))
            .ToArray();
        var respuesta = await EnviarAsync(Lote(operaciones));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        respuesta.Dispose();
    }

    public async Task DisposeAsync()
    {
        _cliente.Dispose();
        await _fabrica.DisposeAsync();
        await _conexion.DisposeAsync();
    }

    private Dictionary<string, object?> Lote(params Dictionary<string, object?>[] operaciones) =>
        new()
        {
            ["dispositivoId"] = _dispositivo,
            ["versionEsquema"] = 1,
            ["operaciones"] = operaciones
        };

    private Dictionary<string, object?> Operacion(string tipo, int secuencia, Guid idOperacion, int numeroActa, Guid idCliente, string? descripcion) =>
        new()
        {
            ["idOperacion"] = idOperacion,
            ["secuencia"] = secuencia,
            ["tipo"] = tipo,
            ["numeroActa"] = numeroActa,
            ["idCliente"] = idCliente,
            ["creadaEn"] = DateTime.UtcNow,
            ["linea"] = descripcion is null
                ? null
                : new Dictionary<string, object?>
                {
                    ["construccionId"] = _construccionId,
                    ["conceptoId"] = _conceptoId,
                    ["cantidad"] = "1.5000",
                    ["precio"] = "10.00",
                    ["descripcion"] = descripcion
                }
        };

    private async Task<HttpResponseMessage> EnviarAsync(object cuerpo)
    {
        var ingreso = await LoginAsync();
        using var sesion = await JsonDocument.ParseAsync(await ingreso.Content.ReadAsStreamAsync());
        var token = sesion.RootElement.GetProperty("accessToken").GetString();
        ingreso.Dispose();
        var peticion = new HttpRequestMessage(HttpMethod.Post, "/api/movil/v1/sincronizar")
        {
            Content = JsonContent.Create(cuerpo)
        };
        peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        peticion.Headers.TryAddWithoutValidation("X-Version-App", "1.0.0");
        peticion.Headers.TryAddWithoutValidation("X-Requested-With", "arquitectos");
        return await _cliente.SendAsync(peticion);
    }

    private Task<HttpResponseMessage> LoginAsync()
    {
        var peticion = new HttpRequestMessage(HttpMethod.Post, "/api/movil/v1/auth/login")
        {
            Content = JsonContent.Create(new
            {
                usuario = "arq.sync",
                contrasena = Clave,
                dispositivo = new { id = _dispositivo, nombre = "Teléfono de obra", plataforma = "android", versionApp = "1.0.0" }
            })
        };
        peticion.Headers.TryAddWithoutValidation("X-Requested-With", "arquitectos");
        peticion.Headers.TryAddWithoutValidation("X-Version-App", "1.0.0");
        return _cliente.SendAsync(peticion);
    }
}
