*"Documento histórico; el estado vigente está en docs/ARQUITECTURA.md"*

# Auditoría de desarrollo – Plan de mejoras

> Documento para **Cursor**. Sale de la auditoría del 12-nov-2026 sobre `C:\Arquitectos` (backend .NET 8, frontend React 19, 84 tests).
> **No cambia funcionalidad de negocio**: endurece seguridad, plataforma, operación y mantenibilidad.
> Reglas:
> 1. Ejecutar **una tarea por prompt**, en el orden indicado.
> 2. Al cerrar cada tarea: `dotnet test`, `dotnet-ef migrations has-pending-model-changes`, `npm run build` y `npm run lint`, todo en verde.
> 3. Toda migración se genera con `dotnet-ef migrations add` (herramienta global, porque `dotnet ef` falla dentro de `backend`). **Nunca a mano.**
> 4. Las tareas marcadas **👤 Acción humana** las ejecuta el usuario. Cursor solo prepara lo necesario y documenta.

---

## Fase 0 – Esta semana (bloqueantes)

### T0.1 🔴 Secretos fuera del repositorio
**Problema.** `appsettings.Production.json` tiene la cadena de conexión y la clave JWT en texto plano, y se copia a `backend\publish\` y a `backend\publish\Api.zip`, que no están en `.gitignore`.

**Cursor:**
1. Agregar a `C:\Arquitectos\.gitignore`:
   ```
   **/publish/
   *.zip
   ```
2. Hacer que `appsettings.Production.json` **no se publique**: `<Content Update="appsettings.Production.json" CopyToPublishDirectory="Never" />` en `Arquitectos.Api.csproj`.
3. Crear `appsettings.Production.example.json` con las mismas claves y valores vacíos (`""`), y documentar en el README que en el servidor los secretos van como **variables de entorno de IIS**: `ConnectionStrings__Arquitectos` y `Jwt__Key`.
4. Al iniciar la API en Production: si `Jwt:Key` mide menos de 32 caracteres o la cadena de conexión está vacía, **detener el arranque** con un mensaje claro (ya existe para la clave; agregarlo para la cadena).

**👤 Acción humana:**
- `git log --all --oneline -- backend/publish "*.zip" "**/appsettings.Production.json"`. Si aparece algo, los secretos ya se filtraron: limpiar el historial (`git filter-repo`) y rotar.
- **Rotar** la contraseña del usuario de SQL Server y generar una **nueva clave JWT** de 64 caracteres o más.
- Rotar también las credenciales que tenía el sistema VB.NET anterior (`Codigo_anterior\web.config` y la DLL de ImpresionActas).
- Borrar `backend\publish\Api.zip` del disco si ya no se usa.

**Aceptación:** `git status` no muestra `publish/` ni archivos `.zip`; la carpeta `publish/` no contiene `appsettings.Production.json`; la API no arranca en Production sin variables de entorno.

### T0.2 🔴 Licencia de QuestPDF
**👤 Acción humana:** verificar en el sitio de QuestPDF los términos vigentes de la licencia *Community* (según lo conocido, es gratuita solo por debajo de cierto umbral de ingresos anuales). Si el cliente o Celuweb lo superan, adquirir la licencia comercial.
**Cursor:** mover el tipo de licencia a configuración, `"QuestPdf": { "Licencia": "Community" }`, leído en `DependencyInjection.cs` (valores: `Community`, `Professional`, `Enterprise`), con un comentario que remita a esta tarea.

### T0.3 🟠 Protección del login
**Cursor:**
```csharp
// Program.cs
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("login", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) }));
});
app.UseRateLimiter();
// AuthController.Login → [EnableRateLimiting("login")]
```
- **Bloqueo de cuenta**: columnas `Usuarios.IntentosFallidos int` y `Usuarios.BloqueadoHasta datetime2 null`, generadas con una migración `SeguridadLogin`. Tras 5 fallos se bloquea 15 minutos, y un login exitoso pone el contador en 0. El mensaje es siempre el genérico *"Usuario o contraseña inválidos"*, para no revelar si el usuario existe.
- **Cambio obligatorio de contraseña**: columna `Usuarios.DebeCambiarContrasena bit`. Vale `true` para el `admin` sembrado y para los usuarios creados por el administrador. El frontend redirige a `/cuenta/contrasena` y bloquea el resto de rutas hasta cambiarla.
- Eliminar la contraseña del archivo `Arquitectos.Api.http` y usar una variable `@password = {{$dotenv ADMIN_PASSWORD}}`.
- **Política de contraseña**: mínimo 10 caracteres, letras y números, y no puede ser igual al usuario. Rechazar una lista de 50 contraseñas comunes (archivo embebido).

**Tests:**
- La sexta petición de login en un minuto → 429.
- 5 fallos → la cuenta queda bloqueada, y un sexto intento con la contraseña correcta → 401.
- Login exitoso → contador en 0.
- `DebeCambiarContrasena` aparece en la respuesta del login.

### T0.4 🟠 Respaldos de la base de datos
**👤 Acción humana** (la base original ya se perdió una vez): configurar en SQL Server un **backup completo diario + logs cada 15 minutos** (modelo de recuperación FULL), con copia fuera del servidor. **Probar una restauración** en otra base.
**Cursor:** crear `docs\OPERACION.md` con el script T-SQL de los respaldos (Maintenance Plan o `BACKUP DATABASE … WITH COMPRESSION, CHECKSUM`) y el procedimiento de restauración paso a paso.

---

## Fase 1 – 30 días

### T1.1 🔴 Migrar a .NET 10 (LTS)
**Problema.** El soporte de .NET 8 terminó el 10-nov-2026.
1. `global.json` → SDK 10.0.x con `rollForward: latestFeature`.
2. `TargetFramework` → `net10.0` en los 5 proyectos.
3. Paquetes Microsoft.* y EF Core → 10.0.x. `dotnet-ef` global → 10.0.x, y actualizar `.config\dotnet-tools.json`.
4. Swashbuckle → la versión compatible con .NET 10, **o** reemplazarlo por `Microsoft.AspNetCore.OpenApi` + Scalar. Mantener la autenticación Bearer en la UI de documentación.
5. Revisar los cambios incompatibles de EF Core 9 y 10 que afecten: comportamiento de `HasData`, `Contains` sobre colecciones, mapeo de `DateOnly` y la advertencia de cambios de modelo pendientes, que ahora es un error.
6. Generar una migración vacía `ActualizarNet10` **solo si** `has-pending-model-changes` reporta diferencias por la nueva versión.

**Aceptación:** los 84 tests o más en verde con `net10.0`; `dotnet list package --outdated` sin paquetes Microsoft en 8.x; la API arranca y Swagger o Scalar funciona.

### T1.2 🟠 Sesión segura (cookie HttpOnly + renovación)
- **Token de acceso de 20 minutos** en una cookie `HttpOnly; Secure; SameSite=Strict; Path=/api`. En `JwtBearerEvents.OnMessageReceived`, leer el token desde la cookie.
- **Token de renovación** (aleatorio de 64 bytes) guardado **con hash** en una tabla nueva `SesionesUsuario` (UsuarioId, HashToken, Expira (7 días), CreadoEn, RevocadoEn, Ip, UserAgent), en una cookie `HttpOnly` en `Path=/api/auth/renovar`. Se rota en cada uso; si se reutiliza uno ya rotado, se revocan todas las sesiones del usuario.
- Endpoints `POST /api/auth/renovar` y `POST /api/auth/salir` (revoca la sesión y borra las cookies).
- **Usuario activo**: un evento `OnTokenValidated` verifica que el usuario siga activo y no bloqueado, con `IMemoryCache` de 60 s. Al desactivar un usuario, revocar sus sesiones e invalidar la caché.
- **CSRF**: con `SameSite=Strict` y API del mismo origen basta. Además, exigir la cabecera `X-Requested-With: arquitectos` en POST, PUT y DELETE.
- Frontend: quitar el token de `localStorage`; en `api/sesion.ts` guardar solo datos de presentación (nombre, perfil). Cliente Axios con `withCredentials: true` y un interceptor que, ante un 401, llame una vez a `/api/auth/renovar` y repita la petición; si falla, va a `/login`.

**Tests:** renovación con rotación; reutilizar un token de renovación revoca todo; un usuario desactivado recibe 401 en la siguiente petición (después de la caché); salir revoca la sesión.

### T1.3 🟠 Cabeceras de seguridad y HSTS
```csharp
if (!app.Environment.IsDevelopment()) app.UseHsts();
app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["X-Frame-Options"] = "DENY";
    h["Referrer-Policy"] = "strict-origin-when-cross-origin";
    h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    h["Content-Security-Policy"] = "default-src 'self'; img-src 'self' data: blob:; style-src 'self' 'unsafe-inline'; script-src 'self'; frame-ancestors 'none'";
    await next();
});
```
- **CORS por entorno**: `"Cors": { "Origenes": ["http://localhost:5173"] }` en Development. En Production, lista vacía si el frontend se sirve desde el mismo origen (documentarlo en el README).
- Verificar que el PDF abierto en una pestaña nueva (`blob:`) siga funcionando con la CSP.

**Test:** un test de integración (`WebApplicationFactory`) verifica las cabeceras en `/api/health`.

### T1.4 🟠 Rastro de auditoría
- Entidad `Auditoria` (Id bigint, Fecha UTC, UsuarioId, Entidad, EntidadId, Accion [Crear | Modificar | Eliminar | Aprobar | Rechazar | Cerrar | Reabrir | Importar], CambiosJson nvarchar(max), Ip). Migración `Auditoria`. Índices `(Entidad, EntidadId)` y `(Fecha)`.
- `AuditoriaInterceptor : SaveChangesInterceptor`, registrado en `AddDbContext`:
  - Audita las entidades `Acta`, `ActaDetalle`, `Presupuesto`, `PresupuestoItem`, `Insumo` (precio), `Usuario` (sin hash de contraseña), `ProveedorMaterial` y `Cuadrilla`/`Cargo` (salario).
  - `CambiosJson` guarda `{campo: [antes, después]}` solo con lo que cambió.
  - **Excluir** `PasswordHash`, `RowVersion` y los tokens.
  - El usuario se obtiene con `IHttpContextAccessor` (claim `sub`).
- Acciones semánticas (aprobar o rechazar un ítem, cerrar un acta, aprobar un presupuesto) se registran explícitamente desde el servicio con `IAuditoria.RegistrarAsync(...)`.
- API `GET /api/auditoria?entidad=&entidadId=&desde=&hasta=&usuarioId=` (solo el administrador, paginada).
- UI: pestaña **"Historial"** en el detalle del acta y del presupuesto (línea de tiempo), y una página `/admin/auditoria`.

**Tests:** modificar un acta crea un registro con el campo cambiado; aprobar un ítem registra la acción `Aprobar`; `PasswordHash` nunca aparece en `CambiosJson`.

### T1.5 🟠 Concurrencia en actas
- `Acta.RowVersion` (`[Timestamp]`), con migración `ConcurrenciaActas`.
- Los PUT del acta (editar, revisar, cerrar o reabrir) reciben `rowVersion` y responden **409** si no coincide, igual que el presupuesto.
- `TomaActas.tsx`: enviar `rowVersion`. Ante un 409, mostrar el aviso *"Otra persona modificó esta acta"* con un botón **Recargar**, conservando lo digitado para copiarlo.

**Test:** dos actualizaciones con el mismo `rowVersion` → la segunda devuelve 409.

### T1.6 🟡 Integración continua
`.github/workflows/ci.yml` (o el equivalente en Azure DevOps):
```yaml
name: ci
on: [push, pull_request]
jobs:
  backend:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { global-json-file: backend/global.json }
      - run: dotnet tool install --global dotnet-ef
      - run: dotnet test backend --configuration Release
      - run: dotnet-ef migrations has-pending-model-changes -p backend/src/Arquitectos.Infrastructure -s backend/src/Arquitectos.Api
      - run: dotnet list backend package --vulnerable --include-transitive
  frontend:
    runs-on: ubuntu-latest
    defaults: { run: { working-directory: frontend } }
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version: 22, cache: npm, cache-dependency-path: frontend/package-lock.json }
      - run: npm ci
      - run: npm run lint
      - run: npm run build
      - run: npm test --if-present
      - run: npm audit --audit-level=high
