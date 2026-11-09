"""
Genera datos_demo.sql: datos simulados (determinísticos) para la app Arquitectos.
Uso:  python generar_datos_demo.py [--hoy 2026-11-09] [--salida datos_demo.sql]
Todos los montos en COP. Semilla fija => mismo resultado siempre.
"""
import argparse
import random
from collections import defaultdict
from datetime import date, datetime, timedelta

SEMILLA = 2026

ARQUITECTOS = ["Laura Restrepo Gómez", "Andrés Felipe Ocampo", "Catalina Henao Ruiz",
               "Julián Marín Castaño", "Paula Andrea Giraldo"]
REVISORES = ["Ing. Carlos Arango", "Ing. Natalia Quintero", "Ing. Mauricio Salazar"]
CONTRATISTAS = [
    "Construcciones Quindío S.A.S.",          # 1 grande (concentración)
    "Obras y Acabados El Roble",              # 2 infractor de precios
    "Estructuras del Café Ltda.",             # 3
    "Ingeniería Montenegro S.A.S.",           # 4
    "Redes y Montajes Calarcá",               # 5 doble cobro
    "Acabados Finos Circasia",                # 6
]
PROVEEDORES = ["Ferretería La Sexta", "Homecenter Armenia", "Distribuidora Cemex Eje",
               "Aceros del Quindío"]
UM = [("m3", "Metro cúbico"), ("m2", "Metro cuadrado"), ("ml", "Metro lineal"),
      ("kg", "Kilogramo"), ("und", "Unidad"), ("gl", "Global")]
UMI = {c: i + 1 for i, (c, _) in enumerate(UM)}

# (descripcion, unidad, precio_base_material_COP, proveedores_que_lo_venden)
MATERIALES = [
    ("Cemento gris 50 kg", "und", 34000, [1, 2, 3]),
    ("Arena de río", "m3", 95000, [1, 3]),
    ("Gravilla triturada 3/4", "m3", 110000, [1, 3]),
    ("Acero corrugado 1/2\" x 6 m", "und", 42000, [2, 4]),
    ("Acero corrugado 3/8\" x 6 m", "und", 24000, [2, 4]),
    ("Bloque de arcilla #5", "und", 1900, [1, 2]),
    ("Ladrillo tolete común", "und", 950, [1]),            # un solo proveedor
    ("Cerámica piso 45x45", "m2", 38000, [1, 2]),
    ("Porcelanato 60x60", "m2", 72000, [2]),               # un solo proveedor
    ("Pintura vinilo tipo 1 (galón)", "gl", 68000, [1, 2]),
    ("Estuco plástico (galón)", "gl", 42000, [1, 2]),
    ("Teja termoacústica", "m2", 56000, [2, 4]),
    ("Tubería PVC presión 1/2\"", "ml", 6200, [1, 2]),
    ("Cable THHN #12", "ml", 3100, [1, 2]),
    ("Malla electrosoldada", "m2", 21000, [4]),
    ("Impermeabilizante (galón)", "gl", 89000, []),        # sin proveedor (calidad datos)
]

# Conceptos: (descripcion, um, precio_unit_base, material_idx|None, rango_cantidad)
# Los "Suministro ..." llevan material y precio ~ precio de lista del proveedor.
CONCEPTOS = [
    ("Excavación manual", "m3", 48000, None, (8, 60)),
    ("Relleno compactado", "m3", 62000, None, (5, 40)),
    ("Concreto zapatas 3000 PSI", "m3", 780000, None, (2, 18)),
    ("Acero de refuerzo figurado", "kg", 6800, None, (300, 2500)),
    ("Placa de entrepiso", "m2", 165000, None, (20, 180)),
    ("Mampostería bloque #5", "m2", 52000, None, (30, 220)),
    ("Pañete de muros", "m2", 24000, None, (40, 300)),
    ("Estuco y pintura 3 manos", "m2", 19500, None, (60, 400)),
    ("Enchape piso cerámica", "m2", 38000, None, (20, 160)),
    ("Instalación hidrosanitaria", "ml", 31000, None, (20, 140)),
    ("Instalación eléctrica", "ml", 18500, None, (40, 260)),
    ("Cubierta teja termoacústica", "m2", 43000, None, (40, 260)),
    ("Suministro cemento gris", "und", None, 0, (40, 400)),
    ("Suministro acero 1/2\"", "und", None, 3, (30, 250)),
    ("Suministro bloque #5", "und", None, 5, (500, 4000)),
    ("Suministro cerámica piso", "m2", None, 7, (20, 160)),
    ("Suministro pintura vinilo", "gl", None, 9, (5, 40)),
    ("Suministro teja termoacústica", "m2", None, 11, (40, 220)),
]

