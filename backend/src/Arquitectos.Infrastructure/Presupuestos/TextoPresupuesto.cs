using System.Globalization;
using System.Text;
using Arquitectos.Domain.Entidades;

namespace Arquitectos.Infrastructure.Presupuestos;

public static class TextoPresupuesto
{
    public static string Normalizar(string? texto)
    {
        var valor = (texto ?? "").Trim().Normalize(NormalizationForm.FormD);
        var plano = new StringBuilder(valor.Length);
        foreach (var caracter in valor)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark)
            {
                plano.Append(char.ToUpperInvariant(caracter));
            }
        }

        return plano.ToString().Normalize(NormalizationForm.FormC);
    }

    public static string CanonicaUnidad(string? texto)
    {
        var valor = Normalizar(texto)
            .Replace(" ", "", StringComparison.Ordinal)
            .Replace(".", "", StringComparison.Ordinal)
            .Replace("²", "2", StringComparison.Ordinal)
            .Replace("³", "3", StringComparison.Ordinal);
        return valor switch
        {
            "MTS2" or "MT2" => "M2",
            "MTS3" or "MT3" => "M3",
            "UN" or "UNID" or "UNIDAD" => "UND",
            "GLOBAL" => "GLB",
            _ => valor
        };
    }

    public static bool MismaUnidad(string? izquierda, string? derecha) =>
        string.IsNullOrWhiteSpace(izquierda)
        || string.IsNullOrWhiteSpace(derecha)
        || CanonicaUnidad(izquierda) == CanonicaUnidad(derecha);

    public static string Recortar(string? texto, int maximo)
    {
        var valor = (texto ?? "").Trim();
        return valor.Length <= maximo ? valor : valor[..maximo];
    }

    public static bool TryTipoInsumo(string? texto, out TipoInsumo tipo)
    {
        var clave = Normalizar(texto).Replace(" ", "", StringComparison.Ordinal);
        switch (clave)
        {
            case "MATERIAL":
                tipo = TipoInsumo.Material;
                return true;
            case "EQUIPO":
                tipo = TipoInsumo.Equipo;
                return true;
            case "HERRAMIENTA":
                tipo = TipoInsumo.Herramienta;
                return true;
            case "LABORATORIO":
                tipo = TipoInsumo.Laboratorio;
                return true;
            case "TRANSPORTE":
                tipo = TipoInsumo.Transporte;
                return true;
            case "MANOOBRADESTAJO":
            case "MANODEOBRADESTAJO":
                tipo = TipoInsumo.ManoObraDestajo;
                return true;
            case "OTRO":
                tipo = TipoInsumo.Otro;
                return true;
            default:
                tipo = TipoInsumo.Material;
                return false;
        }
    }

    public static bool TryGrupo(string? texto, out GrupoApu grupo)
    {
        var clave = Normalizar(texto).Replace(" ", "", StringComparison.Ordinal);
        switch (clave)
        {
            case "MATERIAL":
            case "MEZCLA":
                grupo = GrupoApu.Material;
                return true;
            case "MANOOBRA":
            case "MANODEOBRA":
                grupo = GrupoApu.ManoObra;
                return true;
            case "EQUIPO":
                grupo = GrupoApu.Equipo;
                return true;
            case "TRANSPORTE":
                grupo = GrupoApu.Transporte;
                return true;
            case "OTROS":
                grupo = GrupoApu.Otros;
                return true;
            default:
                grupo = GrupoApu.Otros;
                return false;
        }
    }

    public static bool TryModo(string? texto, out ModoLineaApu modo)
    {
        var clave = Normalizar(texto).Replace(" ", "", StringComparison.Ordinal);
        switch (clave)
        {
            case "RENDIMIENTO":
                modo = ModoLineaApu.Rendimiento;
                return true;
            case "DIVISOR":
                modo = ModoLineaApu.Divisor;
                return true;
            case "PORCENTAJEMANOOBRA":
            case "HERRAMIENTAMENOR":
                modo = ModoLineaApu.PorcentajeManoObra;
                return true;
            default:
                modo = ModoLineaApu.Desperdicio;
                return clave is "DESPERDICIO" or "";
        }
    }
}
