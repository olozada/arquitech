using Arquitectos.Application.Presupuestos;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Infrastructure.Presupuestos;

public sealed class ApuServicio : IApuServicio
{
    private readonly ArquitectosDbContext _context;
    private readonly MotorCostos _motor;

    public ApuServicio(ArquitectosDbContext context, MotorCostos motor)
    {
        _context = context;
        _motor = motor;
    }

    public async Task<PaginaDto<ApuResumenDto>> ListarAsync(
        string? texto,
        bool incluirInactivos,
        CancellationToken cancellationToken,
        int pagina = 1,
        int tamano = Paginacion.TamanoDefecto,
        bool? esSubanalisis = null)
    {
        var (paginaOk, tamanoOk) = Paginacion.Normalizar(pagina, tamano);
        var consulta = _context.Apus.AsNoTracking();
        if (!incluirInactivos)
        {
            consulta = consulta.Where(apu => apu.Activo);
        }

        if (esSubanalisis is bool subanalisis)
        {
            consulta = consulta.Where(apu => apu.EsSubanalisis == subanalisis);
        }

        if (!string.IsNullOrWhiteSpace(texto))
        {
            var filtro = texto.Trim();
            consulta = consulta.Where(apu => apu.Descripcion.Contains(filtro) || (apu.Codigo != null && apu.Codigo.Contains(filtro)));
        }

        var total = await consulta.CountAsync(cancellationToken);
        var apus = await consulta
            .OrderBy(apu => apu.Codigo)
            .ThenBy(apu => apu.Descripcion)
            .Skip((paginaOk - 1) * tamanoOk)
            .Take(tamanoOk)
            .ToListAsync(cancellationToken);
        var contexto = await _motor.CargarAsync(cancellationToken);
        var ids = apus.Select(apu => apu.Id).ToList();
        var enItems = await _context.Set<PresupuestoItem>().AsNoTracking()
            .Where(item => item.ApuId != null && ids.Contains(item.ApuId.Value))
            .Select(item => item.ApuId!.Value)
            .ToListAsync(cancellationToken);
        var enLineas = await _context.Set<ApuLinea>().AsNoTracking()
            .Where(linea => linea.SubApuId != null && ids.Contains(linea.SubApuId.Value))
            .Select(linea => linea.SubApuId!.Value)
            .ToListAsync(cancellationToken);
        var usos = enItems.Concat(enLineas).GroupBy(id => id).ToDictionary(grupo => grupo.Key, grupo => grupo.Count());
        var items = apus.Select(apu =>
        {
            var costo = MotorCostos.Calcular(apu.Id, contexto)?.Total ?? 0;
            return new ApuResumenDto(apu.Id, apu.Codigo, apu.Descripcion, apu.Unidad, apu.EsSubanalisis, costo, apu.Activo, usos.GetValueOrDefault(apu.Id));
        }).ToList();
        return new PaginaDto<ApuResumenDto>(items, total, paginaOk, tamanoOk);
    }

    public async Task<IReadOnlyList<ApuResumenDto>> BuscarAsync(string? texto, int? unidadId, int top, CancellationToken cancellationToken)
    {
        var apus = await _context.Apus.AsNoTracking().Where(apu => apu.Activo).ToListAsync(cancellationToken);
        if (unidadId is > 0)
        {
            apus = apus.Where(apu => apu.UnidadMedidaId == unidadId).ToList();
        }

        if (!string.IsNullOrWhiteSpace(texto))
        {
            var filtro = TextoPresupuesto.Normalizar(texto);
            apus = apus.Where(apu =>
                TextoPresupuesto.Normalizar(apu.Descripcion).Contains(filtro) ||
                TextoPresupuesto.Normalizar(apu.Codigo ?? "").Contains(filtro)).ToList();
        }

        var limite = top is > 0 and <= 50 ? top : 20;
        var contexto = await _motor.CargarAsync(cancellationToken);
        return apus
            .OrderBy(apu => apu.Codigo)
            .ThenBy(apu => apu.Descripcion)
            .Take(limite)
            .Select(apu => new ApuResumenDto(apu.Id, apu.Codigo, apu.Descripcion, apu.Unidad, apu.EsSubanalisis, MotorCostos.Calcular(apu.Id, contexto)?.Total ?? 0, apu.Activo))
            .ToList();
    }

