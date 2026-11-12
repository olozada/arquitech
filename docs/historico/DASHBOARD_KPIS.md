*"Documento histórico; el estado vigente está en docs/ARQUITECTURA.md"*

# Dashboard de KPIs – App "Arquitectos"

> Documento para **Cursor**. Extiende la app ya migrada (`C:\Arquitectos\backend` .NET 8 + `C:\Arquitectos\frontend` React/Vite).
> Antes de empezar, lee `MIGRACION_ARQUITECTOS.md` para conocer la arquitectura y las convenciones (dominio en español, sin SQL concatenado y respuestas de error con ProblemDetails).
> **Objetivo:** después del login, el usuario llega a `/` y ve un tablero con datos reales de las actas que le ayude a decidir: dónde se va el dinero, si hay sobrecostos o un posible doble cobro, qué obras están quietas y dónde hay ahorro con los proveedores.

---

## 1. Principios

1. **Solo se usan datos que ya existen**: `Actas`, `ActaDetalles`, catálogos y `ProveedorMateriales`. En la fase 1 no se crean tablas nuevas. El presupuesto se agrega como fase 2 opcional (sección 8).
2. **Fecha de referencia de un acta: `FechaFinal`**, es decir, el fin del periodo ejecutado. Todos los filtros y la agrupación por mes usan esa fecha.
3. **Valor = Σ `ActaDetalles.Subtotal`** (`Cantidad × Precio`, columna calculada que ya existe).
4. Cada KPI muestra el valor del periodo, la **comparación con el periodo anterior de igual duración** (Δ %) y un enlace al detalle.
5. Los umbrales se configuran en `appsettings.json` y nunca se escriben en el código.
6. Las fechas de registro se guardan en UTC y se convierten a `America/Bogota` antes de calcular días.

---

## 2. Filtros globales (barra superior del dashboard)

| Filtro | Valor por defecto |
|---|---|
| Desde / Hasta | Últimos 12 meses completos más el mes en curso (`desde` = día 1, 12 meses atrás; `hasta` = hoy) |
| Construcción | Todas |
| Contratista | Todos |
| Arquitecto | Todos |

Botones de atajo: **Mes actual · Trimestre · Año · 12 meses**. Los filtros viven en la URL (`?desde=&hasta=&construccionId=…`) para poder compartir la vista.

---

## 3. KPIs y su fórmula

### 3.1 Tarjetas (fila superior)

| # | KPI | Fórmula | Decisión que apoya |
|---|---|---|---|
| K1 | **Valor ejecutado** | Σ Subtotal de las actas con `FechaFinal` dentro del rango, más el Δ % frente al periodo anterior | ¿El ritmo de inversión va como se esperaba? |
| K2 | **Actas registradas** | Número de actas en el rango, más Δ % | Nivel de actividad |
| K3 | **Valor promedio por acta** | K1 / K2 | Detecta actas inusualmente grandes o pequeñas |
| K4 | **Obras activas** | Construcciones distintas con al menos un acta en el rango / construcciones activas en el catálogo | Cobertura de seguimiento |
| K5 | **Sobrecosto detectado** | Σ impacto de las alertas A1 (precio sobre la referencia histórica) | Cuánto se paga de más frente a la historia |
| K6 | **Ahorro potencial con proveedores** | Σ impacto de las alertas A2 (precio del acta frente al mejor precio de lista) | Negociación y compras |
| K7 | **Días promedio de registro** | Promedio de `FechaRegistro(local) − FechaFinal`, más el % de actas con más de `DiasRegistroTardio` | Disciplina operativa: actas tardías = control tardío |
| K8 | **Alertas abiertas** | Número de alertas A1–A4, separadas por severidad | Por dónde empezar hoy |

### 3.2 Gráficos

| # | Visual | Datos | Tipo |
|---|---|---|---|
| G1 | **Tendencia mensual** | Valor ejecutado y número de actas por mes (`FechaFinal`), con el mismo periodo del año anterior como línea punteada | Barras (valor) + línea (actas), con doble eje |
| G2 | **Top 10 construcciones por valor** | Σ Subtotal por `ConstruccionId` y % del total | Barras horizontales |
| G3 | **Concentración por contratista (Pareto)** | Valor por contratista ordenado y % acumulado. Destacar el % que suman los 3 primeros | Barras + línea acumulada |
| G4 | **Mezcla de costos por concepto** | Top 8 conceptos por valor; el resto se agrupa como "Otros" | Barras apiladas al 100 % o donut |
| G5 | **Carga por arquitecto / revisor** | Número de actas y valor por persona | Barras agrupadas |
| G6 | **Evolución del precio unitario** | Para el concepto o material que el usuario elija: precio promedio mensual, mínimo y máximo | Línea con banda min–max |

