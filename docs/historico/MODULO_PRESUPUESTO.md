*"Documento histórico; el estado vigente está en docs/ARQUITECTURA.md"*

# Módulo de Presupuesto de Obra – App "Arquitectos"

> Documento para **Cursor**. Extiende la app que ya existe (`C:\Arquitectos\backend` en .NET 8 y `C:\Arquitectos\frontend` en React).
> Antes de empezar, lee `MIGRACION_ARQUITECTOS.md` (arquitectura y convenciones) y `DASHBOARD_KPIS.md` (dashboard y alertas).
> El modelo a replicar es el libro `C:\Arquitectos\Presupuesto Preliminar Casa Murillo.xlsx`, que ya está analizado y extraído en `C:\Arquitectos\presupuesto_demo\`.
> **Objetivo:** construir dentro del software el presupuesto completo de una obra, igual que lo hace la hoja de cálculo: precios de insumos → mano de obra y cuadrillas → APU → presupuesto por capítulos → costos indirectos → valor por m². Además, conectarlo con las **actas** para medir cuánto del presupuesto se ha ejecutado.

---

## 0. Resumen funcional (qué debe poder hacer el usuario)

1. **Precios base, compartidos entre todas las obras.** Insumos (materiales, equipos, herramienta, laboratorio y MO a destajo) con precio, IVA e **historial de precios**. Salarios por cargo y % de prestaciones, de donde sale el **costo diario de cada cuadrilla**. Opción de tomar el **precio del proveedor más barato** de la lista que ya existe.
2. **APU (análisis de precio unitario).** Para cada actividad: materiales con desperdicio, mano de obra con cuadrilla y rendimiento, herramienta menor y equipo. Las **mezclas** (concretos y morteros) se analizan una vez y se reutilizan dentro de otros APU. El costo se calcula **en vivo** y se actualiza cuando cambia un precio.
3. **Presupuesto de una obra** en una pantalla tipo hoja:
   - Capítulos e ítems; el precio de cada ítem sale **enlazado** a su APU.
   - Cantidades con **memoria de cálculo** (medidas por espacio).
   - Avisos de errores, por ejemplo una unidad del ítem que no coincide con la de su APU.
   - Totales por capítulo con % de incidencia, imprevistos, incremento por inflación, AIU opcional, total y **valor por m²**.
4. **Aprobación y versiones.** En borrador se recalcula. Al aprobar, los valores **quedan congelados** como línea base. Los cambios se hacen en una nueva versión (v2, v3…). Opción "cuánto costaría hoy". Exporta a Excel (formato original) y PDF.
5. **Presupuestado frente a ejecutado.** Cada ítem de acta se liga a un ítem del presupuesto. Por ítem, capítulo y obra se ve:
   - avance físico y financiero, y saldo por ejecutar;
   - precio pagado frente a precio presupuestado;
   - **proyección al cierre** y lo **ejecutado no presupuestado**.
6. **Dashboard.** Nuevos indicadores (% de ejecución del presupuesto y desviación proyectada) y nuevas alertas: precio del acta por encima del presupuesto, sobre-ejecución de cantidades y pagos no presupuestados.
7. **Importar el Excel actual** (formato Casa Murillo). Carga insumos, cuadrillas, APU y presupuesto, y entrega un **informe de inconsistencias** para corregir antes de aprobar.
8. **Permisos.** Administrador: todo. Arquitecto: solo lectura de sus obras. Contratista y Revisor: sin acceso (costos sensibles).

**Resultado esperado:** hoy el Excel solo dice cuánto *debería* costar la obra. Con el módulo, el sistema muestra en todo momento **cuánto se ha gastado frente a lo planeado, dónde se paga de más y cuánto costará la obra al terminar**.

---

## 1. Qué hace la hoja de cálculo (y a qué parte del módulo corresponde)

| Hoja(s) del Excel | Qué contiene | Equivalente en el software |
|---|---|---|
| `PRECIOS MATERIALES` (≈700), `PRECIO HERRAMIENTA`, `ALQULER EQUIPO`, `LABORATORIO` | Código, descripción, unidad de compra, valor + IVA, cantidad por unidad → **valor unitario de uso** | Catálogo **Insumos** (§3.1) |
| `PRECIOS M.O.` (≈190) | Precio por actividad a destajo (ej. "Localización y replanteo $4.000/m²") | Insumos de tipo `ManoObraDestajo` |
| `P.M.O.` | Prestaciones sociales (72 %), salarios por cargo, **12 cuadrillas** (A–L) con su costo diario | **Cargos**, **Prestaciones** y **Cuadrillas** (§3.2) |
| `A.P.U.` (122 análisis) | Por ítem: grupo Material, grupo Mano de obra y grupo Equipo, con cantidad, precio, % de desperdicio o rendimiento, herramienta menor (10 % de la MO) y **sub-análisis** (concretos 2000–3300 PSI, morteros 1:3 y 1:4, concreto impermeabilizado y premezclado) | **Biblioteca de APU** (§3.3) |
| `Presupuesto` | 20 capítulos y 139 ítems con unidad, cantidad, valor unitario de material, valor unitario de MO, totales, total del capítulo y % de incidencia. Abajo: subtotal, imprevistos 5 %, incremento por inflación 7 %, total de costo directo y **valor por m²** | **Presupuesto** (§3.4) |
| `Volumen`, `refuerzos`, `ENCHAPE PISOS/PARED`, `GUARDAESCOBA`, `PUERTAS`, `CLOSETHS`, `HIDROSANITARIAS`, `ELECTRICOS`, `VENTANAS`, `BARANDAS`, `CIELO RASO`, `EQUIPAMENTO Y APARATOS` | **Memorias de cantidades**: espacio × medidas o conteos que alimentan la cantidad de cada ítem | **Mediciones** por ítem (§3.4) |

## 2. Por qué vale la pena llevarlo al software (hallazgos en el Excel)

Los encontró la extracción automática (`presupuesto_demo\motor_apu.py`):

1. **El presupuesto no está enlazado a los APU.** Los valores unitarios de material y MO se copiaron a mano. Si cambia un precio, el presupuesto no se entera.
2. **8 ítems usan un APU calculado en otra unidad.** El más grave es el **7.04 "Entramado cubierta"**: el presupuesto lo cuenta en KG y su APU está calculado por M2. Si se enlaza sin control, el total sube **≈ $1.286 millones**. Los otros casos: 3.02, 4.04, 4.05, 4.13, 8.06, 8.12 y 9.03. → **El software debe exigir que la unidad del ítem coincida con la del APU** o pedir un factor de conversión.
3. **6 APU tienen el total mal formulado**: sumas que omiten la mano de obra o rangos incompletos. Son "Aseo y remates" (×2), "Estuco y pintura exteriores" (×2), "Entramado metálico cubierta" y "Concreto piso exterior y andén", que no tiene fórmula de total. Los otros **116 de 122** se reproducen exactos con las reglas de la §4.
4. **Códigos de ítem duplicados**: 3.04, 11.02, 18.01 y 20.02.
5. **27 ítems no tienen APU** (valor directo). Ejemplos: gas, cocina integral, jardines y gastos generales.
6. **Nombres de cuadrilla cruzados.** En los APU, "Cuadrilla J (1 Of + 2 Ayud)" usa $275.200, que es el costo de la cuadrilla **G**; en `P.M.O.` la J es 1 Of + 3 Ayud ($355.467).
7. **Factor prestacional inconsistente.** El texto de `P.M.O.` dice "J.R. + P.Soc. = J.B. × 1,95", pero la fórmula aplica solo × 1,72; no suma el 23 % de días no hábiles.
8. **IVA del 16 %** en herramienta y laboratorio, mientras que en materiales y equipo es del 19 %. Hay que verificarlo con el negocio.
9. El encabezado dice "CASA CAMPESTRE **MARIA URIBE**", pero el archivo se llama "Casa **Murillo**".

---

## 3. Modelo de datos (EF Core, migraciones nuevas)

> **Las migraciones se generan con `dotnet-ef migrations add <Nombre>`. No se escriben a mano** (las últimas 6 de la app no tienen `.Designer.cs`). Usar `dotnet-ef` global: dentro de `backend`, `dotnet ef` falla con la herramienta local.

### 3.1 Insumos
```csharp
public enum TipoInsumo { Material = 1, Equipo = 2, Herramienta = 3, Laboratorio = 4, Transporte = 5, ManoObraDestajo = 6, Otro = 7 }

