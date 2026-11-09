using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Infrastructure.Persistence;

public static class SemillaDemo
{
    public static async Task<string> AplicarAsync(ArquitectosDbContext context, CancellationToken cancellationToken = default)
    {
        if (await context.Actas.AnyAsync(cancellationToken))
        {
            var cantidad = await context.Actas.CountAsync(cancellationToken);
            var desde = await context.Actas.MinAsync(acta => acta.FechaFinal, cancellationToken);
            var hasta = await context.Actas.MaxAsync(acta => acta.FechaFinal, cancellationToken);
            return $"Ya hay {cantidad} actas ({desde:yyyy-MM-dd} a {hasta:yyyy-MM-dd}). La semilla de demostración no hizo cambios.";
        }

        var zona = ZonaHorariaDashboard.Resolver("America/Bogota");
        var hoy = ZonaHorariaDashboard.FechaLocal(DateTime.UtcNow, zona);
        var aleatorio = new Random(2026);

        var arquitectos = Nombres(5, "Demo Arquitecto", nombre => new Arquitecto { Nombre = nombre });
        var revisores = Nombres(3, "Demo Revisor", nombre => new Revisor { Nombre = nombre });
        var contratistas = Nombres(6, "Demo Contratista", nombre => new Contratista { Nombre = nombre });
        var proveedores = Nombres(4, "Demo Proveedor", nombre => new Proveedor { Nombre = nombre });
        var unidades = new[]
        {
            new UnidadMedida { Nombre = "Demo m2", Descripcion = "Metro cuadrado" },
            new UnidadMedida { Nombre = "Demo m3", Descripcion = "Metro cúbico" },
            new UnidadMedida { Nombre = "Demo und", Descripcion = "Unidad" },
            new UnidadMedida { Nombre = "Demo kg", Descripcion = "Kilogramo" },
            new UnidadMedida { Nombre = "Demo ml", Descripcion = "Metro lineal" },
            new UnidadMedida { Nombre = "Demo gl", Descripcion = "Global" }
        };
        var construcciones = Enumerable.Range(1, 8)
            .Select(indice => new Construccion
            {
                Descripcion = indice == 8 ? "Demo Obra Quieta" : $"Demo Construcción {indice}",
                Propietario = "Demo"
            })
            .ToList();
        var conceptos = Enumerable.Range(1, 15)
            .Select(indice => new Concepto
            {
                Descripcion = indice switch
                {
                    14 => "Demo Concreto 3000 PSI",
                    15 => "Demo Cimentación",
                    _ => $"Demo Concepto {indice:00}"
                },
                Unidad = unidades[(indice - 1) % unidades.Length].Nombre
            })
            .ToList();
        var materiales = Enumerable.Range(1, 20)
            .Select(indice => new Material
            {
                Descripcion = indice switch
                {
                    1 => "Demo Cemento",
                    2 => "Demo Concreto",
                    _ => $"Demo Material {indice:00}"
                },
                Unidad = unidades[(indice - 1) % unidades.Length].Nombre
            })
            .ToList();

        context.AddRange(arquitectos);
        context.AddRange(revisores);
        context.AddRange(contratistas);
        context.AddRange(proveedores);
        context.AddRange(unidades);
        context.AddRange(construcciones);
        context.AddRange(conceptos);
        context.AddRange(materiales);
        await context.SaveChangesAsync(cancellationToken);

        var listas = new List<ProveedorMaterial>();
        for (var indice = 0; indice < materiales.Count; indice++)
        {
            listas.Add(new ProveedorMaterial
            {
                ProveedorId = proveedores[0].Id,
                MaterialId = materiales[indice].Id,
                Precio = indice == 0 ? 50_000m : 5_000_000m
            });
            if (indice > 0)
            {
                listas.Add(new ProveedorMaterial
                {
                    ProveedorId = proveedores[1 + (indice % 3)].Id,
                    MaterialId = materiales[indice].Id,
                    Precio = 4_800_000m
                });
            }
        }

        context.ProveedorMateriales.AddRange(listas);

        var activas = construcciones.Take(6).ToList();
        var inicio = new DateOnly(hoy.Year, hoy.Month, 1).AddMonths(-17);
        for (var mes = 0; mes < 18; mes++)
        {
            var fechaMes = inicio.AddMonths(mes);
            var tope = fechaMes.Year == hoy.Year && fechaMes.Month == hoy.Month
                ? hoy.Day
                : DateTime.DaysInMonth(fechaMes.Year, fechaMes.Month);
            var actasDelMes = 4 + (mes / 2);
            for (var indice = 0; indice < actasDelMes; indice++)
            {
                var fin = new DateOnly(fechaMes.Year, fechaMes.Month, aleatorio.Next(1, tope + 1));
                if (fin > hoy)
                {
                    fin = hoy;
                }

                var acta = CrearActa(
                    arquitectos[aleatorio.Next(arquitectos.Count)],
                    revisores[aleatorio.Next(revisores.Count)],
                    contratistas[aleatorio.Next(contratistas.Count)],
                    fin.AddDays(-aleatorio.Next(1, 15)),
                    fin,
                    aleatorio.Next(4) == 0 ? null : "Ejecución del periodo",
                    AUtc(fin.AddDays(aleatorio.Next(0, 11)), zona));

                var lineas = aleatorio.Next(3, 13);
                for (var linea = 0; linea < lineas; linea++)
                {
                    var sinMaterial = aleatorio.Next(5) == 0;
                    acta.Detalles.Add(new ActaDetalle
                    {
                        ConstruccionId = activas[aleatorio.Next(activas.Count)].Id,
                        ConceptoId = conceptos[aleatorio.Next(12)].Id,
                        UnidadMedidaId = unidades[aleatorio.Next(unidades.Length)].Id,
                        MaterialId = sinMaterial ? null : materiales[2 + aleatorio.Next(materiales.Count - 2)].Id,
                        Cantidad = 1 + aleatorio.Next(1, 6) + (mes / 6),
                        Precio = 10_000m + (mes * 350m) + aleatorio.Next(0, 2000),
                        Descripcion = "Ítem de demostración"
                    });
                }

                context.Actas.Add(acta);
            }
        }

        var concreto = materiales[1];
        var conceptoConcreto = conceptos[13];
        var unidadConcreto = unidades[1];
        foreach (var dias in new[] { 120, 90, 70, 50 })
        {
            context.Actas.Add(LineaUnica(
                arquitectos[0], revisores[0], contratistas[0], construcciones[0], conceptoConcreto, unidadConcreto, concreto,
                hoy.AddDays(-dias - 5), hoy.AddDays(-dias), 1, 100_000m, "Referencia de concreto",
                AUtc(hoy.AddDays(-dias), zona)));
        }

        context.Actas.Add(LineaUnica(
            arquitectos[0], revisores[0], contratistas[0], construcciones[0], conceptoConcreto, unidadConcreto, concreto,
            hoy.AddDays(-12), hoy.AddDays(-4), 2, 140_000m, "Concreto con precio inflado",
            AUtc(hoy.AddDays(-4), zona)));

        context.Actas.Add(LineaUnica(
            arquitectos[1], revisores[1], contratistas[1], construcciones[1], conceptos[0], unidades[0], materiales[0],
            hoy.AddDays(-20), hoy.AddDays(-6), 3, 80_000m, "Cemento sobre la lista",
            AUtc(hoy.AddDays(-6), zona)));

        context.Actas.Add(LineaUnica(
            arquitectos[2], revisores[0], contratistas[2], construcciones[2], conceptos[14], unidades[2], null,
            hoy.AddDays(-25), hoy.AddDays(-10), 5, 1_000m, "Primer cobro cruzado",
            AUtc(hoy.AddDays(-10), zona)));
        context.Actas.Add(LineaUnica(
            arquitectos[2], revisores[0], contratistas[2], construcciones[2], conceptos[14], unidades[2], null,
            hoy.AddDays(-12), hoy.AddDays(-3), 2, 4_000m, "Segundo cobro cruzado",
            AUtc(hoy.AddDays(-3), zona)));

        context.Actas.Add(LineaUnica(
            arquitectos[3], revisores[2], contratistas[3], construcciones[7], conceptos[2], unidades[3], materiales[4],
            hoy.AddDays(-70), hoy.AddDays(-60), 1, 20_000m, "Última actividad",
            AUtc(hoy.AddDays(-60), zona)));

        context.Actas.Add(LineaUnica(
            arquitectos[4], revisores[1], contratistas[4], construcciones[3], conceptos[3], unidades[4], materiales[5],
            hoy.AddDays(-50), hoy.AddDays(-40), 1, 15_000m, null,
            AUtc(hoy.AddDays(-15), zona)));

        await context.SaveChangesAsync(cancellationToken);
        return "Se cargaron los datos de demostración del tablero.";
    }

