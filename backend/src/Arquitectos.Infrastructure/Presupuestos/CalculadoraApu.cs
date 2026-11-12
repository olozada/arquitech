namespace Arquitectos.Infrastructure.Presupuestos;

public sealed record LineaCalculo(
    ModoCalculo Modo,
    decimal Cantidad,
    decimal Factor,
    decimal Precio,
    int? SubApuId,
    bool HerramientaMenor);

public enum ModoCalculo
{
    Desperdicio = 1,
    Rendimiento = 2,
    Divisor = 3
}

public sealed record GrupoCalculo(string Tipo, decimal Divisor, IReadOnlyList<LineaCalculo> Lineas);

public sealed record ApuCalculo(int Id, decimal DivisorGeneral, IReadOnlyList<GrupoCalculo> Grupos);

public sealed record LineaValor(decimal Precio, decimal Valor);

public sealed record GrupoValor(string Tipo, decimal Divisor, decimal Subtotal, IReadOnlyList<LineaValor> Lineas);

public sealed record ResultadoApu(
    decimal Total,
    IReadOnlyDictionary<string, decimal> Grupos,
    IReadOnlyList<GrupoValor>? Detalle = null);

public static class CalculadoraApu
{
    public static ResultadoApu Calcular(int apuId, IReadOnlyDictionary<int, ApuCalculo> apus)
    {
        var cache = new Dictionary<int, ResultadoApu>();
        return Calcular(apuId, apus, [], cache);
    }

    public static decimal PrecioUnitarioInsumo(decimal precioBase, decimal porcentajeIva, decimal cantidadPorUnidad) =>
        cantidadPorUnidad == 0 ? 0 : precioBase * (1 + porcentajeIva) / cantidadPorUnidad;

    public static decimal FactorPrestacional(IEnumerable<decimal> porcentajes, bool incluirDiasNoHabiles, int diasHabilesAnio)
    {
        var factor = porcentajes.Sum();
        if (incluirDiasNoHabiles && diasHabilesAnio > 0)
        {
            factor += 365m / diasHabilesAnio - 1m;
        }

        return factor;
    }

    public static decimal Jornal(decimal salarioMensual, int diasMes, decimal factorPrestacional) =>
        diasMes == 0 ? 0 : salarioMensual / diasMes * (1 + factorPrestacional);

    public static decimal CostoDiaCuadrilla(
        IEnumerable<(decimal SalarioMensual, decimal Cantidad)> integrantes,
        int diasMes,
        decimal factorPrestacional) =>
        integrantes.Sum(item => Jornal(item.SalarioMensual, diasMes, factorPrestacional) * item.Cantidad);

    public static decimal ParcialMedicion(decimal veces, decimal? largo, decimal? ancho, decimal? alto) =>
        veces * (largo ?? 1) * (ancho ?? 1) * (alto ?? 1);

    private static ResultadoApu Calcular(
        int apuId,
        IReadOnlyDictionary<int, ApuCalculo> apus,
        HashSet<int> pila,
        Dictionary<int, ResultadoApu> cache)
    {
        if (cache.TryGetValue(apuId, out var guardado))
        {
            return guardado;
        }

        if (!pila.Add(apuId))
        {
            throw new CicloApuException(apuId);
        }

        if (!apus.TryGetValue(apuId, out var apu))
        {
            throw new InvalidOperationException($"No se encontró el APU {apuId}.");
        }

        var grupos = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var detalle = new List<GrupoValor>();
        foreach (var grupo in apu.Grupos)
        {
            decimal sumaManoObra = 0;
            decimal total = 0;
            var lineas = new List<LineaValor>();
            foreach (var linea in grupo.Lineas)
            {
                var precio = linea.SubApuId is int sub
                    ? Calcular(sub, apus, pila, cache).Total
                    : linea.Precio;
                decimal valor;
                if (linea.HerramientaMenor)
                {
                    valor = linea.Factor * sumaManoObra;
                }
                else if (linea.Modo == ModoCalculo.Divisor)
                {
                    valor = linea.Factor == 0 ? 0 : linea.Cantidad * precio / linea.Factor;
                }
                else if (linea.Modo == ModoCalculo.Rendimiento)
                {
                    valor = linea.Cantidad * precio * linea.Factor;
                }
                else
                {
                    valor = linea.Cantidad * precio * (1 + linea.Factor);
                }

                var esManoObra = grupo.Tipo.Equals("ManoObra", StringComparison.OrdinalIgnoreCase);
                if (esManoObra && !linea.HerramientaMenor)
                {
                    sumaManoObra += valor;
                }

                total += valor;
                lineas.Add(new LineaValor(precio, valor));
            }

            var divisor = grupo.Divisor == 0 ? 1 : grupo.Divisor;
            var subtotal = total / divisor;
            grupos[grupo.Tipo] = grupos.GetValueOrDefault(grupo.Tipo) + subtotal;
            detalle.Add(new GrupoValor(grupo.Tipo, divisor, subtotal, lineas));
        }

        var divisorGeneral = apu.DivisorGeneral == 0 ? 1 : apu.DivisorGeneral;
        var ajustados = grupos.ToDictionary(par => par.Key, par => par.Value / divisorGeneral, StringComparer.OrdinalIgnoreCase);
        var resultado = new ResultadoApu(ajustados.Values.Sum(), ajustados, detalle);
        cache[apuId] = resultado;
        pila.Remove(apuId);
        return resultado;
    }
}

public sealed class CicloApuException(int apuId) : InvalidOperationException($"Hay un ciclo en el APU {apuId}.");
