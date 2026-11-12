using System.Text.Json;
using Arquitectos.Application.Auditoria;
using Arquitectos.Application.Presupuestos;
using Arquitectos.Application.Usuarios;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Auditoria;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Arquitectos.Infrastructure.Presupuestos;

public sealed partial class PresupuestoServicio
{
    public async Task<ResultadoOperacion<SeguimientoDto>> SeguimientoAsync(int id, int usuarioId, CancellationToken cancellationToken)
    {
        var presupuesto = await CargarAsync(id, cancellationToken);
        if (presupuesto is null)
        {
            return ResultadoOperacion<SeguimientoDto>.NoEncontrado("No se encontró el presupuesto.");
        }

        if (presupuesto.EsPlantilla)
        {
            return ResultadoOperacion<SeguimientoDto>.Validacion("Una plantilla no tiene seguimiento.");
        }

        if (!await PuedeVerAsync(usuarioId, presupuesto.ConstruccionId, cancellationToken))
        {
            return ResultadoOperacion<SeguimientoDto>.Prohibido("No tiene acceso a este presupuesto.");
        }

        var items = presupuesto.Capitulos.SelectMany(capitulo => capitulo.Items).ToList();
        var ejecutado = await _context.ActaDetalles.AsNoTracking()
            .Where(detalle => detalle.Aprobado && detalle.PresupuestoItemId != null && items.Select(item => item.Id).Contains(detalle.PresupuestoItemId!.Value))
            .Select(detalle => new { detalle.PresupuestoItemId, detalle.Cantidad, detalle.Subtotal, detalle.Acta.FechaFinal })
            .ToListAsync(cancellationToken);
        var porItem = ejecutado.GroupBy(linea => linea.PresupuestoItemId!.Value)
            .ToDictionary(grupo => grupo.Key, grupo => (Cantidad: grupo.Sum(linea => linea.Cantidad), Valor: grupo.Sum(linea => linea.Subtotal)));

        var filas = new List<SeguimientoFilaDto>();
        var capitulos = new List<SeguimientoFilaDto>();
        foreach (var capitulo in presupuesto.Capitulos.OrderBy(item => item.Orden))
        {
            decimal cantidadPres = 0, cantidadEje = 0, valorPres = 0, valorEje = 0, proyeccion = 0;
            foreach (var item in capitulo.Items.OrderBy(fila => fila.Orden))
            {
                var eje = porItem.GetValueOrDefault(item.Id);
                var seguimiento = CalculadoraPresupuesto.Seguimiento(
                    item.Cantidad,
                    item.ValorMaterialUnitario + item.ValorManoObraUnitario,
                    eje.Cantidad,
                    eje.Valor);
                filas.Add(Fila(item.Id, capitulo.Id, capitulo.Nombre, item.Codigo, item.Descripcion, item.Unidad, seguimiento));
                cantidadPres += seguimiento.CantidadPresupuestada;
                cantidadEje += seguimiento.CantidadEjecutada;
                valorPres += seguimiento.ValorPresupuestado;
                valorEje += seguimiento.ValorEjecutado;
                proyeccion += seguimiento.ProyeccionCierre;
            }

            capitulos.Add(new SeguimientoFilaDto(
                null,
                capitulo.Id,
                capitulo.Nombre,
                capitulo.Numero.ToString(),
                capitulo.Nombre,
                "",
                cantidadPres,
                cantidadEje,
                cantidadPres == 0 ? 0 : cantidadEje / cantidadPres,
                valorPres,
                valorEje,
                valorPres == 0 ? 0 : valorEje / valorPres,
                valorPres - valorEje,
                0,
                cantidadEje == 0 ? 0 : valorEje / cantidadEje,
                proyeccion));
        }

        var sueltos = await _context.ActaDetalles.AsNoTracking()
            .Where(detalle => detalle.Aprobado && detalle.PresupuestoItemId == null && detalle.ConstruccionId == presupuesto.ConstruccionId)
            .Select(detalle => new { detalle.Concepto.Descripcion, detalle.Cantidad, detalle.Precio, detalle.Subtotal, detalle.Acta.FechaFinal, detalle.NumeroActa })
            .ToListAsync(cancellationToken);
        var noPresupuestados = sueltos.Select(linea => new SeguimientoFilaDto(
            null,
            null,
            "",
            linea.NumeroActa.ToString(),
            linea.Descripcion,
            "",
            0,
            linea.Cantidad,
            0,
            0,
            linea.Subtotal,
            0,
            -linea.Subtotal,
            0,
            linea.Precio,
            linea.Subtotal)).ToList();

        var meses = ejecutado
            .GroupBy(linea => (linea.FechaFinal.Year, linea.FechaFinal.Month))
            .OrderBy(grupo => grupo.Key.Year)
            .ThenBy(grupo => grupo.Key.Month)
            .ToList();
        decimal acumulado = 0;
        var puntos = new List<PuntoEjecucionDto>();
        foreach (var mes in meses)
        {
            var valor = mes.Sum(linea => linea.Subtotal);
            acumulado += valor;
            puntos.Add(new PuntoEjecucionDto(mes.Key.Year, mes.Key.Month, valor, acumulado));
        }

        return ResultadoOperacion<SeguimientoDto>.Ok(new SeguimientoDto(
            presupuesto.Id,
            presupuesto.Construccion?.Descripcion ?? "",
            CostoDe(presupuesto),
            filas.Sum(fila => fila.ValorEjecutado),
            filas.Sum(fila => fila.ProyeccionCierre),
            noPresupuestados.Sum(fila => fila.ValorEjecutado),
            capitulos,
            filas,
            noPresupuestados,
            puntos));
    }


    private static SeguimientoFilaDto Fila(int id, int capituloId, string capitulo, string codigo, string descripcion, string unidad, SeguimientoCalculado seguimiento) =>
        new(
            id,
            capituloId,
            capitulo,
            codigo,
            descripcion,
            unidad,
            seguimiento.CantidadPresupuestada,
            seguimiento.CantidadEjecutada,
            seguimiento.AvanceFisico,
            seguimiento.ValorPresupuestado,
            seguimiento.ValorEjecutado,
            seguimiento.AvanceFinanciero,
            seguimiento.Saldo,
            seguimiento.PrecioPresupuestado,
            seguimiento.PrecioActas,
            seguimiento.ProyeccionCierre);

    private async Task<Dictionary<int, decimal>> EjecutadoPorItemAsync(IReadOnlyCollection<int> items, CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return [];
        }

        var lineas = await _context.ActaDetalles.AsNoTracking()
            .Where(detalle => detalle.Aprobado && detalle.PresupuestoItemId != null && items.Contains(detalle.PresupuestoItemId.Value))
            .Select(detalle => new { detalle.PresupuestoItemId, detalle.Subtotal })
            .ToListAsync(cancellationToken);
        return lineas.GroupBy(linea => linea.PresupuestoItemId!.Value).ToDictionary(grupo => grupo.Key, grupo => grupo.Sum(linea => linea.Subtotal));
    }

}
