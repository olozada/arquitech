*"Documento histórico; el estado vigente está en docs/ARQUITECTURA.md"*

# Correcciones – Módulo de Presupuesto

> Documento para **Cursor**. Corrige lo que quedó incompleto al implementar `MODULO_PRESUPUESTO.md` y `MODULO_PRESUPUESTO_FASE2_WEB.md`.
> Sale de una revisión del código del 11-nov-2026. **No reescribir lo que ya funciona**: el motor de cálculo, el importador, el seguimiento, los permisos y el asistente quedaron bien.
> Orden obligatorio: **C1 primero**, porque sin el snapshot correcto ninguna migración futura funciona. Después C2 → C9. Ejecutar `dotnet test` en verde después de cada punto.

---

## C1. 🔴 Resincronizar el snapshot de EF Core (bloqueante)

**Problema.** Las migraciones `20261111190000_ModuloPresupuesto` y `20261111200000_PresupuestoCreacionWeb` se escribieron a mano con `migrationBuilder.Sql(...)`, no tienen `.Designer.cs`, y **`ArquitectosDbContextModelSnapshot.cs` no contiene ninguna entidad del presupuesto** (está igual desde el 10-nov). El próximo `migrations add` generaría `CreateTable` de todo el módulo y fallaría.

**Pasos** (usar `dotnet-ef` global; dentro de `backend`, `dotnet ef` no funciona con la herramienta local):
```powershell
cd C:\Arquitectos\backend
dotnet-ef migrations add SincronizarSnapshot -p src\Arquitectos.Infrastructure -s src\Arquitectos.Api
```
1. Abrir `Migrations\<timestamp>_SincronizarSnapshot.cs` y **dejar vacíos** `Up()` y `Down()`, con un comentario: `// Solo sincroniza el snapshot: las tablas ya existen (ModuloPresupuesto y PresupuestoCreacionWeb).`
2. **Conservar** `<timestamp>_SincronizarSnapshot.Designer.cs` y el `ArquitectosDbContextModelSnapshot.cs` regenerado.
3. Revisar el snapshot regenerado: debe incluir `Insumos`, `InsumoPrecioHistorial`, `PrestacionesSociales`, `ParametrosManoObra`, `Cargos`, `Cuadrillas`, `CuadrillaCargos`, `Apus`, `ApuGrupos`, `ApuLineas`, `Presupuestos` (con `RowVersion` como rowversion, `CK_Presupuestos_Plantilla` e `IX_Presupuestos_Aprobado` filtrado), `PresupuestoCapitulos`, `PresupuestoItems`, `PresupuestoMediciones`, `ActaDetalles.PresupuestoItemId` y las opciones de menú 18–22 en `HasData`.
4. Aplicar y verificar:
```powershell
dotnet-ef database update -p src\Arquitectos.Infrastructure -s src\Arquitectos.Api
dotnet-ef migrations has-pending-model-changes -p src\Arquitectos.Infrastructure -s src\Arquitectos.Api
```
   El último comando debe responder **"No changes have been made to the model since the last migration"**.

**Regla desde ahora:** toda migración nueva se genera con `dotnet-ef migrations add`. Si se necesita SQL idempotente para producción, se genera con `dotnet-ef migrations script --idempotent`; no se escribe a mano. Los scripts de `backend\scripts\*.sql` quedan solo como histórico.

**Test nuevo** (`ModeloTests`):
```csharp
[Fact]
public void El_snapshot_esta_sincronizado_con_el_modelo()
{
    using var context = CrearContexto();
    var diferencias = context.GetService<IMigrationsModelDiffer>().GetDifferences(
        context.GetService<IMigrationsAssembly>().ModelSnapshot!.Model.GetRelationalModel(),
        context.GetService<IDesignTimeModel>().Model.GetRelationalModel());
    Assert.Empty(diferencias);
}
```
(Si `ModelSnapshot.Model` necesita inicializarse, usar `context.GetService<IModelRuntimeInitializer>().Initialize(snapshot.Model)` antes de comparar.)

---

## C2. 🔴 Editor de APU completo (`/presupuesto/apu/:id`)

**Problema.** `paginas/Apus.tsx` solo **muestra** el análisis: no permite agregar o quitar líneas, elegir insumo, cuadrilla o sub-APU, ni editar cantidad, modo o factor. El botón "Guardar" envía lo mismo que llegó. **La API ya lo soporta** (`PUT /api/apu/{id}` con `ApuSolicitud` → grupos → `LineaApuSolicitud`); el trabajo es de frontend.