    public async Task<ApuDetalleDto?> ObtenerAsync(int id, CancellationToken cancellationToken)
    {
        var apu = await Cargar(id, cancellationToken);
        return apu is null ? null : await DetalleAsync(apu, cancellationToken);
    }

    public async Task<ResultadoOperacion<ApuDetalleDto>> GuardarAsync(int? id, ApuSolicitud solicitud, CancellationToken cancellationToken)
    {
        var descripcion = TextoPresupuesto.Recortar(solicitud.Descripcion, 500);
        if (descripcion.Length == 0)
        {
            return ResultadoOperacion<ApuDetalleDto>.Validacion("La descripción del APU es obligatoria.");
        }

        var errorLineas = ValidarLineas(solicitud);
        if (errorLineas is not null)
        {
            return ResultadoOperacion<ApuDetalleDto>.Validacion(errorLineas);
        }

        if (await HayCicloAsync(id ?? 0, solicitud, cancellationToken))
        {
            return ResultadoOperacion<ApuDetalleDto>.Validacion("Este sub-análisis crearía una referencia circular.");
        }

        Apu entidad;
        if (id is null)
        {
            entidad = new Apu { Descripcion = descripcion };
            _context.Apus.Add(entidad);
        }
        else
        {
            var existente = await _context.Apus.Include(apu => apu.Grupos).ThenInclude(grupo => grupo.Lineas)
                .FirstOrDefaultAsync(apu => apu.Id == id, cancellationToken);
            if (existente is null)
            {
                return ResultadoOperacion<ApuDetalleDto>.NoEncontrado("No se encontró el APU.");
            }

            _context.Set<ApuLinea>().RemoveRange(existente.Grupos.SelectMany(grupo => grupo.Lineas));
            _context.Set<ApuGrupo>().RemoveRange(existente.Grupos);
            entidad = existente;
        }

        Copiar(entidad, solicitud, descripcion);
        await _context.SaveChangesAsync(cancellationToken);
        var guardado = await Cargar(entidad.Id, cancellationToken);
        return ResultadoOperacion<ApuDetalleDto>.Ok((await DetalleAsync(guardado!, cancellationToken))!);
    }

    public async Task<ResultadoOperacion<bool>> EliminarAsync(int id, CancellationToken cancellationToken)
    {
        var apu = await _context.Apus.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (apu is null)
        {
            return ResultadoOperacion<bool>.NoEncontrado("No se encontró el APU.");
        }

        if (await _context.Set<ApuLinea>().AnyAsync(linea => linea.SubApuId == id, cancellationToken))
        {
            return ResultadoOperacion<bool>.Conflicto("No se puede eliminar un sub-análisis que otro APU usa.");
        }

        var items = await _context.Set<PresupuestoItem>().Include(item => item.Capitulo).ThenInclude(capitulo => capitulo.Presupuesto)
            .Where(item => item.ApuId == id)
            .ToListAsync(cancellationToken);
        if (items.Any(item => item.Capitulo.Presupuesto.Estado != EstadoPresupuesto.Borrador))
        {
            return ResultadoOperacion<bool>.Conflicto("No se puede eliminar un APU usado por un presupuesto aprobado.");
        }

        foreach (var item in items)
        {
            item.ApuId = null;
        }

        _context.Apus.Remove(apu);
        await _context.SaveChangesAsync(cancellationToken);
        return ResultadoOperacion<bool>.Ok(true);
    }

    public async Task<IReadOnlyList<UsoApuDto>> UsosAsync(int id, CancellationToken cancellationToken)
    {
        var presupuestos = await _context.Set<PresupuestoItem>().AsNoTracking()
            .Where(item => item.ApuId == id)
            .Select(item => new UsoApuDto(
                "Presupuesto",
                item.Capitulo.PresupuestoId,
                item.Capitulo.Presupuesto.Nombre + " · " + item.Codigo,
                item.Capitulo.Presupuesto.Estado == EstadoPresupuesto.Aprobado))
            .ToListAsync(cancellationToken);
        var apus = await _context.Set<ApuLinea>().AsNoTracking()
            .Where(linea => linea.SubApuId == id)
            .Select(linea => new UsoApuDto("Apu", linea.Grupo.ApuId, linea.Grupo.Apu.Descripcion, false))
            .ToListAsync(cancellationToken);
        return presupuestos.Concat(apus).DistinctBy(uso => (uso.Tipo, uso.Id, uso.Nombre)).ToList();
    }

