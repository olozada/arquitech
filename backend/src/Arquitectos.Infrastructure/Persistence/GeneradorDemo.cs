namespace Arquitectos.Infrastructure.Persistence;

/// <summary>
/// Mismos casos y la misma secuencia de azar que datos_demo/generar_datos_demo.py.
/// Los materiales se guardan como insumos.
/// </summary>
internal static class GeneradorDemo
{
    internal sealed record Persona(string Nombre, string Documento);
    internal sealed record UnidadDemo(string Codigo, string Descripcion);
    internal sealed record MaterialDemo(string Nombre, string Unidad, double Precio, int[] Proveedores);
    internal sealed record ConceptoDemo(string Nombre, string Unidad, double? Precio, int? Material, double CantidadMin, double CantidadMax);
    internal sealed record ObraDemo(string Nombre, string Propietario, DateOnly Inicio, DateOnly? Fin, int[] Contratistas, double Peso);

    internal sealed class LineaDemo
    {
        public int Construccion { get; init; }
        public int Concepto { get; init; }
        public int? Material { get; init; }
        public string Unidad { get; init; } = "";
        public decimal Cantidad { get; set; }
        public decimal Precio { get; set; }
        public string Descripcion { get; init; } = "";
        public string? Anomalia { get; set; }
    }

    internal sealed class ActaDemo
    {
        public int Arquitecto { get; init; }
        public int Revisor { get; init; }
        public int Contratista { get; init; }
        public DateOnly Inicio { get; init; }
        public DateOnly Fin { get; init; }
        public DateTime Registro { get; init; }
        public string? Observacion { get; init; }
        public List<LineaDemo> Lineas { get; } = [];
    }

    public static readonly Persona[] Arquitectos =
    [
        new("Laura Restrepo Gómez", "1094567101"),
        new("Andrés Felipe Ocampo", "1094567823"),
        new("Catalina Henao Ruiz", "1094876540"),
        new("Julián Marín Castaño", "1094123987"),
        new("Paula Andrea Giraldo", "1094654321"),
    ];

    public static readonly Persona[] Revisores =
    [
        new("Ing. Carlos Arango", "1093987601"),
        new("Ing. Natalia Quintero", "1093765432"),
        new("Ing. Mauricio Salazar", "1093543210"),
    ];

    public static readonly Persona[] Contratistas =
    [
        new("Construcciones Quindío S.A.S.", "900456781-2"),
        new("Obras y Acabados El Roble", "901234567-8"),
        new("Estructuras del Café Ltda.", "800987654-3"),
        new("Ingeniería Montenegro S.A.S.", "901876543-5"),
        new("Redes y Montajes Calarcá", "900654321-9"),
        new("Acabados Finos Circasia", "901112233-4"),
    ];

    public static readonly Persona[] Proveedores =
    [
        new("Ferretería La Sexta", "890123456-1"),
        new("Homecenter Armenia", "800111222-3"),
        new("Distribuidora Cemex Eje", "860333444-5"),
        new("Aceros del Quindío", "901555666-7"),
    ];

    public static readonly UnidadDemo[] Unidades =
    [
        new("m3", "Metro cúbico"),
        new("m2", "Metro cuadrado"),
        new("ml", "Metro lineal"),
        new("kg", "Kilogramo"),
        new("und", "Unidad"),
        new("gl", "Global"),
    ];

    public static readonly MaterialDemo[] Materiales =
    [
        new("Cemento gris 50 kg", "und", 34000, [1, 2, 3]),
        new("Arena de río", "m3", 95000, [1, 3]),
        new("Gravilla triturada 3/4", "m3", 110000, [1, 3]),
        new("Acero corrugado 1/2\" x 6 m", "und", 42000, [2, 4]),
        new("Acero corrugado 3/8\" x 6 m", "und", 24000, [2, 4]),
        new("Bloque de arcilla #5", "und", 1900, [1, 2]),
        new("Ladrillo tolete común", "und", 950, [1]),
        new("Cerámica piso 45x45", "m2", 38000, [1, 2]),
        new("Porcelanato 60x60", "m2", 72000, [2]),
        new("Pintura vinilo tipo 1 (galón)", "gl", 68000, [1, 2]),
        new("Estuco plástico (galón)", "gl", 42000, [1, 2]),
        new("Teja termoacústica", "m2", 56000, [2, 4]),
        new("Tubería PVC presión 1/2\"", "ml", 6200, [1, 2]),
        new("Cable THHN #12", "ml", 3100, [1, 2]),
        new("Malla electrosoldada", "m2", 21000, [4]),
        new("Impermeabilizante (galón)", "gl", 89000, []),
    ];