**Requisitos:**
- **Encabezado editable**: código, descripción, **unidad (selector del catálogo `UnidadesMedida`; enviar `UnidadMedidaId` y no texto libre)**, es sub-análisis, divisor general, concepto (selector de `Conceptos`, opcional) y activo.
- **Grupos**: botón "+ Grupo" (Material, ManoObra, Equipo, Transporte, Otros; sin repetir tipos), divisor editable, botón para eliminar el grupo con confirmación.
- **Líneas**, por grupo:
  - "+ Línea" abre un selector de **origen** con tres pestañas: **Insumo** (buscador por código o descripción, filtrado por tipo según el grupo: Material → Material/Laboratorio/Otro; Equipo → Equipo/Herramienta; Transporte → Transporte; ManoObra → ManoObraDestajo), **Cuadrilla** (lista con costo por día) y **Sub-APU** (solo `EsSubanalisis = true` y distinto del APU actual). La opción "Precio manual" no tiene referencia.
  - Al elegir el origen se prellenan la descripción, la unidad y el precio (solo lectura si hay referencia).
  - Columnas editables: cantidad, **modo** (Desperdicio / Rendimiento / Divisor / % de MO), factor.
  - **Ayuda por modo**: Desperdicio → "% desperdicio (0,05 = 5 %)"; Rendimiento con cuadrilla → **pedir "rendimiento (unidades por día)" y guardar `Factor = 1 / rendimiento`**, mostrando ambos valores; Divisor → "usos por unidad"; % de MO → "porcentaje sobre la mano de obra del grupo (0,10)". Este último modo solo se permite en el grupo ManoObra.
  - Botones para subir o bajar una línea (orden) y eliminarla.
- **Cálculo en vivo**: valor por línea, subtotal por grupo, costos por grupo y total. Recalcular en el cliente con las mismas reglas de `CalculadoraApu` (portar las ~40 líneas a `src/presupuesto/calculadoraApu.ts`) para que la respuesta sea instantánea. Al guardar, el servidor es la fuente de verdad: mostrar su total y avisar si difiere del cliente en más de $1.
- **Errores de la API**: ciclo de sub-APU (`CicloApuException` → 400) con el mensaje *"Este sub-análisis crearía una referencia circular"*.
- **Usos**: panel "Se usa en" (`GET /api/apu/{id}/usos`) con enlaces. Si se usa en un presupuesto **aprobado**, aviso: *"Los presupuestos aprobados no cambian; los borradores se recalculan al pulsar Recalcular"*.
- **Acciones**: Guardar, Duplicar (`POST /duplicar` → navega al nuevo), Volver.
- **Listado `/presupuesto/apu`**: además del buscador, filtro *Sub-análisis / Normales*, columna "Usos" y botón **"+ Nuevo APU"** que crea uno con un grupo Material y un grupo ManoObra vacíos y abre el editor.
- Componentes reutilizables en `src/presupuesto/`: `EditorApu.tsx` (recibe `apuId` y `alGuardar`), `SelectorOrigenLinea.tsx` y `calculadoraApu.ts`. **La página y el panel lateral de C3 usan el mismo `EditorApu`.**

**Test frontend (opcional pero recomendado):** si se agrega Vitest, probar `calculadoraApu.ts` con los mismos casos de `CalculadoraApuTests`.

---

## C3. 🔴 "Crear APU nuevo" desde un ítem del presupuesto

**Problema.** En `EditorPresupuesto.tsx`, el modal `FormularioApu` crea un APU **sin líneas** (costo $0) y lo enlaza al ítem. Como el editor de APU no permitía editarlo, el ítem queda en $0 para siempre.

**Corrección:**
- Reemplazar el modal por un **panel lateral** (`Offcanvas` de react-bootstrap, ancho 75 %) que use el `EditorApu` de C2 en modo creación. La unidad se prellena con la del ítem si existe.
- Solo cuando el usuario pulse **Guardar** y el APU tenga **al menos una línea con valor > 0**: crear el ítem con `desde-apu`. Si se cancela, no se crea nada.
- Eliminar el componente `FormularioApu` y su `unidad` en texto libre.
- Agregar al menú de cada ítem: **"Editar APU"**. Abre el mismo panel con el APU del ítem; al guardar, recalcular el ítem si el presupuesto está en Borrador.

---

## C4. 🟠 "Mover a capítulo…" con selector

**Problema.** `moverItem` en `EditorPresupuesto.tsx` mueve automáticamente al **primer capítulo distinto** del actual.