# Construcciones: (descripcion, propietario, inicio, fin|None, contratistas, peso)
CONSTRUCCIONES = [
    ("Edificio Torre Café – Armenia", "Inversiones Cafeteras S.A.", date(2024, 11, 1), None, [1, 3, 5], 3.0),
    ("Conjunto Guaduales de La Tebaida", "Constructora Eje Verde", date(2025, 2, 1), None, [1, 2, 6], 2.5),
    ("Bodega Zona Franca Quindío", "Logística ZFQ S.A.S.", date(2025, 7, 1), date(2026, 3, 31), [3, 4], 2.0),
    ("Casa campestre Vereda El Caimo", "Familia Ramírez Ospina", date(2024, 11, 1), date(2025, 8, 31), [2, 6], 1.2),
    ("Colegio Rural Montenegro – Bloque B", "Gobernación del Quindío", date(2025, 9, 1), None, [1, 4, 5], 2.2),
    ("Centro Comercial Portal del Quindío – Locales", "Portal del Quindío P.H.", date(2025, 11, 1), None, [2, 3, 6], 1.6),
    ("Hotel boutique Salento", "Turismo Cocora S.A.S.", date(2026, 1, 1), date(2026, 8, 20), [1, 6], 1.4),  # sin actividad >30d
    ("Clínica veterinaria Calarcá", "Vet Calarcá S.A.S.", date(2026, 11, 1), None, [4], 0.0),              # nunca ha tenido actas
]

# Conceptos por fase de obra (índices de CONCEPTOS)
FASES = [
    [0, 1, 2, 3, 12, 13],            # cimentación
    [2, 3, 4, 5, 12, 13, 14],        # estructura / mampostería
    [5, 6, 9, 10, 14],               # obra gris / redes
    [7, 8, 11, 15, 16, 17, 10],      # acabados / cubierta
]

OBSERVACIONES = [
    "Avance conforme a cronograma.", "Se recibe obra a satisfacción.",
    "Pendiente corregir plomo en muro eje 3.", "Lluvias retrasaron 2 días el vaciado.",
    "Se aprueba con observaciones de interventoría.", "Cantidades verificadas en sitio.",
    "Material de cambio aprobado por el arquitecto.", None, None,
]


def sql_str(v):
    if v is None:
        return "NULL"
    return "N'" + str(v).replace("'", "''") + "'"


def sql_dec(v, nd=2):
    return f"{v:.{nd}f}"


