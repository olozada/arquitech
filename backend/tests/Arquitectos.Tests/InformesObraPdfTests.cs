using System.IO.Compression;
using Arquitectos.Application.Actas;
using Arquitectos.Application.Facturas;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Actas;
using Arquitectos.Infrastructure.Facturas;
using Arquitectos.Infrastructure.Persistence;
using Arquitectos.Infrastructure.Presupuestos;
using QuestPDF.Infrastructure;

namespace Arquitectos.Tests;

public sealed class InformesObraPdfTests
{
    public InformesObraPdfTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public void El_acta_la_factura_y_los_informes_generan_pdf()
    {
        var acta = GeneradorPdfActa.Generar(Acta());
        var factura = GeneradorPdfFactura.Generar(Factura());
        var acumulado = GeneradorPdfInformeItems.Generar(InformeObra(true, false));
        var detallado = GeneradorPdfInformeItems.Generar(InformeObra(false, true));
        var facturas = GeneradorPdfFactura.GenerarInforme(InformeFacturas(false));
        var estimado = new ExportadorPresupuesto(null!).Pdf(Presupuesto());

        Assert.All(new[] { acta, factura, acumulado, detallado, facturas, estimado }, EsPdf);
    }

    [Fact]
    public void El_excel_de_materiales_incluye_el_subtotal_del_material_repetido()
    {
        var informe = new InformeMaterialesDto(
            null,
            null,
            [
                new ItemMaterialInformeDto("MAT-1", "Cemento", "und", 2m, 1000m, 2000m, true, 4),
                new ItemMaterialInformeDto("MAT-9", "Arena", "m3", 1m, 500m, 500m, true, 5),
                new ItemMaterialInformeDto("MAT-1", "Cemento", "und", 3m, 1100m, 3300m, true, 6)
            ],
            5800m,
            ["Construcción: Casa"],
            false);

        var excel = GeneradorExcelMateriales.Generar(informe, true);
        var pdf = GeneradorPdfInformeMateriales.Generar(informe, true);
        Assert.Equal((byte)'%', pdf[0]);
        using var paquete = new ZipArchive(new MemoryStream(excel));
        using var lector = new StreamReader(paquete.GetEntry("xl/sharedStrings.xml")!.Open());
        var texto = lector.ReadToEnd();

        Assert.Contains("Cemento", texto, StringComparison.Ordinal);
        Assert.Contains("Subtotal", texto, StringComparison.Ordinal);
        Assert.DoesNotContain("Subtotal Cemento", texto, StringComparison.Ordinal);
        Assert.DoesNotContain("Subtotal Arena", texto, StringComparison.Ordinal);
        Assert.DoesNotContain("Estado", texto, StringComparison.Ordinal);
        Assert.DoesNotContain("Aprobado", texto, StringComparison.Ordinal);
        Assert.DoesNotContain("Rechazado", texto, StringComparison.Ordinal);
    }

    private static void EsPdf(byte[] archivo)
    {
        Assert.True(archivo.Length > 100);
        Assert.Equal((byte)'%', archivo[0]);
        Assert.Equal((byte)'P', archivo[1]);
        Assert.Equal((byte)'D', archivo[2]);
        Assert.Equal((byte)'F', archivo[3]);
    }

    private static ActaDto Acta() => new(
        12,
        new DateOnly(2026, 1, 1),
        new DateOnly(2026, 1, 31),
        1,
        "Ana Ruiz",
        2,
        "Luis Gómez",
        3,
        "Obra SAS",
        "Periodo de enero",
        DateTime.UtcNow,
        1,
        25000m,
        [
            new LineaActaDto(1, 1, "Casa 1", 1, "Muro", 1, "m2", 4, "Cemento", 2.5m, 10000m, "Pañete norte", 25000m, 1, "Oficial", true),
            new LineaActaDto(2, 2, "Casa 2", 1, "Piso", 1, "m2", null, null, 1m, 8000m, "Rechazado", 8000m, 1, "Oficial", false)
        ],
        false,
        "");

    private static FacturaDto Factura() => new(
        4,
        "F-18",
        new DateOnly(2026, 2, 3),
        1,
        "Ferretería Central",
        "OC-9",
        "Entrega en obra",
        DateTime.UtcNow,
        1,
        15000m,
        [
            new LineaFacturaDto(1, 1, "Casa 1", 8, "CEM", "Cemento", 1, "bulto", 3m, 5000m, "Gris", 15000m)
        ],
        "");

    private static InformeItemsDto InformeObra(bool agrupado, bool romper) => new(
        agrupado,
        new DateOnly(2026, 1, 1),
        new DateOnly(2026, 1, 31),
        [
            new ItemInformeDto(1, 12, "Casa 1", "Muro", "Cemento", "Pañete", "m2", 2.5m, 10000m, 25000m, true),
            new ItemInformeDto(2, 13, "Casa 1", "Piso", "", "Sala", "m2", 4m, 9000m, 36000m, true)
        ],
        61000m,
        romper,
        true,
        ["Casa: Casa 1"]);

    private static InformeFacturasDto InformeFacturas(bool agrupado) => new(
        agrupado,
        new DateOnly(2026, 2, 1),
        new DateOnly(2026, 2, 28),
        [
            new ItemFacturaInformeDto(4, "F-18", new DateOnly(2026, 2, 3), "Casa 1", "CEM", "Cemento", "Gris", "bulto", 3m, 5000m, 15000m)
        ],
        15000m,
        ["Casa: Casa 1"],
        1,
        1,
        1);

    private static Presupuesto Presupuesto() => new()
    {
        Nombre = "Vivienda",
        Version = 1,
        Estado = EstadoPresupuesto.Borrador,
        Construccion = new Construccion { Descripcion = "Casa 1" },
        Capitulos =
        [
            new PresupuestoCapitulo
            {
                Numero = 1,
                Nombre = "Obra negra",
                Orden = 1,
                Items =
                [
                    new PresupuestoItem
                    {
                        Codigo = "1.1",
                        Descripcion = "Muro en ladrillo",
                        Unidad = "m2",
                        Cantidad = 12.5m,
                        ValorMaterialUnitario = 40000m,
                        ValorManoObraUnitario = 15000.5m
                    }
                ]
            }
        ]
    };
}