**Corrección:** la acción "Mover a capítulo…" abre un modal con un `Form.Select` de los capítulos del presupuesto (excepto el actual), mostrando *número y nombre*. Botones Mover y Cancelar. Enviar `capituloDestinoId` elegido y `rowVersion`. Tras mover, hacer scroll al ítem en su nuevo capítulo y resaltarlo durante 2 s.

---

## C5. 🟠 Concepto en los ítems (enlace con actas)

**Problema.** Los ítems manuales se crean con `conceptoId: null`, y el editor no permite asignarlo. Un ítem sin concepto **no se puede usar en un acta**, porque `ActaDetalle` exige `ConceptoId`.

**Corrección:**
- Backend: al crear un ítem manual sin `ConceptoId`, **buscar un concepto por descripción** (sin distinguir mayúsculas ni tildes, igual que el importador) y, si no existe, **crearlo**. Lo mismo en `desde-apu` cuando el APU no tiene concepto. Reutilizar la lógica del importador; no duplicarla.
- Validación nueva en `GET /validacion`: tipo `SinConcepto`. **Bloquea la aprobación**, como `UnidadDistintaApu`.
- Frontend: columna o acción **"Concepto"** en el menú del ítem, con un selector buscable de `Conceptos` para cambiarlo.
- Test: ítem manual sin concepto → se crea o enlaza uno; aprobar con un ítem `SinConcepto` → 400.

---

## C6. 🟠 Pantalla de mano de obra completa (`/presupuesto/mano-obra`)

**Problema.** `ManoObraPresupuesto.tsx` solo edita los parámetros. La API ya tiene `PUT /api/mano-obra/prestaciones`, CRUD de `/api/cargos` y CRUD de `/api/cuadrillas`.

**Corrección**, en tres bloques en la misma página:
1. **Prestaciones**: tabla editable (concepto, %) con agregar, eliminar y reordenar. Pie con el **factor prestacional** total y el interruptor *Incluir días no hábiles* (muestra el factor resultante, ej. 0,72 → 0,953). Botón Guardar (PUT con toda la lista).
2. **Cargos**: `CatalogoCrud` con nombre y salario mensual; columna calculada **jornal con prestaciones**.
3. **Cuadrillas**: lista con su costo por día. El editor permite cambiar el nombre y la descripción, y agregar o quitar integrantes (cargo + cantidad), con el costo por día calculado en vivo.

Aviso fijo arriba: *"Cambiar salarios o prestaciones afecta todos los APU. Los presupuestos aprobados no cambian; los borradores se actualizan con Recalcular."*

---

## C7. 🟠 Pantalla de insumos completa (`/presupuesto/insumos`)

**Problema.** `InsumosPresupuesto.tsx` solo edita precios y toma el precio de proveedores. No crea ni desactiva insumos ni muestra el historial.

**Corrección:**
- Botón **"+ Nuevo insumo"** (`POST /api/insumos`) con todos los campos: código, descripción, tipo, unidad de compra, precio base, % IVA, cantidad por unidad, unidad de uso y **material enlazado** (selector opcional de `Materiales`). Mostrar en vivo el **precio unitario** = base × (1 + IVA) / cantidad por unidad.
- Botón para desactivar (`DELETE`, borrado lógico), con aviso si el insumo se usa en APU.
- Acción **"Historial"** (`GET /api/insumos/{id}/historial`): modal con una tabla (fecha, precio base, IVA, usuario) y una línea de Recharts con la evolución del precio.
- Pestañas por tipo, con contador de insumos en cada una, y buscador por código o descripción.
- **Exportar a Excel** la pestaña actual. Si no existe el endpoint, crear `GET /api/insumos/excel?tipo=` con ClosedXML, igual que los otros exportadores.

---

## C8. 🟡 Recalcular y "cuánto costaría hoy" en el editor

**Problema.** `POST /api/presupuestos/{id}/recalcular?aplicar=` existe en el backend, pero **no hay botón** en `EditorPresupuesto.tsx`.

**Corrección:**
- **Borrador**: botón **"Recalcular con precios actuales"** → llamar con `aplicar=false`. Abrir un modal con la tabla de diferencias (código, descripción, valor unitario actual, valor nuevo, diferencia en $ y %; ordenada por |diferencia|) y el total actual frente al nuevo. Botones **Aplicar** (`aplicar=true` con `rowVersion`) y Cancelar.
- **Aprobado o Cerrado**: botón **"¿Cuánto costaría hoy?"** → mismo modal en **solo lectura** (sin Aplicar), con la nota *"El presupuesto aprobado no cambia"*.
- Permitir la vista previa también al perfil Arquitecto (solo lectura), si el backend ya lo permite para `aplicar=false`.

---

## C9. 🟡 Excel de prueba y tests faltantes

