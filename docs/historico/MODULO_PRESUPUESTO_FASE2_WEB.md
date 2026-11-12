*"Documento histórico; el estado vigente está en docs/ARQUITECTURA.md"*

# Presupuesto – Fase 2: creación del presupuesto desde la web

> Documento para **Cursor**. **Se implementa DESPUÉS de terminar `MODULO_PRESUPUESTO.md`**, con sus fases 1–9 completas y `dotnet test` en verde.
> Lee primero `MODULO_PRESUPUESTO.md` (modelo, reglas de cálculo y convenciones). Este documento **solo agrega** funcionalidad; no reescribe lo existente.
> **Regla general:** antes de crear algo, revisa si la fase 1 ya lo dejó hecho (por ejemplo, el CRUD de capítulos e ítems o el editor tipo hoja). Si existe, **extiéndelo**; no lo dupliques.

---

## 1. Objetivo

Hoy el presupuesto se puede **importar desde Excel** y editar. Esta fase hace que el camino principal sea **crearlo desde la web**:

1. El usuario pulsa **"Nuevo presupuesto"**, **elige la construcción** y define los datos generales.
2. Elige el **punto de partida**: en blanco, **desde una plantilla** o **copiando el presupuesto de otra obra**. Importar Excel queda como una opción más.
3. Arma el presupuesto en el editor:
   - agrega capítulos;
   - agrega ítems desde la biblioteca de APU, manuales o creando un APU nuevo;
   - carga cantidades digitadas, con mediciones o tomadas de otro ítem;
   - cuenta con validaciones en vivo y guardado automático.
4. Cualquier presupuesto se puede **guardar como plantilla** para reutilizarlo en otras obras.

---

## 2. Cambios en el modelo

Migración nueva **`PresupuestoCreacionWeb`**, generada con `dotnet-ef migrations add` (no a mano):

```csharp
public class Presupuesto
{
    // ... campos existentes de la fase 1 ...
    public int? ConstruccionId { get; set; }            // CAMBIA a nullable: null solo si EsPlantilla
    public bool EsPlantilla { get; set; }               // nuevo
    public int? PresupuestoOrigenId { get; set; }       // nuevo: de qué plantilla o presupuesto se copió
    [Timestamp] public byte[] RowVersion { get; set; } = []; // nuevo: concurrencia optimista
}
```

- `CHECK (EsPlantilla = 1 OR ConstruccionId IS NOT NULL)`.
- FK `PresupuestoOrigenId → Presupuestos(Id)` con `ON DELETE SET NULL`.
- El índice único "un solo Aprobado por construcción" debe filtrar además `EsPlantilla = 0`.
- Una plantilla **nunca se aprueba**: su estado queda en Borrador y no se puede ligar a actas ni al seguimiento.
- Revisar todas las consultas de la fase 1 que asumen `ConstruccionId` no nulo: listado, seguimiento, dashboard, permisos del Arquitecto y enlace con actas. **Deben excluir las plantillas.**

Configuración nueva en `appsettings.json`:
```json
"Presupuesto": {
  "PctImprevistos": 0.05,
  "PctIncremento": 0.07,
  "PctAdministracion": 0,
  "PctUtilidad": 0,
  "PctIvaUtilidad": 0.19,
  "AutoguardadoMs": 600
}
```
Clase `OpcionesPresupuesto` con `ValidateOnStart`, igual que `OpcionesDashboard`.

---

## 3. API (nueva o ampliada)

| Método | Ruta | Comportamiento |
|---|---|---|
| POST | `/api/presupuestos` | **Crear.** Cuerpo: `{ construccionId, nombre, fecha, mesesProyecto, areaConstruidaM2, pctImprevistos?, pctIncremento?, pctAdministracion?, pctUtilidad?, pctIvaUtilidad?, origen: "vacio" \| "plantilla" \| "copia", origenPresupuestoId?, conservarCantidades }` → `201 { id }`. Los porcentajes omitidos se toman de `OpcionesPresupuesto`. Si la construcción ya tiene un presupuesto Aprobado → nace como `Version = máx + 1` en Borrador |
| GET | `/api/presupuestos?construccionId=&estado=&plantillas=false` | El listado existente con el filtro `plantillas` |
| GET | `/api/presupuestos/plantillas` | `[{ id, nombre, capitulos, items, actualizado }]` |
| POST | `/api/presupuestos/{id}/guardar-como-plantilla` | `{ nombre }` → copia profunda **sin construcción**, cantidades en 0, sin mediciones, `EsPlantilla = true` |
| POST | `/api/presupuestos/{id}/capitulos/{capId}/items/desde-apu` | `{ apuIds: [] }` → crea un ítem por APU: descripción, unidad y `ConceptoId` del APU, cantidad 0, código siguiente |
| POST | `/api/presupuestos/{id}/renumerar` | Códigos 1, 1.01, 1.02… según el orden actual |
| PUT | `/api/presupuestos/{id}/orden` | `{ capitulos: [{ id, orden, items: [ids] }] }` |
| POST | `/api/presupuestos/{id}/items/{itemId}/duplicar` | Copia el ítem justo debajo, con el código siguiente |
| POST | `/api/presupuestos/{id}/items/{itemId}/mover` | `{ capituloDestinoId }` |
| GET | `/api/presupuestos/{id}/validacion` | `[{ itemId, codigo, tipo, mensaje }]`, con estos tipos: `UnidadDistintaApu`, `CantidadCero`, `SinApuNiValores`, `CodigoRepetido` |
| GET | `/api/apu/buscar?texto=&unidadId=&top=20` | Búsqueda rápida sin distinguir tildes ni mayúsculas: `[{ id, codigo, descripcion, unidad, costoTotal }]` |