### 3.3 Alertas (tabla "Requiere atención", ordenada por impacto en $)

| Código | Regla | Severidad | Impacto en $ |
|---|---|---|---|
| **A1 Precio sobre la referencia** | Para cada línea del rango: `ref` = precio promedio de las líneas con el mismo `ConceptoId + UnidadMedidaId + MaterialId`, en los `VentanaReferenciaDias` anteriores a la `FechaFinal` del acta, sin contar el acta misma. Solo se calcula si hay al menos `MinObservacionesReferencia` observaciones. Se genera la alerta si `(Precio − ref) / ref > UmbralDesvioPrecioPct` | Alta si supera 2× el umbral; Media en los demás casos | `(Precio − ref) × Cantidad` |
| **A2 Precio sobre la lista del proveedor** | Líneas con `MaterialId` cuyo `Precio` > `MIN(ProveedorMateriales.Precio)` de ese material. Se muestra el proveedor más barato | Media | `(Precio − minLista) × Cantidad` |
| **A3 Posible doble cobro** | Dos actas distintas con el mismo `ContratistaId`, con líneas del mismo `ConstruccionId + ConceptoId` y periodos que se cruzan: `A.FechaInicial ≤ B.FechaFinal AND B.FechaInicial ≤ A.FechaFinal` | **Alta** | El menor subtotal de las dos líneas |
| **A4 Obra sin actividad** | Construcción activa cuya última `FechaFinal` es anterior a `hoy − DiasSinActividad`, o que nunca ha tenido actas | Baja | — |
| **A5 Acta registrada tarde** | Más de `DiasRegistroTardio` días entre `FechaFinal` y `FechaRegistro` | Baja | — |

> **Advertencia que debe verse en la UI (tooltip de A2):** el precio de un ítem del acta puede incluir mano de obra. A2 es una *referencia* para negociar, no un sobrecosto confirmado.

Cada alerta trae `numeroActa`. Al hacer clic se abre el PDF (`/api/actas/{n}/pdf`) o se navega a `/informes/actas` con los filtros aplicados.

### 3.4 Calidad de datos (bloque pequeño al final)
- % de materiales del catálogo que tienen al menos un precio de proveedor.
- Materiales con **un solo proveedor** (riesgo de dependencia).
- Líneas de acta sin material.
- Actas sin observación.

---

## 4. Configuración

`backend/src/Arquitectos.Api/appsettings.json`:
```json
"Dashboard": {
  "UmbralDesvioPrecioPct": 15,
  "VentanaReferenciaDias": 180,
  "MinObservacionesReferencia": 3,
  "DiasSinActividad": 30,
  "DiasRegistroTardio": 15,
  "ZonaHoraria": "America/Bogota",
  "MaxAlertas": 100
}
```
Clase `OpcionesDashboard` en `Arquitectos.Application/Dashboard`, registrada con `services.Configure<OpcionesDashboard>(…)` y validada al iniciar (`ValidateOnStart`).

---

## 5. Backend

### 5.1 Estructura
```
Arquitectos.Application/Dashboard/
  OpcionesDashboard.cs
  FiltroDashboard.cs            // Desde, Hasta, ConstruccionId?, ContratistaId?, ArquitectoId?
  ContratosDashboard.cs         // DTOs (5.3)
  IDashboardServicio.cs
  FiltroDashboardValidador.cs   // hasta >= desde; rango <= 36 meses
Arquitectos.Infrastructure/Dashboard/
  DashboardServicio.cs          // orquesta las consultas
  ReglasAlertas.cs              // A1–A5 como métodos puros y testeables
Arquitectos.Api/Controllers/DashboardController.cs
```

### 5.2 Endpoints
| Método | Ruta | Respuesta |
|---|---|---|
| GET | `/api/dashboard?desde=&hasta=&construccionId=&contratistaId=&arquitectoId=` | `DashboardDto` completo (tarjetas, G1–G5, resumen de alertas, calidad) |
| GET | `/api/dashboard/alertas?…&tipo=A1&severidad=Alta&pagina=1` | Lista paginada de `AlertaDto` |
| GET | `/api/dashboard/precio-unitario?conceptoId=&materialId=&desde=&hasta=` | Serie de G6 |
| GET | `/api/dashboard/export?…` | `.xlsx` con una hoja por bloque (ClosedXML, como el export que ya existe) |

Todos requieren autenticación (la política global ya existe).

