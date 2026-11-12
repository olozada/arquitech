using Arquitectos.Application.Presupuestos;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Infrastructure.Presupuestos;

public sealed class ImportadorPresupuesto
{
    private readonly ArquitectosDbContext _context;

    public ImportadorPresupuesto(ArquitectosDbContext context)
    {
        _context = context;
    }

    public async Task<InformeImportacionDto> GuardarAsync(ModeloImportado modelo, int construccionId, CancellationToken cancellationToken)
    {
        var evaluacion = LectorModelo.Evaluar(modelo);
        await GuardarPrestacionesAsync(modelo, cancellationToken);
        var insumos = await GuardarInsumosAsync(modelo, cancellationToken);
        var cargos = await GuardarCargosAsync(modelo, cancellationToken);
        var cuadrillas = await GuardarCuadrillasAsync(modelo, cargos, cancellationToken);
        var apus = await GuardarApusAsync(modelo, insumos, cuadrillas, cancellationToken);
        var presupuesto = await GuardarPresupuestoAsync(modelo, construccionId, apus, cancellationToken);
        return new InformeImportacionDto(
            presupuesto.Id,
            evaluacion.ApusRevisados,
            evaluacion.ApusReproducidos,
            evaluacion.UnidadesDistintas,
            evaluacion.CodigosDuplicados,
            evaluacion.ItemsSinApu,
            evaluacion.Subtotal,
            evaluacion.TotalMaterial,
            evaluacion.TotalManoObra,
            evaluacion.Imprevistos,
            evaluacion.Incremento,
            evaluacion.Total,
            evaluacion.ValorPorM2,
            evaluacion.UnidadesQueNoCoinciden,
            evaluacion.ApusDistintos);
    }

    private async Task<Dictionary<string, Insumo>> GuardarInsumosAsync(ModeloImportado modelo, CancellationToken cancellationToken)
    {
        var existentes = await _context.Insumos.ToListAsync(cancellationToken);
        var porCodigo = existentes.Where(insumo => !string.IsNullOrWhiteSpace(insumo.Codigo))
            .GroupBy(insumo => insumo.Codigo!)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.First());
        var porNombre = existentes.ToDictionary(insumo => Clave(insumo.Tipo, insumo.Descripcion), insumo => insumo);
        var resultado = new Dictionary<string, Insumo>();
        foreach (var fila in modelo.Insumos)
        {
            if (!TextoPresupuesto.TryTipoInsumo(fila.Tipo, out var tipo))
            {
                tipo = TipoInsumo.Otro;
            }

            Insumo? entidad = null;
            if (!string.IsNullOrWhiteSpace(fila.Codigo))
            {
                porCodigo.TryGetValue(fila.Codigo, out entidad);
            }

            entidad ??= porNombre.GetValueOrDefault(Clave(tipo, fila.Descripcion));
            if (entidad is null)
            {
                entidad = new Insumo { Descripcion = TextoPresupuesto.Recortar(fila.Descripcion, 500) };
                _context.Insumos.Add(entidad);
            }

            var cantidad = fila.CantidadPorUnidad == 0 ? 1 : fila.CantidadPorUnidad;
            entidad.Codigo = string.IsNullOrWhiteSpace(fila.Codigo) ? entidad.Codigo : fila.Codigo.Trim();
            entidad.Descripcion = TextoPresupuesto.Recortar(fila.Descripcion, 500);
            entidad.Tipo = tipo;
            entidad.UnidadCompra = TextoPresupuesto.Recortar(fila.UnidadCompra, 30);
            entidad.PrecioBase = fila.PrecioBase;
            entidad.PorcentajeIva = fila.Iva;
            entidad.CantidadPorUnidad = cantidad;
            entidad.UnidadUso = TextoPresupuesto.Recortar(fila.UnidadUso, 30);
            entidad.PrecioUnitario = fila.PrecioUnitario != 0
                ? fila.PrecioUnitario
                : CalculadoraApu.PrecioUnitarioInsumo(fila.PrecioBase, fila.Iva, cantidad);
            entidad.FechaPrecio = DateTime.UtcNow;
            entidad.Activo = true;
            if (!string.IsNullOrWhiteSpace(entidad.Codigo))
            {
                porCodigo[entidad.Codigo] = entidad;
                resultado[entidad.Codigo] = entidad;
            }

            porNombre[Clave(tipo, entidad.Descripcion)] = entidad;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return resultado;
    }

