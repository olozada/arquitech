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

public sealed partial class PresupuestoServicio : IPresupuestoServicio
{
    private readonly ArquitectosDbContext _context;
    private readonly MotorCostos _motor;
    private readonly OpcionesPresupuesto _opciones;
    private readonly IAuditoria _auditoria;

    public PresupuestoServicio(
        ArquitectosDbContext context,
        MotorCostos motor,
        IOptions<OpcionesPresupuesto>? opciones = null,
        IAuditoria? auditoria = null)
    {
        _context = context;
        _motor = motor;
        _opciones = opciones?.Value ?? new OpcionesPresupuesto();
        _auditoria = auditoria ?? SinAuditoria.Instancia;
    }

    public async Task<ResultadoOperacion<IReadOnlyList<PresupuestoResumenDto>>> ListarAsync(
        int? construccionId,
        int usuarioId,
        CancellationToken cancellationToken,
        string? estado = null,
        bool plantillas = false)
    {
        var actor = await ActorAsync(usuarioId, cancellationToken);
        if (actor is null)
        {
            return ResultadoOperacion<IReadOnlyList<PresupuestoResumenDto>>.Prohibido("No tiene acceso a los presupuestos.");
        }

        var consulta = _context.Presupuestos.AsNoTracking().Include(presupuesto => presupuesto.Construccion).AsQueryable();
        consulta = plantillas
            ? consulta.Where(presupuesto => presupuesto.EsPlantilla)
            : consulta.Where(presupuesto => !presupuesto.EsPlantilla);
        if (construccionId is > 0)
        {
            consulta = consulta.Where(presupuesto => presupuesto.ConstruccionId == construccionId);
        }

        if (!string.IsNullOrWhiteSpace(estado) && Enum.TryParse<EstadoPresupuesto>(estado, true, out var estadoFiltro))
        {
            consulta = consulta.Where(presupuesto => presupuesto.Estado == estadoFiltro);
        }

        if (!actor.Administrador)
        {
            if (plantillas)
            {
                return ResultadoOperacion<IReadOnlyList<PresupuestoResumenDto>>.Ok([]);
            }

            var obras = await ObrasDelArquitectoAsync(actor.ArquitectoId!.Value, cancellationToken);
            consulta = consulta.Where(presupuesto => presupuesto.ConstruccionId != null && obras.Contains(presupuesto.ConstruccionId.Value));
        }

        var presupuestos = await consulta
            .Include(presupuesto => presupuesto.Capitulos)
            .ThenInclude(capitulo => capitulo.Items)
            .OrderByDescending(presupuesto => presupuesto.Id)
            .ToListAsync(cancellationToken);
        var itemPresupuesto = presupuestos
            .SelectMany(presupuesto => presupuesto.Capitulos.SelectMany(capitulo => capitulo.Items.Select(item => new { ItemId = item.Id, PresupuestoId = presupuesto.Id })))
            .ToDictionary(par => par.ItemId, par => par.PresupuestoId);
        var ejecutado = await EjecutadoPorItemAsync(itemPresupuesto.Keys.ToList(), cancellationToken);
        var porPresupuesto = ejecutado
            .GroupBy(par => itemPresupuesto[par.Key])
            .ToDictionary(grupo => grupo.Key, grupo => grupo.Sum(par => par.Value));

        var lista = presupuestos.Select(presupuesto =>
        {
            var costo = CostoDe(presupuesto);
            var gastado = porPresupuesto.GetValueOrDefault(presupuesto.Id);
            var avance = costo.Subtotal == 0 ? 0 : gastado / costo.Subtotal;
            return new PresupuestoResumenDto(
                presupuesto.Id,
                presupuesto.ConstruccionId ?? 0,
                presupuesto.Construccion?.Descripcion ?? "",
                presupuesto.Nombre,
                presupuesto.Version,
                presupuesto.Estado.ToString(),
                costo.Total,
                costo.ValorPorM2,
                avance);
        }).ToList();
        return ResultadoOperacion<IReadOnlyList<PresupuestoResumenDto>>.Ok(lista);
    }

    public async Task<ResultadoOperacion<PresupuestoDetalleDto>> ObtenerAsync(int id, int usuarioId, CancellationToken cancellationToken)
    {
        var presupuesto = await CargarAsync(id, cancellationToken);
        if (presupuesto is null)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.NoEncontrado("No se encontró el presupuesto.");
        }

