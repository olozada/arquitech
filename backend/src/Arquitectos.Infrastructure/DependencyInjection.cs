using Arquitectos.Application.Actas;
using Arquitectos.Application.Auth;
using Arquitectos.Application.Catalogos;
using Arquitectos.Application.Dashboard;
using Arquitectos.Application.Menu;
using Arquitectos.Application.ProveedorMateriales;
using Arquitectos.Application.Usuarios;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Actas;
using Arquitectos.Infrastructure.Catalogos;
using Arquitectos.Infrastructure.Dashboard;
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
        QuestPDF.Settings.License = LicenseType.Community;

        var connectionString = configuration.GetConnectionString("Arquitectos");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Arquitectos no está configurada. Defínela en el archivo .env o con dotnet user-secrets.");
        }

        services.AddDbContext<ArquitectosDbContext>(opciones =>
            opciones.UseSqlServer(connectionString));

        services.Configure<OpcionesJwt>(configuration.GetSection(OpcionesJwt.Seccion));
        services.AddSingleton<EmisorJwt>();
        services.AddScoped<IAutenticacionServicio, AutenticacionServicio>();
        services.AddScoped<IUsuarioServicio, UsuarioServicio>();
        services.AddScoped<IMenuServicio, MenuServicio>();

        services.AddScoped<ICatalogoServicio<Arquitecto, CatalogoNombreDto, CatalogoNombreSolicitud>>(
            sp => new CatalogoNombreServicio<Arquitecto>(sp.GetRequiredService<ArquitectosDbContext>(), "un arquitecto"));
        services.AddScoped<ICatalogoServicio<Revisor, CatalogoNombreDto, CatalogoNombreSolicitud>>(
            sp => new CatalogoNombreServicio<Revisor>(sp.GetRequiredService<ArquitectosDbContext>(), "un revisor"));
        services.AddScoped<ICatalogoServicio<Contratista, CatalogoNombreDto, CatalogoNombreSolicitud>>(
            sp => new CatalogoNombreServicio<Contratista>(sp.GetRequiredService<ArquitectosDbContext>(), "un contratista"));
        services.AddScoped<ICatalogoServicio<Proveedor, CatalogoNombreDto, CatalogoNombreSolicitud>>(
            sp => new CatalogoNombreServicio<Proveedor>(sp.GetRequiredService<ArquitectosDbContext>(), "un proveedor"));
        services.AddScoped<ICatalogoServicio<Construccion, ConstruccionDto, ConstruccionSolicitud>, ConstruccionServicio>();
        services.AddScoped<ICatalogoServicio<Concepto, DescripcionUnidadDto, DescripcionUnidadSolicitud>>(
            sp => new DescripcionUnidadServicio<Concepto>(sp.GetRequiredService<ArquitectosDbContext>(), "un concepto"));
        services.AddScoped<ICatalogoServicio<Material, DescripcionUnidadDto, DescripcionUnidadSolicitud>>(
            sp => new DescripcionUnidadServicio<Material>(sp.GetRequiredService<ArquitectosDbContext>(), "un material"));
        services.AddScoped<ICatalogoServicio<UnidadMedida, UnidadMedidaDto, UnidadMedidaSolicitud>, UnidadMedidaServicio>();
        services.AddScoped<IProveedorMaterialServicio, ProveedorMaterialServicio>();
        services.AddScoped<IActaServicio, ActaServicio>();
        services.AddMemoryCache();
        services.AddOptions<OpcionesDashboard>()
            .Bind(configuration.GetSection(OpcionesDashboard.Seccion))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<IDashboardServicio, DashboardServicio>();

        return services;
    }
}