### 5.3 Contratos
```csharp
namespace Arquitectos.Application.Dashboard;

public sealed record FiltroDashboard(DateOnly Desde, DateOnly Hasta, int? ConstruccionId, int? ContratistaId, int? ArquitectoId);

public sealed record KpiDto(string Codigo, string Titulo, decimal Valor, decimal? ValorAnterior, decimal? VariacionPct, string Formato); // Formato: "moneda" | "entero" | "dias" | "pct"

public sealed record PuntoMensualDto(int Anio, int Mes, decimal Valor, int Actas, decimal? ValorAnioAnterior);

public sealed record RankingDto(int Id, string Nombre, decimal Valor, int Actas, decimal Participacion, decimal ParticipacionAcumulada);

public sealed record AlertaDto(
    string Tipo,            // A1..A5
    string Severidad,       // Alta | Media | Baja
    string Titulo,          // "Precio 32% sobre referencia: Concreto 3000 PSI"
    string Detalle,
    int? NumeroActa,
    int? ConstruccionId,
    string? Construccion,
    decimal? Impacto);

public sealed record CalidadDatosDto(decimal PctMaterialesConPrecio, int MaterialesUnProveedor, int LineasSinMaterial, int ActasSinObservacion);

public sealed record DashboardDto(
    FiltroDashboard Filtro,
    DateOnly DesdeAnterior, DateOnly HastaAnterior,
    IReadOnlyList<KpiDto> Kpis,                       // K1..K8
    IReadOnlyList<PuntoMensualDto> Tendencia,         // G1
    IReadOnlyList<RankingDto> TopConstrucciones,      // G2
    IReadOnlyList<RankingDto> Contratistas,           // G3
    IReadOnlyList<RankingDto> Conceptos,              // G4 (top 8 + "Otros" con Id = 0)
    IReadOnlyList<RankingDto> Arquitectos,            // G5
    IReadOnlyList<RankingDto> Revisores,              // G5
    IReadOnlyDictionary<string, int> AlertasPorSeveridad,
    IReadOnlyList<AlertaDto> AlertasTop,              // las 10 de mayor impacto
    CalidadDatosDto Calidad);
```

### 5.4 Implementación (pautas para Cursor)
- **Periodo anterior:** `dias = Hasta.DayNumber − Desde.DayNumber + 1; HastaAnterior = Desde.AddDays(-1); DesdeAnterior = HastaAnterior.AddDays(-(dias-1))`.
- Escribir una sola `IQueryable<ActaDetalle> LineasFiltradas(filtro, desde, hasta)` y reutilizarla en todos los cálculos. Agregar en SQL (`GroupBy` en EF) y **no traer filas a memoria**. Las excepciones son A1 y A3, que se calculan sobre un conjunto acotado.
- **A1 en EF:** traer las líneas del rango más las de la ventana de referencia (`desde − VentanaReferenciaDias`), proyectadas a `{Id, NumeroActa, FechaFinal, ConceptoId, UnidadMedidaId, MaterialId, Cantidad, Precio}`, y calcular la referencia en memoria por clave. Si el volumen crece, pasar a SQL con funciones de ventana mediante `FromSql` parametrizado:
  ```sql
  AVG(d.Precio) OVER (PARTITION BY d.ConceptoId, d.UnidadMedidaId, ISNULL(d.MaterialId,0)
                      ORDER BY a.FechaFinal RANGE BETWEEN ... )
  ```
  (SQL Server no permite RANGE con intervalos de fecha; usar `OUTER APPLY` con un filtro de fechas).
- **A3 en EF:** self-join sobre `ActaDetalles` con `Actas`:
  ```csharp
  from d1 in lineas
  join d2 in ctx.ActaDetalles on new { d1.ConstruccionId, d1.ConceptoId } equals new { d2.ConstruccionId, d2.ConceptoId }
  where d1.NumeroActa < d2.NumeroActa
     && d1.Acta.ContratistaId == d2.Acta.ContratistaId
     && d1.Acta.FechaInicial <= d2.Acta.FechaFinal
     && d2.Acta.FechaInicial <= d1.Acta.FechaFinal
  select new { ... }
  ```
- **K7:** convertir `FechaRegistro` con `TimeZoneInfo.FindSystemTimeZoneById(opciones.ZonaHoraria)` (en Windows el id es `"SA Pacific Standard Time"`; usar `TimeZoneInfo.TryConvertIanaIdToWindowsId`).
- `VariacionPct = null` cuando el valor anterior es 0, para no dividir por cero.
- Las reglas A1–A5 se escriben como **funciones puras** en `ReglasAlertas` (reciben listas y opciones, devuelven `AlertaDto`). Así se prueban sin base de datos.
- Caché: `IMemoryCache` de 60 s, con la clave = filtro serializado.