        if (!await PuedeVerAsync(usuarioId, presupuesto.ConstruccionId, cancellationToken))
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.Prohibido("No tiene acceso a este presupuesto.");
        }

        return ResultadoOperacion<PresupuestoDetalleDto>.Ok(await DetalleAsync(presupuesto, cancellationToken));
    }

    public async Task<ResultadoOperacion<PresupuestoDetalleDto>> CrearAsync(
        PresupuestoSolicitud solicitud,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        if (!await EsAdministradorAsync(usuarioId, cancellationToken))
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.Prohibido("Solo el administrador puede crear presupuestos.");
        }

        if (!await _context.Construcciones.AnyAsync(obra => obra.Id == solicitud.ConstruccionId && obra.Activo, cancellationToken))
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.Validacion("La construcción no existe.");
        }

        var version = await _context.Presupuestos
            .Where(item => item.ConstruccionId == solicitud.ConstruccionId)
            .Select(item => (int?)item.Version)
            .MaxAsync(cancellationToken);
        var presupuesto = new Presupuesto
        {
            ConstruccionId = solicitud.ConstruccionId,
            Nombre = TextoPresupuesto.Recortar(string.IsNullOrWhiteSpace(solicitud.Nombre) ? "Presupuesto" : solicitud.Nombre, 200),
            Version = (version ?? 0) + 1,
            Estado = EstadoPresupuesto.Borrador,
            Fecha = solicitud.Fecha == default ? DateOnly.FromDateTime(DateTime.UtcNow) : solicitud.Fecha
        };
        CopiarEncabezado(presupuesto, solicitud);
        var error = await CopiarArbolAsync(presupuesto, solicitud, cancellationToken);
        if (error is not null)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.Validacion(error);
        }

        await AsegurarConceptosAsync(presupuesto, cancellationToken);
        _context.Presupuestos.Add(presupuesto);
        await AplicarCalculoAsync(presupuesto, true, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return await ObtenerAsync(presupuesto.Id, usuarioId, cancellationToken);
    }

    public async Task<ResultadoOperacion<PresupuestoDetalleDto>> ActualizarAsync(
        int id,
        PresupuestoSolicitud solicitud,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        var presupuesto = await CargarEditableAsync(id, cancellationToken);
        if (presupuesto is null)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.NoEncontrado("No se encontró el presupuesto o no está en borrador.");
        }

        if (!await EsAdministradorAsync(usuarioId, cancellationToken))
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.Prohibido("Solo el administrador puede editar presupuestos.");
        }

        if (VersionDistinta(presupuesto, solicitud.RowVersion))
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.Conflicto("El presupuesto fue modificado por otro usuario.");
        }

        presupuesto.Nombre = TextoPresupuesto.Recortar(string.IsNullOrWhiteSpace(solicitud.Nombre) ? presupuesto.Nombre : solicitud.Nombre, 200);
        if (solicitud.Fecha != default)
        {
            presupuesto.Fecha = solicitud.Fecha;
        }

        CopiarEncabezado(presupuesto, solicitud);
        var error = await ReemplazarArbolAsync(presupuesto, solicitud, cancellationToken);
        if (error is not null)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.Validacion(error);
        }

        await AsegurarConceptosAsync(presupuesto, cancellationToken);
        await AplicarCalculoAsync(presupuesto, true, cancellationToken);
        var conflicto = await GuardarAsync(cancellationToken);
        if (conflicto is not null)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.Conflicto(conflicto);
        }

        return await ObtenerAsync(id, usuarioId, cancellationToken);
    }

    public async Task<ResultadoOperacion<bool>> EliminarAsync(int id, int usuarioId, CancellationToken cancellationToken, string? rowVersion = null)
    {
        if (!await EsAdministradorAsync(usuarioId, cancellationToken))
        {
            return ResultadoOperacion<bool>.Prohibido("Solo el administrador puede eliminar presupuestos.");
        }

        var presupuesto = await _context.Presupuestos.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (presupuesto is null)
        {
            return ResultadoOperacion<bool>.NoEncontrado("No se encontró el presupuesto.");
        }

        if (VersionDistinta(presupuesto, rowVersion))
        {
            return ResultadoOperacion<bool>.Conflicto("El presupuesto fue modificado por otro usuario.");
        }

        if (presupuesto.Estado != EstadoPresupuesto.Borrador)
        {
            return ResultadoOperacion<bool>.Validacion("Solo se puede eliminar un presupuesto en borrador.");
        }

        var hijos = await _context.Presupuestos.Where(item => item.PresupuestoOrigenId == id).ToListAsync(cancellationToken);
        foreach (var hijo in hijos)
        {
            hijo.PresupuestoOrigenId = null;
        }

        var ligado = await _context.ActaDetalles.AnyAsync(
            detalle => detalle.PresupuestoItem != null && detalle.PresupuestoItem.Capitulo.PresupuestoId == id,
            cancellationToken);
        if (ligado)
        {
            return ResultadoOperacion<bool>.Conflicto("Hay actas ligadas a ítems de este presupuesto.");
        }

        _context.Presupuestos.Remove(presupuesto);
        await _context.SaveChangesAsync(cancellationToken);
        return ResultadoOperacion<bool>.Ok(true);
    }

    public async Task<ResultadoOperacion<RecalculoDto>> RecalcularAsync(int id, bool aplicar, int usuarioId, CancellationToken cancellationToken, string? rowVersion = null)
    {
        var presupuesto = await CargarAsync(id, cancellationToken);
        if (presupuesto is null)
        {
            return ResultadoOperacion<RecalculoDto>.NoEncontrado("No se encontró el presupuesto.");
        }

        if (!await PuedeVerAsync(usuarioId, presupuesto.ConstruccionId, cancellationToken))
        {
            return ResultadoOperacion<RecalculoDto>.Prohibido("No tiene acceso a este presupuesto.");
        }

        if (aplicar && presupuesto.Estado != EstadoPresupuesto.Borrador)
        {
            return ResultadoOperacion<RecalculoDto>.Validacion("Solo un borrador puede guardar el recálculo.");
        }

        if (aplicar && !await EsAdministradorAsync(usuarioId, cancellationToken))
        {
            return ResultadoOperacion<RecalculoDto>.Prohibido("Solo el administrador puede guardar el recálculo.");
        }

        if (aplicar && VersionDistinta(presupuesto, rowVersion))
        {
            return ResultadoOperacion<RecalculoDto>.Conflicto("El presupuesto fue modificado por otro usuario.");
        }

        var actual = CostoDe(presupuesto);
        var propuesta = await ProponerAsync(presupuesto, cancellationToken);
        if (aplicar)
        {
            await AplicarCalculoAsync(presupuesto, true, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return ResultadoOperacion<RecalculoDto>.Ok(new RecalculoDto(actual, propuesta.Costo, propuesta.Diferencias, aplicar));
    }

    public async Task<ResultadoOperacion<PresupuestoDetalleDto>> AprobarAsync(int id, int usuarioId, CancellationToken cancellationToken, bool confirmarCantidadesCero = false)
    {
        if (!await EsAdministradorAsync(usuarioId, cancellationToken))
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.Prohibido("Solo el administrador puede aprobar.");
        }

        var presupuesto = await CargarAsync(id, cancellationToken);
        if (presupuesto is null)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.NoEncontrado("No se encontró el presupuesto.");
        }

        if (presupuesto.EsPlantilla)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.Validacion("Una plantilla no se aprueba.");
        }

        if (presupuesto.Estado != EstadoPresupuesto.Borrador)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.Validacion("Solo se aprueba un presupuesto en borrador.");
        }

        var validacion = await ValidarInternoAsync(presupuesto, cancellationToken);
        var bloqueos = validacion.Where(aviso => aviso.Tipo != "CantidadCero").ToList();
        if (bloqueos.Count > 0)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.Validacion(string.Join(" ", bloqueos.Select(aviso => aviso.Mensaje)));
        }

        if (validacion.Any(aviso => aviso.Tipo == "CantidadCero") && !confirmarCantidadesCero)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.Validacion("Hay cantidades en cero. Confirme para aprobar.");
        }

        await using var transaccion = await _context.Database.BeginTransactionAsync(cancellationToken);
        var anteriores = await _context.Presupuestos
            .Where(item => item.ConstruccionId == presupuesto.ConstruccionId && item.Estado == EstadoPresupuesto.Aprobado && item.Id != id)
            .ToListAsync(cancellationToken);
        foreach (var anterior in anteriores)
        {
            await _auditoria.RegistrarAsync(
                "Presupuesto",
                anterior.Id.ToString(),
                AccionAuditoria.Cerrar,
                usuarioId,
                CambiosAuditoria.Json("Estado", anterior.Estado.ToString(), EstadoPresupuesto.Cerrado.ToString()),
                cancellationToken);
            anterior.Estado = EstadoPresupuesto.Cerrado;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await AplicarCalculoAsync(presupuesto, true, cancellationToken);
        var contexto = await _motor.CargarAsync(cancellationToken);
        foreach (var item in presupuesto.Capitulos.SelectMany(capitulo => capitulo.Items))
        {
            if (item.ApuId is int apuId && MotorCostos.Calcular(apuId, contexto) is ResultadoApu resultado)
            {
                item.DesgloseApuJson = JsonSerializer.Serialize(new { resultado.Total, resultado.Grupos });
            }
        }

        await _auditoria.RegistrarAsync(
            "Presupuesto",
            presupuesto.Id.ToString(),
            AccionAuditoria.Aprobar,
            usuarioId,
            CambiosAuditoria.Json("Estado", presupuesto.Estado.ToString(), EstadoPresupuesto.Aprobado.ToString()),
            cancellationToken);
        presupuesto.Estado = EstadoPresupuesto.Aprobado;
        presupuesto.FechaAprobacion = DateTime.UtcNow;
        presupuesto.UsuarioAprobacionId = usuarioId;
        await _context.SaveChangesAsync(cancellationToken);
        await transaccion.CommitAsync(cancellationToken);
        return await ObtenerAsync(id, usuarioId, cancellationToken);
    }

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

    public async Task<ResultadoOperacion<PresupuestoDetalleDto>> CerrarAsync(int id, int usuarioId, CancellationToken cancellationToken)
    {
        if (!await EsAdministradorAsync(usuarioId, cancellationToken))
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.Prohibido("Solo el administrador puede cerrar un presupuesto.");
        }

        var presupuesto = await _context.Presupuestos.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (presupuesto is null)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.NoEncontrado("No se encontró el presupuesto.");
        }

        await _auditoria.RegistrarAsync(
            "Presupuesto",
            presupuesto.Id.ToString(),
            AccionAuditoria.Cerrar,
            usuarioId,
            CambiosAuditoria.Json("Estado", presupuesto.Estado.ToString(), EstadoPresupuesto.Cerrado.ToString()),
            cancellationToken);
        presupuesto.Estado = EstadoPresupuesto.Cerrado;
        await _context.SaveChangesAsync(cancellationToken);
        return await ObtenerAsync(id, usuarioId, cancellationToken);
    }

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

    public Task<ResultadoOperacion<byte[]>> ExcelAsync(int id, int usuarioId, CancellationToken cancellationToken) =>
        ExportarAsync(id, usuarioId, (presupuesto, token) => new ExportadorPresupuesto(_context).ExcelAsync(presupuesto, token), cancellationToken);

    public Task<ResultadoOperacion<byte[]>> PdfAsync(int id, int usuarioId, CancellationToken cancellationToken) =>
        ExportarAsync(id, usuarioId, (presupuesto, token) => Task.FromResult(new ExportadorPresupuesto(_context).Pdf(presupuesto)), cancellationToken);

    public async Task<ResultadoOperacion<InformeImportacionDto>> ImportarAsync(
        Stream archivo,
        int construccionId,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        if (!await EsAdministradorAsync(usuarioId, cancellationToken))
        {
            return ResultadoOperacion<InformeImportacionDto>.Prohibido("Solo el administrador puede importar.");
        }

        if (!await _context.Construcciones.AnyAsync(obra => obra.Id == construccionId && obra.Activo, cancellationToken))
        {
            return ResultadoOperacion<InformeImportacionDto>.Validacion("La construcción no existe.");
        }

        ModeloImportado modelo;
        try
        {
            modelo = LectorExcel.Leer(archivo);
        }
        catch (Exception excepcion) when (excepcion is InvalidOperationException or IOException)
        {
            return ResultadoOperacion<InformeImportacionDto>.Validacion(excepcion.Message);
        }

        var informe = await new ImportadorPresupuesto(_context).GuardarAsync(modelo, construccionId, cancellationToken);
        await _auditoria.RegistrarAsync(
            "Presupuesto",
            informe.PresupuestoId.ToString(),
            AccionAuditoria.Importar,
            usuarioId,
            null,
            cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return ResultadoOperacion<InformeImportacionDto>.Ok(informe);
    }

    public async Task<IReadOnlyList<ItemActaDto>> ItemsParaActaAsync(int construccionId, bool incluirPrecio, CancellationToken cancellationToken)
    {
        var items = await _context.Set<PresupuestoItem>().AsNoTracking()
            .Where(item => !item.Capitulo.Presupuesto.EsPlantilla && item.Capitulo.Presupuesto.ConstruccionId == construccionId && item.Capitulo.Presupuesto.Estado == EstadoPresupuesto.Aprobado)
            .OrderBy(item => item.Codigo)
            .Select(item => new ItemActaDto(
                item.Id,
                item.Codigo,
                item.Descripcion,
                item.Unidad,
                item.ConceptoId,
                item.UnidadMedidaId,
                incluirPrecio ? item.ValorMaterialUnitario + item.ValorManoObraUnitario : null))
            .ToListAsync(cancellationToken);
        return items;
    }

    private async Task<ResultadoOperacion<byte[]>> ExportarAsync(
        int id,
        int usuarioId,
        Func<Presupuesto, CancellationToken, Task<byte[]>> generar,
        CancellationToken cancellationToken)
    {
        var presupuesto = await CargarAsync(id, cancellationToken);
        if (presupuesto is null)
        {
            return ResultadoOperacion<byte[]>.NoEncontrado("No se encontró el presupuesto.");
        }

        if (!await PuedeVerAsync(usuarioId, presupuesto.ConstruccionId, cancellationToken))
        {
            return ResultadoOperacion<byte[]>.Prohibido("No tiene acceso a este presupuesto.");
        }

        return ResultadoOperacion<byte[]>.Ok(await generar(presupuesto, cancellationToken));
    }

    private async Task<Presupuesto?> CargarAsync(int id, CancellationToken cancellationToken) =>
        await _context.Presupuestos
            .Include(presupuesto => presupuesto.Construccion)
            .Include(presupuesto => presupuesto.Capitulos)
            .ThenInclude(capitulo => capitulo.Items)
            .ThenInclude(item => item.Mediciones)
            .FirstOrDefaultAsync(presupuesto => presupuesto.Id == id, cancellationToken);

    private async Task<Presupuesto?> CargarEditableAsync(int id, CancellationToken cancellationToken)
    {
        var presupuesto = await CargarAsync(id, cancellationToken);
        return presupuesto is { Estado: EstadoPresupuesto.Borrador } ? presupuesto : null;
    }

    private async Task<string?> CopiarArbolAsync(Presupuesto presupuesto, PresupuestoSolicitud solicitud, CancellationToken cancellationToken)
    {
        var error = Codigos(solicitud);
        if (error is not null)
        {
            return error;
        }

        var ordenCapitulo = 1;
        foreach (var capitulo in solicitud.Capitulos)
        {
            var nuevo = new PresupuestoCapitulo
            {
                Numero = capitulo.Numero,
                Nombre = TextoPresupuesto.Recortar(capitulo.Nombre, 200),
                Orden = ordenCapitulo++
            };
            var orden = 1;
            foreach (var item in capitulo.Items)
            {
                nuevo.Items.Add(CrearItem(item, orden++));
            }

            presupuesto.Capitulos.Add(nuevo);
        }

        return await Task.FromResult(CicloCantidades(presupuesto));
    }

    private async Task<string?> ReemplazarArbolAsync(Presupuesto presupuesto, PresupuestoSolicitud solicitud, CancellationToken cancellationToken)
    {
        var error = Codigos(solicitud);
        if (error is not null)
        {
            return error;
        }

        var idsConservados = solicitud.Capitulos.SelectMany(capitulo => capitulo.Items).Where(item => item.Id is > 0).Select(item => item.Id!.Value).ToHashSet();
        var actuales = presupuesto.Capitulos.SelectMany(capitulo => capitulo.Items).ToList();
        var eliminados = actuales.Where(item => !idsConservados.Contains(item.Id)).Select(item => item.Id).ToList();
        if (eliminados.Count > 0 && await _context.ActaDetalles.AnyAsync(detalle => detalle.PresupuestoItemId != null && eliminados.Contains(detalle.PresupuestoItemId.Value), cancellationToken))
        {
            return "No se puede quitar un ítem que ya está en un acta.";
        }

        foreach (var item in actuales.Where(item => eliminados.Contains(item.Id)))
        {
            _context.Set<PresupuestoMedicion>().RemoveRange(item.Mediciones);
            _context.Set<PresupuestoItem>().Remove(item);
            item.Capitulo.Items.Remove(item);
        }

        var ordenCapitulo = 1;
        foreach (var capitulo in solicitud.Capitulos)
        {
            var entidad = capitulo.Id is > 0
                ? presupuesto.Capitulos.FirstOrDefault(item => item.Id == capitulo.Id)
                : null;
            if (entidad is null)
            {
                entidad = new PresupuestoCapitulo { Nombre = "" };
                presupuesto.Capitulos.Add(entidad);
            }

            entidad.Numero = capitulo.Numero;
            entidad.Nombre = TextoPresupuesto.Recortar(capitulo.Nombre, 200);
            entidad.Orden = ordenCapitulo++;
            var orden = 1;
            foreach (var item in capitulo.Items)
            {
                var fila = item.Id is > 0 ? actuales.FirstOrDefault(actual => actual.Id == item.Id) : null;
                if (fila is null)
                {
                    fila = CrearItem(item, orden++);
                    entidad.Items.Add(fila);
                }
                else
                {
                    if (fila.Capitulo != entidad)
                    {
                        fila.Capitulo.Items.Remove(fila);
                        entidad.Items.Add(fila);
                    }

                    CopiarItem(fila, item, orden++);
                    _context.Set<PresupuestoMedicion>().RemoveRange(fila.Mediciones);
                    fila.Mediciones.Clear();
                    foreach (var medicion in item.Mediciones)
                    {
                        fila.Mediciones.Add(Medicion(medicion));
                    }
                }
            }
        }

        var idsCapitulos = solicitud.Capitulos.Where(capitulo => capitulo.Id is > 0).Select(capitulo => capitulo.Id!.Value).ToHashSet();
        foreach (var capitulo in presupuesto.Capitulos.ToList())
        {
            var tieneVivos = capitulo.Items.Any(item => _context.Entry(item).State != EntityState.Deleted);
            if (capitulo.Id > 0 && !idsCapitulos.Contains(capitulo.Id) && !tieneVivos)
            {
                _context.Set<PresupuestoCapitulo>().Remove(capitulo);
                presupuesto.Capitulos.Remove(capitulo);
            }
        }

        var ciclo = CicloCantidades(presupuesto);
        return ciclo;
    }

    private static string? CicloCantidades(Presupuesto presupuesto)
    {
        var porId = presupuesto.Capitulos.SelectMany(capitulo => capitulo.Items).Where(item => item.Id > 0).ToDictionary(item => item.Id);
        foreach (var inicio in porId.Values)
        {
            var visto = new HashSet<int>();
            var actual = inicio;
            while (actual.CantidadOrigenItemId is int origen && porId.TryGetValue(origen, out var siguiente))
            {
                if (!visto.Add(actual.Id))
                {
                    return "No se permite una referencia circular entre ítems.";
                }

                actual = siguiente;
            }
        }

        return null;
    }

    private async Task AsegurarConceptosAsync(Presupuesto presupuesto, CancellationToken cancellationToken)
    {
        var conceptos = await _context.Conceptos.ToListAsync(cancellationToken);
        var indice = new Dictionary<string, Concepto>();
        foreach (var concepto in conceptos)
        {
            indice.TryAdd(TextoPresupuesto.Normalizar(concepto.Descripcion), concepto);
        }

        foreach (var item in presupuesto.Capitulos.SelectMany(capitulo => capitulo.Items).Where(item => item.ConceptoId is not > 0 && !string.IsNullOrWhiteSpace(item.Descripcion)))
        {
            var concepto = ImportadorPresupuesto.AsegurarConcepto(_context, indice, item.Descripcion, item.Unidad);
            item.Concepto = concepto;
            if (concepto.Id > 0)
            {
                item.ConceptoId = concepto.Id;
            }
        }
    }

    private static PresupuestoItem CrearItem(ItemPresupuestoSolicitud solicitud, int orden)
    {
        var item = new PresupuestoItem { Codigo = "", Descripcion = "" };
        CopiarItem(item, solicitud, orden);
        foreach (var medicion in solicitud.Mediciones)
        {
            item.Mediciones.Add(Medicion(medicion));
        }

        return item;
    }

    private static void CopiarItem(PresupuestoItem item, ItemPresupuestoSolicitud solicitud, int orden)
    {
        item.Codigo = TextoPresupuesto.Recortar(solicitud.Codigo, 20);
        item.Descripcion = TextoPresupuesto.Recortar(solicitud.Descripcion, 500);
        item.Unidad = TextoPresupuesto.Recortar(solicitud.Unidad, 20);
        item.UnidadMedidaId = solicitud.UnidadMedidaId is > 0 ? solicitud.UnidadMedidaId : null;
        item.ApuId = solicitud.ApuId is > 0 ? solicitud.ApuId : null;
        item.FactorConversionApu = solicitud.FactorConversionApu == 0 ? 1 : solicitud.FactorConversionApu;
        item.ConceptoId = solicitud.ConceptoId is > 0 ? solicitud.ConceptoId : null;
        item.Cantidad = solicitud.Cantidad;
        item.CantidadOrigenItemId = solicitud.CantidadOrigenItemId is > 0 ? solicitud.CantidadOrigenItemId : null;
        item.CantidadOrigenFactor = solicitud.CantidadOrigenFactor == 0 ? 1 : solicitud.CantidadOrigenFactor;
        item.ValorMaterialUnitario = solicitud.ValorMaterialUnitario;
        item.ValorManoObraUnitario = solicitud.ValorManoObraUnitario;
        item.Orden = orden;
    }

    private static PresupuestoMedicion Medicion(MedicionSolicitud solicitud)
    {
        var parcial = CalculadoraApu.ParcialMedicion(solicitud.Veces == 0 ? 1 : solicitud.Veces, solicitud.Largo, solicitud.Ancho, solicitud.Alto);
        return new PresupuestoMedicion
        {
            Ubicacion = TextoPresupuesto.Recortar(solicitud.Ubicacion, 200),
            Veces = solicitud.Veces == 0 ? 1 : solicitud.Veces,
            Largo = solicitud.Largo,
            Ancho = solicitud.Ancho,
            Alto = solicitud.Alto,
            Parcial = parcial
        };
    }

    private async Task AplicarCalculoAsync(Presupuesto presupuesto, bool escribirPrecios, CancellationToken cancellationToken)
    {
        var items = presupuesto.Capitulos.SelectMany(capitulo => capitulo.Items).ToList();
        foreach (var item in items)
        {
            if (item.Mediciones.Count > 0 && item.CantidadOrigenItemId is null)
            {
                item.Cantidad = item.Mediciones.Sum(medicion => medicion.Parcial);
            }
        }

        for (var paso = 0; paso < items.Count; paso++)
        {
            var cambio = false;
            foreach (var item in items)
            {
                if (item.CantidadOrigenItemId is not int origenId)
                {
                    continue;
                }

                var origen = items.FirstOrDefault(candidato => candidato.Id == origenId);
                if (origen is null)
                {
                    continue;
                }

                var cantidad = origen.Cantidad * (item.CantidadOrigenFactor == 0 ? 1 : item.CantidadOrigenFactor);
                if (cantidad != item.Cantidad)
                {
                    item.Cantidad = cantidad;
                    cambio = true;
                }
            }

            if (!cambio)
            {
                break;
            }
        }

        if (!escribirPrecios)
        {
            return;
        }

        var contexto = await _motor.CargarAsync(cancellationToken);
        foreach (var item in items.Where(fila => fila.ApuId is > 0))
        {
            if (MotorCostos.Calcular(item.ApuId!.Value, contexto) is not ResultadoApu resultado)
            {
                continue;
            }

            var (material, manoObra) = CalculadoraPresupuesto.UnitariosDesdeApu(resultado.Grupos, item.FactorConversionApu);
            item.ValorMaterialUnitario = material;
            item.ValorManoObraUnitario = manoObra;
        }
    }

    private async Task<(CostoPresupuestoDto Costo, List<DiferenciaItemDto> Diferencias)> ProponerAsync(
        Presupuesto presupuesto,
        CancellationToken cancellationToken)
    {
        var contexto = await _motor.CargarAsync(cancellationToken);
        decimal material = 0;
        decimal manoObra = 0;
        var diferencias = new List<DiferenciaItemDto>();
        foreach (var item in presupuesto.Capitulos.SelectMany(capitulo => capitulo.Items))
        {
            var nuevoMaterial = item.ValorMaterialUnitario;
            var nuevaManoObra = item.ValorManoObraUnitario;
            if (item.ApuId is int apuId && MotorCostos.Calcular(apuId, contexto) is ResultadoApu resultado)
            {
                (nuevoMaterial, nuevaManoObra) = CalculadoraPresupuesto.UnitariosDesdeApu(resultado.Grupos, item.FactorConversionApu);
            }

            material += item.Cantidad * nuevoMaterial;
            manoObra += item.Cantidad * nuevaManoObra;
            var delta = item.Cantidad * ((nuevoMaterial + nuevaManoObra) - (item.ValorMaterialUnitario + item.ValorManoObraUnitario));
            if (Math.Abs(delta) > 0.5m)
            {
                diferencias.Add(new DiferenciaItemDto(item.Codigo, item.Descripcion, item.ValorMaterialUnitario, nuevoMaterial, item.ValorManoObraUnitario, nuevaManoObra, delta));
            }
        }

        return (Resumen(presupuesto, material + manoObra, material, manoObra), diferencias);
    }

    private async Task<PresupuestoDetalleDto> DetalleAsync(Presupuesto presupuesto, CancellationToken cancellationToken)
    {
        var apus = await _context.Apus.AsNoTracking()
            .Select(apu => new { apu.Id, apu.Descripcion, apu.Unidad })
            .ToDictionaryAsync(apu => apu.Id, cancellationToken);
        var costo = CostoDe(presupuesto);
        var capitulos = presupuesto.Capitulos.OrderBy(capitulo => capitulo.Orden).Select(capitulo =>
        {
            var items = capitulo.Items.OrderBy(item => item.Orden).Select(item =>
            {
                string? apu = null;
                string? unidadApu = null;
                if (item.ApuId is int apuId && apus.TryGetValue(apuId, out var datos))
                {
                    apu = datos.Descripcion;
                    unidadApu = datos.Unidad;
                }

                var unitario = item.ValorMaterialUnitario + item.ValorManoObraUnitario;
                return new ItemPresupuestoDto(
                    item.Id,
                    item.Codigo,
                    item.Descripcion,
                    item.Unidad,
                    item.UnidadMedidaId,
                    item.ApuId,
                    apu,
                    unidadApu,
                    item.FactorConversionApu,
                    item.ApuId is > 0 && item.FactorConversionApu == 1 && !TextoPresupuesto.MismaUnidad(item.Unidad, unidadApu),
                    item.ConceptoId,
                    item.Cantidad,
                    item.CantidadOrigenItemId,
                    item.CantidadOrigenFactor,
                    item.ValorMaterialUnitario,
                    item.ValorManoObraUnitario,
                    unitario,
                    item.Cantidad * unitario,
                    item.ApuId is > 0,
                    item.Mediciones.Select(medicion => new MedicionDto(medicion.Id, medicion.Ubicacion, medicion.Veces, medicion.Largo, medicion.Ancho, medicion.Alto, medicion.Parcial)).ToList());
            }).ToList();
            var total = items.Sum(item => item.Total);
            return new CapituloPresupuestoDto(capitulo.Id, capitulo.Numero, capitulo.Nombre, total, costo.Subtotal == 0 ? 0 : total / costo.Subtotal, items);
        }).ToList();

        return new PresupuestoDetalleDto(
            presupuesto.Id,
            presupuesto.ConstruccionId ?? 0,
            presupuesto.Construccion?.Descripcion ?? "",
            presupuesto.Nombre,
            presupuesto.Version,
            presupuesto.Estado.ToString(),
            presupuesto.Fecha,
            presupuesto.MesesProyecto,
            presupuesto.AreaConstruidaM2,
            presupuesto.PctImprevistos,
            presupuesto.PctIncremento,
            presupuesto.PctAdministracion,
            presupuesto.PctUtilidad,
            presupuesto.PctIvaUtilidad,
            presupuesto.FechaAprobacion,
            costo,
            capitulos,
            Convert.ToBase64String(presupuesto.RowVersion ?? []),
            presupuesto.EsPlantilla);
    }

    private static void CopiarEncabezado(Presupuesto presupuesto, PresupuestoSolicitud solicitud)
    {
        presupuesto.MesesProyecto = solicitud.MesesProyecto;
        presupuesto.AreaConstruidaM2 = solicitud.AreaConstruidaM2;
        presupuesto.PctImprevistos = solicitud.PctImprevistos;
        presupuesto.PctIncremento = solicitud.PctIncremento;
        presupuesto.PctAdministracion = solicitud.PctAdministracion;
        presupuesto.PctUtilidad = solicitud.PctUtilidad;
        presupuesto.PctIvaUtilidad = solicitud.PctIvaUtilidad;
    }

    private static string? Codigos(PresupuestoSolicitud solicitud)
    {
        var repetido = solicitud.Capitulos.SelectMany(capitulo => capitulo.Items)
            .Select(item => item.Codigo.Trim())
            .Where(codigo => codigo.Length > 0)
            .GroupBy(codigo => codigo)
            .FirstOrDefault(grupo => grupo.Count() > 1);
        return repetido is null ? null : $"El código {repetido.Key} está repetido en el presupuesto.";
    }

    private static CostoPresupuestoDto CostoDe(Presupuesto presupuesto)
    {
        var items = presupuesto.Capitulos.SelectMany(capitulo => capitulo.Items).ToList();
        var material = items.Sum(item => item.Cantidad * item.ValorMaterialUnitario);
        var manoObra = items.Sum(item => item.Cantidad * item.ValorManoObraUnitario);
        return Resumen(presupuesto, material + manoObra, material, manoObra);
    }

    private static CostoPresupuestoDto Resumen(Presupuesto presupuesto, decimal subtotal, decimal material, decimal manoObra)
    {
        var resumen = CalculadoraPresupuesto.Resumir(
            subtotal,
            material,
            manoObra,
            presupuesto.PctImprevistos,
            presupuesto.PctIncremento,
            presupuesto.PctAdministracion,
            presupuesto.PctUtilidad,
            presupuesto.PctIvaUtilidad,
            presupuesto.AreaConstruidaM2);
        return new CostoPresupuestoDto(
            resumen.Subtotal,
            resumen.TotalMaterial,
            resumen.TotalManoObra,
            resumen.Imprevistos,
            resumen.Incremento,
            resumen.Administracion,
            resumen.Utilidad,
            resumen.IvaUtilidad,
            resumen.Total,
            resumen.ValorPorM2);
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

    private async Task<bool> EsAdministradorAsync(int usuarioId, CancellationToken cancellationToken)
    {
        var actor = await ActorAsync(usuarioId, cancellationToken);
        return actor is { Administrador: true };
    }

    private async Task<bool> PuedeVerAsync(int usuarioId, int? construccionId, CancellationToken cancellationToken)
    {
        var actor = await ActorAsync(usuarioId, cancellationToken);
        if (actor is null)
        {
            return false;
        }

        if (actor.Administrador)
        {
            return true;
        }

        if (construccionId is not int obraId)
        {
            return false;
        }

        var obras = await ObrasDelArquitectoAsync(actor.ArquitectoId!.Value, cancellationToken);
        return obras.Contains(obraId);
    }

    private async Task<HashSet<int>> ObrasDelArquitectoAsync(int arquitectoId, CancellationToken cancellationToken)
    {
        var obras = await _context.ActaDetalles.AsNoTracking()
            .Where(detalle => detalle.Acta.ArquitectoId == arquitectoId)
            .Select(detalle => detalle.ConstruccionId)
            .Distinct()
            .ToListAsync(cancellationToken);
        return obras.ToHashSet();
    }

    private async Task<Actor?> ActorAsync(int usuarioId, CancellationToken cancellationToken)
    {
        var usuario = await _context.Usuarios.AsNoTracking()
            .Where(item => item.Id == usuarioId)
            .Select(item => new { item.TipoUsuarioId, item.ArquitectoId })
            .FirstOrDefaultAsync(cancellationToken);
        if (usuario is null)
        {
            return null;
        }

        if (usuario.TipoUsuarioId == PerfilesUsuario.Administrador)
        {
            return new Actor(true, null);
        }

        if (usuario.TipoUsuarioId == PerfilesUsuario.Arquitecto && usuario.ArquitectoId is int arquitectoId)
        {
            return new Actor(false, arquitectoId);
        }

        return null;
    }

    private sealed record Actor(bool Administrador, int? ArquitectoId);
}