    private async Task GuardarPrestacionesAsync(ModeloImportado modelo, CancellationToken cancellationToken)
    {
        if (modelo.ManoObra.Prestaciones.Count == 0)
        {
            return;
        }

        var actuales = await _context.PrestacionesSociales.ToListAsync(cancellationToken);
        _context.PrestacionesSociales.RemoveRange(actuales);
        var orden = 1;
        foreach (var prestacion in modelo.ManoObra.Prestaciones)
        {
            _context.PrestacionesSociales.Add(new PrestacionSocial
            {
                Concepto = TextoPresupuesto.Recortar(prestacion.Concepto, 120),
                Porcentaje = prestacion.Porcentaje,
                Orden = orden++
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<Dictionary<string, Cargo>> GuardarCargosAsync(ModeloImportado modelo, CancellationToken cancellationToken)
    {
        var cargos = await _context.Cargos.ToListAsync(cancellationToken);
        var porNombre = cargos.ToDictionary(cargo => TextoPresupuesto.Normalizar(cargo.Nombre), cargo => cargo);
        foreach (var fila in modelo.ManoObra.Cargos)
        {
            var clave = TextoPresupuesto.Normalizar(fila.Cargo);
            if (!porNombre.TryGetValue(clave, out var cargo))
            {
                cargo = new Cargo { Nombre = TextoPresupuesto.Recortar(fila.Cargo, 80) };
                _context.Cargos.Add(cargo);
                porNombre[clave] = cargo;
            }

            cargo.SalarioMensual = fila.SalarioMensual;
            cargo.Activo = true;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return porNombre;
    }

    private async Task<List<Cuadrilla>> GuardarCuadrillasAsync(
        ModeloImportado modelo,
        Dictionary<string, Cargo> cargos,
        CancellationToken cancellationToken)
    {
        var actuales = await _context.Cuadrillas.Include(cuadrilla => cuadrilla.Integrantes).ToListAsync(cancellationToken);
        var porNombre = actuales.ToDictionary(cuadrilla => TextoPresupuesto.Normalizar(cuadrilla.Nombre), cuadrilla => cuadrilla);
        Cargo? Buscar(string fragmento) => cargos.FirstOrDefault(par => par.Key.Contains(fragmento, StringComparison.Ordinal)).Value;
        var maestro = Buscar("MAESTRO");
        var oficial = Buscar("OFICIAL");
        var ayudante = Buscar("AYUDANTE");
        var guardadas = new List<Cuadrilla>();
        foreach (var fila in modelo.ManoObra.Cuadrillas.Where(cuadrilla => !string.IsNullOrWhiteSpace(cuadrilla.Nombre)))
        {
            var clave = TextoPresupuesto.Normalizar(fila.Nombre);
            if (!porNombre.TryGetValue(clave, out var cuadrilla))
            {
                cuadrilla = new Cuadrilla { Nombre = TextoPresupuesto.Recortar(fila.Nombre, 80) };
                _context.Cuadrillas.Add(cuadrilla);
                porNombre[clave] = cuadrilla;
            }

            _context.Set<CuadrillaCargo>().RemoveRange(cuadrilla.Integrantes);
            cuadrilla.Integrantes.Clear();
            Agregar(cuadrilla, maestro, fila.Maestros);
            Agregar(cuadrilla, oficial, fila.Oficiales);
            Agregar(cuadrilla, ayudante, fila.Ayudantes);
            cuadrilla.Activo = true;
            guardadas.Add(cuadrilla);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return guardadas;
    }

    private async Task<Dictionary<int, Apu>> GuardarApusAsync(
        ModeloImportado modelo,
        Dictionary<string, Insumo> insumos,
        List<Cuadrilla> cuadrillas,
        CancellationToken cancellationToken)
    {
        var nombres = cuadrillas.OrderByDescending(cuadrilla => cuadrilla.Nombre.Length).ToList();
        var mapa = new Dictionary<int, Apu>();
        foreach (var fila in modelo.Apus)
        {
            var apu = new Apu
            {
                Descripcion = TextoPresupuesto.Recortar(fila.Descripcion, 500),
                Unidad = TextoPresupuesto.Recortar(fila.Unidad, 20),
                EsSubanalisis = fila.EsSubanalisis,
                DivisorGeneral = fila.Divisor == 0 ? 1 : fila.Divisor,
                Activo = true
            };
            _context.Apus.Add(apu);
            mapa[fila.Id] = apu;
        }

        await _context.SaveChangesAsync(cancellationToken);
        foreach (var fila in modelo.Apus)
        {
            var apu = mapa[fila.Id];
            var ordenGrupo = 1;
            foreach (var grupo in fila.Grupos)
            {
                TextoPresupuesto.TryGrupo(grupo.Tipo, out var tipo);
                if (grupo.Tipo.Equals("Mezcla", StringComparison.OrdinalIgnoreCase))
                {
                    tipo = GrupoApu.Material;
                }

                var nuevo = new ApuGrupo { Grupo = tipo, Divisor = grupo.Divisor == 0 ? 1 : grupo.Divisor, Orden = ordenGrupo++ };
                var orden = 1;
                foreach (var linea in grupo.Lineas)
                {
                    var herramienta = string.Equals(linea.Tipo, "HerramientaMenor", StringComparison.OrdinalIgnoreCase);
                    TextoPresupuesto.TryModo(herramienta ? "PorcentajeManoObra" : linea.Modo, out var modo);
                    int? sub = linea.SubApuId is int subId && mapa.TryGetValue(subId, out var subApu) ? subApu.Id : null;
                    int? insumo = null;
                    int? cuadrilla = null;
                    if (!herramienta && sub is null)
                    {
                        var nombreCuadrilla = nombres.FirstOrDefault(item =>
                            linea.Descripcion.Contains(item.Nombre, StringComparison.OrdinalIgnoreCase));
                        if (nombreCuadrilla is not null && string.Equals(linea.Tipo, "ManoObra", StringComparison.OrdinalIgnoreCase))
                        {
                            cuadrilla = nombreCuadrilla.Id;
                        }
                        else if (!string.IsNullOrWhiteSpace(linea.CodigoInsumo) && insumos.TryGetValue(linea.CodigoInsumo, out var encontrado))
                        {
                            insumo = encontrado.Id;
                        }
                    }

                    nuevo.Lineas.Add(new ApuLinea
                    {
                        Orden = orden++,
                        InsumoId = insumo,
                        CuadrillaId = cuadrilla,
                        SubApuId = sub,
                        Descripcion = TextoPresupuesto.Recortar(linea.Descripcion, 500),
                        Unidad = TextoPresupuesto.Recortar(linea.Unidad, 20),
                        Cantidad = linea.Cantidad,
                        PrecioManual = insumo is null && cuadrilla is null && sub is null ? linea.Precio : null,
                        Modo = modo,
                        Factor = linea.PorcentajeMO ?? linea.Factor
                    });
                }

                apu.Grupos.Add(nuevo);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return mapa;
    }

    private async Task<Presupuesto> GuardarPresupuestoAsync(
        ModeloImportado modelo,
        int construccionId,
        Dictionary<int, Apu> apus,
        CancellationToken cancellationToken)
    {
        var conceptos = await _context.Conceptos.ToListAsync(cancellationToken);
        var porConcepto = conceptos.ToDictionary(concepto => TextoPresupuesto.Normalizar(concepto.Descripcion), concepto => concepto);
        var unidades = await _context.UnidadesMedida.ToListAsync(cancellationToken);
        var porUnidad = unidades.ToDictionary(unidad => TextoPresupuesto.Normalizar(unidad.Nombre), unidad => unidad);
        var version = (await _context.Presupuestos.Where(item => item.ConstruccionId == construccionId).Select(item => (int?)item.Version).MaxAsync(cancellationToken) ?? 0) + 1;
        var presupuesto = new Presupuesto
        {
            ConstruccionId = construccionId,
            Nombre = TextoPresupuesto.Recortar(string.IsNullOrWhiteSpace(modelo.Proyecto) ? "Presupuesto importado" : modelo.Proyecto, 200),
            Version = version,
            Estado = EstadoPresupuesto.Borrador,
            Fecha = DateOnly.FromDateTime(DateTime.UtcNow),
            MesesProyecto = modelo.MesesProyecto,
            AreaConstruidaM2 = modelo.Presupuesto.AreaM2,
            PctImprevistos = modelo.Presupuesto.PorcentajeImprevistos,
            PctIncremento = modelo.Presupuesto.PorcentajeIncremento
        };
        foreach (var item in modelo.Presupuesto.Capitulos.SelectMany(capitulo => capitulo.Items))
        {
            ConceptoDe(item, porConcepto);
            UnidadDe(item.Unidad, porUnidad);
        }

        await _context.SaveChangesAsync(cancellationToken);
        var vistos = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var ordenCapitulo = 1;
        foreach (var capitulo in modelo.Presupuesto.Capitulos)
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
                var codigo = CodigoUnico(item.Codigo, vistos);
                var concepto = ConceptoDe(item, porConcepto);
                var unidad = UnidadDe(item.Unidad, porUnidad);
                Apu? apu = item.ApuId is int apuId && apus.TryGetValue(apuId, out var encontrado) ? encontrado : null;
                if (apu is not null && string.IsNullOrWhiteSpace(apu.Unidad))
                {
                    apu.Unidad = TextoPresupuesto.Recortar(item.Unidad, 20);
                }

                if (apu is not null && string.IsNullOrWhiteSpace(apu.Codigo))
                {
                    apu.Codigo = codigo;
                }

                nuevo.Items.Add(new PresupuestoItem
                {
                    Codigo = codigo,
                    Descripcion = TextoPresupuesto.Recortar(item.Descripcion, 500),
                    Unidad = TextoPresupuesto.Recortar(item.Unidad, 20),
                    UnidadMedidaId = unidad?.Id,
                    ApuId = apu?.Id,
                    FactorConversionApu = 1,
                    ConceptoId = concepto.Id,
                    Cantidad = item.Cantidad,
                    ValorMaterialUnitario = item.ValorMaterial,
                    ValorManoObraUnitario = item.ValorManoObra,
                    Orden = orden++
                });
            }

            presupuesto.Capitulos.Add(nuevo);
        }

        _context.Presupuestos.Add(presupuesto);
        await _context.SaveChangesAsync(cancellationToken);
        return presupuesto;
    }

    public static Concepto AsegurarConcepto(ArquitectosDbContext contexto, Dictionary<string, Concepto> porConcepto, string descripcion, string? unidad)
    {
        var texto = TextoPresupuesto.Recortar(descripcion, 200);
        var clave = TextoPresupuesto.Normalizar(texto);
        if (porConcepto.TryGetValue(clave, out var concepto))
        {
            return concepto;
        }

        concepto = new Concepto { Descripcion = texto, Unidad = TextoPresupuesto.Recortar(unidad, 20), Activo = true };
        contexto.Conceptos.Add(concepto);
        porConcepto[clave] = concepto;
        return concepto;
    }

    private Concepto ConceptoDe(ItemImportado item, Dictionary<string, Concepto> porConcepto) =>
        AsegurarConcepto(_context, porConcepto, item.Descripcion, item.Unidad);

    private UnidadMedida? UnidadDe(string? nombre, Dictionary<string, UnidadMedida> porUnidad)
    {
        var texto = TextoPresupuesto.Recortar(nombre, 20);
        if (texto.Length == 0)
        {
            return null;
        }

        var clave = TextoPresupuesto.Normalizar(texto);
        if (porUnidad.TryGetValue(clave, out var unidad))
        {
            return unidad;
        }

        unidad = new UnidadMedida { Nombre = texto, Activo = true };
        _context.UnidadesMedida.Add(unidad);
        porUnidad[clave] = unidad;
        return unidad;
    }

    private static void Agregar(Cuadrilla cuadrilla, Cargo? cargo, decimal cantidad)
    {
        if (cargo is null || cantidad <= 0)
        {
            return;
        }

        cuadrilla.Integrantes.Add(new CuadrillaCargo { CargoId = cargo.Id, Cantidad = cantidad });
    }

    private static string CodigoUnico(string codigo, Dictionary<string, int> vistos)
    {
        var baseCodigo = string.IsNullOrWhiteSpace(codigo) ? "S/C" : codigo.Trim();
        if (!vistos.TryGetValue(baseCodigo, out var veces))
        {
            vistos[baseCodigo] = 1;
            return TextoPresupuesto.Recortar(baseCodigo, 20);
        }

        veces++;
        vistos[baseCodigo] = veces;
        var sufijo = veces == 2 ? "-b" : "-b" + (veces - 1);
        var disponible = baseCodigo.Length + sufijo.Length <= 20 ? baseCodigo + sufijo : baseCodigo[..Math.Max(0, 20 - sufijo.Length)] + sufijo;
        return disponible;
    }

    private static string Clave(TipoInsumo tipo, string descripcion) => tipo + "|" + TextoPresupuesto.Normalizar(descripcion);
}
