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
    public async Task<ResultadoOperacion<PresupuestoDetalleDto>> NuevaVersionAsync(int id, int usuarioId, CancellationToken cancellationToken)
    {
        if (!await EsAdministradorAsync(usuarioId, cancellationToken))
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.Prohibido("Solo el administrador puede crear una versión.");
        }

        var origen = await CargarAsync(id, cancellationToken);
        if (origen is null)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.NoEncontrado("No se encontró el presupuesto.");
        }

        if (origen.EsPlantilla)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.Validacion("Una plantilla no se versiona. Dúpliquela desde plantillas.");
        }

        var copia = new Presupuesto
        {
            ConstruccionId = origen.ConstruccionId,
            Nombre = origen.Nombre,
            Version = origen.Version + 1,
            Estado = EstadoPresupuesto.Borrador,
            Fecha = DateOnly.FromDateTime(DateTime.UtcNow),
            MesesProyecto = origen.MesesProyecto,
            AreaConstruidaM2 = origen.AreaConstruidaM2,
            PctImprevistos = origen.PctImprevistos,
            PctIncremento = origen.PctIncremento,
            PctAdministracion = origen.PctAdministracion,
            PctUtilidad = origen.PctUtilidad,
            PctIvaUtilidad = origen.PctIvaUtilidad
        };
        var mapa = new Dictionary<int, PresupuestoItem>();
        var origenes = new Dictionary<PresupuestoItem, int>();
        foreach (var capitulo in origen.Capitulos.OrderBy(item => item.Orden))
        {
            var nuevoCapitulo = new PresupuestoCapitulo { Numero = capitulo.Numero, Nombre = capitulo.Nombre, Orden = capitulo.Orden };
            foreach (var item in capitulo.Items.OrderBy(fila => fila.Orden))
            {
                var nuevo = new PresupuestoItem
                {
                    Codigo = item.Codigo,
                    Descripcion = item.Descripcion,
                    Unidad = item.Unidad,
                    UnidadMedidaId = item.UnidadMedidaId,
                    ApuId = item.ApuId,
                    FactorConversionApu = item.FactorConversionApu,
                    ConceptoId = item.ConceptoId,
                    Cantidad = item.Cantidad,
                    CantidadOrigenFactor = item.CantidadOrigenFactor,
                    ValorMaterialUnitario = item.ValorMaterialUnitario,
                    ValorManoObraUnitario = item.ValorManoObraUnitario,
                    DesgloseApuJson = item.DesgloseApuJson,
                    Orden = item.Orden
                };
                foreach (var medicion in item.Mediciones)
                {
                    nuevo.Mediciones.Add(new PresupuestoMedicion
                    {
                        Ubicacion = medicion.Ubicacion,
                        Veces = medicion.Veces,
                        Largo = medicion.Largo,
                        Ancho = medicion.Ancho,
                        Alto = medicion.Alto,
                        Parcial = medicion.Parcial
                    });
                }

                if (item.CantidadOrigenItemId is int origenItem)
                {
                    origenes[nuevo] = origenItem;
                }

                mapa[item.Id] = nuevo;
                nuevoCapitulo.Items.Add(nuevo);
            }

            copia.Capitulos.Add(nuevoCapitulo);
        }

        _context.Presupuestos.Add(copia);
        await _context.SaveChangesAsync(cancellationToken);
        foreach (var par in origenes)
        {
            if (mapa.TryGetValue(par.Value, out var destino))
            {
                par.Key.CantidadOrigenItemId = destino.Id;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return await ObtenerAsync(copia.Id, usuarioId, cancellationToken);
    }


}
