"""Motor de cálculo propuesto (referencia para el backend). Valida contra los totales del Excel."""
import json, sys
d = json.load(open(sys.argv[1], encoding='utf-8'))
apus = {a["id"]: a for a in d["apus"]}
cache = {}

def costo_apu(aid, pila=()):
    if aid in pila: raise ValueError(f"ciclo en APU {aid}")
    if aid in cache: return cache[aid]
    a = apus[aid]; grupos = {}
    for g in a["grupos"]:
        suma_mo = 0.0; total = 0.0
        for l in g["lineas"]:
            precio = costo_apu(l["subApuId"], pila + (aid,))["total"] if "subApuId" in l else l["precio"]
            if l.get("tipo") == "HerramientaMenor":
                v = l["porcentajeMO"] * suma_mo                     # % sobre la MO del grupo
            elif l["modo"] == "divisor":
                v = l["cantidad"] * precio / (l["factor"] or 1)
            elif l["modo"] == "rendimiento":
                v = l["cantidad"] * precio * l["factor"]
            else:
                v = l["cantidad"] * precio * (1 + l["factor"])
            if l.get("tipo") == "ManoObra" or g["tipo"] == "ManoObra" and l.get("tipo") != "HerramientaMenor":
                suma_mo += v
            total += v
        sub = total / (g["divisor"] or 1)
        grupos[g["tipo"]] = grupos.get(g["tipo"], 0) + sub
    div = a.get("divisor") or 1
    grupos = {k: v / div for k, v in grupos.items()}
    r = {"total": sum(grupos.values()), "grupos": grupos}
    cache[aid] = r
    return r

ok = mal = 0; peores = []
for a in d["apus"]:
    c = costo_apu(a["id"])["total"]; e = a["totalExcel"] or 0
    if abs(c - e) <= max(1, abs(e) * 0.001): ok += 1
    else: mal += 1; peores.append((abs(c - e) / (abs(e) or 1), a["descripcion"], e, c))
print(f"APU reproducidos: {ok}/{ok+mal}")
for p in sorted(peores, reverse=True)[:10]: print(f"  {p[1][:45]:45} Excel {p[2]:>14,.0f}  motor {p[3]:>14,.0f}")

# presupuesto con valores del propio presupuesto (lo que hoy muestra el Excel)
P = d["presupuesto"]; sub = sum(i["cantidad"] * (i["valorMaterial"] + i["valorManoObra"]) for c in P["capitulos"] for i in c["items"])
imp = sub * P["porcentajeImprevistos"]; inc = sub * P["porcentajeIncremento"]; tot = sub + imp + inc
t = P["totalesExcel"]
print(f"Subtotal {sub:,.0f} (Excel {t['subtotal']:,.0f}) | Total CD {tot:,.0f} (Excel {t['totalCostoDirecto']:,.0f}) | m2 {tot/P['areaM2']:,.0f}")

# Presupuesto recalculado desde los APU (lo que haría el software con todo enlazado)
sub2 = 0; difs = []
for c in P["capitulos"]:
    for i in c["items"]:
        if i["apuId"]:
            g = costo_apu(i["apuId"])["grupos"]
            mo = g.get("ManoObra", 0); mat = sum(v for k, v in g.items() if k != "ManoObra")
        else:
            mat, mo = i["valorMaterial"], i["valorManoObra"]
        v_new = i["cantidad"] * (mat + mo); v_old = i["cantidad"] * (i["valorMaterial"] + i["valorManoObra"])
        sub2 += v_new
        if abs(v_new - v_old) > 1: difs.append((v_new - v_old, i["codigo"], i["descripcion"]))
tot2 = sub2 * (1 + P["porcentajeImprevistos"] + P["porcentajeIncremento"])
print(f"Recalculado desde APU: subtotal {sub2:,.0f} | total CD {tot2:,.0f} | diferencia {tot2 - tot:,.0f} ({(tot2/tot-1)*100:.1f}%)")
print(f"Ítems con diferencia: {len(difs)}")
for d_, cod, des in sorted(difs, key=lambda x: -abs(x[0]))[:8]: print(f"  {cod:6} {des[:45]:45} {d_:>14,.0f}")
