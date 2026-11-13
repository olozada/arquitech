# AUDITORIA_MEJORAS_2 — Segunda auditoría (Arquitectos)

> Para Cursor. Implementar **en el orden indicado**, un bloque por commit.
> Reglas generales (siguen vigentes):
> - Migraciones solo con `dotnet-ef migrations add` (herramienta global). Nunca a mano.
> - No imprimir ni commitear secretos. No tocar `appsettings.Production.json`.
> - Ningún test ni script escribe en la raíz del proyecto.
> - Al terminar cada bloque: `dotnet test backend -c Release`, `dotnet-ef migrations has-pending-model-changes`, `npm test`, `npm run build`, `npm run lint` en verde.
> - Estado base: commit `c12e165` + Fase 3 en curso (refactor, `IndicesConsulta`, `IconosLucide`, `Directory.Packages.props`). **Primero estabilizar y commitear la Fase 3 (bloque 0).**

---

## Bloque 0 — Cerrar Fase 3 en curso

| Id | Tarea | Aceptación |
|---|---|---|
| F0.1 | Terminar `Directory.Packages.props` (versiones centralizadas) y actualizar paquetes de test: `xunit` → 2.9.x (o xunit.v3), `xunit.runner.visualstudio` → 3.x, `Microsoft.NET.Test.Sdk` → 17.14+, `coverlet.collector` → 6.0.4+. | Ningún `.csproj` tiene `Version=` en `PackageReference`. Tests en verde. `dotnet list package --vulnerable` sin hallazgos. |
| F0.2 | Regenerar `backend/scripts/actualizar_local.sql` desde `20261112221024_ConcurrenciaActas` (incluye `IndicesConsulta` e `IconosLucide`): `dotnet-ef migrations script 20261112221024_ConcurrenciaActas --idempotent -o backend/scripts/actualizar_local.sql ...` | El script contiene las 2 migraciones nuevas y es idempotente. |
| F0.3 | Mover scripts SQL viejos hechos a mano (`cedula_catalogo.sql`, `cierre_acta.sql`, `item_opcional.sql`, `modulo_presupuesto.sql`, `nit_proveedor.sql`, `perfiles_usuario.sql`, `presupuesto_creacion_web.sql`, `revision_acta.sql`) a `backend/scripts/historico/` con un `README.md` que diga que están cubiertos por migraciones y no se ejecutan. | `backend/scripts/` solo contiene `actualizar_local.sql` y `historico/`. |
| F0.4 | En `datos_demo/README.md` advertir que `recrear_tablas.sql` y `recrear_pruebas.sql` hacen DROP/CREATE fuera de EF, no están en git y no deben ejecutarse contra la base del sitio. | Texto agregado. |
| F0.5 | En `docs/OPERACION.md`: `IconosLucide` cambia `MenuOpciones.Icono`; aplicar el script **junto** con la publicación del frontend nuevo. | Nota agregada. |

---

## Bloque 1 — 🔴 Críticos: origen de la cadena de conexión

### C1. El `.env` pisa la configuración de producción

**Hoy:** `Program.cs:32` llama `ArchivoEnv.AplicarConexion(builder.Configuration, ContentRootPath)` después de cargar las variables de entorno, y sobrescribe `ConnectionStrings:Arquitectos`. `ArchivoEnv.Buscar` sube carpetas hasta la raíz del disco.

**Hacer:**
1. Cargar el `.env` **solo en Development**, y como **fuente de configuración de menor prioridad** que las variables de entorno y los user-secrets (no escribir con `configuration[...] =`).
   ```csharp
   if (builder.Environment.IsDevelopment())
   {
       var valores = ArchivoEnv.CrearValores(builder.Environment.ContentRootPath);
       // Insertar ANTES de las variables de entorno: queda con menor prioridad.
       var indiceEnv = builder.Configuration.Sources
           .Select((s, i) => (s, i))
           .FirstOrDefault(x => x.s is EnvironmentVariablesConfigurationSource).i;
       builder.Configuration.Sources.Insert(indiceEnv,
           new MemoryConfigurationSource { InitialData = valores });
   }
   ```
