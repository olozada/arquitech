using Arquitectos.Application.Dashboard;

namespace Arquitectos.Application.Dashboard;

public static class ReglasAlertas
{
    public static IReadOnlyList<AlertaDto> PreciosSobreReferencia(
        IReadOnlyList<LineaParaAlerta> candidatas,
        IReadOnlyList<LineaParaAlerta> historico,
        OpcionesDashboard opciones)
    {
        var umbral = opciones.UmbralFraccion;
        var alertas = new List<AlertaDto>();
        foreach (var linea in candidatas)
        {
            if (linea.UnidadMedidaId is not > 0)
            {
                continue;
            }

            var desde = linea.FechaFinal.AddDays(-opciones.VentanaReferenciaDias);
            var previas = historico.Where(otra =>
                otra.NumeroActa != linea.NumeroActa
                && otra.ConceptoId == linea.ConceptoId
                && otra.UnidadMedidaId == linea.UnidadMedidaId
                && otra.InsumoId == linea.InsumoId
                && otra.FechaFinal < linea.FechaFinal
                && otra.FechaFinal >= desde).ToList();
            if (previas.Count < opciones.MinObservacionesReferencia)
            {
                continue;
            }

            var referencia = previas.Average(otra => otra.Precio);
            if (referencia <= 0)
            {
                continue;
            }

            var ratio = (linea.Precio - referencia) / referencia;
            if (ratio <= umbral)
            {
                continue;
            }

            var porcentaje = Math.Round(ratio * 100m, 0);
            var nombre = linea.Insumo ?? linea.Concepto;
            alertas.Add(new AlertaDto(
                "A1",
                ratio > umbral * 2 ? "Alta" : "Media",
                $"Precio {porcentaje:0}% sobre referencia: {nombre}",
                $"El precio {linea.Precio:0} supera la referencia {referencia:0} del concepto {linea.Concepto}.",
                linea.NumeroActa,
                linea.ConstruccionId,
                linea.Construccion,
                (linea.Precio - referencia) * linea.Cantidad));
        }

        return alertas;
    }

    public static IReadOnlyList<AlertaDto> PreciosSobreLista(
        IReadOnlyList<LineaParaAlerta> lineas,
        IReadOnlyList<PrecioProveedor> listas)
    {
        var porMaterial = listas.ToDictionary(lista => lista.InsumoId);
        var alertas = new List<AlertaDto>();
        foreach (var linea in lineas)
        {
            if (linea.InsumoId is not int insumoId || !porMaterial.TryGetValue(insumoId, out var lista))
            {
                continue;
            }

            if (linea.Precio <= lista.PrecioMinimo)
            {
                continue;
            }

            var nombre = linea.Insumo ?? linea.Concepto;
            alertas.Add(new AlertaDto(
                "A2",
                "Media",
                $"Precio sobre la lista de {lista.ProveedorMasBarato}: {nombre}",
                $"El acta cobra {linea.Precio:0} y el menor precio de lista es {lista.PrecioMinimo:0} ({lista.ProveedorMasBarato}). El precio del ítem puede incluir mano de obra: es una referencia para negociar, no un sobrecosto confirmado.",
                linea.NumeroActa,
                linea.ConstruccionId,
                linea.Construccion,
                (linea.Precio - lista.PrecioMinimo) * linea.Cantidad));
        }

        return alertas;
    }

    public static IReadOnlyList<AlertaDto> DoblesCobros(IReadOnlyList<LineaParaAlerta> lineas)
    {
        var alertas = new List<AlertaDto>();
        for (var i = 0; i < lineas.Count; i++)
        {
            var primera = lineas[i];
            for (var j = i + 1; j < lineas.Count; j++)
            {
                var segunda = lineas[j];
                if (primera.NumeroActa == segunda.NumeroActa
                    || primera.ContratistaId != segunda.ContratistaId
                    || primera.ConstruccionId != segunda.ConstruccionId
                    || primera.ConceptoId != segunda.ConceptoId)
                {
                    continue;
                }

                var seCruzan = primera.FechaInicial <= segunda.FechaFinal
                    && segunda.FechaInicial <= primera.FechaFinal;
                if (!seCruzan)
                {
                    continue;
                }

                alertas.Add(new AlertaDto(
                    "A3",
                    "Alta",
                    $"Posible doble cobro: {primera.Concepto} en {primera.Construccion}",
                    $"Las actas {primera.NumeroActa} y {segunda.NumeroActa} de {primera.Contratista} se cruzan en el mismo concepto.",
                    primera.NumeroActa,
                    primera.ConstruccionId,
                    primera.Construccion,
                    Math.Min(primera.Subtotal, segunda.Subtotal)));
            }
        }

        return alertas;
    }