public class Insumo : ICatalogo           // reutiliza CatalogoServicioBase (Activo, reactivación)
{
    public int Id { get; set; }
    public string? Codigo { get; set; }   // único si no es null (ej. 10002)
    public required string Descripcion { get; set; }
    public TipoInsumo Tipo { get; set; }
    public string UnidadCompra { get; set; } = "";     // BULTO
    public decimal PrecioBase { get; set; }            // sin IVA
    public decimal PorcentajeIva { get; set; }         // 0.19
    public decimal CantidadPorUnidad { get; set; } = 1; // ej. agua: 1 m3 = 1000 L
    public string UnidadUso { get; set; } = "";        // L
    public decimal PrecioUnitario { get; set; }        // calculado y guardado: PrecioBase*(1+IVA)/CantidadPorUnidad
    public int? MaterialId { get; set; }               // enlace opcional al catálogo Materiales (actas y proveedores)
    public DateTime FechaPrecio { get; set; }
    public bool Activo { get; set; } = true;
}
public class InsumoPrecioHistorial { public int Id; public int InsumoId; public decimal PrecioBase; public decimal PorcentajeIva; public DateTime Fecha; public int? UsuarioId; }
```
- Guardar historial en cada cambio de precio.
- Acción **"Tomar precio de proveedores"**: para insumos con `MaterialId`, ofrecer `MIN(ProveedorMateriales.Precio)` con el nombre del proveedor y aplicarlo solo si el usuario confirma.

### 3.2 Mano de obra
```csharp
public class PrestacionSocial { public int Id; public required string Concepto; public decimal Porcentaje; public int Orden; }   // 13 filas de P.M.O.
public class ParametrosManoObra { public int Id; public int DiasMes = 30; public bool IncluirDiasNoHabiles; public int DiasHabilesAnio = 296; }
public class Cargo { public int Id; public required string Nombre; public decimal SalarioMensual; public bool Activo = true; }
public class Cuadrilla { public int Id; public required string Nombre; public string? Descripcion; public bool Activo = true;
                         public ICollection<CuadrillaCargo> Integrantes { get; set; } = []; }
