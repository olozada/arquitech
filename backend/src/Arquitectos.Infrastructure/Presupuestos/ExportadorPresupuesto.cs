using System.Globalization;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Persistence;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Arquitectos.Infrastructure.Presupuestos;

public sealed class ExportadorPresupuesto
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-CO");
    private readonly ArquitectosDbContext _context;

    public ExportadorPresupuesto(ArquitectosDbContext context)
    {
        _context = context;
    }

    public async Task<byte[]> ExcelAsync(Presupuesto presupuesto, CancellationToken cancellationToken)
    {
        var motor = await new MotorCostos(_context).CargarAsync(cancellationToken);
        var apus = await _context.Apus.AsNoTracking()
            .Include(apu => apu.Grupos)
            .ThenInclude(grupo => grupo.Lineas)
            .ToListAsync(cancellationToken);
        var insumos = await _context.Insumos.AsNoTracking().OrderBy(insumo => insumo.Descripcion).ToListAsync(cancellationToken);
        var cuadrillas = await _context.Cuadrillas.AsNoTracking()
            .Include(cuadrilla => cuadrilla.Integrantes)
            .ThenInclude(integrante => integrante.Cargo)
            .OrderBy(cuadrilla => cuadrilla.Nombre)
            .ToListAsync(cancellationToken);

        using var libro = new XLWorkbook();
        HojaPresupuesto(libro, presupuesto);
        HojaApu(libro, apus, motor);
        HojaInsumos(libro, insumos);
        HojaCuadrillas(libro, cuadrillas, motor);
        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return memoria.ToArray();
    }

    public byte[] Pdf(Presupuesto presupuesto)
    {
        var costo = Totales(presupuesto);
        return Document.Create(documento =>
        {
            documento.Page(pagina =>
            {
                pagina.Size(PageSizes.Letter.Landscape());
                pagina.Margin(28);
                pagina.DefaultTextStyle(estilo => estilo.FontSize(9));
                pagina.Content().Column(columna =>
                {
                    columna.Item().Text(presupuesto.Nombre).FontSize(16).SemiBold().FontColor("#0f766e");
                    columna.Item().Text($"{presupuesto.Construccion?.Descripcion ?? "Plantilla"} · versión {presupuesto.Version} · {presupuesto.Estado}").FontSize(10);
                    columna.Item().PaddingTop(8).Table(tabla =>
                    {
                        tabla.ColumnsDefinition(columnas =>
                        {
                            columnas.ConstantColumn(46);
                            columnas.RelativeColumn();
                            columnas.ConstantColumn(110);
                            columnas.ConstantColumn(70);
                        });
                        tabla.Header(encabezado =>
                        {
                            foreach (var titulo in new[] { "Cap.", "Capítulo", "Total", "%" })
                            {
                                encabezado.Cell().Background("#0f766e").Padding(4).Text(titulo).FontColor(Colors.White).SemiBold();
                            }
                        });
                        foreach (var capitulo in presupuesto.Capitulos.OrderBy(item => item.Orden))
                        {
                            var total = capitulo.Items.Sum(item => item.Cantidad * (item.ValorMaterialUnitario + item.ValorManoObraUnitario));
                            tabla.Cell().Padding(3).Text(capitulo.Numero.ToString(Cultura));
                            tabla.Cell().Padding(3).Text(capitulo.Nombre);
                            tabla.Cell().Padding(3).AlignRight().Text(Dinero(total));
                            tabla.Cell().Padding(3).AlignRight().Text(costo.Subtotal == 0 ? "" : (total / costo.Subtotal).ToString("P1", Cultura));
                        }
                    });
                    columna.Item().PaddingTop(10).Table(tabla =>
                    {
                        tabla.ColumnsDefinition(columnas =>
                        {
                            columnas.RelativeColumn();
                            columnas.ConstantColumn(140);
                        });
                        Fila(tabla, "Subtotal costo directo", costo.Subtotal, false);
                        Fila(tabla, "Imprevistos", costo.Imprevistos, false);
                        Fila(tabla, "Incremento", costo.Incremento, false);
                        Fila(tabla, "Administración", costo.Administracion, false);
                        Fila(tabla, "Utilidad", costo.Utilidad, false);
                        Fila(tabla, "IVA utilidad", costo.IvaUtilidad, false);
                        Fila(tabla, "TOTAL", costo.Total, true);
                        if (costo.ValorPorM2 is decimal metro)
                        {
                            Fila(tabla, "Valor por m²", metro, true);
                        }
                    });
                });
            });
        }).GeneratePdf();
    }

    private static void HojaPresupuesto(XLWorkbook libro, Presupuesto presupuesto)
    {
        var hoja = libro.AddWorksheet("Presupuesto");
        var encabezados = new[] { "Código", "Descripción", "Unidad", "Cantidad", "Valor material", "Valor MO", "Valor unitario", "Total" };
        for (var columna = 0; columna < encabezados.Length; columna++)
        {
            hoja.Cell(1, columna + 1).Value = encabezados[columna];
        }

        var fila = 2;
        foreach (var capitulo in presupuesto.Capitulos.OrderBy(item => item.Orden))
        {
            hoja.Cell(fila, 1).Value = capitulo.Numero;
            hoja.Cell(fila, 2).Value = capitulo.Nombre;
            hoja.Range(fila, 1, fila, 8).Style.Font.Bold = true;
            fila++;
            foreach (var item in capitulo.Items.OrderBy(linea => linea.Orden))
            {
                var unitario = item.ValorMaterialUnitario + item.ValorManoObraUnitario;
                hoja.Cell(fila, 1).Value = item.Codigo;
                hoja.Cell(fila, 2).Value = item.Descripcion;
                hoja.Cell(fila, 3).Value = item.Unidad;
                hoja.Cell(fila, 4).Value = item.Cantidad;
                Pesos(hoja.Cell(fila, 5), item.ValorMaterialUnitario);
                Pesos(hoja.Cell(fila, 6), item.ValorManoObraUnitario);
                Pesos(hoja.Cell(fila, 7), unitario);
                Pesos(hoja.Cell(fila, 8), item.Cantidad * unitario);
                fila++;
            }
        }

        var costo = Totales(presupuesto);
        hoja.Cell(fila, 7).Value = "Subtotal";
        Pesos(hoja.Cell(fila, 8), costo.Subtotal);
        hoja.Cell(fila + 1, 7).Value = "Imprevistos";
        Pesos(hoja.Cell(fila + 1, 8), costo.Imprevistos);
        hoja.Cell(fila + 2, 7).Value = "Incremento";
        Pesos(hoja.Cell(fila + 2, 8), costo.Incremento);
        hoja.Cell(fila + 3, 7).Value = "TOTAL";
        Pesos(hoja.Cell(fila + 3, 8), costo.Total);
        hoja.Columns().AdjustToContents();
    }

    private static void HojaApu(XLWorkbook libro, IReadOnlyList<Apu> apus, ContextoCosto motor)
    {
        var hoja = libro.AddWorksheet("APU");
        var encabezados = new[] { "APU", "Grupo", "Descripción", "Unidad", "Cantidad", "Modo", "Factor", "Valor" };
        for (var columna = 0; columna < encabezados.Length; columna++)
        {
            hoja.Cell(1, columna + 1).Value = encabezados[columna];
        }

        var fila = 2;
        foreach (var apu in apus.OrderBy(item => item.Codigo).ThenBy(item => item.Descripcion))
        {
            var resultado = MotorCostos.Calcular(apu.Id, motor);
            var detalle = resultado?.Detalle ?? [];
            var grupos = apu.Grupos.OrderBy(grupo => grupo.Orden).ToList();
            for (var indice = 0; indice < grupos.Count; indice++)
            {
                var valores = indice < detalle.Count ? detalle[indice].Lineas : [];
                var lineas = grupos[indice].Lineas.OrderBy(linea => linea.Orden).ToList();
                for (var lineaIndice = 0; lineaIndice < lineas.Count; lineaIndice++)
                {
                    var linea = lineas[lineaIndice];
                    hoja.Cell(fila, 1).Value = apu.Descripcion;
                    hoja.Cell(fila, 2).Value = grupos[indice].Grupo.ToString();
                    hoja.Cell(fila, 3).Value = linea.Descripcion;
                    hoja.Cell(fila, 4).Value = linea.Unidad;
                    hoja.Cell(fila, 5).Value = linea.Cantidad;
                    hoja.Cell(fila, 6).Value = linea.Modo.ToString();
                    hoja.Cell(fila, 7).Value = linea.Factor;
                    Pesos(hoja.Cell(fila, 8), lineaIndice < valores.Count ? valores[lineaIndice].Valor : 0);
                    fila++;
                }
            }
        }

        hoja.Columns().AdjustToContents();
    }

    private static void HojaInsumos(XLWorkbook libro, IReadOnlyList<Insumo> insumos)
    {
        var hoja = libro.AddWorksheet("Insumos");
        var encabezados = new[] { "Código", "Tipo", "Descripción", "Unidad compra", "Precio base", "IVA", "Cantidad", "Unidad uso", "Precio unitario" };
        for (var columna = 0; columna < encabezados.Length; columna++)
        {
            hoja.Cell(1, columna + 1).Value = encabezados[columna];
        }

        var fila = 2;
        foreach (var insumo in insumos)
        {
            hoja.Cell(fila, 1).Value = insumo.Codigo;
            hoja.Cell(fila, 2).Value = insumo.Tipo.ToString();
            hoja.Cell(fila, 3).Value = insumo.Descripcion;
            hoja.Cell(fila, 4).Value = insumo.UnidadCompra;
            Pesos(hoja.Cell(fila, 5), insumo.PrecioBase);
            hoja.Cell(fila, 6).Value = insumo.PorcentajeIva;
            hoja.Cell(fila, 7).Value = insumo.CantidadPorUnidad;
            hoja.Cell(fila, 8).Value = insumo.UnidadUso;
            Pesos(hoja.Cell(fila, 9), insumo.PrecioUnitario);
            fila++;
        }

        hoja.Columns().AdjustToContents();
    }

    private static void HojaCuadrillas(XLWorkbook libro, IReadOnlyList<Cuadrilla> cuadrillas, ContextoCosto motor)
    {
        var hoja = libro.AddWorksheet("Cuadrillas");
        hoja.Cell(1, 1).Value = "Cuadrilla";
        hoja.Cell(1, 2).Value = "Integrantes";
        hoja.Cell(1, 3).Value = "Costo día";
        var fila = 2;
        foreach (var cuadrilla in cuadrillas)
        {
            hoja.Cell(fila, 1).Value = cuadrilla.Nombre;
            hoja.Cell(fila, 2).Value = string.Join(", ", cuadrilla.Integrantes.Select(integrante => $"{integrante.Cantidad:0.##} {integrante.Cargo.Nombre}"));
            Pesos(hoja.Cell(fila, 3), motor.CostoDia.GetValueOrDefault(cuadrilla.Id));
            fila++;
        }

        hoja.Columns().AdjustToContents();
    }

    private static void Fila(TableDescriptor tabla, string etiqueta, decimal valor, bool fuerte)
    {
        var etiquetaCelda = tabla.Cell().Padding(3).Text(etiqueta);
        var valorCelda = tabla.Cell().Padding(3).AlignRight().Text(Dinero(valor));
        if (fuerte)
        {
            etiquetaCelda.SemiBold();
            valorCelda.SemiBold();
        }
    }

    private static ResumenCosto Totales(Presupuesto presupuesto)
    {
        var items = presupuesto.Capitulos.SelectMany(capitulo => capitulo.Items).ToList();
        var material = items.Sum(item => item.Cantidad * item.ValorMaterialUnitario);
        var manoObra = items.Sum(item => item.Cantidad * item.ValorManoObraUnitario);
        return CalculadoraPresupuesto.Resumir(
            material + manoObra,
            material,
            manoObra,
            presupuesto.PctImprevistos,
            presupuesto.PctIncremento,
            presupuesto.PctAdministracion,
            presupuesto.PctUtilidad,
            presupuesto.PctIvaUtilidad,
            presupuesto.AreaConstruidaM2);
    }

    private static void Pesos(IXLCell celda, decimal valor)
    {
        celda.Value = valor;
        celda.Style.NumberFormat.Format = "#,##0.00";
    }

    private static string Dinero(decimal valor) => valor.ToString("C2", Cultura);
}