### 5.5 Índices (nueva migración `DashboardIndices`)
```csharp
builder.HasIndex(a => new { a.FechaFinal, a.ContratistaId });          // Actas
builder.HasIndex(a => new { a.ContratistaId, a.FechaInicial, a.FechaFinal });
builder.HasIndex(d => new { d.ConstruccionId, d.ConceptoId });         // ActaDetalles
builder.HasIndex(d => new { d.ConceptoId, d.UnidadMedidaId, d.MaterialId });
```
En la misma migración, agregar al menú semilla: `Id = 15, Descripcion = "Dashboard", Url = "/", Icono = "fa fa-tachometer", Orden = 0` y asignarlo al tipo Administrador.

### 5.6 Datos de demostración (YA EXISTEN, no crear otra semilla)
Los datos simulados ya están generados en `C:\Arquitectos\datos_demo\`:
- `datos_demo.sql`: script T-SQL transaccional que **se detiene si ya hay datos**. Con `@Reiniciar = 1` borra las actas, los precios y los catálogos y vuelve a cargar; no toca usuarios ni menú. Se ejecuta en SSMS sobre la base creada por la migración `Inicial`.
- `generar_datos_demo.py`: el generador. Es determinístico (semilla 2026) y acepta `--hoy` para correr las fechas.

Contenido: 152 actas en 24 meses (nov-2024 a oct-2026) con 801 líneas, 8 construcciones (2 terminadas e inactivas), 6 contratistas, 18 conceptos, 16 materiales y 28 precios de proveedor.

**Resultados esperados (criterio de aceptación con filtro: últimos 12 meses al 2026-11-09 y umbrales por defecto):**

| Indicador | Esperado |
|---|---|
| K1 Valor ejecutado | ≈ $4.010 M COP, Δ ≈ +18 % frente al periodo anterior |
| K2 Actas | ≈ 96 (anterior ≈ 56) |
| A1 Precio sobre la referencia | 13 alertas (las 13 sembradas). 10 son del contratista **Obras y Acabados El Roble**. Impacto ≈ $30 M |
| A2 Precio sobre la lista | ≈ 78 líneas, ahorro potencial ≈ $27 M |
| A3 Posible doble cobro | **Exactamente 2 pares**, ambos de **Redes y Montajes Calarcá** |
| A4 Obra sin actividad | **Hotel boutique Salento** (última acta 15-jun-2026) y **Clínica veterinaria Calarcá** (nunca ha tenido actas) |
| A5 Registro tardío (>15 días) | ≈ 9 actas |
| G3 Concentración | Construcciones Quindío S.A.S. ≈ 47 % del valor |
| Calidad | 1 material sin proveedor (Impermeabilizante), 3 con un solo proveedor (ladrillo, porcelanato y malla), y las líneas de mano de obra sin material |

Las variaciones pequeñas (±2 %) en los montos se aceptan según cómo se redondee la referencia. En cambio, los conteos de A3 y A4 deben coincidir exactamente.

### 5.7 Tests (xUnit + SQLite en memoria, igual que `CatalogoTests`)
- `ReglasAlertas`: un caso positivo y uno negativo para cada una de A1–A5, incluidos los bordes (exactamente en el umbral, menos de `MinObservaciones`, periodos que se tocan en un solo día).
- `DashboardServicio`: cálculo del periodo anterior; `VariacionPct` nulo cuando el anterior es 0; que los filtros por construcción, contratista y arquitecto se apliquen a todas las secciones; que la suma de `TopConstrucciones` no supere K1.
- Validador: `hasta < desde` → 400.

---

## 6. Frontend

### 6.1 Dependencias
```bash
npm i recharts
```
(Recharts 3, compatible con React 19). Formato con `Intl.NumberFormat('es-CO', { style: 'currency', currency: 'COP', maximumFractionDigits: 0 })`. Reutilizar `actas/formato.ts` si ya tiene utilidades parecidas.

### 6.2 Archivos
```
src/api/dashboard.ts               // useDashboard(filtro), useAlertas(...), usePrecioUnitario(...) con TanStack Query
src/dashboard/FiltrosDashboard.tsx  // filtros sincronizados con la URL (useSearchParams)
src/dashboard/TarjetaKpi.tsx        // valor grande, Δ% con flecha verde o roja, subtítulo con el periodo anterior
src/dashboard/GraficoTendencia.tsx  // G1 ComposedChart
src/dashboard/GraficoRanking.tsx    // G2/G5 barras horizontales (genérico)
src/dashboard/GraficoPareto.tsx     // G3
src/dashboard/GraficoConceptos.tsx  // G4
src/dashboard/GraficoPrecio.tsx     // G6 con selector de concepto o material
src/dashboard/TablaAlertas.tsx      // filtro por tipo y severidad, enlace al PDF del acta
src/dashboard/CalidadDatos.tsx
src/paginas/Dashboard.tsx           // reemplaza a Inicio en la ruta "/"
```
Borrar `paginas/Inicio.tsx` y en `App.tsx` cambiar `path="/"` para que use `<Dashboard />`. Cargar la página del dashboard con `React.lazy`, lo que además resuelve el aviso de que el bundle pesa más de 500 kB.

### 6.3 Layout (Bootstrap 5 grid)
```
[ Filtros ................................................ Exportar ]
[ K1 ][ K2 ][ K3 ][ K4 ]
[ K5 ][ K6 ][ K7 ][ K8 ]
[ G1 Tendencia (col-12) ]
[ G2 Top construcciones (col-lg-6) ][ G3 Pareto contratistas (col-lg-6) ]
[ G4 Conceptos (col-lg-6)          ][ G5 Arquitectos/Revisores (col-lg-6) ]
[ Tabla "Requiere atención" (col-12) ]
[ G6 Precio unitario (col-lg-8) ][ Calidad de datos (col-lg-4) ]
```
- Colores de la app: barra lateral `#23323c`, acento `#1abb9c`. Severidades: Alta `#d9534f`, Media `#f0ad4e`, Baja `#5bc0de`.
- En K5, K6 y K8 un Δ positivo es **malo** (rojo); en K1 y K2 es bueno (verde). Para eso, `TarjetaKpi` recibe la propiedad `mayorEsMejor`.
- Clic en una barra de G2 o G3 → aplica ese filtro de construcción o contratista a todo el tablero.
- Estados de carga (skeleton) y vacío ("No hay actas en el rango. Ajusta los filtros o registra una en Toma de actas", con enlace).
- Responsive: en pantallas de menos de 768 px, las tarjetas van de 2 en 2 y los gráficos ocupan el 100 % del ancho.