    public static readonly ConceptoDemo[] Conceptos =
    [
        new("Excavación manual", "m3", 48000, null, 8, 60),
        new("Relleno compactado", "m3", 62000, null, 5, 40),
        new("Concreto zapatas 3000 PSI", "m3", 780000, null, 2, 18),
        new("Acero de refuerzo figurado", "kg", 6800, null, 300, 2500),
        new("Placa de entrepiso", "m2", 165000, null, 20, 180),
        new("Mampostería bloque #5", "m2", 52000, null, 30, 220),
        new("Pañete de muros", "m2", 24000, null, 40, 300),
        new("Estuco y pintura 3 manos", "m2", 19500, null, 60, 400),
        new("Enchape piso cerámica", "m2", 38000, null, 20, 160),
        new("Instalación hidrosanitaria", "ml", 31000, null, 20, 140),
        new("Instalación eléctrica", "ml", 18500, null, 40, 260),
        new("Cubierta teja termoacústica", "m2", 43000, null, 40, 260),
        new("Suministro cemento gris", "und", null, 0, 40, 400),
        new("Suministro acero 1/2\"", "und", null, 3, 30, 250),
        new("Suministro bloque #5", "und", null, 5, 500, 4000),
        new("Suministro cerámica piso", "m2", null, 7, 20, 160),
        new("Suministro pintura vinilo", "gl", null, 9, 5, 40),
        new("Suministro teja termoacústica", "m2", null, 11, 40, 220),
    ];

    public static readonly ObraDemo[] Obras =
    [
        new("Edificio Torre Café – Armenia", "Inversiones Cafeteras S.A.", new DateOnly(2024, 11, 1), null, [1, 3, 5], 3),
        new("Conjunto Guaduales de La Tebaida", "Constructora Eje Verde", new DateOnly(2025, 2, 1), null, [1, 2, 6], 2.5),
        new("Bodega Zona Franca Quindío", "Logística ZFQ S.A.S.", new DateOnly(2025, 7, 1), new DateOnly(2026, 3, 31), [3, 4], 2),
        new("Casa campestre Vereda El Caimo", "Familia Ramírez Ospina", new DateOnly(2024, 11, 1), new DateOnly(2025, 8, 31), [2, 6], 1.2),
        new("Colegio Rural Montenegro – Bloque B", "Gobernación del Quindío", new DateOnly(2025, 9, 1), null, [1, 4, 5], 2.2),
        new("Centro Comercial Portal del Quindío – Locales", "Portal del Quindío P.H.", new DateOnly(2025, 11, 1), null, [2, 3, 6], 1.6),
        new("Hotel boutique Salento", "Turismo Cocora S.A.S.", new DateOnly(2026, 1, 1), new DateOnly(2026, 8, 20), [1, 6], 1.4),
        new("Clínica veterinaria Calarcá", "Vet Calarcá S.A.S.", new DateOnly(2026, 11, 1), null, [4], 0),
    ];

    private static readonly int[][] Fases =
    [
        [0, 1, 2, 3, 12, 13],
        [2, 3, 4, 5, 12, 13, 14],
        [5, 6, 9, 10, 14],
        [7, 8, 11, 15, 16, 17, 10],
    ];

    private static readonly string?[] Observaciones =
    [
        "Avance conforme a cronograma.",
        "Se recibe obra a satisfacción.",
        "Pendiente corregir plomo en muro eje 3.",
        "Lluvias retrasaron 2 días el vaciado.",
        "Se aprueba con observaciones de interventoría.",
        "Cantidades verificadas en sitio.",
        "Material de cambio aprobado por el arquitecto.",
        null,
        null,
    ];