    public static IReadOnlyList<AlertaDto> ObrasSinActividad(
        IReadOnlyList<ObraActividad> obras,
        DateOnly hoy,
        OpcionesDashboard opciones)
    {
        var limite = hoy.AddDays(-opciones.DiasSinActividad);
        var alertas = new List<AlertaDto>();
        foreach (var obra in obras)
        {
            if (obra.UltimaFechaFinal is DateOnly ultima && ultima >= limite)
            {
                continue;
            }

            var detalle = obra.UltimaFechaFinal is DateOnly fecha
                ? $"La última acta termina el {fecha:yyyy-MM-dd}."
                : "La construcción no tiene actas.";
            alertas.Add(new AlertaDto(
                "A4",
                "Baja",
                $"Obra sin actividad: {obra.Nombre}",
                detalle,
                null,
                obra.Id,
                obra.Nombre,
                null));
        }

        return alertas;
    }

    public static IReadOnlyList<AlertaDto> ActasTardias(
        IReadOnlyList<ActaRegistro> actas,
        OpcionesDashboard opciones)
    {
        var alertas = new List<AlertaDto>();
        foreach (var acta in actas)
        {
            var dias = acta.FechaRegistroLocal.DayNumber - acta.FechaFinal.DayNumber;
            if (dias <= opciones.DiasRegistroTardio)
            {
                continue;
            }

            alertas.Add(new AlertaDto(
                "A5",
                "Baja",
                $"Acta registrada tarde: N° {acta.NumeroActa}",
                $"Se registró {dias} días después del fin del periodo ({acta.FechaFinal:yyyy-MM-dd}).",
                acta.NumeroActa,
                acta.ConstruccionId,
                acta.Construccion,
                null));
        }

        return alertas;
    }

    public static IReadOnlyList<AlertaDto> PrecioSobrePresupuesto(
        IReadOnlyList<LineaContraPresupuesto> lineas,
        decimal umbralFraccion)
    {
        var alertas = new List<AlertaDto>();
        foreach (var linea in lineas)
        {
            var tope = linea.PrecioPresupuesto * (1 + umbralFraccion);
            if (linea.PrecioPresupuesto <= 0 || linea.PrecioActa <= tope)
            {
                continue;
            }

            alertas.Add(new AlertaDto(
                "A6",
                "Media",
                $"Precio de acta sobre el presupuesto: {linea.Codigo}",
                $"{linea.Construccion}: el acta paga {linea.PrecioActa:N0} y el presupuesto trae {linea.PrecioPresupuesto:N0}.",
                linea.NumeroActa,
                linea.ConstruccionId,
                linea.Construccion,
                (linea.PrecioActa - linea.PrecioPresupuesto) * linea.CantidadActa));
        }

        return alertas;
    }

    public static IReadOnlyList<AlertaDto> SobreEjecucion(IReadOnlyList<LineaContraPresupuesto> items)
    {
        var alertas = new List<AlertaDto>();
        foreach (var item in items)
        {
            if (item.CantidadPresupuesto <= 0 || item.CantidadEjecutada <= item.CantidadPresupuesto * 1.05m)
            {
                continue;
            }

            var alta = item.CantidadEjecutada > item.CantidadPresupuesto * 1.20m;
            alertas.Add(new AlertaDto(
                "A7",
                alta ? "Alta" : "Media",
                $"Sobre-ejecución: {item.Codigo}",
                $"{item.Construccion}: se ejecutaron {item.CantidadEjecutada:N2} de {item.CantidadPresupuesto:N2} presupuestados.",
                item.NumeroActa,
                item.ConstruccionId,
                item.Construccion,
                (item.CantidadEjecutada - item.CantidadPresupuesto) * item.PrecioPresupuesto));
        }

        return alertas;
    }

    public static IReadOnlyList<AlertaDto> EjecutadoNoPresupuestado(IReadOnlyList<LineaContraPresupuesto> lineas) =>
        lineas.Select(linea => new AlertaDto(
            "A8",
            "Media",
            $"Ejecutado no presupuestado: acta {linea.NumeroActa}",
            $"{linea.Construccion}: {linea.Codigo} no está ligado a un ítem del presupuesto.",
            linea.NumeroActa,
            linea.ConstruccionId,
            linea.Construccion,
            linea.PrecioActa * linea.CantidadActa)).ToList();

    public static IReadOnlyList<AlertaDto> Ordenar(IEnumerable<AlertaDto> alertas) =>
        alertas
            .OrderByDescending(alerta => alerta.Impacto ?? decimal.MinValue)
            .ThenBy(alerta => alerta.Tipo)
            .ThenBy(alerta => alerta.NumeroActa)
            .ToList();
}