def generar(hoy: date):
    rnd = random.Random(SEMILLA)
    inicio_hist = date(2024, 11, 1)  # 24 meses: permite comparar contra el año anterior

    def factor_inflacion(f: date):
        meses = (f.year - 2024) * 12 + f.month - 11
        return 1.006 ** meses   # ~7.4% anual

    # --- lista de precios proveedores ---
    lista = {}  # (prov, mat) -> precio
    for mi, (_, _, base, provs) in enumerate(MATERIALES, start=1):
        for p in provs:
            lista[(p, mi)] = round(base * factor_inflacion(hoy) * rnd.uniform(0.92, 1.12), -1)

    def estacional(f: date):
        return {12: 0.55, 1: 0.7, 4: 0.9}.get(f.month, 1.0)

    actas, detalles = [], []
    num = 0
    det_id = 0
    construcciones_actas = defaultdict(list)

    # periodos quincenales
    periodos = []
    d = inicio_hist
    while d <= hoy:
        p1 = (d, d.replace(day=15))
        nxt = (d.replace(day=28) + timedelta(days=4)).replace(day=1)
        p2 = (d.replace(day=16), nxt - timedelta(days=1))
        periodos += [p1, p2]
        d = nxt
    periodos = [p for p in periodos if p[1] < hoy - timedelta(days=2)]  # solo periodos cerrados

    for (fi, ff) in periodos:
        for ci, (cdesc, _, cini, cfin, contras, peso) in enumerate(CONSTRUCCIONES, start=1):
            if peso == 0 or ff < cini or (cfin and fi > cfin):
                continue
            # probabilidad de acta en la quincena según peso y estacionalidad
            prob = min(0.95, 0.40 * peso) * estacional(ff)
            if rnd.random() > prob:
                continue
            dur = (cfin or hoy) - cini
            avance = min(0.999, max(0.0, (ff - cini).days / max(dur.days, 1)))
            fase = FASES[min(3, int(avance * 4))]
            # contratista: el #1 domina donde participa
            pesos_c = [3.0 if c == 1 else 1.0 for c in contras]
            contratista = rnd.choices(contras, weights=pesos_c)[0]
            num += 1
            lag = rnd.choices([rnd.randint(1, 7), rnd.randint(8, 14), rnd.randint(16, 30)], weights=[75, 15, 10])[0]
            freg = datetime.combine(ff + timedelta(days=lag), datetime.min.time()) + timedelta(hours=rnd.randint(13, 22), minutes=rnd.randint(0, 59))
            if freg.date() > hoy:
                freg = datetime.combine(hoy, datetime.min.time()) + timedelta(hours=14)
            acta = dict(n=num, fi=fi, ff=ff, arq=1 + (ci + rnd.randint(0, 1)) % len(ARQUITECTOS),
                        rev=1 + rnd.randint(0, len(REVISORES) - 1), con=contratista,
                        obs=rnd.choice(OBSERVACIONES), freg=freg, constr=ci)
            actas.append(acta)
            construcciones_actas[ci].append(acta)
            n_lineas = rnd.randint(3, 9)
            usados = rnd.sample(fase, k=min(n_lineas, len(fase)))
            for k in usados:
                desc, um, pbase, mat, (qmin, qmax) = CONCEPTOS[k]
                escala = 0.6 + 0.4 * peso
                cant = round(rnd.uniform(qmin, qmax) * escala, 2)
                if mat is not None:
                    provs = MATERIALES[mat][3]
                    # precio de un proveedor al azar, deflactado a la fecha del acta
                    precio_ref = lista[(rnd.choice(provs), mat + 1)] if provs else MATERIALES[mat][2] * factor_inflacion(hoy)
                    precio = precio_ref * factor_inflacion(ff) / factor_inflacion(hoy) * rnd.uniform(0.99, 1.04)
                    mat_id = mat + 1
                else:
                    precio = pbase * factor_inflacion(ff) * rnd.uniform(0.95, 1.05)
                    mat_id = None
                det_id += 1
                detalles.append(dict(id=det_id, n=num, constr=ci, concepto=k + 1, um=UMI[um], mat=mat_id,
                                     cant=cant, precio=round(precio, -1), desc=f"{desc} – {cdesc.split(' – ')[0]}",
                                     anomalia=None))

    # --- anomalías sembradas ---
    # A1: El Roble (2) con sobreprecio 35–60% en ~10 líneas recientes (últimos 4 meses)
    corte = hoy - timedelta(days=120)
    cand = [dl for dl in detalles if next(a for a in actas if a["n"] == dl["n"])["con"] == 2
            and next(a for a in actas if a["n"] == dl["n"])["ff"] >= corte and dl["mat"] is None]
    for dl in rnd.sample(cand, k=min(10, len(cand))):
        dl["precio"] = round(dl["precio"] * rnd.uniform(1.35, 1.60), -1)
        dl["anomalia"] = "A1"
    # A1 adicional aislado en otro contratista (severidad media)
    cand2 = [dl for dl in detalles if next(a for a in actas if a["n"] == dl["n"])["con"] in (3, 4)
             and next(a for a in actas if a["n"] == dl["n"])["ff"] >= corte and dl["mat"] is None and not dl["anomalia"]]
    for dl in rnd.sample(cand2, k=min(3, len(cand2))):
        dl["precio"] = round(dl["precio"] * rnd.uniform(1.18, 1.25), -1)
        dl["anomalia"] = "A1"

    # A3: dos casos de doble cobro (Redes y Montajes Calarcá = 5)
    for (constr, concepto_idx, fi, ff, solape_fi, solape_ff) in [
        (1, 10, hoy - timedelta(days=70), hoy - timedelta(days=56), hoy - timedelta(days=62), hoy - timedelta(days=48)),
        (5, 9, hoy - timedelta(days=40), hoy - timedelta(days=26), hoy - timedelta(days=33), hoy - timedelta(days=20)),
    ]:
        desc, um, pbase, _, (qmin, qmax) = CONCEPTOS[concepto_idx]
        cant = round(rnd.uniform(qmin, qmax), 2)
        for (a_fi, a_ff) in [(fi, ff), (solape_fi, solape_ff)]:
            num += 1
            freg = datetime.combine(a_ff + timedelta(days=3), datetime.min.time()) + timedelta(hours=15)
            actas.append(dict(n=num, fi=a_fi, ff=a_ff, arq=2, rev=1, con=5, obs="Avance redes.",
                              freg=freg, constr=constr))
            det_id += 1
            detalles.append(dict(id=det_id, n=num, constr=constr, concepto=concepto_idx + 1, um=UMI[um], mat=None,
                                 cant=cant, precio=round(pbase * factor_inflacion(a_ff), -1),
                                 desc=f"{desc} – mismo frente de trabajo", anomalia="A3"))

    actas.sort(key=lambda a: a["n"])
    return lista, actas, detalles


