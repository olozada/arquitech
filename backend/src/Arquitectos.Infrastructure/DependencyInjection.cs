using Arquitectos.Application.Actas;
using Arquitectos.Application.Auditoria;
using Arquitectos.Application.Auth;
using Arquitectos.Application.Catalogos;
using Arquitectos.Application.Dashboard;
using Arquitectos.Application.Menu;
using Arquitectos.Application.Presupuestos;
using Arquitectos.Application.ProveedorMateriales;
using Arquitectos.Application.Usuarios;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Actas;
using Arquitectos.Infrastructure.Auditoria;
using Arquitectos.Infrastructure.Catalogos;
using Arquitectos.Infrastructure.Dashboard;
using Arquitectos.Infrastructure.Presupuestos;
using Arquitectos.Infrastructure.Persistence;
using Arquitectos.Infrastructure.ProveedorMateriales;
using Arquitectos.Infrastructure.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QuestPDF.Infrastructure;

namespace Arquitectos.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // T0.2: Community, Professional o Enterprise. La licencia Community de QuestPDF
        // solo es gratuita por debajo del umbral de ingresos que publica QuestPDF.
        QuestPDF.Settings.License = LicenciaQuestPdf(configuration["QuestPdf:Licencia"]);

        var connectionString = configuration.GetConnectionString("Arquitectos");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Arquitectos no está configurada. En Development puede definirse con user-secrets o con el archivo .env. En producción, con la variable de entorno ConnectionStrings__Arquitectos.");
        }

        services.AddHttpContextAccessor();
        services.AddScoped<AuditoriaInterceptor>();
        services.AddScoped<AuditoriaServicio>();
        services.AddScoped<IAuditoria>(proveedor => proveedor.GetRequiredService<AuditoriaServicio>());
        services.AddScoped<IConsultaAuditoria>(proveedor => proveedor.GetRequiredService<AuditoriaServicio>());
        services.AddDbContext<ArquitectosDbContext>((proveedor, opciones) =>
            opciones.UseSqlServer(connectionString)
                .AddInterceptors(proveedor.GetRequiredService<AuditoriaInterceptor>()));
        services.AddSingleton(TimeProvider.System);

        services.Configure<OpcionesJwt>(configuration.GetSection(OpcionesJwt.Seccion));
        services.AddSingleton<EmisorJwt>();
        services.AddScoped<ValidadorSesion>();
        services.AddScoped<AutenticacionServicio>();
        services.AddScoped<IAutenticacionServicio>(proveedor => proveedor.GetRequiredService<AutenticacionServicio>());
        services.AddScoped<IRevocacionSesiones>(proveedor => proveedor.GetRequiredService<AutenticacionServicio>());
        services.AddScoped<IUsuarioServicio, UsuarioServicio>();
        services.AddScoped<IMenuServicio, MenuServicio>();

        services.AddScoped<ICatalogoServicio<Arquitecto, CatalogoPersonaDto, CatalogoPersonaSolicitud>>(
            sp => new CatalogoPersonaServicio<Arquitecto>(sp.GetRequiredService<ArquitectosDbContext>(), "un arquitecto"));
        services.AddScoped<ICatalogoServicio<Revisor, CatalogoPersonaDto, CatalogoPersonaSolicitud>>(
            sp => new CatalogoPersonaServicio<Revisor>(sp.GetRequiredService<ArquitectosDbContext>(), "un revisor"));
        services.AddScoped<ICatalogoServicio<Contratista, CatalogoPersonaDto, CatalogoPersonaSolicitud>>(
            sp => new CatalogoPersonaServicio<Contratista>(sp.GetRequiredService<ArquitectosDbContext>(), "un contratista"));
        services.AddScoped<ICatalogoServicio<Proveedor, ProveedorDto, ProveedorSolicitud>, ProveedorServicio>();
        services.AddScoped<ICatalogoServicio<Construccion, ConstruccionDto, ConstruccionSolicitud>, ConstruccionServicio>();
        services.AddScoped<ICatalogoServicio<Concepto, DescripcionUnidadDto, DescripcionUnidadSolicitud>>(
            sp => new DescripcionUnidadServicio<Concepto>(sp.GetRequiredService<ArquitectosDbContext>(), "un concepto"));
        services.AddScoped<ICatalogoServicio<UnidadMedida, UnidadMedidaDto, UnidadMedidaSolicitud>, UnidadMedidaServicio>();
        services.AddScoped<IProveedorMaterialServicio, ProveedorMaterialServicio>();
        services.AddScoped<IActaServicio, ActaServicio>();
        services.AddScoped<MotorCostos>();
        services.AddScoped<IInsumoServicio, InsumoServicio>();
        services.AddScoped<IManoObraServicio, ManoObraServicio>();
        services.AddScoped<IApuServicio, ApuServicio>();
        services.AddScoped<IPresupuestoServicio, PresupuestoServicio>();
        services.AddMemoryCache(opciones => opciones.SizeLimit = 200);
        services.AddOptions<OpcionesPresupuesto>()
            .Bind(configuration.GetSection(OpcionesPresupuesto.Seccion))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<OpcionesDashboard>()
            .Bind(configuration.GetSection(OpcionesDashboard.Seccion))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<IDashboardServicio, DashboardServicio>();

        return services;
    }

    internal static LicenseType LicenciaQuestPdf(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor) ||
            valor.Equals("Community", StringComparison.OrdinalIgnoreCase))
        {
            return LicenseType.Community;
        }

        if (valor.Equals("Professional", StringComparison.OrdinalIgnoreCase))
        {
            return LicenseType.Professional;
        }

        if (valor.Equals("Enterprise", StringComparison.OrdinalIgnoreCase))
        {
            return LicenseType.Enterprise;
        }

        throw new InvalidOperationException(
            "QuestPdf:Licencia debe ser Community, Professional o Enterprise.");
    }
}