Reglas:
- Todos los PUT y DELETE del presupuesto, sus capítulos e ítems envían y validan **`RowVersion`**. Si no coincide, responden `409 ProblemDetails` con *"El presupuesto fue modificado por otro usuario"*.
- **Copia profunda** (plantilla o copia): capítulos, ítems, `ApuId`, `FactorConversionApu`, `ConceptoId`, `CantidadOrigenItemId` (remapeado a los Ids nuevos) y valores unitarios. Con `conservarCantidades = false`: cantidad 0 y sin mediciones. Con `conservarCantidades = true`: copia las cantidades y las mediciones.
- **Aprobar** (endpoint de la fase 1) ahora **rechaza con 400** si `validacion` devuelve errores distintos de `CantidadCero`. Las cantidades en 0 se permiten si llega `?confirmarCantidadesCero=true`.
- Permisos: solo el **Administrador** crea, edita y gestiona plantillas. El Arquitecto sigue en solo lectura.

---

## 4. Frontend

### 4.1 Listado `/presupuestos` (ya existe, se amplía)
- Botón **"Nuevo presupuesto"**, que abre el asistente.
- Pestañas **Presupuestos | Plantillas**.
- En la ficha o listado de **Construcciones** (si existe una vista de detalle), un botón "Crear presupuesto" que abre el asistente con la construcción ya elegida.

### 4.2 Asistente "Nuevo presupuesto" (modal en 2 pasos)

**Paso 1: Datos generales**
| Campo | Regla |
|---|---|
| Construcción* | Selector con búsqueda; solo construcciones activas. Si ya tiene un presupuesto Aprobado, aviso: *"Se creará la versión N+1 en borrador; la aprobada sigue vigente hasta que apruebe la nueva"* |
| Nombre* | Por defecto: `Presupuesto <construcción>` |
| Fecha*, Meses de proyecto, Área construida m²* | El área es obligatoria (valor por m²) |
| % Imprevistos, % Incremento, % Administración, % Utilidad, % IVA sobre utilidad | Prellenados desde `GET /api/presupuestos/configuracion` (exponer `OpcionesPresupuesto`) |

**Paso 2: Punto de partida** (tarjetas para elegir)
| Opción | Al confirmar |
|---|---|
| **En blanco** | `POST origen = "vacio"` → abre el editor |
| **Desde plantilla** | Selector de plantilla que muestra capítulos e ítems → `origen = "plantilla"` |
| **Copiar de otra obra** | Selector de presupuesto (construcción · versión · total) + casilla *Conservar cantidades* (por defecto, desmarcada) → `origen = "copia"` |
| **Importar Excel** | Navega al importador de la fase 1 con `construccionId` ya elegido |

Validación con Zod y React Hook Form, como en el resto de la app. Al crear, navega a `/presupuestos/:id`.

### 4.3 Editor `/presupuestos/:id` (ya existe, se amplía)
- **+ Capítulo**: nombre; el número es automático. Los capítulos se reordenan arrastrando su encabezado.
- **+ Ítem** (menú del capítulo):
  1. **Desde biblioteca de APU**: modal con buscador (`/api/apu/buscar`, búsqueda tras 300 ms sin teclear) y casillas para selección múltiple, mostrando unidad y costo → "Agregar" (`desde-apu`).
  2. **Ítem manual**: fila nueva sin APU; se digitan descripción, unidad, valor de material y valor de MO.
  3. **Crear APU nuevo**: abre el **editor de APU de la fase 1** en un panel lateral. Al guardar, crea el ítem enlazado a ese APU.
- **Cantidad** (columna con menú de 3 opciones): *Digitar* · *Mediciones…* (el modal de la fase 1) · *Igual a otro ítem × factor* (selector de ítem + factor; muestra la fórmula, ej. `= 7.01 + 7.02 × 18,3`).

  Si se necesita sumar varios ítems, ampliar `CantidadOrigen` a una lista de pares `(ItemId, Factor)` en la tabla nueva `PresupuestoItemCantidadOrigen`. **Esto es opcional**: hacerlo solo si el modelo de la fase 1 no lo permite.