    private static List<T> Nombres<T>(int cantidad, string prefijo, Func<string, T> crear) =>
        Enumerable.Range(1, cantidad).Select(indice => crear($"{prefijo} {indice}")).ToList();

    private static Acta LineaUnica(
        Arquitecto arquitecto,
        Revisor revisor,
        Contratista contratista,
        Construccion construccion,
        Concepto concepto,
        UnidadMedida unidad,
        Material? material,
        DateOnly inicio,
        DateOnly fin,
        decimal cantidad,
        decimal precio,
        string? observacion,
        DateTime registro)
    {
        var acta = CrearActa(arquitecto, revisor, contratista, inicio, fin, observacion, registro);
        acta.Detalles.Add(new ActaDetalle
        {
            ConstruccionId = construccion.Id,
            ConceptoId = concepto.Id,
            UnidadMedidaId = unidad.Id,
            MaterialId = material?.Id,
            Cantidad = cantidad,
            Precio = precio,
            Descripcion = concepto.Descripcion
        });
        return acta;
    }

    private static Acta CrearActa(
        Arquitecto arquitecto,
        Revisor revisor,
        Contratista contratista,
        DateOnly inicio,
        DateOnly fin,
        string? observacion,
        DateTime registro) =>
        new()
        {
            ArquitectoId = arquitecto.Id,
            RevisorId = revisor.Id,
            ContratistaId = contratista.Id,
            FechaInicial = inicio,
            FechaFinal = fin,
            Observacion = observacion,
            FechaRegistro = registro,
            UsuarioRegistroId = SemillaInicial.UsuarioAdminId
        };

    private static DateTime AUtc(DateOnly fecha, TimeZoneInfo zona)
    {
        var local = DateTime.SpecifyKind(fecha.ToDateTime(new TimeOnly(12, 0)), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, zona);
    }
}