2. `ArchivoEnv.Buscar`: buscar como máximo hasta la carpeta que contiene `backend/` (raíz del repo, donde existe `.git` o `Arquitectos.sln`), no hasta la raíz del disco.
3. `ArquitectosDbContextFactory` (design-time): mismo orden — `.env` antes de `AddEnvironmentVariables()` (ya está así; verificar), y que el mensaje de error no sugiera el `.env` en producción.

**Aceptación:**
- Test nuevo `ConfiguracionConexionTests`:
  - En `Production`, con un `.env` en una carpeta padre y la variable `ConnectionStrings__Arquitectos` definida, la cadena efectiva es la de la variable.
  - En `Production`, sin variable y con `.env`, la API **no arranca** (mensaje claro), no usa el `.env`.
  - En `Development`, sin variable, usa el `.env`; con variable, gana la variable.

### C2. Credenciales compartidas y comandos destructivos sin guarda

**Hoy:** `ArchivoEnv` armaba la cadena de la base `ARQUITECTOS` con variables de otro sistema. `--reset`, `--seed-demo` y `dotnet-ef database update` podían terminar apuntando a ese servidor.

**Hacer:**
1. Usar solo `ARQUITECTOS_DB_HOST`, `ARQUITECTOS_DB_NAME`, `ARQUITECTOS_DB_USER` y `ARQUITECTOS_DB_PASSWORD`. No leer variables de otro sistema.
2. Agregar `.env.example` (sin valores) en la raíz con las variables nuevas. Verificar que `.gitignore` ignora `.env` pero **no** `.env.example` (agregar `!.env.example`).
3. Guarda para comandos destructivos (`--reset`, `--seed-demo`, `--seed-plantilla`) en `Program.cs`:
   - Rechazar si `!IsDevelopment()` (ya debería; verificar).
   - Rechazar si `InitialCatalog` de la cadena efectiva **no** termina en `_DEV`, `_E2E`, `_TEST` o `_QA` (configurable en `Demo:SufijosPermitidos`).
   - Imprimir servidor y base (sin usuario ni clave) y exigir `--confirmar` para `--reset`.
   - `e2e.yml` usa `ARQUITECTOS_E2E`, que ya cumple; solo verificar que siga pasando. Actualizar el README.
4. `TrustServerCertificate` configurable (`Sql:ConfiarCertificado`, default `false` fuera de Development). (Cubre M4.)

**Aceptación:**
- Tests: `--reset` contra una cadena con `Database=ARQUITECTOS` termina con código ≠ 0 y no toca datos; con `ARQUITECTOS_DEV` procede.
- README sección "Base de datos local" documenta las variables nuevas y el login SQL dedicado.

> **Acción humana (no Cursor):** crear un login SQL exclusivo `arquitectos_app` con permisos solo sobre la base `ARQUITECTOS` (db_datareader, db_datawriter, EXECUTE; **sin db_owner**) y otro `arquitectos_migraciones` con db_ddladmin para aplicar scripts. Rotar la clave del login que no es exclusivo de esta base.

---

## Bloque 2 — 🟠 Altos (código)

### A1. Paginación real en SQL para el listado de actas

**Hoy:** `Infrastructure/Actas/ConsultasActa.cs` (≈ líneas 58–91) hace `ToListAsync()` de todas las actas, filtra por nombre y ordena en C#, calcula totales para **todos** los ids filtrados y luego `Skip/Take`.

**Hacer:**
- Filtros de texto en SQL, sin tildes ni mayúsculas:
  ```csharp
  const string Ci = "Latin1_General_CI_AI";
  if (arquitectoTexto.Length > 0)
      consulta = consulta.Where(a => EF.Functions.Like(
          EF.Functions.Collate(a.Arquitecto.Nombre, Ci), $"%{Escapar(arquitectoTexto)}%"));
  ```
  (`Escapar` escapa `%`, `_`, `[`.) Número de acta: `EF.Functions.Like(a.NumeroActa.ToString(), ...)` o igualdad si es numérico.