```
Si `has-pending-model-changes` necesita una conexión a la base, usar `ArquitectosDbContextFactory` con una cadena ficticia (no se conecta).

---

## Fase 2 – 60 días

### T2.1 🟡 Observabilidad
- **Serilog**: además de consola, un archivo con rotación diaria y 30 días de retención (`logs/arquitectos-.log`) en formato JSON compacto (`Serilog.Formatting.Compact`). Agregar `Enrich.WithCorrelationId` (cabecera `X-Correlation-Id`, que se genera si no viene y se devuelve en la respuesta) y `UsuarioId`. Opcional: sink a Seq o Application Insights según `appsettings`.
- **No registrar** cuerpos de peticiones, contraseñas ni tokens.
- **Health checks**: `AddHealthChecks().AddSqlServer(...)` (paquete `AspNetCore.HealthChecks.SqlServer`) en `/api/health/ready`; `/api/health` queda como verificación de vida. Ambos anónimos, sin detalles internos.
- `GlobalExceptionHandler`: incluir `correlationId` en el ProblemDetails, para que el usuario lo reporte.

### T2.2 🟡 Pruebas de frontend y E2E
- **Vitest**: `npm i -D vitest`, con script `"test": "vitest run"`.
  - `src/presupuesto/calculadoraApu.test.ts` con **los mismos casos que `CalculadoraApuTests.cs`**: los 4 modos, divisores, herramienta menor y sub-APU anidado.
  - Agregar un caso con el APU "Concreto Zapatas" del fixture JSON, que debe coincidir con C# con tolerancia de $1. Agregar también ese caso en C# (pendiente de C9).
  - Tests de `actas/formato.ts` y `dashboard/filtroUrl.ts`.
- **Playwright** (ya instalado en el entorno) en `frontend/e2e/`, contra la API con una base de prueba que se crea con `--reset --seed-demo`:
  1. Login → crear acta → revisor aprueba o rechaza ítems → administrador cierra → PDF.
  2. Nuevo presupuesto desde plantilla → cantidades → aprobar → seguimiento.
  3. Importar `tests\Fixtures\Presupuesto_Casa_Murillo.xlsx` → el informe muestra 116 de 122 APU reproducidos.

  Se ejecuta como un job aparte en CI (manual o nocturno).

### T2.3 🟡 Consolidar migraciones (antes del primer despliegue a producción)
**👤 Confirmar antes** que no hay una base de producción con datos reales; si la hay, **no** hacer esta tarea y pasar a T2.4.
1. Mover las migraciones actuales a `Migrations\_historico\` (excluidas de la compilación) y borrar `backend\scripts\*.sql` (quedan en git).
2. `dotnet-ef migrations add Baseline` desde el modelo actual.
3. Para las bases de desarrollo existentes, generar un script que vacíe `__EFMigrationsHistory` e inserte `Baseline`, documentado en `docs\OPERACION.md`.
4. El despliegue se hace con `dotnet-ef migrations script --idempotent -o deploy.sql`, revisado y ejecutado por el DBA.

### T2.4 🟡 Datos de prueba únicos y documentación vigente
- **Datos de prueba**: dejar **una sola fuente**, `SemillaDemo.cs` (`--seed-demo`), con los **mismos casos** que `datos_demo\generar_datos_demo.py`: 152 actas en 24 meses, las anomalías A1–A5 sembradas y las obras sin actividad. El esquema actual usa **Insumos en lugar de Materiales**.
  - Test de aceptación: después de `--seed-demo`, el dashboard de los últimos 12 meses cumple la tabla de §5.6 de `DASHBOARD_KPIS.md` (2 pares A3 exactos, A4 = Hotel Salento + Clínica Calarcá, y montos con ±2 %).
  - Marcar `datos_demo\datos_demo.sql` como obsoleto (README en la carpeta) o eliminarlo.
- **Documentación**:
  - `README.md` en la raíz: requisitos, configuración de secretos, `dotnet-ef database update`, `--seed-demo`, cómo ejecutar el backend y el frontend, tests, publicación en IIS.
  - `docs\ARQUITECTURA.md`: capas, módulos, modelo de datos actual (con Insumos unificados), perfiles y permisos, y reglas de cálculo.
  - Mover `MIGRACION_ARQUITECTOS.md`, `DASHBOARD_KPIS.md`, `MODULO_PRESUPUESTO*.md`, `CORRECCIONES_PRESUPUESTO.md` y este archivo a `docs\historico\`, con una línea al inicio: *"Documento histórico; el estado vigente está en docs/ARQUITECTURA.md"*.

---

## Fase 3 – 90 días

### T3.1 🟡 Refactorización por casos de uso
**Objetivo:** ningún archivo de servicio por encima de ~600 líneas, y las reglas puras fuera de Infrastructure.
- Backend:
  - `PresupuestoServicio` (74 KB) → `CrearPresupuesto`, `EditarPresupuesto`, `AprobarPresupuesto`, `VersionarPresupuesto`, `RecalcularPresupuesto`, `ValidarPresupuesto`, `SeguimientoPresupuesto` y `ConsultasPresupuesto`.
  - `ActaServicio` (47 KB) → `CrearActa`, `EditarActa`, `RevisarActa`, `CerrarActa`, `ConsultasActa` e `InformesActa`.
  - `DashboardServicio` → `MetricasDashboard`, `AlertasDashboard` y `SeriesDashboard`.
  - Reglas puras (validaciones de presupuesto, permisos por perfil, cálculo de totales) → `Arquitectos.Application`, sin EF, siguiendo el ejemplo de `CalculadoraApu` y `ReglasAlertas`.
  - **Sin cambiar contratos HTTP**: los tests existentes deben pasar sin modificaciones, salvo los namespaces.
- Frontend:
  - `EditorPresupuesto.tsx` → `useEditorPresupuesto` (estado + autoguardado + 409), `CapituloPresupuesto.tsx`, `FilaItem.tsx`, `ResumenCostos.tsx` y `ModalMover.tsx`.
  - `TomaActas.tsx` → `useBorradorActa`, `EncabezadoActa.tsx`, `TablaItemsActa.tsx` y `ModalItemActa.tsx`.
  - Corregir de paso los avisos del linter (`set-state-in-effect`, `exhaustive-deps`, `purity` con `Date.now`).

### T3.2 🔵 Rendimiento
- Paginación en el servidor (`?pagina=&tamano=`, máximo 200) en los listados de actas, insumos, APU, usuarios y auditoría, con la respuesta `{ items, total, pagina, tamano }`.
- Dashboard A1 y A3: medir con `--seed-demo` ×10 (≈1.500 actas). Si `GET /api/dashboard` tarda más de 1,5 s, mover A1 a SQL (`OUTER APPLY` con promedio por ventana) y A3 a un self-join en la base, como ya sugería `DASHBOARD_KPIS.md` §5.4.
- Índices sugeridos por el plan de ejecución de las consultas del reporte por materiales y del seguimiento.

### T3.3 🔵 Mantenimiento menor
- Font Awesome 4.7 → **lucide-react**, reemplazando los íconos del menú y guardando en `MenuOpciones.Icono` el nombre de Lucide. Migración de datos para los Id 1–23.
- `npm outdated` y `dotnet list package --outdated`: actualizar las versiones menores y de parches. Documentar cualquier paquete que quede fijo y por qué.
- Agregar `Directory.Packages.props` (gestión central de versiones NuGet).

---

## Resumen de verificación final
```powershell
cd C:\Arquitectos\backend
dotnet test
dotnet-ef migrations has-pending-model-changes -p src\Arquitectos.Infrastructure -s src\Arquitectos.Api
dotnet list package --vulnerable --include-transitive
cd ..\frontend
npm run lint; npm run build; npm test; npx playwright test
```
Además: el pipeline de CI en verde; las cabeceras de seguridad visibles en `/api/health`; el login bloquea tras 5 intentos; el historial de auditoría visible en un acta; `git status` limpio, sin `publish/` ni archivos `.zip`.
