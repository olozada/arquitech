using System.Globalization;
using System.Text;
using System.Threading.RateLimiting;
using Arquitectos.Api.Configuracion;
using Arquitectos.Infrastructure.Seguridad;
using Arquitectos.Application;
using Arquitectos.Application.Auth;
using Arquitectos.Infrastructure;
using Arquitectos.Infrastructure.Configuration;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    ArchivoEnv.AgregarDesarrollo(builder.Configuration, builder.Environment, builder.Environment.ContentRootPath);
    ArchivoEnv.FijarConfianza(builder.Configuration);

    builder.Host.UseSerilog((context, configuration) =>
    {
        var ruta = Path.Combine(context.HostingEnvironment.ContentRootPath, "logs", "arquitectos-.log");
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithCorrelationId()
            .Enrich.With(new UsuarioIdEnricher())
            .WriteTo.Console()
            .WriteTo.File(
                new CompactJsonFormatter(),
                ruta,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                retainedFileTimeLimit: TimeSpan.FromDays(30),
                shared: true);

        var seq = context.Configuration["Observabilidad:Seq"];
        if (!string.IsNullOrWhiteSpace(seq))
        {
            configuration.WriteTo.Seq(seq, apiKey: context.Configuration["Observabilidad:SeqApiKey"]);
        }

        var insights = context.Configuration["Observabilidad:ApplicationInsights"];
        if (!string.IsNullOrWhiteSpace(insights))
        {
            configuration.WriteTo.ApplicationInsights(
                new TelemetryConfiguration { ConnectionString = insights },
                new TraceTelemetryConverter());
        }
    });

    var jwtKey = builder.Configuration["Jwt:Key"];
    if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    {
        var indicacion = builder.Environment.IsProduction()
            ? "En IIS defínela como variable de entorno Jwt__Key (mínimo 32 caracteres)."
            : "Defínela con dotnet user-secrets (mínimo 32 caracteres).";
        throw new InvalidOperationException(
            $"Jwt:Key no está configurada o es demasiado corta. {indicacion}");
    }

    if (builder.Environment.IsProduction() &&
        string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("Arquitectos")))
    {
        throw new InvalidOperationException(
            "ConnectionStrings:Arquitectos no está configurada. En IIS defínela como variable de entorno ConnectionStrings__Arquitectos.");
    }

    var cultura = new CultureInfo("es-CO");
    builder.Services.Configure<RequestLocalizationOptions>(opciones =>
    {
        opciones.DefaultRequestCulture = new RequestCulture(cultura);
        opciones.SupportedCultures = [cultura];
        opciones.SupportedUICultures = [cultura];
    });

    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddHealthChecks()
        .AddSqlServer(
            builder.Configuration.GetConnectionString("Arquitectos")!,
            healthQuery: "SELECT 1;",
            name: "sqlserver",
            timeout: TimeSpan.FromSeconds(3));

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opciones =>
        {
            opciones.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)) { KeyId = "arquitectos" },
                IssuerSigningKeyResolver = (_, _, _, _) =>
                {
                    var clave = builder.Configuration["Jwt:Key"] ?? jwtKey;
                    return [new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave)) { KeyId = "arquitectos" }];
                },
                ClockSkew = TimeSpan.FromMinutes(1),
                NameClaimType = "nombre",
                RoleClaimType = "tipoUsuario"
            };
            opciones.MapInboundClaims = false;
            opciones.Events = new JwtBearerEvents
            {
                OnMessageReceived = contexto =>
                {
                    if (string.IsNullOrEmpty(contexto.Token) &&
                        contexto.Request.Cookies.TryGetValue(CookiesSesion.Acceso, out var acceso))
                    {
                        contexto.Token = acceso;
                    }

                    return Task.CompletedTask;
                },
                OnTokenValidated = async contexto =>
                {
                    var sub = contexto.Principal?.FindFirst("sub")?.Value
                        ?? contexto.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    var sid = contexto.Principal?.FindFirst(ClaimsArquitectos.Sesion)?.Value;
                    if (!int.TryParse(sub, out var usuarioId) || !long.TryParse(sid, out var sesionId))
                    {
                        contexto.Fail("Sesión inválida");
                        return;
                    }

                    var validador = contexto.HttpContext.RequestServices.GetRequiredService<ValidadorSesion>();
                    if (!await validador.VigenteAsync(usuarioId, sesionId, contexto.HttpContext.RequestAborted))
                    {
                        contexto.Fail("Sesión inválida");
                    }
                }
            };
        });

    builder.Services.AddAuthorization(opciones =>
    {
        opciones.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
        Politicas.Registrar(opciones);
    });

    builder.Services.AddRateLimiter(opciones =>
    {
        opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        opciones.OnRejected = async (contexto, cancellationToken) =>
        {
            contexto.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await contexto.HttpContext.Response.WriteAsJsonAsync(
                new
                {
                    title = "Demasiados intentos. Espere un minuto.",
                    detail = "Demasiados intentos. Espere un minuto."
                },
                cancellationToken);
        };
        opciones.AddPolicy("login", contexto => RateLimitPartition.GetFixedWindowLimiter(
            contexto.Connection.RemoteIpAddress?.ToString() ?? "anon",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1)
            }));
    });

    const string politicaCors = "Frontend";
    var origenes = builder.Configuration.GetSection("Cors:Origenes").Get<string[]>() ?? [];
    builder.Services.AddCors(opciones =>
    {
        opciones.AddPolicy(politicaCors, policy =>
        {
            if (origenes.Length == 0)
            {
                return;
            }

            policy.WithOrigins(origenes)
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

    builder.Services.AddControllers(opciones =>
        {
            opciones.ModelBinderProviders.Insert(0, new FechaOpcionalModelBinderProvider());
        })
        .AddJsonOptions(opciones =>
        {
            opciones.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(opciones =>
    {
        opciones.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Arquitectos API",
            Version = "v1"
        });
        opciones.SupportNonNullableReferenceTypes();
        opciones.SchemaFilter<PropiedadesPresentes>();

        opciones.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Description = "Token JWT. Esquema: Bearer {token}",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });

        opciones.AddSecurityRequirement(documento => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", documento)] = []
        });
    });

    var app = builder.Build();

    // En IIS la API queda como aplicación en /api, y ese prefijo pasa a PathBase.
    // Los controladores ya incluyen "api/...", así que la ruta pública sigue siendo /api/auth/login.
    // UseRouting va justo después: si queda antes, la ruta se resuelve sin el prefijo y el login responde 401.
    app.Use((context, next) =>
    {
        var pathBase = context.Request.PathBase;
        if (pathBase.HasValue)
        {
            context.Request.Path = pathBase + context.Request.Path;
            context.Request.PathBase = PathString.Empty;
        }

        return next(context);
    });
    app.UseRouting();

    if (args.Contains("--reset") || args.Contains("--seed-demo") || args.Contains("--seed-plantilla"))
    {
        var resultado = GuardaDatosDemo.Evaluar(
            app.Environment.IsDevelopment(),
            app.Configuration.GetConnectionString("Arquitectos"),
            app.Configuration.GetSection("Demo:SufijosPermitidos").Get<string[]>(),
            args.Contains("--reset"),
            args.Contains("--confirmar"));
        if (resultado.CodigoSalida != 0)
        {
            Log.Error("{Mensaje}", resultado.Mensaje);
            Environment.ExitCode = resultado.CodigoSalida;
            return;
        }

        Log.Information("{Mensaje}", resultado.Mensaje);
    }

    if (args.Contains("--reset") || args.Contains("--seed-demo"))
    {
        await using var alcance = app.Services.CreateAsyncScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<ArquitectosDbContext>();
        if (args.Contains("--reset"))
        {
            await ReinicioDatos.VaciarYSembrarInicialAsync(contexto);
            Log.Information("Se vaciaron las tablas y se restauró el usuario admin y el menú.");
        }

        var mensaje = await SemillaDemo.AplicarAsync(contexto);
        Log.Information(mensaje);
        return;
    }

    if (args.Contains("--seed-plantilla"))
    {
        await using var alcancePlantilla = app.Services.CreateAsyncScope();
        var contextoPlantilla = alcancePlantilla.ServiceProvider.GetRequiredService<ArquitectosDbContext>();
        Log.Information(await SemillaPlantilla.AplicarAsync(contextoPlantilla));
        return;
    }

    if (args.Contains("--importar-murillo"))
    {
        if (!app.Environment.IsDevelopment())
        {
            throw new InvalidOperationException("La importación de Casa Murillo solo está disponible en Development.");
        }

        await using var alcanceMurillo = app.Services.CreateAsyncScope();
        var contextoMurillo = alcanceMurillo.ServiceProvider.GetRequiredService<ArquitectosDbContext>();
        Log.Information(await SemillaCasaMurillo.AplicarAsync(contextoMurillo));
        return;
    }

    app.Use(async (contexto, siguiente) =>
    {
        Correlacion.Asegurar(contexto);
        await siguiente();
    });
    app.UseExceptionHandler();
    app.UseSerilogRequestLogging(opciones =>
    {
        opciones.MessageTemplate = "HTTP {RequestMethod} {RequestPath} respondió {StatusCode} en {Elapsed:0.0000} ms";
        opciones.GetLevel = (http, _, excepcion) =>
            excepcion is not null || http.Response.StatusCode >= 500
                ? LogEventLevel.Error
                : LogEventLevel.Information;
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseRequestLocalization();
    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    app.Use(async (contexto, siguiente) =>
    {
        var cabeceras = contexto.Response.Headers;
        cabeceras["X-Content-Type-Options"] = "nosniff";
        cabeceras["X-Frame-Options"] = "DENY";
        cabeceras["Referrer-Policy"] = "strict-origin-when-cross-origin";
        cabeceras["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        cabeceras["Content-Security-Policy"] =
            "default-src 'self'; img-src 'self' data: blob:; style-src 'self' 'unsafe-inline'; script-src 'self'; frame-ancestors 'none'";
        await siguiente();
    });
    app.UseHttpsRedirection();
    app.UseCors(politicaCors);
    app.UseRateLimiter();
    app.Use(async (contexto, siguiente) =>
    {
        var metodo = contexto.Request.Method;
        var lectura = HttpMethods.IsGet(metodo)
            || HttpMethods.IsHead(metodo)
            || HttpMethods.IsOptions(metodo)
            || HttpMethods.IsTrace(metodo);
        if (!lectura &&
            !string.Equals(contexto.Request.Headers[CookiesSesion.Encabezado], CookiesSesion.ValorEncabezado, StringComparison.Ordinal))
        {
            contexto.Response.StatusCode = StatusCodes.Status400BadRequest;
            await contexto.Response.WriteAsJsonAsync(new
            {
                title = "Solicitud rechazada.",
                detail = "Solicitud rechazada."
            });
            return;
        }

        await siguiente();
    });
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapHealthChecks("/api/health/ready", new HealthCheckOptions
    {
        ResponseWriter = async (contexto, reporte) =>
        {
            contexto.Response.ContentType = "application/json";
            await contexto.Response.WriteAsJsonAsync(new
            {
                estado = reporte.Status == HealthStatus.Healthy ? "ok" : "no disponible"
            });
        }
    }).AllowAnonymous();
    app.MapControllers();

    app.Run();
}
catch (Exception excepcion)
{
    if (excepcion is not Microsoft.Extensions.Hosting.HostAbortedException)
    {
        Log.Fatal(excepcion, "La API no pudo iniciar");
    }

    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