public class CuadrillaCargo { public int CuadrillaId; public int CargoId; public decimal Cantidad; }
```
- `FactorPrestacional = Σ Prestaciones.Porcentaje`. Si `IncluirDiasNoHabiles`, se suma además `(365 / DiasHabilesAnio − 1)`. **Por defecto va desactivado, para reproducir el Excel (1,72).**
- `JornalCargo = SalarioMensual / DiasMes × (1 + FactorPrestacional)`.
- `CostoDiaCuadrilla = Σ JornalCargo × Cantidad`. Se calcula al vuelo, no se guarda.

### 3.3 Biblioteca de APU (compartida entre presupuestos)
```csharp
public enum GrupoApu { Material = 1, ManoObra = 2, Equipo = 3, Transporte = 4, Otros = 5 }
public enum ModoLinea { Desperdicio = 1, Rendimiento = 2, Divisor = 3, PorcentajeManoObra = 4 }

public class Apu
{
    public int Id { get; set; }
    public string? Codigo { get; set; }
    public required string Descripcion { get; set; }
    public int UnidadMedidaId { get; set; }            // catálogo existente UnidadesMedida
    public bool EsSubanalisis { get; set; }            // concretos, morteros: se usan dentro de otros APU
    public decimal DivisorGeneral { get; set; } = 1;   // ej. "Retiro de escombros": viaje de 6 m3 → /6
    public int? ConceptoId { get; set; }               // enlace a Conceptos (actas)
    public bool Activo { get; set; } = true;
    public ICollection<ApuGrupo> Grupos { get; set; } = [];
}
public class ApuGrupo { public int Id; public int ApuId; public GrupoApu Grupo; public decimal Divisor = 1; /* "rend, uso" del grupo, ej. campamento 75 m2, MO 0,7 */ public ICollection<ApuLinea> Lineas = []; }
public class ApuLinea
{
    public int Id { get; set; }
    public int ApuGrupoId { get; set; }
    public int Orden { get; set; }
    public int? InsumoId { get; set; }        // exactamente uno de: InsumoId, CuadrillaId, SubApuId,
    public int? CuadrillaId { get; set; }     // o ninguno si es precio manual / herramienta menor
    public int? SubApuId { get; set; }
    public string Descripcion { get; set; } = "";
    public string Unidad { get; set; } = "";
    public decimal Cantidad { get; set; } = 1;
    public decimal? PrecioManual { get; set; } // solo si no hay referencia
    public ModoLinea Modo { get; set; }
    public decimal Factor { get; set; }        // desperdicio (0,05), factor de rendimiento (1/45), divisor (60) o % de MO (0,10)
}
```
Validaciones:
- **No se permiten ciclos** entre sub-análisis (A usa B, B usa A); se detectan con DFS al guardar.
- No se puede eliminar un APU que esté en un presupuesto aprobado, ni un sub-análisis que otro APU use.
- Para cuadrillas, la UI pide el **rendimiento** ("m² por día") y guarda `Factor = 1 / rendimiento`.

### 3.4 Presupuesto
```csharp
public enum EstadoPresupuesto { Borrador = 1, Aprobado = 2, Cerrado = 3 }
public class Presupuesto
{
    public int Id { get; set; }
    public int ConstruccionId { get; set; }             // catálogo existente
    public required string Nombre { get; set; }
    public int Version { get; set; } = 1;
    public EstadoPresupuesto Estado { get; set; }
    public DateOnly Fecha { get; set; }
    public int MesesProyecto { get; set; }              // usado en "Alquiler de equipos = meses − 5"
    public decimal AreaConstruidaM2 { get; set; }       // para valor por m2 (Excel: 557,65)
    public decimal PctImprevistos { get; set; } = 0.05m;
    public decimal PctIncremento { get; set; } = 0.07m; // incremento esperado de precios durante la obra
    public decimal PctAdministracion { get; set; }      // AIU opcional (0 por defecto, el Excel no lo usa)
    public decimal PctUtilidad { get; set; }
    public decimal PctIvaUtilidad { get; set; }
    public DateTime? FechaAprobacion { get; set; }
    public int? UsuarioAprobacionId { get; set; }
    public ICollection<PresupuestoCapitulo> Capitulos { get; set; } = [];
}
public class PresupuestoCapitulo { public int Id; public int PresupuestoId; public int Numero; public required string Nombre; public ICollection<PresupuestoItem> Items = []; }
public class PresupuestoItem
{
    public int Id { get; set; }
    public int CapituloId { get; set; }
    public required string Codigo { get; set; }         // "1.01", único dentro del presupuesto
    public required string Descripcion { get; set; }
    public int UnidadMedidaId { get; set; }
    public int? ApuId { get; set; }
    public decimal FactorConversionApu { get; set; } = 1; // si la unidad del ítem ≠ la del APU (ej. 18,3 kg por m2)
    public int? ConceptoId { get; set; }                // enlace con actas (obligatorio para el seguimiento)
    public decimal Cantidad { get; set; }               // = Σ mediciones si las hay; si no, se digita
    public int? CantidadOrigenItemId { get; set; }      // "igual que otro ítem" (Excel: D12 = D11)
    public decimal CantidadOrigenFactor { get; set; } = 1;
    public decimal ValorMaterialUnitario { get; set; }  // calculado desde el APU o digitado si no hay APU
    public decimal ValorManoObraUnitario { get; set; }
    public string? DesgloseApuJson { get; set; }        // copia del APU calculado al aprobar (auditoría)
    public int Orden { get; set; }
    public ICollection<PresupuestoMedicion> Mediciones { get; set; } = [];
}
public class PresupuestoMedicion   // reemplaza las hojas de memorias de cantidades
{
    public int Id { get; set; }
    public int PresupuestoItemId { get; set; }
    public string Ubicacion { get; set; } = "";   // "SALA - COMEDOR", "VF-1 Entrada principal", "Dado Z1"
    public decimal Veces { get; set; } = 1;
    public decimal? Largo { get; set; }
    public decimal? Ancho { get; set; }
    public decimal? Alto { get; set; }
    public decimal Parcial { get; set; }           // Veces × (Largo ?? 1) × (Ancho ?? 1) × (Alto ?? 1)
}
```
- Índice único filtrado: **un solo presupuesto Aprobado por construcción**.
- **Nueva versión** = copia profunda en estado Borrador, con `Version + 1`. Al aprobar la nueva, la anterior pasa a Cerrado.

### 3.5 Enlace con actas
- `ActaDetalle.PresupuestoItemId int NULL` (FK, Restrict).
- Al crear un ítem de acta en una construcción con presupuesto Aprobado, la UI ofrece elegir el **ítem del presupuesto**. Eso precarga el concepto, la unidad y el precio presupuestado, que se puede editar.

---

## 4. Reglas de cálculo (replican el Excel; validadas en 116 de 122 APU)

```
Precio de la línea (P):
  InsumoId      → Insumo.PrecioUnitario
  CuadrillaId   → CostoDiaCuadrilla
  SubApuId      → CostoTotal(SubApu)            (recursivo, con detección de ciclos)
  sin referencia→ PrecioManual

