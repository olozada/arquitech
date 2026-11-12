using Arquitectos.Application.Dashboard;

namespace Arquitectos.Application.Dashboard;

public static class ZonaHorariaDashboard
{
    public static TimeZoneInfo Resolver(string id)
    {
        if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var directa))
        {
            return directa;
        }

        if (TimeZoneInfo.TryConvertIanaIdToWindowsId(id, out var windows)
            && TimeZoneInfo.TryFindSystemTimeZoneById(windows, out var zona))
        {
            return zona;
        }

        throw new InvalidOperationException($"No se reconoció la zona horaria '{id}'.");
    }

    public static DateOnly FechaLocal(DateTime instante, TimeZoneInfo zona)
    {
        var utc = instante.Kind switch
        {
            DateTimeKind.Utc => instante,
            DateTimeKind.Local => instante.ToUniversalTime(),
            _ => DateTime.SpecifyKind(instante, DateTimeKind.Utc)
        };
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, zona));
    }
}

public static class CalculosDashboard
{
    public static decimal? Variacion(decimal actual, decimal anterior) =>
        anterior == 0 ? null : Math.Round((actual - anterior) / Math.Abs(anterior) * 100m, 1);

    public static IReadOnlyList<RankingDto> Rankings(
        IEnumerable<(int Id, string Nombre, decimal Valor, int Actas)> filas,
        decimal total,
        int? tope)
    {
        var ordenados = filas
            .Where(fila => fila.Valor != 0 || fila.Actas != 0)
            .OrderByDescending(fila => fila.Valor)
            .ThenBy(fila => fila.Nombre)
            .ToList();
        if (tope is int limite)
        {
            ordenados = ordenados.Take(limite).ToList();
        }

        decimal acumulado = 0;
        var resultado = new List<RankingDto>(ordenados.Count);
        foreach (var fila in ordenados)
        {
            var participacion = total == 0 ? 0 : Math.Round(fila.Valor / total * 100m, 1);
            acumulado += fila.Valor;
            var acumulada = total == 0 ? 0 : Math.Round(acumulado / total * 100m, 1);
            resultado.Add(new RankingDto(fila.Id, fila.Nombre, fila.Valor, fila.Actas, participacion, acumulada));
        }

        return resultado;
    }

    public static IReadOnlyList<PuntoMensualDto> Tendencia(
        IReadOnlyList<LineaParaAlerta> actual,
        IReadOnlyList<LineaParaAlerta> anioAnterior,
        DateOnly desde,
        DateOnly hasta)
    {
        var puntos = new List<PuntoMensualDto>();
        for (var cursor = new DateOnly(desde.Year, desde.Month, 1); cursor <= hasta; cursor = cursor.AddMonths(1))
        {
            var valor = SumarMes(actual, cursor.Year, cursor.Month);
            var actas = actual.Where(linea => linea.FechaFinal.Year == cursor.Year && linea.FechaFinal.Month == cursor.Month)
                .Select(linea => linea.NumeroActa)
                .Distinct()
                .Count();
            var previo = SumarMes(anioAnterior, cursor.Year - 1, cursor.Month);
            puntos.Add(new PuntoMensualDto(cursor.Year, cursor.Month, valor.Valor, actas, previo.Valor));
        }

        return puntos;
    }

    public static (decimal Valor, int Actas) SumarMes(IReadOnlyList<LineaParaAlerta> lineas, int anio, int mes)
    {
        var delMes = lineas.Where(linea => linea.FechaFinal.Year == anio && linea.FechaFinal.Month == mes).ToList();
        return (delMes.Sum(linea => linea.Subtotal), delMes.Select(linea => linea.NumeroActa).Distinct().Count());
    }
}