- Ordenamiento con `IQueryable` (`OrderBy/ThenBy` sobre columnas), desempate siempre por `NumeroActa`.
- `var total = await consulta.CountAsync(ct);` luego `Skip/Take` en SQL.
- Totales (`Sum(Subtotal)` de líneas aprobadas) **solo para los ids de la página**.
- En SQLite (tests) `Collate("Latin1_General_CI_AI")` no existe: encapsular en un método `FiltroTexto` que use `Collate` solo si `Database.IsSqlServer()`, y `ToLower()` en SQLite.
- Revisar con el mismo criterio los reportes (`InformesActa.cs`, `ReporteActas`, `ReporteMateriales`) y el uso de `listarTodo` en el frontend: `listarTodo` solo para catálogos pequeños (selectores); los reportes deben paginar o exportar del lado del servidor.

**Aceptación:**
- Test con 1.000 actas (SQLite): página 3 de 50 devuelve 50 filas, `Total = 1000`, orden estable.
- Test: filtro "lopez" encuentra "López" (en SQL Server; en SQLite al menos case-insensitive).
- Logging de EF en un test muestra `OFFSET ... FETCH` en la consulta del listado (o verificación equivalente con `ToQueryString()`).

### A2. Endurecer la importación de Excel

**Hoy:** `PresupuestoControllers.cs:350-361` acepta hasta 40 MB sin validar tipo ni contenido.

**Hacer:**
- `[RequestSizeLimit(10_000_000)]` y `[RequestFormLimits(MultipartBodyLengthLimit = 10_000_000)]`, configurable en `Presupuesto:TamanoMaximoImportacion`.
- Validar extensión `.xlsx` y firma ZIP (`50 4B 03 04`) leyendo los primeros 4 bytes.
- Antes de abrir con ClosedXML, inspeccionar el ZIP con `System.IO.Compression.ZipArchive`: rechazar si el tamaño descomprimido total > 100 MB o hay > 2.000 entradas (zip bomb).
- `LectorExcel`: máximo de filas por hoja (`Presupuesto:FilasMaximas`, 20.000) y de hojas (50); timeout con `CancellationTokenSource.CreateLinkedTokenSource(ct)` + `CancelAfter(60 s)`.
- Respuestas 400/413 con `ProblemDetails` claros.

**Aceptación:** tests para: archivo `.txt` renombrado a `.xlsx` → 400; ZIP con entrada de 200 MB descomprimidos → 400; 11 MB → 413; el fixture `Presupuesto_Casa_Murillo.xlsx` sigue importando igual.

### A5. Roles como políticas con nombre

**Hoy:** más de 20 `[Authorize(Roles = "Administrador")]` / `"Administrador,Arquitecto"` con texto literal.

**Hacer:**
- `Application/Auth/Perfiles.cs` con constantes (`Administrador`, `Arquitecto`, `Contratista`, `Revisor`) y `Politicas.cs` (`SoloAdministrador`, `GestionPresupuesto`, `TomaActas`, `RevisionActas`, `Tablero`).
- Registrar políticas en `Program.cs`; reemplazar todos los `Roles = "..."` por `Policy = Politicas.X`.
- Mantener la `FallbackPolicy` (autenticado) existente.

**Aceptación:** `grep -r 'Roles = "' backend/src` sin resultados. Test que recorre todos los controladores por reflexión y verifica que cada acción tiene `[Authorize(Policy=...)]`, `[Authorize]` o `[AllowAnonymous]` explícito y que las políticas existen.

---

## Bloque 3 — 🟡 Medios (código)