def validar(hoy, lista, actas, detalles):
    """Replica las reglas del dashboard (DASHBOARD_KPIS.md §3) para comprobar que los datos las disparan."""
    am = {a["n"]: a for a in actas}
    res = {}
    # K1/K2 últimos 12 meses vs anteriores
    desde = date(hoy.year - 1, hoy.month, 1)
    dias = (hoy - desde).days + 1
    hasta_ant = desde - timedelta(days=1)
    desde_ant = hasta_ant - timedelta(days=dias - 1)

    def val(d0, d1):
        ns = {a["n"] for a in actas if d0 <= a["ff"] <= d1}
        return sum(dl["cant"] * dl["precio"] for dl in detalles if dl["n"] in ns), len(ns)
    res["K1_valor"], res["K2_actas"] = val(desde, hoy)
    res["K1_ant"], res["K2_ant"] = val(desde_ant, hasta_ant)

    # A1
    a1 = 0; imp1 = 0; a1s = 0
    for dl in detalles:
        a = am[dl["n"]]
        if not (desde <= a["ff"] <= hoy):
            continue
        refs = [x["precio"] for x in detalles
                if x["n"] != dl["n"] and (x["concepto"], x["um"], x["mat"]) == (dl["concepto"], dl["um"], dl["mat"])
                and a["ff"] - timedelta(days=180) <= am[x["n"]]["ff"] < a["ff"]]
        if len(refs) >= 3:
            ref = sum(refs) / len(refs)
            if (dl["precio"] - ref) / ref > 0.15:
                a1 += 1; imp1 += (dl["precio"] - ref) * dl["cant"]
                if dl["anomalia"] == "A1": a1s += 1
    res["A1_alertas"], res["A1_impacto"] = a1, imp1
    res["A1_sembradas_detectadas"] = f'{a1s} de {sum(1 for d in detalles if d["anomalia"]=="A1")}'
    # A2
    minl = defaultdict(lambda: None)
    for (p, m), pr in lista.items():
        minl[m] = pr if minl[m] is None else min(minl[m], pr)
    a2 = [(dl["precio"] - minl[dl["mat"]]) * dl["cant"] for dl in detalles
          if dl["mat"] and minl[dl["mat"]] and dl["precio"] > minl[dl["mat"]] and desde <= am[dl["n"]]["ff"] <= hoy]
    res["A2_lineas"], res["A2_ahorro"] = len(a2), sum(a2)
    # A3
    a3 = 0
    for i, d1 in enumerate(detalles):
        for d2 in detalles[i + 1:]:
            x, y = am[d1["n"]], am[d2["n"]]
            if d1["n"] != d2["n"] and (d1["constr"], d1["concepto"]) == (d2["constr"], d2["concepto"]) \
                    and x["con"] == y["con"] and x["fi"] <= y["ff"] and y["fi"] <= x["ff"]:
                a3 += 1
    res["A3_pares"] = a3
    # A4
    ult = {}
    for a in actas:
        ult[a["constr"]] = max(ult.get(a["constr"], date.min), a["ff"])
    res["A4_obras"] = [CONSTRUCCIONES[c - 1][0] for c in range(1, len(CONSTRUCCIONES) + 1)
                       if ult.get(c, date.min) < hoy - timedelta(days=30)
                       and (CONSTRUCCIONES[c - 1][3] is None or CONSTRUCCIONES[c - 1][3] >= hoy - timedelta(days=90))]
    # A5 (freg en UTC -> local -5h)
    res["A5_tardias"] = sum(1 for a in actas if desde <= a["ff"] <= hoy
                            and ((a["freg"] - timedelta(hours=5)).date() - a["ff"]).days > 15)
    # concentración
    porc = defaultdict(float)
    for dl in detalles:
        if desde <= am[dl["n"]]["ff"] <= hoy:
            porc[am[dl["n"]]["con"]] += dl["cant"] * dl["precio"]
    tot = sum(porc.values())
    res["share_contratistas"] = {CONTRATISTAS[k - 1]: round(v / tot * 100, 1) for k, v in sorted(porc.items(), key=lambda t: -t[1])}
    return res