- **Acciones por fila** (menú "⋯"): Duplicar · Mover a capítulo… · Ver desglose del APU · Eliminar (con confirmación).
- Botón **Renumerar** en la cabecera.
- **Validaciones en vivo**: ícono en la fila con el mensaje en un tooltip, y contador "⚠ 3 advertencias" en la cabecera que filtra las filas al hacer clic. Se refresca con `GET validacion` después de cada guardado.
- **Guardado automático** por fila: PUT 600 ms después de dejar de escribir, con indicador *Guardando… / Guardado ✓ / Error*. Si llega un `409`, aparece el banner *"Otro usuario modificó este presupuesto"* con el botón **Recargar**, sin perder lo digitado: se muestra a un lado para copiarlo.
- **Resumen fijo** inferior, que se actualiza en vivo: subtotal, imprevistos, incremento, AIU, total, valor por m² y barra de % de incidencia por capítulo.
- Menú "⋯" del presupuesto: **Guardar como plantilla** · Exportar Excel · PDF.
- **Aprobar**: si hay errores, muestra el listado y bloquea. Si solo hay cantidades en 0, pide confirmación.

### 4.4 Plantillas
- Pestaña **Plantillas** con la lista, y acciones Editar (el mismo editor, sin cantidades, mediciones ni Aprobar), Duplicar y Eliminar.
- **Plantilla semilla "Vivienda unifamiliar"**: crearla con un comando de desarrollo `dotnet run -- --seed-plantilla`, que **solo funciona en Development**, igual que `--seed-demo`. Toma el presupuesto importado de Casa Murillo (`presupuesto_demo\presupuesto_casa_murillo.json`) y lo guarda como plantilla, con 20 capítulos, 139 ítems y cantidades en 0. Es idempotente: si ya existe, no hace nada.

---

## 5. Tests (`CreacionPresupuestoTests`, xUnit + SQLite, mismo patrón)

- Crear **en blanco**.
- Crear **desde plantilla**: copia profunda, cantidades en 0, `ApuId` y `ConceptoId` conservados, `PresupuestoOrigenId` asignado.
- **Copiar otra obra**:
  - sin cantidades: 0 y sin mediciones;
  - con cantidades: copia cantidades y mediciones, y **remapea `CantidadOrigenItemId`** a los Ids nuevos.
- Construcción con presupuesto Aprobado → el nuevo nace como versión N+1 en Borrador.
- Plantilla sin construcción permitida; presupuesto normal sin construcción → 400.
- Una plantilla no se puede aprobar ni aparece en el seguimiento, el dashboard ni el selector de actas.
- `desde-apu`: N ítems con códigos consecutivos.
- `renumerar`, `orden`, `duplicar`, `mover`.
- Código repetido → 400.
- `RowVersion` desactualizado → 409.
- `validacion`: devuelve los 4 tipos.
- Aprobar con errores → 400; solo con cantidades en 0 → requiere confirmación.
- `--seed-plantilla`: idempotente.
- Actualizar `ModeloTests` si cambian las tablas o el menú.

---

## 6. Plan para Cursor (un prompt por fase; `dotnet test` en verde antes de pasar a la siguiente)

1. **Modelo y migración** `PresupuestoCreacionWeb` (§2). Ajustar las consultas existentes para que excluyan plantillas. Agregar `OpcionesPresupuesto`.
2. **API** de §3 con sus tests (§5).
3. **Asistente** "Nuevo presupuesto" (§4.2) y ajustes del listado (§4.1).
4. **Editor**: alta desde APU, ítem manual, crear APU nuevo en panel lateral, cantidades por referencia, duplicar, mover, renumerar y orden (§4.3).
5. **Guardado automático, concurrencia (409) y validaciones en vivo**; bloqueo al aprobar.
6. **Plantillas**: pestaña, guardar como plantilla y `--seed-plantilla`.
7. **Verificación**: `dotnet test`, `npm run build` y `npm run lint`. Recorrido manual:
   1. Nuevo presupuesto → construcción "Clínica veterinaria Calarcá" (de `datos_demo.sql`) → **desde plantilla "Vivienda unifamiliar"**.
   2. Cargar cantidades: al menos una con mediciones y otra "igual a ítem × factor".
   3. Agregar un ítem desde APU, uno manual y uno creando un APU nuevo.
   4. Abrir el mismo presupuesto en otra pestaña, editar en ambas y verificar el aviso de conflicto (409).
   5. Intentar aprobar con una unidad distinta a la del APU y verificar que lo bloquea; corregirla y aprobar.
   6. Registrar un acta con ítems de este presupuesto y verificar el seguimiento.
   7. **Copiar de otra obra**: nuevo presupuesto copiando el anterior con cantidades, y verificar que los totales coinciden.