    public async Task<ResultadoOperacion<ApuDetalleDto>> DuplicarAsync(int id, CancellationToken cancellationToken)
    {
        var origen = await _context.Apus.AsNoTracking().Include(apu => apu.Grupos).ThenInclude(grupo => grupo.Lineas)
            .FirstOrDefaultAsync(apu => apu.Id == id, cancellationToken);
        if (origen is null)
        {
            return ResultadoOperacion<ApuDetalleDto>.NoEncontrado("No se encontró el APU.");
        }

        var copia = new Apu
        {
            Codigo = null,
            Descripcion = TextoPresupuesto.Recortar(origen.Descripcion + " (copia)", 500),
            Unidad = origen.Unidad,
            UnidadMedidaId = origen.UnidadMedidaId,
            EsSubanalisis = origen.EsSubanalisis,
            DivisorGeneral = origen.DivisorGeneral,
            ConceptoId = origen.ConceptoId,
            Activo = true
        };
        foreach (var grupo in origen.Grupos.OrderBy(item => item.Orden))
        {
            var nuevo = new ApuGrupo { Grupo = grupo.Grupo, Divisor = grupo.Divisor, Orden = grupo.Orden };
            foreach (var linea in grupo.Lineas.OrderBy(item => item.Orden))
            {
                nuevo.Lineas.Add(new ApuLinea
                {
                    Orden = linea.Orden,
                    InsumoId = linea.InsumoId,
                    CuadrillaId = linea.CuadrillaId,
                    SubApuId = linea.SubApuId,
                    Descripcion = linea.Descripcion,
                    Unidad = linea.Unidad,
                    Cantidad = linea.Cantidad,
                    PrecioManual = linea.PrecioManual,
                    Modo = linea.Modo,
                    Factor = linea.Factor
                });
            }

            copia.Grupos.Add(nuevo);
        }

        _context.Apus.Add(copia);
        await _context.SaveChangesAsync(cancellationToken);
        return ResultadoOperacion<ApuDetalleDto>.Ok((await ObtenerAsync(copia.Id, cancellationToken))!);
    }

    private async Task<Apu?> Cargar(int id, CancellationToken cancellationToken) =>
        await _context.Apus.AsNoTracking()
            .Include(apu => apu.Grupos)
            .ThenInclude(grupo => grupo.Lineas)
            .FirstOrDefaultAsync(apu => apu.Id == id, cancellationToken);

    private async Task<ApuDetalleDto> DetalleAsync(Apu apu, CancellationToken cancellationToken)
    {
        var contexto = await _motor.CargarAsync(cancellationToken);
        var resultado = MotorCostos.Calcular(apu.Id, contexto);
        var grupos = apu.Grupos.OrderBy(grupo => grupo.Orden).ToList();
        var detalle = resultado?.Detalle ?? [];
        var dtos = new List<GrupoApuDto>();
        for (var indice = 0; indice < grupos.Count; indice++)
        {
            var grupo = grupos[indice];
            var valores = indice < detalle.Count ? detalle[indice].Lineas : [];
            var lineas = grupo.Lineas.OrderBy(linea => linea.Orden).ToList();
            var lineasDto = new List<LineaApuDto>();
            for (var lineaIndice = 0; lineaIndice < lineas.Count; lineaIndice++)
            {
                var linea = lineas[lineaIndice];
                var valor = lineaIndice < valores.Count ? valores[lineaIndice] : new LineaValor(0, 0);
                lineasDto.Add(new LineaApuDto(
                    linea.Id,
                    linea.Orden,
                    linea.InsumoId,
                    linea.CuadrillaId,
                    linea.SubApuId,
                    linea.Descripcion,
                    linea.Unidad,
                    linea.Cantidad,
                    linea.PrecioManual,
                    linea.Modo.ToString(),
                    linea.Factor,
                    valor.Precio,
                    valor.Valor));
            }

            dtos.Add(new GrupoApuDto(
                grupo.Id,
                grupo.Grupo.ToString(),
                grupo.Divisor,
                indice < detalle.Count ? detalle[indice].Subtotal : 0,
                lineasDto));
        }

        return new ApuDetalleDto(
            apu.Id,
            apu.Codigo,
            apu.Descripcion,
            apu.Unidad,
            apu.UnidadMedidaId,
            apu.EsSubanalisis,
            apu.DivisorGeneral,
            apu.ConceptoId,
            apu.Activo,
            resultado?.Total ?? 0,
            resultado?.Grupos ?? new Dictionary<string, decimal>(),
            dtos);
    }

