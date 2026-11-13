using Microsoft.AspNetCore.Authorization;

namespace Arquitectos.Application.Auth;

public static class Politicas
{
    public const string SoloAdministrador = "SoloAdministrador";

    public const string GestionPresupuesto = "GestionPresupuesto";

    public const string TomaActas = "TomaActas";

    public const string RevisionActas = "RevisionActas";

    public const string Tablero = "Tablero";

    public const string PerfilOperativo = "PerfilOperativo";

    public static void Registrar(AuthorizationOptions opciones)
    {
        opciones.AddPolicy(SoloAdministrador, politica => politica.RequireRole(Perfiles.Administrador));
        opciones.AddPolicy(GestionPresupuesto, politica => politica.RequireRole(Perfiles.Administrador, Perfiles.Arquitecto));
        opciones.AddPolicy(TomaActas, politica => politica.RequireRole(Perfiles.Administrador, Perfiles.Arquitecto, Perfiles.Contratista));
        opciones.AddPolicy(RevisionActas, politica => politica.RequireRole(Perfiles.Administrador, Perfiles.Revisor));
        opciones.AddPolicy(Tablero, politica => politica.RequireRole(Perfiles.Administrador));
        opciones.AddPolicy(PerfilOperativo, politica => politica.RequireRole(Perfiles.Arquitecto, Perfiles.Contratista, Perfiles.Revisor));
    }
}
