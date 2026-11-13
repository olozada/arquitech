using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;

namespace Arquitectos.Infrastructure.Presupuestos;

public static class LectorExcel
{
    public static ModeloImportado Leer(string ruta)
    {
        using var libro = new XLWorkbook(ruta);
        return Leer(libro, CancellationToken.None, 20_000, ValidadorLibroExcel.HojasMaximas);
    }

    public static ModeloImportado Leer(Stream archivo, CancellationToken cancellationToken = default, int filasMaximas = 20_000, int hojasMaximas = ValidadorLibroExcel.HojasMaximas)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var libro = new XLWorkbook(archivo);
        return Leer(libro, cancellationToken, filasMaximas, hojasMaximas);
    }

    private static ModeloImportado Leer(XLWorkbook libro, CancellationToken cancellationToken, int filasMaximas, int hojasMaximas)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var filas = filasMaximas > 0 ? filasMaximas : 20_000;
        var hojas = hojasMaximas > 0 ? hojasMaximas : ValidadorLibroExcel.HojasMaximas;
        if (libro.Worksheets.Count > hojas)
        {
            throw new InvalidOperationException($"El libro supera las {hojas} hojas.");
        }

        foreach (var hojaUsada in libro.Worksheets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ultima = hojaUsada.LastRowUsed()?.RowNumber() ?? 0;
            if (ultima > filas)
            {
                throw new InvalidOperationException($"La hoja {hojaUsada.Name} supera las {filas} filas.");
            }
        }

        var presupuesto = Hoja(libro, "Presupuesto");
        var modelo = new ModeloImportado
        {
            Proyecto = Texto(presupuesto.Cell("B1")),
            MesesProyecto = (int)(Numero(presupuesto.Cell("K4")) ?? 0)
        };
        modelo.Insumos.AddRange(Insumos(libro, cancellationToken));
        modelo.ManoObra = ManoObra(Hoja(libro, "P.M.O."), cancellationToken);
        modelo.Apus.AddRange(Apus(Hoja(libro, "A.P.U."), cancellationToken));
        modelo.Presupuesto = Presupuesto(presupuesto, cancellationToken);
        var apuPorFila = modelo.Apus
            .Where(apu => apu.FilaPresupuesto is > 0)
            .GroupBy(apu => apu.FilaPresupuesto!.Value)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.First().Id);
        foreach (var item in modelo.Presupuesto.Capitulos.SelectMany(capitulo => capitulo.Items))
        {
            if (apuPorFila.TryGetValue(item.Fila, out var apuId))
            {
                item.ApuId = apuId;
            }
        }

        return modelo;
    }

    private static List<InsumoImportado> Insumos(XLWorkbook libro, CancellationToken cancellationToken)
    {
        var insumos = new List<InsumoImportado>();
        HojaPrecios(insumos, Hoja(libro, "PRECIOS MATERIALES"), "Material", 5, 0.19m, cancellationToken);
        HojaPrecios(insumos, Hoja(libro, "PRECIO HERRAMIENTA"), "Herramienta", 5, 0.16m, cancellationToken);
        HojaPrecios(insumos, Hoja(libro, "ALQULER EQUIPO"), "Equipo", 5, 0.19m, cancellationToken);
        HojaPrecios(insumos, Hoja(libro, "LABORATORIO"), "Laboratorio", 4, 0.16m, cancellationToken);
        var manoObra = Hoja(libro, "PRECIOS M.O.");
        for (var fila = 5; fila <= Ultima(manoObra); fila++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var descripcion = Texto(manoObra.Cell(fila, 2));
            var unidad = Texto(manoObra.Cell(fila, 3));
            var valor = Numero(manoObra.Cell(fila, 4));
            if (descripcion.Length == 0 || unidad.Length == 0 || valor is null)
            {
                continue;
            }

            insumos.Add(new InsumoImportado
            {
                Tipo = "ManoObraDestajo",
                Descripcion = descripcion,
                UnidadCompra = unidad,
                PrecioBase = valor.Value,
                Iva = 0,
                CantidadPorUnidad = 1,
                UnidadUso = unidad,
                PrecioUnitario = valor.Value
            });
        }

        return insumos;
    }

    private static void HojaPrecios(List<InsumoImportado> insumos, IXLWorksheet hoja, string tipo, int filaEncabezado, decimal ivaPorDefecto, CancellationToken cancellationToken)
    {
        for (var fila = filaEncabezado + 1; fila <= Ultima(hoja); fila++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var descripcion = Texto(hoja.Cell(fila, 3));
            var precio = Numero(hoja.Cell(fila, 5));
            if (descripcion.Length == 0 || precio is null)
            {
                continue;
            }

            var iva = ivaPorDefecto;
            var formula = Formula(hoja.Cell(fila, 6));
            var coincidencia = Regex.Match(formula, @"\*\s*1\.(\d+)");
            if (coincidencia.Success)
            {
                iva = decimal.Parse("0." + coincidencia.Groups[1].Value, CultureInfo.InvariantCulture);
            }

            if (formula.Contains("$F$6", StringComparison.Ordinal))
            {
                iva = Numero(hoja.Cell("F6")) ?? ivaPorDefecto;
            }

            var codigo = hoja.Cell(fila, 2).TryGetValue(out int entero) ? entero.ToString(CultureInfo.InvariantCulture) : null;
            insumos.Add(new InsumoImportado
            {
                Codigo = codigo,
                Tipo = tipo,
                Descripcion = descripcion,
                UnidadCompra = Texto(hoja.Cell(fila, 4)),
                PrecioBase = precio.Value,
                Iva = iva,
                CantidadPorUnidad = Numero(hoja.Cell(fila, 7)) ?? 1,
                UnidadUso = Texto(hoja.Cell(fila, 8)),
                PrecioUnitario = Numero(hoja.Cell(fila, 9)) ?? 0
            });
        }
    }

    private static ManoObraImportada ManoObra(IXLWorksheet hoja, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var mano = new ManoObraImportada();
        for (var fila = 18; fila <= 31; fila++)
        {
            var concepto = Texto(hoja.Cell(fila, 2));
            var porcentaje = Numero(hoja.Cell(fila, 4));
            if (concepto.Length == 0 || porcentaje is null)
            {
                continue;
            }

            mano.Prestaciones.Add(new PrestacionImportada { Concepto = concepto, Porcentaje = porcentaje.Value });
        }

        for (var fila = 49; fila <= 54; fila++)
        {
            var cargo = Texto(hoja.Cell(fila, 2));
            var salario = Numero(hoja.Cell(fila, 4));
            if (cargo.Length == 0 || salario is null)
            {
                continue;
            }

            mano.Cargos.Add(new CargoImportado { Cargo = cargo, SalarioMensual = salario.Value });
        }

        for (var fila = 62; fila <= 73; fila++)
        {
            mano.Cuadrillas.Add(new CuadrillaImportada
            {
                Nombre = Texto(hoja.Cell(fila, 2)),
                Maestros = Numero(hoja.Cell(fila, 4)) ?? 0,
                Oficiales = Numero(hoja.Cell(fila, 5)) ?? 0,
                Ayudantes = Numero(hoja.Cell(fila, 6)) ?? 0,
                CostoDia = Numero(hoja.Cell(fila, 7))
            });
        }

        return mano;
    }

    private static List<ApuImportado> Apus(IXLWorksheet hoja, CancellationToken cancellationToken)
    {
        var ultima = Ultima(hoja);
        var titulos = new List<int>();
        for (var fila = 1; fila < ultima; fila++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Texto(hoja.Cell(fila, 3)).Length > 0 &&
                Texto(hoja.Cell(fila + 2, 3)).Equals("DESCRIPCION", StringComparison.OrdinalIgnoreCase))
            {
                titulos.Add(fila);
            }
        }

        titulos.Add(ultima + 1);
        var apus = new List<ApuImportado>();
        var porFila = new Dictionary<int, ApuImportado>();
        for (var indice = 0; indice < titulos.Count - 1; indice++)
        {
            var inicio = titulos[indice];
            var fin = titulos[indice + 1];
            var referencia = FilaPresupuesto(Formula(hoja.Cell(inicio, 2)));
            var formulaTotal = Formula(hoja.Cell(inicio, 8));
            var apu = new ApuImportado
            {
                Id = apus.Count + 1,
                Fila = inicio,
                FilaPresupuesto = referencia,
                Descripcion = Texto(hoja.Cell(inicio, 3)),
                Unidad = Texto(hoja.Cell(inicio, 4)),
                EsSubanalisis = referencia is null,
                TotalExcel = Numero(hoja.Cell(inicio, 8)),
                Divisor = formulaTotal.Contains("/G", StringComparison.Ordinal) ? Numero(hoja.Cell(inicio, 7)) ?? 1 : 1
            };
            GrupoImportado? grupo = null;
            for (var fila = inicio + 3; fila < fin; fila++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var descripcion = Texto(hoja.Cell(fila, 3));
                if (descripcion.Length == 0)
                {
                    continue;
                }

                var titulo = descripcion.ToUpperInvariant();
                if (titulo.StartsWith("VALOR ", StringComparison.Ordinal))
                {
                    grupo = new GrupoImportado
                    {
                        Tipo = titulo switch
                        {
                            "VALOR MATERIAL" => "Material",
                            "VALOR MANO DE OBRA" => "ManoObra",
                            "VALOR EQUIPO" => "Equipo",
                            _ => "Otros"
                        },
                        Divisor = Numero(hoja.Cell(fila, 7)) ?? 1
                    };
                    apu.Grupos.Add(grupo);
                    continue;
                }

                var formula = Formula(hoja.Cell(fila, 8)).Replace("$", "", StringComparison.Ordinal);
                if (!formula.StartsWith('='))
                {
                    continue;
                }

                grupo ??= new GrupoImportado { Tipo = "Mezcla", Divisor = 1 };
                if (!apu.Grupos.Contains(grupo))
                {
                    apu.Grupos.Add(grupo);
                }

                var rendimiento = Regex.IsMatch(formula, @"^=E\d+\*F\d+\*G\d+$") || Regex.IsMatch(formula, @"^=E\d+\*G\d+\*\(?F\d+\)?$");
                var divisor = Regex.IsMatch(formula, @"^=E\d+\*F\d+/G\d+$");
                var linea = new LineaImportada
                {
                    CodigoInsumo = hoja.Cell(fila, 2).TryGetValue(out int codigo) ? codigo.ToString(CultureInfo.InvariantCulture) : null,
                    Descripcion = descripcion,
                    Unidad = TextoValor(hoja.Cell(fila, 4)),
                    Cantidad = formula.Contains('E') ? Numero(hoja.Cell(fila, 5)) ?? 0 : 1,
                    Precio = Numero(hoja.Cell(fila, 6)) ?? 0,
                    Factor = Numero(hoja.Cell(fila, 7)) ?? 0,
                    Modo = rendimiento ? "rendimiento" : divisor ? "divisor" : "desperdicio"
                };
                var formulaPrecio = Formula(hoja.Cell(fila, 6)).Trim();
                var sub = Regex.Match(formulaPrecio, @"^=\$?H\$?(\d+)$");
                if (sub.Success)
                {
                    linea.SubApuId = int.Parse(sub.Groups[1].Value, CultureInfo.InvariantCulture);
                }

                if (titulo.Contains("HERRAMIENTA MENOR", StringComparison.Ordinal))
                {
                    linea.Tipo = "HerramientaMenor";
                    linea.PorcentajeMO = linea.Factor;
                }
                else if (titulo.StartsWith("MANO DE OBRA", StringComparison.Ordinal) || titulo.StartsWith("M.O", StringComparison.Ordinal) || titulo.Contains("CUADRILLA", StringComparison.Ordinal))
                {
                    linea.Tipo = "ManoObra";
                }

                grupo.Lineas.Add(linea);
            }

            porFila[inicio] = apu;
            apus.Add(apu);
        }

        foreach (var apu in apus)
        {
            foreach (var grupo in apu.Grupos)
            {
                foreach (var linea in grupo.Lineas)
                {
                    if (linea.SubApuId is int fila && porFila.TryGetValue(fila, out var sub))
                    {
                        linea.SubApuId = sub.Id;
                    }
                    else if (linea.SubApuId is not null && !porFila.ContainsKey(linea.SubApuId.Value))
                    {
                        linea.SubApuId = null;
                    }
                }
            }
        }

        return apus;
    }

    private static PresupuestoImportado Presupuesto(IXLWorksheet hoja, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var presupuesto = new PresupuestoImportado
        {
            PorcentajeImprevistos = Numero(hoja.Cell("D191")) ?? 0.05m,
            PorcentajeIncremento = Numero(hoja.Cell("D195")) ?? 0.07m,
            AreaM2 = Numero(hoja.Cell("E199")) ?? 0
        };
        CapituloImportado? capitulo = null;
        for (var fila = 9; fila <= 188; fila++)
        {
            if (Entero(hoja.Cell(fila, 1), out var numero))
            {
                capitulo = new CapituloImportado { Numero = numero, Nombre = Texto(hoja.Cell(fila, 2)) };
                presupuesto.Capitulos.Add(capitulo);
                continue;
            }

            var formula = Formula(hoja.Cell(fila, 7));
            if (capitulo is null || !formula.StartsWith("=E", StringComparison.Ordinal))
            {
                continue;
            }

            capitulo.Items.Add(new ItemImportado
            {
                Codigo = Texto(hoja.Cell(fila, 1)),
                Fila = fila,
                Descripcion = Texto(hoja.Cell(fila, 2)),
                Unidad = TextoValor(hoja.Cell(fila, 3)),
                Cantidad = NumeroCalculado(hoja.Cell(fila, 4)) ?? 0,
                ValorMaterial = NumeroCalculado(hoja.Cell(fila, 5)) ?? 0,
                ValorManoObra = NumeroCalculado(hoja.Cell(fila, 6)) ?? 0
            });
        }

        return presupuesto;
    }

    private static int? FilaPresupuesto(string formula)
    {
        if (!formula.StartsWith("=Presupuesto!", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var digitos = Regex.Replace(formula.Split('!')[1], @"\D", "");
        return int.TryParse(digitos, out var fila) ? fila : null;
    }

    private static IXLWorksheet Hoja(XLWorkbook libro, string nombre) =>
        libro.Worksheets.TryGetWorksheet(nombre, out var hoja)
            ? hoja
            : throw new InvalidOperationException($"El libro no tiene la hoja {nombre}.");

    private static int Ultima(IXLWorksheet hoja) => hoja.LastRowUsed()?.RowNumber() ?? 1;

    private static string Formula(IXLCell celda)
    {
        var formula = celda.FormulaA1;
        if (string.IsNullOrWhiteSpace(formula))
        {
            return "";
        }

        return formula.StartsWith('=') ? formula : "=" + formula;
    }

    private static string Texto(IXLCell celda) => celda.GetString().Trim();

    private static string TextoValor(IXLCell celda)
    {
        if (celda.Value.IsNumber)
        {
            return celda.Value.GetNumber().ToString(CultureInfo.InvariantCulture);
        }

        return Texto(celda);
    }

    private static decimal? NumeroCalculado(IXLCell celda)
    {
        if (!string.IsNullOrWhiteSpace(celda.FormulaA1) && !celda.Value.IsNumber)
        {
            throw new InvalidOperationException("El libro no tiene valores calculados; ábralo y guárdelo en Excel antes de importar.");
        }

        return Numero(celda);
    }

    private static decimal? Numero(IXLCell celda)
    {
        var valor = celda.Value;
        if (valor.IsNumber)
        {
            var numero = valor.GetNumber();
            return double.IsFinite(numero) ? Convert.ToDecimal(numero) : null;
        }

        if (valor.IsBlank)
        {
            return null;
        }

        return decimal.TryParse(valor.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var convertido)
            ? convertido
            : null;
    }

    private static bool Entero(IXLCell celda, out int numero)
    {
        numero = 0;
        var valor = Numero(celda);
        if (valor is null)
        {
            return false;
        }

        var redondo = decimal.Round(valor.Value, 0);
        if (Math.Abs(redondo - valor.Value) > 0.001m)
        {
            return false;
        }

        numero = (int)redondo;
        return true;
    }
}
