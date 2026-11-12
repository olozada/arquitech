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

    public async Task<ResultadoOperacion<PresupuestoDetalleDto>> AgregarDesdeApuAsync(
        int id,
        int capituloId,
        ItemsDesdeApuSolicitud solicitud,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        var presupuesto = await BorradorDeAdministradorAsync(id, solicitud.RowVersion, usuarioId, cancellationToken);
        if (presupuesto.Estado != EstadoOperacion.Ok)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.MismoEstado(presupuesto);
        }

        var capitulo = presupuesto.Valor!.Capitulos.FirstOrDefault(item => item.Id == capituloId);
        if (capitulo is null)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.NoEncontrado("No se encontró el capítulo.");
        }

        var ids = solicitud.ApuIds.Where(item => item > 0).Distinct().ToList();
        var apus = await _context.Apus.AsNoTracking().Where(apu => ids.Contains(apu.Id) && apu.Activo).ToListAsync(cancellationToken);
        if (apus.Count != ids.Count)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.Validacion("Hay análisis de precios que no existen.");
        }

        var orden = capitulo.Items.Select(item => item.Orden).DefaultIfEmpty(0).Max();
        foreach (var apuId in ids)
        {
            var apu = apus.First(item => item.Id == apuId);
            orden++;
            capitulo.Items.Add(new PresupuestoItem
            {
                Codigo = SiguienteCodigo(presupuesto.Valor, capitulo.Numero),
                Descripcion = TextoPresupuesto.Recortar(apu.Descripcion, 500),
                Unidad = TextoPresupuesto.Recortar(apu.Unidad, 20),
                UnidadMedidaId = apu.UnidadMedidaId,
                ApuId = apu.Id,
                FactorConversionApu = 1,
                ConceptoId = apu.ConceptoId,
                Cantidad = 0,
                CantidadOrigenFactor = 1,
                Orden = orden
            });
        }

        await AsegurarConceptosAsync(presupuesto.Valor, cancellationToken);
        await AplicarCalculoAsync(presupuesto.Valor, true, cancellationToken);
        var conflicto = await GuardarAsync(cancellationToken);
        return conflicto is null
            ? await ObtenerAsync(id, usuarioId, cancellationToken)
            : ResultadoOperacion<PresupuestoDetalleDto>.Conflicto(conflicto);
    }

    public async Task<ResultadoOperacion<PresupuestoDetalleDto>> RenumerarAsync(int id, string? rowVersion, int usuarioId, CancellationToken cancellationToken)
    {
        var presupuesto = await BorradorDeAdministradorAsync(id, rowVersion, usuarioId, cancellationToken);
        if (presupuesto.Estado != EstadoOperacion.Ok)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.MismoEstado(presupuesto);
        }

        var numero = 1;
        foreach (var capitulo in presupuesto.Valor!.Capitulos.OrderBy(item => item.Orden).ThenBy(item => item.Id))
        {
            capitulo.Numero = numero;
            var indice = 1;
            foreach (var item in capitulo.Items.OrderBy(fila => fila.Orden).ThenBy(fila => fila.Id))
            {
                item.Codigo = $"{numero}.{indice:00}";
                indice++;
            }

            numero++;
        }

        var conflicto = await GuardarAsync(cancellationToken);
        return conflicto is null
            ? await ObtenerAsync(id, usuarioId, cancellationToken)
            : ResultadoOperacion<PresupuestoDetalleDto>.Conflicto(conflicto);
    }

    public async Task<ResultadoOperacion<PresupuestoDetalleDto>> OrdenarAsync(int id, OrdenPresupuestoSolicitud solicitud, int usuarioId, CancellationToken cancellationToken)
    {
        var presupuesto = await BorradorDeAdministradorAsync(id, solicitud.RowVersion, usuarioId, cancellationToken);
        if (presupuesto.Estado != EstadoOperacion.Ok)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.MismoEstado(presupuesto);
        }

        foreach (var pedido in solicitud.Capitulos)
        {
            var capitulo = presupuesto.Valor!.Capitulos.FirstOrDefault(item => item.Id == pedido.Id);
            if (capitulo is null)
            {
                continue;
            }

            capitulo.Orden = pedido.Orden;
            var posicion = 1;
            foreach (var itemId in pedido.Items)
            {
                var item = capitulo.Items.FirstOrDefault(fila => fila.Id == itemId);
                if (item is not null)
                {
                    item.Orden = posicion++;
                }
            }
        }

        var conflicto = await GuardarAsync(cancellationToken);
        return conflicto is null
            ? await ObtenerAsync(id, usuarioId, cancellationToken)
            : ResultadoOperacion<PresupuestoDetalleDto>.Conflicto(conflicto);
    }

    public async Task<ResultadoOperacion<PresupuestoDetalleDto>> DuplicarItemAsync(int id, int itemId, string? rowVersion, int usuarioId, CancellationToken cancellationToken)
    {
        var presupuesto = await BorradorDeAdministradorAsync(id, rowVersion, usuarioId, cancellationToken);
        if (presupuesto.Estado != EstadoOperacion.Ok)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.MismoEstado(presupuesto);
        }

        var capitulo = presupuesto.Valor!.Capitulos.FirstOrDefault(item => item.Items.Any(fila => fila.Id == itemId));
        var origen = capitulo?.Items.FirstOrDefault(item => item.Id == itemId);
        if (capitulo is null || origen is null)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.NoEncontrado("No se encontró el ítem.");
        }

        foreach (var item in capitulo.Items.Where(item => item.Orden > origen.Orden))
        {
            item.Orden++;
        }

        capitulo.Items.Add(new PresupuestoItem
        {
            Codigo = SiguienteCodigo(presupuesto.Valor, capitulo.Numero),
            Descripcion = origen.Descripcion,
            Unidad = origen.Unidad,
            UnidadMedidaId = origen.UnidadMedidaId,
            ApuId = origen.ApuId,
            FactorConversionApu = origen.FactorConversionApu,
            ConceptoId = origen.ConceptoId,
            Cantidad = origen.Cantidad,
            CantidadOrigenFactor = origen.CantidadOrigenFactor,
            ValorMaterialUnitario = origen.ValorMaterialUnitario,
            ValorManoObraUnitario = origen.ValorManoObraUnitario,
            Orden = origen.Orden + 1
        });
        var conflicto = await GuardarAsync(cancellationToken);
        return conflicto is null
            ? await ObtenerAsync(id, usuarioId, cancellationToken)
            : ResultadoOperacion<PresupuestoDetalleDto>.Conflicto(conflicto);
    }

    public async Task<ResultadoOperacion<PresupuestoDetalleDto>> MoverItemAsync(int id, int itemId, MoverItemSolicitud solicitud, int usuarioId, CancellationToken cancellationToken)
    {
        var presupuesto = await BorradorDeAdministradorAsync(id, solicitud.RowVersion, usuarioId, cancellationToken);
        if (presupuesto.Estado != EstadoOperacion.Ok)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.MismoEstado(presupuesto);
        }

        var origen = presupuesto.Valor!.Capitulos.FirstOrDefault(item => item.Items.Any(fila => fila.Id == itemId));
        var destino = presupuesto.Valor.Capitulos.FirstOrDefault(item => item.Id == solicitud.CapituloDestinoId);
        var item = origen?.Items.FirstOrDefault(fila => fila.Id == itemId);
        if (origen is null || destino is null || item is null)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.NoEncontrado("No se encontró el capítulo o el ítem.");
        }

        origen.Items.Remove(item);
        item.Capitulo = destino;
        item.CapituloId = destino.Id;
        item.Orden = destino.Items.Select(fila => fila.Orden).DefaultIfEmpty(0).Max() + 1;
        destino.Items.Add(item);
        var conflicto = await GuardarAsync(cancellationToken);
        return conflicto is null
            ? await ObtenerAsync(id, usuarioId, cancellationToken)
            : ResultadoOperacion<PresupuestoDetalleDto>.Conflicto(conflicto);
    }

    public async Task<ResultadoOperacion<IReadOnlyList<ValidacionPresupuestoDto>>> ValidarAsync(int id, int usuarioId, CancellationToken cancellationToken)
    {
        var presupuesto = await CargarAsync(id, cancellationToken);
        if (presupuesto is null)
        {
            return ResultadoOperacion<IReadOnlyList<ValidacionPresupuestoDto>>.NoEncontrado("No se encontró el presupuesto.");
        }

        if (!await PuedeVerAsync(usuarioId, presupuesto.ConstruccionId, cancellationToken))
        {
            return ResultadoOperacion<IReadOnlyList<ValidacionPresupuestoDto>>.Prohibido("No tiene acceso a este presupuesto.");
        }

        return ResultadoOperacion<IReadOnlyList<ValidacionPresupuestoDto>>.Ok(await ValidarInternoAsync(presupuesto, cancellationToken));
    }

    private async Task<ResultadoOperacion<Presupuesto>> BorradorDeAdministradorAsync(int id, string? rowVersion, int usuarioId, CancellationToken cancellationToken)
    {
        if (!await EsAdministradorAsync(usuarioId, cancellationToken))
        {
            return ResultadoOperacion<Presupuesto>.Prohibido("Solo el administrador puede editar presupuestos.");
        }

        var presupuesto = await CargarEditableAsync(id, cancellationToken);
        if (presupuesto is null)
        {
            return ResultadoOperacion<Presupuesto>.NoEncontrado("No se encontró el presupuesto o no está en borrador.");
        }

        if (VersionDistinta(presupuesto, rowVersion))
        {
            return ResultadoOperacion<Presupuesto>.Conflicto("El presupuesto fue modificado por otro usuario.");
        }

        return ResultadoOperacion<Presupuesto>.Ok(presupuesto);
    }

    private async Task<IReadOnlyList<ValidacionPresupuestoDto>> ValidarInternoAsync(Presupuesto presupuesto, CancellationToken cancellationToken)
    {
        var unidades = await _context.Apus.AsNoTracking()
            .Select(apu => new { apu.Id, apu.Unidad })
            .ToDictionaryAsync(apu => apu.Id, apu => apu.Unidad, cancellationToken);
        var avisos = new List<ValidacionPresupuestoDto>();
        var codigos = presupuesto.Capitulos.SelectMany(capitulo => capitulo.Items)
            .Select(item => item.Codigo.Trim())
            .Where(codigo => codigo.Length > 0)
            .GroupBy(codigo => codigo)
            .Where(grupo => grupo.Count() > 1)
            .Select(grupo => grupo.Key)
            .ToHashSet();
        foreach (var item in presupuesto.Capitulos.SelectMany(capitulo => capitulo.Items))
        {
            if (codigos.Contains(item.Codigo.Trim()))
            {
                avisos.Add(new ValidacionPresupuestoDto(item.Id, item.Codigo, "CodigoRepetido", $"El código {item.Codigo} está repetido."));
            }

            if (item.Cantidad == 0)
            {
                avisos.Add(new ValidacionPresupuestoDto(item.Id, item.Codigo, "CantidadCero", $"El ítem {item.Codigo} tiene cantidad cero."));
            }

            if (item.ApuId is null && item.ValorMaterialUnitario == 0 && item.ValorManoObraUnitario == 0)
            {
                avisos.Add(new ValidacionPresupuestoDto(item.Id, item.Codigo, "SinApuNiValores", $"El ítem {item.Codigo} no tiene análisis ni valores."));
            }

            if (item.ApuId is int apuId && item.FactorConversionApu == 1 && unidades.TryGetValue(apuId, out var unidad) && !TextoPresupuesto.MismaUnidad(item.Unidad, unidad))
            {
                avisos.Add(new ValidacionPresupuestoDto(item.Id, item.Codigo, "UnidadDistintaApu", $"La unidad de {item.Codigo} no coincide con el análisis de precios."));
            }

            if (item.ConceptoId is not > 0)
            {
                avisos.Add(new ValidacionPresupuestoDto(item.Id, item.Codigo, "SinConcepto", $"El ítem {item.Codigo} no tiene concepto."));
            }
        }

        return avisos;
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

    private static string SiguienteCodigo(Presupuesto presupuesto, int numeroCapitulo)
    {
        var prefijo = numeroCapitulo.ToString();
        var maximo = 0;
        foreach (var codigo in presupuesto.Capitulos.SelectMany(capitulo => capitulo.Items).Select(item => item.Codigo))
        {
            var partes = codigo.Split('.');
            if (partes.Length == 2 && partes[0] == prefijo && int.TryParse(partes[1], out var numero))
            {
                maximo = Math.Max(maximo, numero);
            }
        }

        var candidato = $"{prefijo}.{(maximo + 1):00}";
        var usados = presupuesto.Capitulos.SelectMany(capitulo => capitulo.Items).Select(item => item.Codigo).ToHashSet();
        while (usados.Contains(candidato))
        {
            maximo++;
            candidato = $"{prefijo}.{(maximo + 1):00}";
        }

        return candidato;
    }

    private async Task<string?> GuardarAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateConcurrencyException)
        {
            return "El presupuesto fue modificado por otro usuario.";
        }
    }

    private static bool VersionDistinta(Presupuesto presupuesto, string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            return !(presupuesto.RowVersion ?? []).SequenceEqual(Convert.FromBase64String(token));
        }
        catch (FormatException)
        {
            return true;
        }
    }
}
