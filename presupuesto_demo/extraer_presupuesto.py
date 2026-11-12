"""Extrae el libro de presupuesto (formato Casa Murillo) a JSON normalizado.
Uso: python extraer_presupuesto.py libro.xlsx salida.json"""
import json, re, sys
import openpyxl

def num(v):
    return float(v) if isinstance(v, (int, float)) else None

def main(ruta, salida):
    wf = openpyxl.load_workbook(ruta)
    wv = openpyxl.load_workbook(ruta, data_only=True)
    out = {"proyecto": wv['Presupuesto']['B1'].value, "fecha": str(wv['Presupuesto']['B4'].value),
           "mesesProyecto": wv['Presupuesto']['K4'].value}

    # ---------- Insumos (precios) ----------
    insumos = []
    def hoja_precios(nombre, tipo, fila_hdr, iva_default):
        f, v = wf[nombre], wv[nombre]
        for r in range(fila_hdr + 1, f.max_row + 1):
            cod, desc, und, base = f.cell(r, 2).value, v.cell(r, 3).value, v.cell(r, 4).value, num(v.cell(r, 5).value)
            if not desc or base is None:
                continue
            fx = f.cell(r, 6).value
            iva = iva_default
            if isinstance(fx, str):
                m = re.search(r"\*\s*1\.(\d+)", fx)
                if m: iva = float("0." + m.group(1))
                if "$F$6" in fx: iva = num(v['F6'].value) or iva_default
            insumos.append({"codigo": str(cod) if isinstance(cod, int) else None, "tipo": tipo,
                            "descripcion": str(desc).strip(), "unidadCompra": und, "precioBase": base,
                            "iva": iva, "cantidadPorUnidad": num(v.cell(r, 7).value) or 1,
                            "unidadUso": v.cell(r, 8).value, "precioUnitario": num(v.cell(r, 9).value)})
    hoja_precios('PRECIOS MATERIALES', 'Material', 5, 0.19)
    hoja_precios('PRECIO HERRAMIENTA', 'Herramienta', 5, 0.16)
    hoja_precios('ALQULER EQUIPO', 'Equipo', 5, 0.19)
    hoja_precios('LABORATORIO', 'Laboratorio', 4, 0.16)
    pm, pmv = wf['PRECIOS M.O.'], wv['PRECIOS M.O.']
    for r in range(5, pm.max_row + 1):
        d, u, val = pmv.cell(r, 2).value, pmv.cell(r, 3).value, num(pmv.cell(r, 4).value)
        if d and u and val is not None:
            insumos.append({"codigo": None, "tipo": "ManoObraDestajo", "descripcion": str(d).strip(),
                            "unidadCompra": u, "precioBase": val, "iva": 0, "cantidadPorUnidad": 1,
                            "unidadUso": u, "precioUnitario": val})
    out["insumos"] = insumos

    # ---------- Mano de obra: prestaciones, cargos, cuadrillas ----------
    p = wv['P.M.O.']
    prest = [{"concepto": p.cell(r, 2).value, "porcentaje": num(p.cell(r, 4).value)} for r in range(18, 32)
             if p.cell(r, 2).value and num(p.cell(r, 4).value) is not None]
    cargos = [{"cargo": p.cell(r, 2).value, "salarioMensual": num(p.cell(r, 4).value),
               "jornalConPrestaciones": num(p.cell(r, 7).value)} for r in range(49, 55)]
    cuadrillas = []
    for r in range(62, 74):
        cuadrillas.append({"nombre": p.cell(r, 2).value, "maestros": num(p.cell(r, 4).value) or 0,
                           "oficiales": num(p.cell(r, 5).value) or 0, "ayudantes": num(p.cell(r, 6).value) or 0,
                           "costoDia": num(p.cell(r, 7).value)})
    out["manoObra"] = {"prestaciones": prest, "factorPrestacional": sum(x["porcentaje"] for x in prest),
                       "cargos": cargos, "cuadrillas": cuadrillas}

    # ---------- APU ----------
    af, av = wf['A.P.U.'], wv['A.P.U.']
    titulos = [r for r in range(1, af.max_row - 1)
               if af.cell(r, 3).value and str(af.cell(r + 2, 3).value).strip().upper() == 'DESCRIPCION']
    titulos.append(af.max_row + 1)
    fila_a_apu = {}
    apus = []
    for i, r0 in enumerate(titulos[:-1]):
        fin = titulos[i + 1]
        b = af.cell(r0, 2).value
        ref = None
        if isinstance(b, str) and b.startswith('=Presupuesto!'):
            ref = int(re.sub(r'[^0-9]', '', b.split('!')[1]))
        apu = {"id": len(apus) + 1, "fila": r0, "descripcion": str(af.cell(r0, 3).value).strip(),
               "unidad": av.cell(r0, 4).value, "filaPresupuesto": ref, "esSubanalisis": ref is None,
               "totalExcel": num(av.cell(r0, 8).value),
               "divisor": (num(av.cell(r0, 7).value) or 1) if "/G" in str(af.cell(r0, 8).value) else 1,
               "grupos": []}
        grupo = None
        for r in range(r0 + 3, fin):
            c = af.cell(r, 3).value
            if not c:
                continue
            cu = str(c).strip().upper()
            if cu.startswith('VALOR '):
                grupo = {"tipo": {"VALOR MATERIAL": "Material", "VALOR MANO DE OBRA": "ManoObra",
                                  "VALOR EQUIPO": "Equipo"}.get(cu, "Otros"),
                         "divisor": num(av.cell(r, 7).value) or 1, "subtotalExcel": num(av.cell(r, 8).value), "lineas": []}
                apu["grupos"].append(grupo)
                continue
            fh = af.cell(r, 8).value
            if not (isinstance(fh, str) and fh.startswith('=')):
                continue
            if grupo is None:  # sub-análisis sin grupos (mezclas): grupo único
                grupo = {"tipo": "Mezcla", "divisor": 1, "subtotalExcel": None, "lineas": []}
                apu["grupos"].append(grupo)
            ff = af.cell(r, 6).value
            g_raw = af.cell(r, 7).value
            fh_n = fh.replace('$', '')
            if re.fullmatch(r"=E\d+\*F\d+\*G\d+", fh_n) or re.fullmatch(r"=E\d+\*G\d+\*\(?F\d+\)?", fh_n):
                modo = "rendimiento"          # cant * precio * factor
            elif re.fullmatch(r"=E\d+\*F\d+/G\d+", fh_n):
                modo = "divisor"              # cant * precio / factor (p.ej. un rollo rinde 60 usos)
            else:
                modo = "desperdicio"          # cant * precio * (1+factor)
            usa_cantidad = "E" in fh_n
            linea = {"codigoInsumo": str(af.cell(r, 2).value) if isinstance(af.cell(r, 2).value, int) else None,
                     "descripcion": str(c).strip(), "unidad": av.cell(r, 4).value,
                     "cantidad": (num(av.cell(r, 5).value) or 0) if usa_cantidad else 1, "precio": num(av.cell(r, 6).value) or 0,
                     "factor": num(av.cell(r, 7).value) or 0, "modo": modo, "totalExcel": num(av.cell(r, 8).value)}
            if isinstance(ff, str) and ff.startswith('='):
                m = re.fullmatch(r"=\$?H\$?(\d+)", ff.strip())
                if m:
                    linea["refFila"] = int(m.group(1))   # sub-APU o línea de MO (herramienta menor)
            if 'HERRAMIENTA MENOR' in cu:
                linea["tipo"] = "HerramientaMenor"
                linea["porcentajeMO"] = linea["factor"]
            elif cu.startswith('MANO DE OBRA') or cu.startswith('M.O') or 'CUADRILLA' in cu:
                linea["tipo"] = "ManoObra"
            grupo["lineas"].append(linea)
        fila_a_apu[r0] = apu
        apus.append(apu)
    # resolver referencias a sub-APU
    for apu in apus:
        for g in apu["grupos"]:
            for l in g["lineas"]:
                rf = l.pop("refFila", None)
                if rf and rf in fila_a_apu:
                    l["subApuId"] = fila_a_apu[rf]["id"]
    out["apus"] = apus

    # ---------- Presupuesto ----------
    pf, pv = wf['Presupuesto'], wv['Presupuesto']
    apu_por_fila = {a["filaPresupuesto"]: a["id"] for a in apus if a["filaPresupuesto"]}
    capitulos, cap = [], None
    for r in range(9, 189):
        a = pf.cell(r, 1).value
        if isinstance(a, int):
            cap = {"numero": a, "nombre": pv.cell(r, 2).value, "items": []}
            capitulos.append(cap)
            continue
        g = pf.cell(r, 7).value
        if cap and isinstance(g, str) and g.startswith('=E'):
            d = pf.cell(r, 4).value
            cap["items"].append({"codigo": str(a), "descripcion": str(pv.cell(r, 2).value).strip(),
                                 "unidad": pv.cell(r, 3).value, "cantidad": num(pv.cell(r, 4).value) or 0,
                                 "memoriaCantidad": d if isinstance(d, str) else None,
                                 "valorMaterial": num(pv.cell(r, 5).value) or 0,
                                 "valorManoObra": num(pv.cell(r, 6).value) or 0,
                                 "apuId": apu_por_fila.get(r), "fila": r})
    out["presupuesto"] = {
        "capitulos": capitulos,
        "porcentajeImprevistos": num(pv['D191'].value), "porcentajeIncremento": num(pv['D195'].value),
        "areaM2": num(pv['E199'].value),
        "totalesExcel": {"subtotal": num(pv['K189'].value), "imprevistos": num(pv['K191'].value),
                         "subtotalCostoDirecto": num(pv['K193'].value), "incremento": num(pv['K195'].value),
                         "totalCostoDirecto": num(pv['K197'].value), "valorM2": num(pv['K199'].value),
                         "totalMaterial": num(pv['H189'].value), "totalManoObra": num(pv['I189'].value)}}
    json.dump(out, open(salida, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)

if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2])