---

## 7. Plan para Cursor (un prompt por fase)

1. **Contratos y opciones:** crear `Application/Dashboard` (records, opciones, validador e interfaz) y registrarlos en la inyección de dependencias.
2. **ReglasAlertas y sus tests:** escribir A1–A5 como funciones puras y sus tests. No avanzar mientras no pasen.
3. **DashboardServicio:** KPIs, series y rankings; caché; tests con SQLite.
4. **Controller y export Excel.**
5. **Migración `DashboardIndices`** con los índices y la opción de menú → `dotnet ef migrations add DashboardIndices` y luego `database update`.
6. **Cargar los datos demo:** ejecutar `C:\Arquitectos\datos_demo\datos_demo.sql` en SSMS. Después, `GET /api/dashboard` en Swagger debe cumplir la tabla de resultados esperados de la sección 5.6. Convertir esa tabla en un test de integración opcional (`[Trait("Categoria","Demo")]`).
7. **Frontend:** filtros, tarjetas y G1.
8. **Frontend:** G2–G6, tabla de alertas, calidad, carga diferida.
9. **Verificación:** `dotnet test`, `npm run build`, `npm run lint`. Recorrido manual: login → el dashboard carga → clic en una barra filtra todo → clic en una alerta abre el PDF → exportar.

---

## 8. Fase 2 opcional (requiere tablas nuevas; preguntar antes de hacerla)

| Mejora | Qué agrega | KPI nuevo |
|---|---|---|
| **Presupuesto por construcción** | Tabla `PresupuestosConstruccion(ConstruccionId, ConceptoId?, Valor, Vigencia)` y una pantalla CRUD | % de ejecución frente al presupuesto, desviación en $ y %, proyección al cierre con el ritmo de los últimos 3 meses |
| **Estado del acta** | Columna `Estado` (Borrador → Revisada → Aprobada → Pagada) con fecha y usuario de cada cambio | Tiempo de ciclo por etapa, actas pendientes de aprobación por revisor |
| **Usuario ↔ arquitecto** | Columna `Usuarios.ArquitectoId` | El arquitecto ve solo sus obras y el administrador ve todo |
| **Alertas por correo** | Job semanal (Hangfire o BackgroundService) | Resumen de alertas altas enviado al administrador |