### C9.1 Excel de prueba
**Problema.** `C:\Arquitectos\Presupuesto Preliminar Casa Murillo.xlsx` **se reescribió con una librería** el 11-nov a las 19:49: cambió el título a "Casa campestre Los Arrayanes" y **perdió los valores calculados en caché** de las fórmulas. `ImportadorPresupuestoTests` y `SemillaCasaMurillo` leen ese archivo de la raíz, que el usuario puede modificar.

**Corrección:**
- Hay una copia intacta en **`C:\Arquitectos\presupuesto_demo\Presupuesto_Casa_Murillo_original.xlsx`**: es el archivo original del usuario, con los valores en caché.
- Copiarla a `backend\tests\Arquitectos.Tests\Fixtures\Presupuesto_Casa_Murillo.xlsx` con `<None Update="Fixtures\**" CopyToOutputDirectory="PreserveNewest" />`, y que **el test lea esa copia**, no la de la raíz.
- Copiar también `presupuesto_casa_murillo.json` a `Fixtures\` y usarlo desde ahí. Así los tests no dependen de recorrer carpetas padre.
- `SemillaCasaMurillo` y `--seed-plantilla`: leer el JSON de `presupuesto_demo\` (inmutable). El `.xlsx` de la raíz no se usa.
- **Nunca escribir sobre archivos de la raíz del proyecto.** Si algún script o prueba manual reescribió el Excel, eliminar ese código.
- **No restaurar ni modificar** el `.xlsx` de la raíz: lo decide el usuario.
- Test nuevo: `LectorExcel` sobre un libro **sin valores en caché** debe dar un error claro (*"El libro no tiene valores calculados; ábralo y guárdelo en Excel antes de importar"*) o calcularlos. Elegir una opción y documentarla en el informe de importación.

### C9.2 Tests faltantes
`PresupuestoServicioTests` tiene solo 2 tests. Agregar:
- Totales con AIU en 0 y con AIU (administración 10 %, utilidad 5 %, IVA sobre utilidad 19 %); el incremento se calcula sobre el SCD y no sobre SCD + imprevistos.
- Valor por m², y área 0 → `null`.
- Mediciones → cantidad = Σ parciales; borrar una medición actualiza la cantidad.
- Cantidad referenciada a otro ítem × factor; cambiar el origen actualiza el destino; no se permite una referencia circular entre ítems (A → B → A).
- Al aprobar se congelan `ValorMaterialUnitario`, `ValorManoObraUnitario` y `DesgloseApuJson`; cambiar después el precio de un insumo no altera el aprobado.
- Nueva versión: copia profunda, `Version + 1`, Borrador. Al aprobar la nueva, la anterior pasa a Cerrado.
- Dos presupuestos aprobados en la misma construcción → error (índice filtrado).
- Recalcular `aplicar=false` no modifica nada; `aplicar=true` sobre un aprobado → 400.
- `FactorConversionApu` ≠ 1 multiplica material y MO y elimina el aviso `UnidadDistintaApu`.
- Agregar a `CalculadoraApuTests` un caso con el **APU real "Concreto Zapatas"** del fixture JSON (sub-APU "concreto de 3000 psi") que reproduzca el total del Excel con una tolerancia de $1.

---

## Verificación final

```powershell
cd C:\Arquitectos\backend
dotnet test
dotnet-ef migrations has-pending-model-changes -p src\Arquitectos.Infrastructure -s src\Arquitectos.Api
cd ..\frontend
npm run build
npm run lint
```

Recorrido manual (administrador):
1. **Mano de obra**: subir el salario del oficial un 5 % → el costo de la cuadrilla J sube.
2. **Insumos**: crear "Cemento gris 50 kg – prueba", cambiarle el precio y ver el historial.
3. **APU**: "+ Nuevo APU" → "Muro prueba" en M2. Material: el insumo nuevo con cantidad 0,25 y 5 % de desperdicio. ManoObra: cuadrilla J con rendimiento de 12 m²/día y herramienta menor del 10 %. Verificar el total en vivo, guardar, y que intentar usarlo como su propio sub-APU muestre el error de ciclo.
4. **Presupuesto** en Borrador: "Crear APU nuevo" desde un ítem en el panel lateral, con la línea creada → el ítem toma valor > $0. Crear un ítem manual → se le asigna concepto. "Mover a capítulo…" a uno elegido.
5. **Recalcular con precios actuales** → ver las diferencias → Aplicar. **Aprobar.** "¿Cuánto costaría hoy?" en solo lectura.
6. Registrar un **acta** con el ítem manual y el del APU nuevo → aparecen en el seguimiento.