Valor de la línea:
  Desperdicio         = Cantidad × P × (1 + Factor)
  Rendimiento         = Cantidad × P × Factor           (MO: Factor = 1/rendimiento)
  Divisor             = Cantidad × P / Factor
  PorcentajeManoObra  = Factor × Σ(valor de las demás líneas del grupo ManoObra)   (herramienta menor)

Subtotal del grupo = Σ líneas / ApuGrupo.Divisor
Costo del APU por grupo = Subtotal del grupo / Apu.DivisorGeneral
Costo total del APU = Σ grupos

Presupuesto (por ítem):
  Material unitario = (APU.Material + APU.Equipo + APU.Transporte + APU.Otros) × FactorConversionApu
  MO unitaria       = APU.ManoObra × FactorConversionApu          (incluye la herramienta menor)
  Valor unitario    = Material + MO
  Total material = Cantidad × Material;  Total MO = Cantidad × MO;  Total = Cantidad × Valor unitario
Capítulo = Σ Total de sus ítems;  % incidencia = Capítulo / Subtotal

Subtotal costo directo (SCD) = Σ capítulos
Imprevistos   = SCD × PctImprevistos
Incremento    = SCD × PctIncremento                  (sobre SCD, no sobre SCD + imprevistos: así lo hace el Excel)
Administración= SCD × PctAdministracion
Utilidad      = SCD × PctUtilidad
IVA utilidad  = Utilidad × PctIvaUtilidad
TOTAL         = SCD + Imprevistos + Incremento + Administración + Utilidad + IVA utilidad
Valor por m2  = TOTAL / AreaConstruidaM2
```
- Usar `decimal` en todo el cálculo y **no redondear los intermedios**. Mostrar los valores con `es-CO`.
- **Borrador**: los valores se recalculan cada vez que cambia un insumo, cuadrilla, APU o cantidad. El endpoint `recalcular` devuelve una vista previa con las diferencias antes de guardar.
- **Aprobado**: los valores quedan **congelados**: se guardan `ValorMaterialUnitario`, `ValorManoObraUnitario` y `DesgloseApuJson`. Los cambios de precio posteriores no lo alteran. La acción "Comparar con precios actuales" muestra cuánto costaría hoy.
- Cálculo en `Arquitectos.Infrastructure/Presupuestos/CalculadoraApu.cs`, como **función pura** y testeable igual que `ReglasAlertas`. Referencia de comportamiento: `presupuesto_demo\motor_apu.py`.

---

## 5. Seguimiento: presupuesto frente a lo ejecutado en actas

Por ítem, capítulo y total. Solo cuentan los ítems de acta **aprobados**:

| Columna | Fórmula |
|---|---|
| Cantidad presupuestada / ejecutada | `Item.Cantidad` / Σ `ActaDetalle.Cantidad` del ítem |
| % avance físico | ejecutada / presupuestada |
| Valor presupuestado / ejecutado | `Item.Total` / Σ `ActaDetalle.Subtotal` |
| % avance financiero | ejecutado / presupuestado |
| Saldo por ejecutar | presupuestado − ejecutado |
| Precio unitario promedio en actas frente al presupuestado | Σ subtotal / Σ cantidad frente a `Item.ValorUnitario` |
| **Proyección al cierre** | ejecutado + (presupuestado − cantidad ejecutada × precio unitario presupuestado) × (precio en actas / precio presupuestado). Si no hay ejecución, = presupuestado |

Los ítems de acta de la construcción **sin** `PresupuestoItemId` se muestran aparte como "Ejecutado no presupuestado".

**Extensión del dashboard** (`DASHBOARD_KPIS.md`). Respetar el patrón de `ReglasAlertas` y las opciones en `appsettings`:

| Nuevo | Regla |
|---|---|
| K9 % de ejecución del presupuesto | Σ ejecutado / Σ presupuestado, solo de obras con presupuesto aprobado |
| K10 Desviación proyectada | Σ proyección al cierre − Σ presupuestado (en $ y %) |
| A6 Precio de acta sobre el presupuesto | `ActaDetalle.Precio > Item.ValorUnitario × (1 + UmbralDesvioPresupuestoPct)` (por defecto 10 %). Impacto = exceso × cantidad |
| A7 Sobre-ejecución | Cantidad ejecutada > presupuestada × (1 + 5 %). Severidad Alta si supera el 20 % |
| A8 Ejecutado no presupuestado | Ítems de acta sin ítem de presupuesto en una obra con presupuesto aprobado |

---

## 6. API

| Método | Ruta | Notas |
|---|---|---|
| GET/POST/PUT/DELETE | `/api/insumos[/{id}]?tipo=` | Usa el CRUD genérico que ya existe |
| GET | `/api/insumos/{id}/historial` | |
| POST | `/api/insumos/{id}/precio-proveedor` | Aplica el mínimo de ProveedorMateriales |
| GET/PUT | `/api/mano-obra/parametros`, `/api/mano-obra/prestaciones` | |
| CRUD | `/api/cargos`, `/api/cuadrillas` | La respuesta de cuadrillas incluye `costoDia` |
| CRUD | `/api/apu` | El GET `{id}` incluye el desglose calculado por grupo y línea |
| GET | `/api/apu/{id}/usos` | Presupuestos y APU donde se usa |
| POST | `/api/apu/{id}/duplicar` | |
| GET/POST | `/api/presupuestos?construccionId=` | |
| GET/PUT/DELETE | `/api/presupuestos/{id}` | Se puede editar y borrar solo en Borrador |
| CRUD | `/api/presupuestos/{id}/capitulos`, `/items`, `/items/{itemId}/mediciones` | |
| POST | `/api/presupuestos/{id}/recalcular?aplicar=false` | Vista previa de las diferencias |
| POST | `/api/presupuestos/{id}/aprobar` · `/nueva-version` · `/cerrar` | |
| GET | `/api/presupuestos/{id}/seguimiento` | §5 |
| GET | `/api/presupuestos/{id}/excel` | Mismo formato que el original: hojas Presupuesto, APU, Insumos y Cuadrillas |
| GET | `/api/presupuestos/{id}/pdf` | Resumen por capítulos con totales e indirectos |
| POST | `/api/presupuestos/importar` (multipart `.xlsx` + `construccionId`) | §8 |

**Permisos**:
- **Administrador**: todo.
- **Arquitecto**: **solo lectura** del presupuesto y el seguimiento de las construcciones donde tiene actas.
- **Contratista y Revisor**: sin acceso; los APU y precios son información sensible.
- En TomaActas, el selector de ítems de presupuesto devuelve solo código, descripción y unidad. El precio presupuestado se envía únicamente al Administrador.

---

## 7. Frontend (nuevo grupo de menú "Presupuestos")

Agregar las opciones de menú en una migración con `InsertData`, usando Id ≥ 18 (hoy el máximo es 17). **Actualizar `ModeloTests` (hoy espera 16 opciones de menú).**

| Ruta | Pantalla |
|---|---|
| `/presupuestos` | Listado: construcción, nombre, versión, estado, total, valor por m², % de ejecución. Botones Nuevo e Importar Excel |
| `/presupuestos/:id` | **Editor tipo hoja**. Capítulos plegables con su subtotal y % de incidencia. Ítems editables en línea: código, descripción, unidad, cantidad (con botón de mediciones), selector de APU con búsqueda, valor de material, valor de MO, valor unitario y total. Los valores que vienen de un APU van en gris y sin edición; si no hay APU se pueden digitar. Aviso si la unidad no coincide con la del APU. Resumen fijo abajo: subtotal, imprevistos, incremento, AIU, total y valor por m². Botones Recalcular (con vista previa), Aprobar, Nueva versión, Excel y PDF |
| `/presupuestos/:id/items/:itemId/mediciones` (modal) | Tabla Ubicación · Veces · Largo · Ancho · Alto · Parcial, con total en vivo |
| `/presupuestos/:id/seguimiento` | Tabla de §5 con barras de avance. Gráfico de lo ejecutado acumulado por mes frente al total presupuestado. Filtro por capítulo |
| `/presupuesto/apu` y `/presupuesto/apu/:id` | Biblioteca de APU con buscador. El editor muestra los grupos (Material, MO, Equipo…) con sus líneas: selector de insumo, cuadrilla o sub-APU, cantidad, modo, factor y valor calculado **en vivo**. Muestra también dónde se usa el APU |
| `/presupuesto/insumos` | Pestañas por tipo. Edición de precios con historial y botón "Tomar precio de proveedores". Importar y exportar Excel |
| `/presupuesto/mano-obra` | Prestaciones (suma = factor), cargos y cuadrillas con su costo diario calculado |
| TomaActas (ya existe) | Si la construcción tiene presupuesto aprobado, agregar la columna "Ítem presupuesto" con selector; precarga concepto, unidad y precio |

Reutilizar `CatalogoCrud` para insumos y cargos, Recharts para el seguimiento, el formato de moneda de `actas/formato.ts` y la carga diferida de páginas con `React.lazy`.

---

## 8. Importación del Excel y datos de prueba

Archivos ya generados en **`C:\Arquitectos\presupuesto_demo\`**:
- `presupuesto_casa_murillo.json`: extracción completa y normalizada del libro. Contiene 939 insumos (708 materiales, 194 de MO a destajo, 21 equipos, 11 herramientas y 5 de laboratorio), 13 prestaciones, 6 cargos, 12 cuadrillas, **122 APU** (11 sub-análisis y 647 líneas, 32 de ellas con referencia a un sub-APU) y el presupuesto con 20 capítulos y 139 ítems (111 con APU).
- `extraer_presupuesto.py`: reglas de lectura del libro. **Portarlo a C# con ClosedXML** para `POST /api/presupuestos/importar`. Reglas clave:
  - Un APU empieza en la fila cuya columna C, dos filas más abajo, dice `DESCRIPCION`.
  - `VALOR MATERIAL`, `VALOR MANO DE OBRA` y `VALOR EQUIPO` son grupos, y su columna G es el divisor.
  - El modo de cada línea se deduce de la fórmula en H: `E*F*(1+G)` = Desperdicio, `E*F*G` = Rendimiento, `E*F/G` = Divisor.
  - `=$H$nnnn` en la columna F es una referencia a un sub-APU.
  - "Herramienta Menor" es `PorcentajeManoObra`.
  - Cada ítem del presupuesto se enlaza con su APU mediante `=Presupuesto!Ann` en la columna B del APU.
- `motor_apu.py`: implementación de referencia de la §4 y su validación.

**El importador:**
1. Crea o actualiza los insumos por código; si no hay código, los busca por descripción y tipo.
2. Crea los cargos, cuadrillas y APU. Si un APU tiene la unidad vacía, toma la del ítem.
3. Crea el presupuesto en Borrador con **los valores unitarios tal como vienen del Excel**, sin recalcular.
4. Devuelve un **informe de importación**: unidades que no coinciden, códigos duplicados (se les agrega el sufijo `-b`), APU cuyo total en el Excel no coincide con el motor e ítems sin APU.
5. Enlaza o crea un Concepto por cada ítem, comparando por descripción sin distinguir mayúsculas ni tildes.

**Criterios de aceptación** (importando el libro en la construcción de prueba y sin recalcular):

| Valor | Esperado (COP) |
|---|---|
| Subtotal costo directo | 1.221.328.767 |
| Total material / Total MO | 828.735.967 / 391.266.188 |
| Imprevistos 5 % | 61.066.438 |
| Incremento 7 % | 85.493.014 |
| **Total costo directo** | **1.367.888.220** |
| Valor por m² (557,65 m²) | 2.452.951 |
| APU reproducidos por el motor | **116 de 122** (exactamente los 6 que se listan en §2.3 difieren) |
| Informe de importación | 8 unidades que no coinciden, 4 códigos duplicados, 27 ítems sin APU |

Tolerancia: ±1 peso por redondeo. Convertir esta tabla en un test de integración con el `.json` como fixture.

---

## 9. Tests (xUnit, mismo patrón que hoy)

- `CalculadoraApuTests`:
  - Los 4 modos de línea.
  - Divisor de grupo y divisor general.
  - Herramienta menor = % de la MO del grupo.
  - Sub-APU anidado de dos niveles.
  - **Ciclo detectado**.
  - Cuadrilla = Σ jornales × (1 + factor).
- `PresupuestoServicioTests`:
  - Totales e indirectos con AIU en 0 y con AIU.
  - Valor por m².
  - Mediciones → cantidad.
  - Cantidad referenciada a otro ítem.
  - Al aprobar, los valores se congelan.
  - Nueva versión.
  - Un solo presupuesto aprobado por construcción.
- `ImportadorPresupuestoTests`: la tabla de §8 sobre el fixture JSON. Si el `.xlsx` existe en la ruta de pruebas, también sobre el libro.
- `SeguimientoTests`: avances, proyección, ítems no presupuestados y alertas A6–A8.
- Actualizar `ModeloTests` (cantidad de opciones de menú y tablas nuevas).

---

## 10. Plan para Cursor (un prompt por fase; `dotnet test` en verde antes de pasar a la siguiente)

1. **Insumos y mano de obra**: entidades, migración `PresupuestoInsumos`, CRUD y pantallas `/presupuesto/insumos` y `/presupuesto/mano-obra`.
2. **APU**: entidades, `CalculadoraApu` y sus tests, migración `PresupuestoApu`, API y editor de APU.
3. **Presupuesto**: entidades, mediciones, cálculo de totales, estados y versiones, migración `Presupuestos`, API y editor tipo hoja.
4. **Importador del Excel** (portar `extraer_presupuesto.py`), informe de importación y test de aceptación de §8.
5. **Exportar** a Excel (formato original) y PDF.
6. **Enlace con actas**: migración `ActaPresupuestoItem` y selector en TomaActas.
7. **Seguimiento**: pantalla, KPIs K9–K10 y alertas A6–A8 en el dashboard.
8. **Menú y permisos**: opciones Id ≥ 18 y `ModeloTests` actualizado.
9. **Verificación**: `dotnet test`, `npm run build` y `npm run lint`. Recorrido manual: importar Casa Murillo → revisar el informe → corregir el ítem 7.04 (factor 18,3) → recalcular → aprobar → registrar un acta con ítems del presupuesto → ver el seguimiento y la alerta A6.
