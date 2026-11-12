using Arquitectos.Application.Presupuestos;
using Arquitectos.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Infrastructure.Presupuestos;

public sealed partial class PresupuestoServicio
{
    public async Task<ResultadoOperacion<PresupuestoCreadoDto>> CrearDesdeAsync(
        CrearPresupuestoSolicitud solicitud,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        if (!await EsAdministradorAsync(usuarioId, cancellationToken))
        {
            return ResultadoOperacion<PresupuestoCreadoDto>.Prohibido("Solo el administrador puede crear presupuestos.");
        }

        if (solicitud.ConstruccionId is not int construccionId ||
            !await _context.Construcciones.AnyAsync(obra => obra.Id == construccionId && obra.Activo, cancellationToken))
        {
            return ResultadoOperacion<PresupuestoCreadoDto>.Validacion("La construcción no existe.");
        }

        if (solicitud.AreaConstruidaM2 is null)
        {
            return ResultadoOperacion<PresupuestoCreadoDto>.Validacion("El área construida es obligatoria.");
        }

        var modo = (solicitud.Origen ?? "vacio").Trim().ToLowerInvariant();
        if (modo is not ("vacio" or "plantilla" or "copia"))
        {
            return ResultadoOperacion<PresupuestoCreadoDto>.Validacion("El punto de partida no es válido.");
        }

        Presupuesto? origen = null;
        if (modo != "vacio")
        {
            if (solicitud.OrigenPresupuestoId is not int origenId)
            {
                return ResultadoOperacion<PresupuestoCreadoDto>.Validacion("Seleccione el presupuesto de origen.");
            }

            origen = await CargarAsync(origenId, cancellationToken);
            if (origen is null)
            {
                return ResultadoOperacion<PresupuestoCreadoDto>.NoEncontrado("No se encontró el presupuesto de origen.");
            }

            if (modo == "plantilla" && !origen.EsPlantilla)
            {
                return ResultadoOperacion<PresupuestoCreadoDto>.Validacion("El origen no es una plantilla.");
            }

            if (modo == "copia" && origen.EsPlantilla)
            {
                return ResultadoOperacion<PresupuestoCreadoDto>.Validacion("Para copiar una plantilla use el origen plantilla.");
            }
        }

        var version = await _context.Presupuestos
            .Where(item => item.ConstruccionId == construccionId)
            .Select(item => (int?)item.Version)
            .MaxAsync(cancellationToken);
        var obra = await _context.Construcciones.AsNoTracking().FirstAsync(item => item.Id == construccionId, cancellationToken);
        var presupuesto = new Presupuesto
        {
            ConstruccionId = construccionId,
            Nombre = TextoPresupuesto.Recortar(string.IsNullOrWhiteSpace(solicitud.Nombre) ? $"Presupuesto {obra.Descripcion}" : solicitud.Nombre, 200),
            Version = (version ?? 0) + 1,
            Estado = EstadoPresupuesto.Borrador,
            Fecha = solicitud.Fecha == default ? DateOnly.FromDateTime(DateTime.UtcNow) : solicitud.Fecha,
            MesesProyecto = solicitud.MesesProyecto,
            AreaConstruidaM2 = solicitud.AreaConstruidaM2.Value,
            PctImprevistos = solicitud.PctImprevistos ?? _opciones.PctImprevistos,
            PctIncremento = solicitud.PctIncremento ?? _opciones.PctIncremento,
            PctAdministracion = solicitud.PctAdministracion ?? _opciones.PctAdministracion,
            PctUtilidad = solicitud.PctUtilidad ?? _opciones.PctUtilidad,
            PctIvaUtilidad = solicitud.PctIvaUtilidad ?? _opciones.PctIvaUtilidad,
            PresupuestoOrigenId = origen?.Id
        };
        CopiaContenido? copia = null;
        if (origen is not null)
        {
            copia = CopiarContenido(presupuesto, origen, solicitud.ConservarCantidades && modo == "copia");
        }

        _context.Presupuestos.Add(presupuesto);
        await _context.SaveChangesAsync(cancellationToken);
        if (copia is { Referencias.Count: > 0 })
        {
            RemapearOrigenes(copia);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return ResultadoOperacion<PresupuestoCreadoDto>.Ok(new PresupuestoCreadoDto(presupuesto.Id));
    }

    public async Task<ResultadoOperacion<IReadOnlyList<PlantillaResumenDto>>> PlantillasAsync(int usuarioId, CancellationToken cancellationToken)
    {
        if (!await EsAdministradorAsync(usuarioId, cancellationToken))
        {
            return ResultadoOperacion<IReadOnlyList<PlantillaResumenDto>>.Prohibido("Solo el administrador gestiona plantillas.");
        }

        var plantillas = await _context.Presupuestos.AsNoTracking()
            .Where(presupuesto => presupuesto.EsPlantilla)
            .OrderBy(presupuesto => presupuesto.Nombre)
            .Select(presupuesto => new PlantillaResumenDto(
                presupuesto.Id,
                presupuesto.Nombre,
                presupuesto.Capitulos.Count,
                presupuesto.Capitulos.Sum(capitulo => capitulo.Items.Count),
                presupuesto.Fecha))
            .ToListAsync(cancellationToken);
        return ResultadoOperacion<IReadOnlyList<PlantillaResumenDto>>.Ok(plantillas);
    }

    public async Task<ResultadoOperacion<PresupuestoCreadoDto>> GuardarComoPlantillaAsync(int id, string nombre, int usuarioId, CancellationToken cancellationToken)
    {
        if (!await EsAdministradorAsync(usuarioId, cancellationToken))
        {
            return ResultadoOperacion<PresupuestoCreadoDto>.Prohibido("Solo el administrador puede guardar plantillas.");
        }

        var origen = await CargarAsync(id, cancellationToken);
        if (origen is null)
        {
            return ResultadoOperacion<PresupuestoCreadoDto>.NoEncontrado("No se encontró el presupuesto.");
        }

        var plantilla = new Presupuesto
        {
            EsPlantilla = true,
            PresupuestoOrigenId = origen.Id,
            Nombre = TextoPresupuesto.Recortar(string.IsNullOrWhiteSpace(nombre) ? origen.Nombre : nombre, 200),
            Version = 1,
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
        CopiarContenido(plantilla, origen, false);
        _context.Presupuestos.Add(plantilla);
        await _context.SaveChangesAsync(cancellationToken);
        return ResultadoOperacion<PresupuestoCreadoDto>.Ok(new PresupuestoCreadoDto(plantilla.Id));
    }

    private static CopiaContenido CopiarContenido(Presupuesto destino, Presupuesto origen, bool conservarCantidades)
    {
        var copia = new CopiaContenido();
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
                    Cantidad = conservarCantidades ? item.Cantidad : 0,
                    CantidadOrigenFactor = item.CantidadOrigenFactor == 0 ? 1 : item.CantidadOrigenFactor,
                    ValorMaterialUnitario = item.ValorMaterialUnitario,
                    ValorManoObraUnitario = item.ValorManoObraUnitario,
                    DesgloseApuJson = conservarCantidades ? item.DesgloseApuJson : null,
                    Orden = item.Orden
                };
                copia.Mapa[item.Id] = nuevo;
                if (conservarCantidades && item.CantidadOrigenItemId is int origenItem)
                {
                    copia.Referencias[nuevo] = origenItem;
                }

                if (conservarCantidades)
                {
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
                }

                nuevoCapitulo.Items.Add(nuevo);
            }

            destino.Capitulos.Add(nuevoCapitulo);
        }

        return copia;
    }

    private static void RemapearOrigenes(CopiaContenido copia)
    {
        foreach (var referencia in copia.Referencias)
        {
            referencia.Key.CantidadOrigenItemId = copia.Mapa.TryGetValue(referencia.Value, out var destino) ? destino.Id : null;
        }
    }

    private sealed class CopiaContenido
    {
        public Dictionary<int, PresupuestoItem> Mapa { get; } = [];

        public Dictionary<PresupuestoItem, int> Referencias { get; } = [];
    }

}