def emitir_sql(hoy, lista, actas, detalles, salida):
    L = []
    w = L.append
    w("/* ============================================================================")
    w("   DATOS DEMO – App Arquitectos   (generado por generar_datos_demo.py)")
    w(f"   Fecha de referencia: {hoy.isoformat()}   Semilla: {SEMILLA}")
    w(f"   {len(actas)} actas · {len(detalles)} líneas · datos SIMULADOS, no usar en producción")
    w("   Requisitos: BD creada con la migración Inicial (dotnet ef database update).")
    w("   Seguridad: si ya hay datos en catálogos o actas, el script se detiene.")
    w("              Para borrar y recargar: SET @Reiniciar = 1 (borra actas, precios y catálogos;")
    w("              NO toca usuarios ni menú).")
    w("   ============================================================================ */")
    w("SET NOCOUNT ON;")
    w("SET XACT_ABORT ON;")
    w("DECLARE @Reiniciar bit = 0;")
    w("")
    w("IF @Reiniciar = 0 AND (EXISTS (SELECT 1 FROM dbo.Actas) OR EXISTS (SELECT 1 FROM dbo.Arquitectos)")
    w("    OR EXISTS (SELECT 1 FROM dbo.Construcciones) OR EXISTS (SELECT 1 FROM dbo.Conceptos))")
    w("BEGIN")
    w("    RAISERROR(N'Ya existen datos. Revise y ejecute con @Reiniciar = 1 para borrarlos y recargar.', 16, 1);")
    w("    RETURN;")
    w("END;")
    w("")
    w("BEGIN TRY")
    w("BEGIN TRANSACTION;")
    w("")
    w("IF @Reiniciar = 1")
    w("BEGIN")
    for t in ["ActaDetalles", "Actas", "ProveedorMateriales", "Arquitectos", "Revisores", "Contratistas",
              "Proveedores", "Construcciones", "Conceptos", "Materiales", "UnidadesMedida"]:
        w(f"    DELETE FROM dbo.{t};")
    for t in ["ActaDetalles", "Actas", "Arquitectos", "Revisores", "Contratistas",
              "Proveedores", "Construcciones", "Conceptos", "Materiales", "UnidadesMedida"]:
        w(f"    DBCC CHECKIDENT ('dbo.{t}', RESEED, 0) WITH NO_INFOMSGS;")
    w("END;")
    w("")

    def bloque(tabla, cols, filas, identity=True):
        if identity:
            w(f"SET IDENTITY_INSERT dbo.{tabla} ON;")
        for i in range(0, len(filas), 900):
            w(f"INSERT INTO dbo.{tabla} ({', '.join(cols)}) VALUES")
            chunk = filas[i:i + 900]
            for j, f in enumerate(chunk):
                w("  (" + ", ".join(f) + ")" + ("," if j < len(chunk) - 1 else ";"))
        if identity:
            w(f"SET IDENTITY_INSERT dbo.{tabla} OFF;")
        w("")

    bloque("UnidadesMedida", ["Id", "Nombre", "Descripcion", "Activo"],
           [[str(i), sql_str(c), sql_str(d), "1"] for i, (c, d) in enumerate(UM, 1)])
    bloque("Arquitectos", ["Id", "Nombre", "Activo"], [[str(i), sql_str(n), "1"] for i, n in enumerate(ARQUITECTOS, 1)])
    bloque("Revisores", ["Id", "Nombre", "Activo"], [[str(i), sql_str(n), "1"] for i, n in enumerate(REVISORES, 1)])
    bloque("Contratistas", ["Id", "Nombre", "Activo"], [[str(i), sql_str(n), "1"] for i, n in enumerate(CONTRATISTAS, 1)])
    bloque("Proveedores", ["Id", "Nombre", "Activo"], [[str(i), sql_str(n), "1"] for i, n in enumerate(PROVEEDORES, 1)])
    bloque("Construcciones", ["Id", "Descripcion", "Propietario", "Activo"],
           [[str(i), sql_str(c[0]), sql_str(c[1]),
             "0" if c[3] is not None and c[3] < hoy - timedelta(days=90) else "1"]   # obra terminada = inactiva
            for i, c in enumerate(CONSTRUCCIONES, 1)])
    bloque("Materiales", ["Id", "Codigo", "Descripcion", "Unidad", "Activo"],
           [[str(i), sql_str(f"MAT-{i:03d}"), sql_str(m[0]), sql_str(m[1]), "1"] for i, m in enumerate(MATERIALES, 1)])
    bloque("Conceptos", ["Id", "Codigo", "Descripcion", "Unidad", "Activo"],
           [[str(i), sql_str(f"CON-{i:03d}"), sql_str(c[0]), sql_str(c[1]), "1"] for i, c in enumerate(CONCEPTOS, 1)])
    bloque("ProveedorMateriales", ["ProveedorId", "MaterialId", "Precio"],
           [[str(p), str(m), sql_dec(pr)] for (p, m), pr in sorted(lista.items())], identity=False)
    bloque("Actas", ["NumeroActa", "FechaInicial", "FechaFinal", "ArquitectoId", "RevisorId", "ContratistaId",
                     "Observacion", "FechaRegistro", "UsuarioRegistroId"],
           [[str(a["n"]), f"'{a['fi'].isoformat()}'", f"'{a['ff'].isoformat()}'", str(a["arq"]), str(a["rev"]),
             str(a["con"]), sql_str(a["obs"]), f"'{a['freg'].strftime('%Y-%m-%dT%H:%M:%S')}'",
             "(SELECT MIN(Id) FROM dbo.Usuarios)"] for a in actas])
    bloque("ActaDetalles", ["Id", "NumeroActa", "ConstruccionId", "ConceptoId", "UnidadMedidaId", "MaterialId",
                            "Cantidad", "Precio", "Descripcion"],
           [[str(d["id"]), str(d["n"]), str(d["constr"]), str(d["concepto"]), str(d["um"]),
             str(d["mat"]) if d["mat"] else "NULL", sql_dec(d["cant"], 4), sql_dec(d["precio"]), sql_str(d["desc"])]
            for d in detalles])

    w("COMMIT TRANSACTION;")
    w("END TRY")
    w("BEGIN CATCH")
    w("    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;")
    w("    THROW;")
    w("END CATCH;")
    w("")
    w("/* ---------------- Verificación rápida ---------------- */")
    w("SELECT 'Actas' AS Tabla, COUNT(*) AS Filas FROM dbo.Actas UNION ALL")
    w("SELECT 'ActaDetalles', COUNT(*) FROM dbo.ActaDetalles UNION ALL")
    w("SELECT 'ProveedorMateriales', COUNT(*) FROM dbo.ProveedorMateriales;")
    w("")
    w("SELECT YEAR(a.FechaFinal) AS Anio, MONTH(a.FechaFinal) AS Mes,")
    w("       COUNT(DISTINCT a.NumeroActa) AS Actas, CAST(SUM(d.Subtotal) AS decimal(18,0)) AS ValorEjecutado")
    w("FROM dbo.Actas a JOIN dbo.ActaDetalles d ON d.NumeroActa = a.NumeroActa")
    w("GROUP BY YEAR(a.FechaFinal), MONTH(a.FechaFinal)")
    w("ORDER BY Anio, Mes;")
    with open(salida, "w", encoding="utf-8-sig") as f:
        f.write("\n".join(L) + "\n")


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--hoy", default=date.today().isoformat())
    ap.add_argument("--salida", default="datos_demo.sql")
    args = ap.parse_args()
    hoy = date.fromisoformat(args.hoy)
    lista, actas, detalles = generar(hoy)
    emitir_sql(hoy, lista, actas, detalles, args.salida)
    r = validar(hoy, lista, actas, detalles)
    print(f"OK -> {args.salida}: {len(actas)} actas, {len(detalles)} líneas, {len(lista)} precios de proveedor")
    for k, v in r.items():
        print(f"  {k}: {v:,.0f}" if isinstance(v, float) else f"  {k}: {v}")