| Id | Tarea | Aceptación |
|---|---|---|
| M1 | Anti-CSRF por exclusión: exigir `X-Requested-With: arquitectos` en todo método que **no** sea GET/HEAD/OPTIONS/TRACE (`Program.cs:346`). Igual en `frontend/src/api/cliente.ts` (agregar el header a todo método no seguro, incluido `patch`). | Test: `PATCH` sin header → 400. `cliente.ts` cubre `post/put/patch/delete`. |
| M2 | Tipos del frontend generados desde OpenAPI: script `npm run api:tipos` con `openapi-typescript` contra `swagger/v1/swagger.json` (generado en build del backend con `Swashbuckle.AspNetCore.Cli` o un endpoint en test). Archivo `src/api/esquema.ts` versionado. Migrar progresivamente `api/*.ts` a esos tipos (empezar por actas y presupuesto). | Paso en CI que regenera y falla si `git diff --exit-code src/api/esquema.ts` tiene cambios. |
| M5 | `IMemoryCache` con `SizeLimit = 200` (`AddMemoryCache(o => o.SizeLimit = 200)`) y `Size = 1` en cada `Set` (`MetricasDashboard`, `SeriesDashboard`, `ValidadorSesion`). | Test: el servicio no lanza por falta de `Size`; entradas se expulsan al superar el límite. |
| M6 | Cobertura en CI: `dotnet test --collect:"XPlat Code Coverage"` + ReportGenerator; publicar resumen en el job. Umbral inicial 60 % en `Application` + `Infrastructure` (subir a 70 % después). Vitest con `--coverage` (`@vitest/coverage-v8`). | El CI muestra el porcentaje y falla bajo el umbral. |
| M7 | e2e corto en cada PR: job `e2e-humo` (login, listado de actas, abrir tablero) usando el mismo SQL Server de servicio del `e2e.yml`. El e2e completo sigue nocturno. | El PR muestra el check `e2e-humo`. |
| M8 | Terminar refactor del frontend: `ReporteActas.tsx`, `ReporteMateriales.tsx` y `presupuesto/EditorApu.tsx` en hook + componentes (mismo patrón que `TomaActas`/`EditorPresupuesto`). | Ningún archivo `.tsx` > 15 KB. Sin cambios de comportamiento (e2e en verde). |

---

## Bloque 4 — Repositorio y entrega (CI/CD)

| Id | Tarea | Aceptación |
|---|---|---|
| M3 | `.github/dependabot.yml` para `nuget` (`/backend`), `npm` (`/frontend`) y `github-actions`, semanal, agrupando minors/patches. Workflow `codeql.yml` (C# y JavaScript/TypeScript). | Archivos creados; CodeQL corre en PR y semanal. |
| A3 | Workflow `release.yml` (manual, `workflow_dispatch`, entrada `version`): `dotnet publish -c Release`, `npm run build`, `dotnet-ef migrations script --idempotent` desde la última migración aplicada en producción (entrada opcional) y empaquetar `api/`, `web/` y `migracion.sql` como artefacto `arquitectos-vX.Y.Z.zip`. **No** incluir `appsettings.Production.json` ni `web.config` del servidor. | Artefacto descargable; `unzip -l` no muestra `appsettings.Production.json`. |
| A4 | Documentar en `docs/OPERACION.md` el ambiente QA (`ARQUITECTOS_QA`, sitio `/Web-qa`) y el procedimiento: aplicar `migracion.sql` en QA → probar → aplicar en producción → copiar `api/` y `web/` con `app_offline.htm`. | Sección "Publicación" reescrita con checklist. |
| O1 | Versión visible: `<Version>` en `Directory.Build.props`, expuesta en `/api/health` (`version`, `commit`) y en el pie del `Layout`. `CHANGELOG.md` en la raíz. | `/api/health` devuelve la versión; el pie la muestra. |

> **Acción humana:** transferir el repo a la organización de Celuweb (privado), proteger `main` (PR obligatorio, CI + CodeQL requeridos, 1 aprobación), activar *secret scanning* y *push protection*, crear el secreto `E2E_CLAVE`.

---

## Bloque 5 — Operación

| Id | Tarea | Aceptación |
|---|---|---|
| O2 | `docs/OPERACION.md`: monitoreo de `/api/health/ready` cada 5 min (Application Insights availability test o UptimeRobot) y alerta por correo; alerta en Seq para `Level = Error` > 5 en 10 min. | Sección "Monitoreo" con pasos concretos. |
| O3 | Restauración mensual de prueba del backup sobre `ARQUITECTOS_QA`, con tabla de registro (fecha, backup usado, duración, resultado, responsable). | Tabla agregada en `docs/OPERACION.md`. |

---

## Orden de ejecución

1. Bloque 0 (commit: "Fase 3: cierre")
2. C1 → C2 (commit por cada uno)
3. A1 → A2 → A5
4. M1 → M5 → M8 → M2 → M6 → M7
5. M3 → A3 → A4 → O1
6. O2 → O3

## Fuera de alcance (no hacer)

- Consolidar migraciones en una sola (la base del sitio ya tiene datos).
- Cambiar el modelo de autenticación (cookies + refresh rotativo se mantiene).
- Ejecutar cualquier script contra la base `ARQUITECTOS` del sitio.