    private static void Copiar(Apu entidad, ApuSolicitud solicitud, string descripcion)
    {
        entidad.Codigo = string.IsNullOrWhiteSpace(solicitud.Codigo) ? null : solicitud.Codigo.Trim();
        entidad.Descripcion = descripcion;
        entidad.Unidad = TextoPresupuesto.Recortar(solicitud.Unidad, 20);
        entidad.UnidadMedidaId = solicitud.UnidadMedidaId is > 0 ? solicitud.UnidadMedidaId : null;
        entidad.EsSubanalisis = solicitud.EsSubanalisis;
        entidad.DivisorGeneral = solicitud.DivisorGeneral == 0 ? 1 : solicitud.DivisorGeneral;
        entidad.ConceptoId = solicitud.ConceptoId is > 0 ? solicitud.ConceptoId : null;
        entidad.Activo = solicitud.Activo;
        entidad.Grupos.Clear();
        var ordenGrupo = 1;
        foreach (var grupo in solicitud.Grupos)
        {
            TextoPresupuesto.TryGrupo(grupo.Grupo, out var tipo);
            var nuevo = new ApuGrupo
            {
                Grupo = tipo,
                Divisor = grupo.Divisor == 0 ? 1 : grupo.Divisor,
                Orden = ordenGrupo++
            };
            var orden = 1;
            foreach (var linea in grupo.Lineas)
            {
                TextoPresupuesto.TryModo(linea.Modo, out var modo);
                nuevo.Lineas.Add(new ApuLinea
                {
                    Orden = orden++,
                    InsumoId = Positivo(linea.InsumoId),
                    CuadrillaId = Positivo(linea.CuadrillaId),
                    SubApuId = Positivo(linea.SubApuId),
                    Descripcion = TextoPresupuesto.Recortar(linea.Descripcion, 500),
                    Unidad = TextoPresupuesto.Recortar(linea.Unidad, 20),
                    Cantidad = linea.Cantidad,
                    PrecioManual = linea.PrecioManual,
                    Modo = modo,
                    Factor = linea.Factor
                });
            }

            entidad.Grupos.Add(nuevo);
        }
    }

    private static string? ValidarLineas(ApuSolicitud solicitud)
    {
        foreach (var linea in solicitud.Grupos.SelectMany(grupo => grupo.Lineas))
        {
            var referencias = new[] { linea.InsumoId is > 0, linea.CuadrillaId is > 0, linea.SubApuId is > 0 }.Count(marcada => marcada);
            if (referencias > 1)
            {
                return $"La línea \"{linea.Descripcion}\" solo puede referenciar un insumo, una cuadrilla o un sub-análisis.";
            }
        }

        return null;
    }

    private async Task<bool> HayCicloAsync(int apuId, ApuSolicitud solicitud, CancellationToken cancellationToken)
    {
        var aristas = await _context.Set<ApuLinea>().AsNoTracking()
            .Where(linea => linea.SubApuId != null)
            .Select(linea => new { Origen = linea.Grupo.ApuId, Destino = linea.SubApuId!.Value })
            .ToListAsync(cancellationToken);
        var hijos = aristas.Where(arista => arista.Origen != apuId)
            .GroupBy(arista => arista.Origen)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.Select(arista => arista.Destino).Distinct().ToList());
        hijos[apuId] = solicitud.Grupos.SelectMany(grupo => grupo.Lineas)
            .Where(linea => linea.SubApuId is > 0)
            .Select(linea => linea.SubApuId!.Value)
            .Distinct()
            .ToList();
        var pila = new HashSet<int>();
        var visto = new HashSet<int>();
        bool Visitar(int id)
        {
            if (!pila.Add(id))
            {
                return true;
            }

            if (visto.Add(id) && hijos.TryGetValue(id, out var siguientes))
            {
                if (siguientes.Any(Visitar))
                {
                    return true;
                }
            }

            pila.Remove(id);
            return false;
        }

        return Visitar(apuId);
    }

    private static int? Positivo(int? valor) => valor is > 0 ? valor : null;
}
