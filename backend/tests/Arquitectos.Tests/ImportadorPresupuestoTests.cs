using Arquitectos.Infrastructure.Presupuestos;

namespace Arquitectos.Tests;

public class ImportadorPresupuestoTests
{
    [Fact]
    public void El_fixture_de_casa_murillo_reproduce_el_excel()
    {
        var ruta = Path.Combine(AppContext.BaseDirectory, "Fixtures", "presupuesto_casa_murillo.json");
        Assert.True(File.Exists(ruta), ruta);
        var modelo = LectorModelo.LeerJson(File.ReadAllText(ruta));
        var informe = LectorModelo.Evaluar(modelo);

        Assert.Equal(122, informe.ApusRevisados);
        Assert.Equal(116, informe.ApusReproducidos);
        Assert.Equal(8, informe.UnidadesDistintas);
        Assert.Equal(4, informe.CodigosDuplicados);
        // El JSON trae 28 ítems sin apuId (139 ítems, 111 enlazados).
        // H189 e I189 del Excel omiten los dos ítems 20.02; la suma de las líneas sí cuadra con K189.
        Assert.Equal(28, informe.ItemsSinApu);
        Assert.InRange(informe.Subtotal, 1_221_328_766m, 1_221_328_768m);
        Assert.InRange(informe.TotalMaterial, 828_938_845m, 828_938_848m);
        Assert.InRange(informe.TotalManoObra, 392_389_919m, 392_389_922m);
        Assert.InRange(informe.Imprevistos, 61_066_437m, 61_066_439m);
        Assert.InRange(informe.Incremento, 85_493_013m, 85_493_015m);
        Assert.InRange(informe.Total, 1_367_888_219m, 1_367_888_221m);
        Assert.InRange(informe.ValorPorM2, 2_452_950m, 2_452_952m);
    }

    [Fact]
    public void El_libro_de_casa_murillo_se_lee_igual_que_el_fixture()
    {
        var ruta = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Presupuesto_Casa_Murillo.xlsx");
        Assert.True(File.Exists(ruta), ruta);

        var informe = LectorModelo.Evaluar(LectorExcel.Leer(ruta));
        Assert.Equal(122, informe.ApusRevisados);
        Assert.Equal(116, informe.ApusReproducidos);
        Assert.Equal(8, informe.UnidadesDistintas);
        Assert.Equal(4, informe.CodigosDuplicados);
        Assert.Equal(28, informe.ItemsSinApu);
        Assert.InRange(informe.Subtotal, 1_221_328_766m, 1_221_328_768m);
    }

    [Fact]
    public void Un_libro_sin_valores_calculados_explica_el_error()
    {
        using var libro = new ClosedXML.Excel.XLWorkbook();
        foreach (var nombre in new[] { "Presupuesto", "PRECIOS MATERIALES", "PRECIO HERRAMIENTA", "ALQULER EQUIPO", "LABORATORIO", "PRECIOS M.O.", "P.M.O.", "A.P.U." })
        {
            libro.AddWorksheet(nombre);
        }

        var hoja = libro.Worksheet("Presupuesto");
        hoja.Cell(10, 1).Value = 1;
        hoja.Cell(10, 2).Value = "Capítulo";
        hoja.Cell(11, 1).Value = "1.01";
        hoja.Cell(11, 2).Value = "Ítem";
        hoja.Cell(11, 7).FormulaA1 = "E11";
        hoja.Cell(11, 5).FormulaA1 = "1/0";
        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        memoria.Position = 0;

        var error = Assert.Throws<InvalidOperationException>(() => LectorExcel.Leer(memoria));
        Assert.Contains("valores calculados", error.Message);
    }
}