    public static (Dictionary<(int Proveedor, int Material), decimal> Lista, List<ActaDemo> Actas) Generar(DateOnly hoy)
    {
        var azar = new AleatorioPython(2026);
        var lista = new Dictionary<(int, int), decimal>();
        for (var material = 0; material < Materiales.Length; material++)
        {
            foreach (var proveedor in Materiales[material].Proveedores)
            {
                var precio = Redondear(Materiales[material].Precio * Inflacion(hoy) * azar.Uniforme(0.92, 1.12), -1);
                lista[(proveedor, material + 1)] = (decimal)precio;
            }
        }

        var actas = new List<ActaDemo>();
        var lineas = new List<LineaDemo>();
        foreach (var (inicio, fin) in Periodos(hoy))
        {
            for (var obra = 0; obra < Obras.Length; obra++)
            {
                var ficha = Obras[obra];
                if (ficha.Peso == 0 || fin < ficha.Inicio || (ficha.Fin is DateOnly termino && inicio > termino))
                {
                    continue;
                }

                var probabilidad = Math.Min(0.95, 0.40 * ficha.Peso) * Estacional(fin);
                if (azar.Siguiente() > probabilidad)
                {
                    continue;
                }

                var duracion = ((ficha.Fin ?? hoy).DayNumber - ficha.Inicio.DayNumber);
                var avance = Math.Min(0.999, Math.Max(0, (fin.DayNumber - ficha.Inicio.DayNumber) / (double)Math.Max(duracion, 1)));
                var fase = Fases[Math.Min(3, (int)(avance * 4))];
                var contratista = azar.ElegirPonderado(ficha.Contratistas, ficha.Contratistas.Select(id => id == 1 ? 3d : 1d).ToList());
                var rezagos = new[] { azar.EnteroIncluyente(1, 7), azar.EnteroIncluyente(8, 14), azar.EnteroIncluyente(16, 30) };
                var rezago = azar.ElegirPonderado(rezagos, [75d, 15d, 10d]);
                var registro = fin.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(rezago).AddHours(azar.EnteroIncluyente(13, 22)).AddMinutes(azar.EnteroIncluyente(0, 59));
                if (DateOnly.FromDateTime(registro) > hoy)
                {
                    registro = hoy.ToDateTime(new TimeOnly(14, 0), DateTimeKind.Utc);
                }

                var acta = new ActaDemo
                {
                    Arquitecto = 1 + (obra + 1 + azar.EnteroIncluyente(0, 1)) % Arquitectos.Length,
                    Revisor = 1 + azar.EnteroIncluyente(0, Revisores.Length - 1),
                    Contratista = contratista,
                    Inicio = inicio,
                    Fin = fin,
                    Registro = registro,
                    Observacion = azar.Elegir(Observaciones),
                };
                var cantidadLineas = azar.EnteroIncluyente(3, 9);
                foreach (var concepto in azar.Muestra(fase, Math.Min(cantidadLineas, fase.Length)))
                {
                    var fichaConcepto = Conceptos[concepto];
                    var escala = 0.6 + 0.4 * ficha.Peso;
                    var cantidad = Redondear(azar.Uniforme(fichaConcepto.CantidadMin, fichaConcepto.CantidadMax) * escala, 2);
                    double precio;
                    int? material = null;
                    if (fichaConcepto.Material is int indice)
                    {
                        var proveedores = Materiales[indice].Proveedores;
                        var referencia = proveedores.Length == 0
                            ? Materiales[indice].Precio * Inflacion(hoy)
                            : (double)lista[(azar.Elegir(proveedores), indice + 1)];
                        precio = referencia * Inflacion(fin) / Inflacion(hoy) * azar.Uniforme(0.99, 1.04);
                        material = indice + 1;
                    }
                    else
                    {
                        precio = fichaConcepto.Precio!.Value * Inflacion(fin) * azar.Uniforme(0.95, 1.05);
                    }

                    var linea = new LineaDemo
                    {
                        Construccion = obra + 1,
                        Concepto = concepto + 1,
                        Material = material,
                        Unidad = fichaConcepto.Unidad,
                        Cantidad = (decimal)cantidad,
                        Precio = (decimal)Redondear(precio, -1),
                        Descripcion = $"{fichaConcepto.Nombre} – {ficha.Nombre.Split(" – ")[0]}",
                    };
                    acta.Lineas.Add(linea);
                    lineas.Add(linea);
                }

                actas.Add(acta);
            }
        }

        SembrarAnomalias(azar, hoy, actas, lineas);
        return (lista, actas);
    }

