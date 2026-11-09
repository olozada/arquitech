using System.Globalization;
using System.Text;
using Arquitectos.Api.Configuracion;
using Arquitectos.Application;
using Arquitectos.Infrastructure;
using Arquitectos.Infrastructure.Configuration;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    ArchivoEnv.AplicarConexion(builder.Configuration, builder.Environment.ContentRootPath);

    builder.Host.UseSerilog((context, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console());

    var jwtKey = builder.Configuration["Jwt:Key"];
    if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    {
        throw new InvalidOperationException(
            "Jwt:Key no está configurada o es demasiado corta. Defínela con dotnet user-secrets (mínimo 32 caracteres).");
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
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew = TimeSpan.FromMinutes(1),
                NameClaimType = "nombre",
                RoleClaimType = "tipoUsuario"
            };
            opciones.MapInboundClaims = false;
        });

    builder.Services.AddAuthorization(opciones =>
    {
        opciones.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    });

    const string politicaCors = "Frontend";
    builder.Services.AddCors(opciones =>
    {
        opciones.AddPolicy(politicaCors, policy =>
            policy.WithOrigins("http://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod());
    });

    builder.Services.AddControllers()
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

        opciones.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Description = "Token JWT. Esquema: Bearer {token}",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });

        opciones.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    var app = builder.Build();

    if (args.Contains("--seed-demo"))
    {
        if (!app.Environment.IsDevelopment())
        {
            throw new InvalidOperationException("La semilla de demostración solo está disponible en Development.");
        }

        await using var alcance = app.Services.CreateAsyncScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<ArquitectosDbContext>();
        var mensaje = await SemillaDemo.AplicarAsync(contexto);
        Log.Information(mensaje);
        return;
    }

    app.UseExceptionHandler();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseRequestLocalization();
    app.UseHttpsRedirection();
    app.UseCors(politicaCors);
    app.UseAuthentication();
    app.UseAuthorization();
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