    private static void SembrarAnomalias(
        AleatorioPython azar,
        DateOnly hoy,
        List<ActaDemo> actas,
        List<LineaDemo> lineas)
    {
        var corte = hoy.AddDays(-120);
        var delRoble = lineas.Where(linea => linea.Material is null && ActaDe(actas, linea) is ActaDemo acta && acta.Contratista == 2 && acta.Fin >= corte).ToList();
        foreach (var linea in azar.Muestra(delRoble, Math.Min(10, delRoble.Count)))
        {
            linea.Precio = (decimal)Redondear((double)linea.Precio * azar.Uniforme(1.35, 1.60), -1);
            linea.Anomalia = "A1";
        }

        var otras = lineas.Where(linea => linea.Anomalia is null && linea.Material is null && ActaDe(actas, linea) is ActaDemo acta && acta.Contratista is 3 or 4 && acta.Fin >= corte).ToList();
        foreach (var linea in azar.Muestra(otras, Math.Min(3, otras.Count)))
        {
            linea.Precio = (decimal)Redondear((double)linea.Precio * azar.Uniforme(1.18, 1.25), -1);
            linea.Anomalia = "A1";
        }

        (int Construccion, int Concepto, int DiasInicio, int DiasFin, int SolapeInicio, int SolapeFin)[] pares =
        [
            (1, 10, 70, 56, 62, 48),
            (5, 9, 40, 26, 33, 20),
        ];
        foreach (var par in pares)
        {
            var concepto = Conceptos[par.Concepto];
            var cantidad = Redondear(azar.Uniforme(concepto.CantidadMin, concepto.CantidadMax), 2);
            foreach (var (diasInicio, diasFin) in new[] { (par.DiasInicio, par.DiasFin), (par.SolapeInicio, par.SolapeFin) })
            {
                var fin = hoy.AddDays(-diasFin);
                var acta = new ActaDemo
                {
                    Arquitecto = 2,
                    Revisor = 1,
                    Contratista = 5,
                    Inicio = hoy.AddDays(-diasInicio),
                    Fin = fin,
                    Registro = fin.AddDays(3).ToDateTime(new TimeOnly(15, 0), DateTimeKind.Utc),
                    Observacion = "Avance redes.",
                };
                acta.Lineas.Add(new LineaDemo
                {
                    Construccion = par.Construccion,
                    Concepto = par.Concepto + 1,
                    Unidad = concepto.Unidad,
                    Cantidad = (decimal)cantidad,
                    Precio = (decimal)Redondear(concepto.Precio!.Value * Inflacion(fin), -1),
                    Descripcion = $"{concepto.Nombre} – mismo frente de trabajo",
                    Anomalia = "A3",
                });
                actas.Add(acta);
            }
        }
    }

    private static ActaDemo? ActaDe(List<ActaDemo> actas, LineaDemo linea) =>
        actas.FirstOrDefault(acta => acta.Lineas.Contains(linea));

    private static List<(DateOnly Inicio, DateOnly Fin)> Periodos(DateOnly hoy)
    {
        var periodos = new List<(DateOnly, DateOnly)>();
        var cursor = new DateOnly(2024, 11, 1);
        while (cursor <= hoy)
        {
            var dia28 = new DateOnly(cursor.Year, cursor.Month, 28);
            var siguiente = dia28.AddDays(4);
            siguiente = new DateOnly(siguiente.Year, siguiente.Month, 1);
            periodos.Add((cursor, new DateOnly(cursor.Year, cursor.Month, 15)));
            periodos.Add((new DateOnly(cursor.Year, cursor.Month, 16), siguiente.AddDays(-1)));
            cursor = siguiente;
        }

        return periodos.Where(periodo => periodo.Item2 < hoy.AddDays(-2)).ToList();
    }

    private static double Inflacion(DateOnly fecha)
    {
        var meses = (fecha.Year - 2024) * 12 + fecha.Month - 11;
        return Math.Pow(1.006, meses);
    }

    private static double Estacional(DateOnly fecha) => fecha.Month switch
    {
        12 => 0.55,
        1 => 0.7,
        4 => 0.9,
        _ => 1,
    };

    private static double Redondear(double valor, int decimales)
    {
        var factor = Math.Pow(10, decimales);
        return Math.Round(valor * factor, MidpointRounding.ToEven) / factor;
    }
}
