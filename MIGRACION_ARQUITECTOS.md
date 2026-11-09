# Migración "Arquitectos": ASP.NET WebForms (VB.NET) → React + .NET 8 Web API + SQL Server

> Documento de contexto para **Cursor**. Léelo completo antes de generar código.
> Código anterior: `C:\Arquitectos\Codigo_anterior` (solo lectura, NO modificar).
> Código nuevo: `C:\Arquitectos\backend` y `C:\Arquitectos\frontend`.
> **La base de datos original se perdió**: el esquema de la sección 4 fue reconstruido a partir de los stored procedures, consultas SQL y columnas de grillas encontradas en el código. Se recrea con **EF Core Code-First + Migrations**.

---

## 1. Qué es el sistema anterior

| Aspecto | Valor |
|---|---|
| Tipo | ASP.NET WebForms, VB.NET, .NET Framework 2.0/3.5 (`System.Web.Extensions 1.0.61025`) |
| Publicación | **Sitio precompilado** (`PrecompiledApp.config`). El code-behind está en DLLs (`bin/App_Web_*.dll`, `bin/co.com.Arquitectos.dll`, `bin/App_Code.dll`). Solo hay 2 `.vb` fuente: `Administracion/AdminArquitectos.aspx.vb` y `AdminProveedoresMateriales.aspx.vb`. El resto se reconstruyó desensamblando IL. |
| UI | Bootstrap 3 (template "Gentelella"), Font Awesome 4, SweetAlert2, AjaxControlToolkit (CalendarExtender, ModalPopupExtender, FilteredTextBoxExtender), UpdatePanel |
| Datos | ADO.NET (`SqlCommand`) + `SqlDataSource` en markup, casi todo con **stored procedures** |
| Reporte impreso | Crystal Reports 13 (`Crystal/InformeActa.rpt`) con datasets `V_registrar_acta` y `V_registrar_acta_detail` |
| Exportación | GridView → `.xls` (HTML con content-type Excel) |
| Auth | Forms + `Session("usuario")`, `Session("tipo")`, `Session("codigousuario")` |
| Menú | Dinámico desde BD: SP `ObtenerOpcionesMenu(@tipousuario, @codigoUsuario)` → `MenuId, PadreId, descripcion, url, icono` |
| Idioma | Recursos `App_GlobalResources` (es-CO / en-US) — clase base `Culture` |
| Versión | `App_Data/AppConfig.xml` → WebVersion 2.0.0, PRODUCCION |

### 1.1 Dominio de negocio
Aplicación para **registrar actas de obra** (avance/cobro de obra): un **arquitecto**, un **revisor** y un **contratista** registran, para un periodo (fecha inicial–final), ítems ejecutados en una o varias **construcciones** (concepto, material, unidad de medida, cantidad, precio, descripción). El acta se numera, se imprime (PDF) y se consulta en un reporte por rango de fechas/arquitecto. Además se mantienen los catálogos y la lista de precios de materiales por proveedor.

---

## 2. Inventario de pantallas (alcance de la migración)

### 2.1 EN ALCANCE (núcleo "Arquitectos")

| Pantalla anterior | Función | Datos (SP / SQL) |
|---|---|---|
| `Index.aspx` | Login (usuario, password) | SP `validarusuario(@Usuario, @Clave, OUT @Resultado, OUT @tipousuario, OUT @codigousuario)` → si `Resultado='OK'` guarda sesión y redirige a `Principal/IndexPrincipal.aspx`; si no: "Usuario o contraseña invalidos." |
| `menu.master` | Layout con sidebar dinámico + "Salir" | SP `ObtenerOpcionesMenu(@tipousuario, @codigoUsuario)` (árbol por `PadreId`) |
| `logoff.aspx` | Cierra sesión → `index.aspx` | — |
| `Administracion/AdminArquitectos.aspx` | CRUD arquitectos (nombre). Valida duplicado por nombre | List `AdminArquitectosSelProc`; `select * from arquitectos where nombre=@codigo`; `IngresarArquitectoInsProc(@nombre)`; `ActualizarArquitectoUpdProc(@nombre,@codigo)`; `EliminarArquitectoDelProc(@codigo)` |
| `AdminRevisores.aspx` | CRUD revisores (nombre) | `AdminRevisoresSelProc`; `IngresarRevisoresInsProc(@nombre)`; `ActualizarRevisorUpdProc(@nombre,@codigo)`; `EliminarRevisorDelProc(@codigo)` |
| `AdminContratista.aspx` | CRUD contratistas (nombre). Valida duplicado | `AdminContratistasSelProc`; `IngresarContratistaInsProc(@nombre)`; `ActualizarContratistaUpdProc`; `EliminarContratistaDelProc` |
| `AdminProveedores.aspx` | CRUD proveedores (nombre) | `AdminProveedoresSelProc`; `IngresarProveedoresInsProc`; `ActualizarProveedorUpdProc`; `EliminarProveedorDelProc` |
| `AdminConstrucciones.aspx` | CRUD construcciones (descripción, propietario) | `AdminConstruccionesSelProc`; `IngresarConstruccionInsProc(@descripcion,@propietario)`; `ActualizarConstruccionesUpdProc(@descripcion,@propietario,@codigo)`; `EliminarConstruccionDelProc` |
| `AdminConceptos.aspx` | CRUD conceptos (descripción, unidad) | `AdminConceptosSelProc`; `IngresarConceptoInsProc(@descripcion,@unidad)`; `ActualizarConceptoUpdProc(@descripcion,@unidad,@codigo)`; `EliminarConceptoDelProc` |
| `AdminMateriales.aspx` | CRUD materiales (descripción, unidad) | `AdminMaterialesSelProc`; `IngresarMaterialInsProc(@nombre,@unidad)`; `ActualizarMaterialUpdProc(@descripcion,@unidad,@codigo)`; `EliminarMaterialDelProc` |
| `AdminUM.aspx` | CRUD unidades de medida (nombre, descripción) | `AdminUMSelProc`; `IngresarUMInsProc(@nombre,@descripcion)`; `ActualizarUMUpdProc`; `EliminarUMDelProc` |
| `AdminProveedoresMateriales.aspx` | Lista de precios: elegir proveedor → 2 grillas: materiales **agregados** (código, descripción, precio) y **sin agregar** (con textbox precio). Checkbox múltiple + Agregar/Quitar | `ddlProveedoresSelProc`; `MaterialesAgregadosSelProc(@proveedor)`; `Materiales_Sin_agregar_Selproc(@proveedor)`; INSERT/DELETE directo a `Lista_Materiales_Proveedores(codigoproducto, valor, codigoproveedor)` (**SQL concatenado → inyección SQL**) |
| `AdminRegistrarActa.aspx` | **Toma de actas** (pantalla principal, ver 3.1) | ddl: `ddlArquitectosSelProc`, `ddlRevisoresSelProc`, `ddlContratistasSelProc`, `ddlConstruccionSelProc`, `ddlConceptosSelProc`, `ddlUmSelProc`, `ddlMaterialesSelProc`; detalle temporal: `DetalleActasTempSelProc(@arquitecto,@revisor,@contratista)`, `IngresaActaTMP(...)`, `ModificaActaTMP(@id,...)`, `EliminarDetalleActaTMP(@codigo)`, `DeleteActaTMP(@arquitecto,@revisor,@contratista)`; cierre: `IngresaActa(@fechainicial,@fechafinal,@arquitecto,@revisor,@contratista,@construccion,@comentarios)`; número: `select top 1 numeroacta from Actas order by NumeroActa desc` |
| `Informes/ReporteActas.aspx` | Reporte de actas: filtros fecha inicial (default 1er día del mes), fecha final, arquitecto. Columnas: N° Acta, Fecha inicial, Fecha final, Contratista, Observación, Total (+ fila TOTAL general). Botón Imprimir por fila. Exportar Excel | `ListaActasSelProc(@fechainicial yyyyMMdd, @fechafinal yyyyMMdd, @arquitecto)` |
| `Informes/ImpresionActas.aspx?numerodoc=N` | Impresión del acta (Crystal → PDF) | `execute Select_registrar_acta 'N'` (encabezado) + `execute Select_registrar_actaDetail 'N'` (detalle) |

### 2.2 FUERA DE ALCANCE (legado del template de ventas Celuweb — confirmar con negocio)
Estas páginas existen pero pertenecen a otra aplicación (preventa/cartera) y referencian tablas que no son del dominio de arquitectos (`vendedores`, `referencias`, `promociondetalle`, `consecutivoped`...). **No migrar salvo que se confirme que se usan:**
`Reportes/CarteraDias, CarteraMes, PedidosRealizados, PedidosVendedor, Recaudo, RecaudosVendedor, ReporteCartera, ReporteHabeasData, ReportePortafolioClientes, ReportePQR, ReporteProductosCompetencia, ReporteRegVisita, iniciodia`, `Filtros/*.ascx`, `Reportes/AutoComplete.asmx`, web reference `UNO_ZYS_EE.WSUNOEE` (Siesa UNOEE), métodos de `App_Code/Informes` para consecutivos/promociones/jobs (`ArquitectosJOBS` → msdb), `AdminUsuariosBO` (tabla `usuariosNoel`, jerarquías de vendedores), `GeneralBO.fotoHabeasData`.

---

## 3. Reglas de negocio extraídas

### 3.1 Flujo "Toma de actas" (AdminRegistrarActa)
1. Usuario selecciona **Fecha inicial**, **Fecha final** (obligatorias), **Arquitecto**, **Revisor**, **Contratista** (valor `-1` = sin seleccionar → error "Debe seleccionar toda la información").
2. "Crear" abre un **modal "Datos de items"** que muestra arquitecto/revisor/contratista y permite agregar ítems:
   - Construcción (obligatoria), Concepto, UM, Precio (numérico, admite `,` `.`), Cantidad (numérico), Material, Descripción.
   - Validación: precio, cantidad, descripción y construcción obligatorios → "Debe ingresar toda la información".
   - El precio/cantidad se normaliza: `,` → `.`.
3. Ítems se guardan en una tabla **temporal** (`DetalleActasTMP`) identificada por la combinación arquitecto+revisor+contratista. Grilla "Actas agregadas": Registro(id), Concepto, UM, Cantidad, Precio, Descripción, Material, Construcción. Seleccionar fila → editar (Modificar) o Eliminar.
4. **Registrar** (con confirmación) → `IngresaActa` crea el encabezado en `Actas`, mueve el detalle temporal a `DetalleActas` y limpia el temporal. Luego obtiene el último `NumeroActa` y abre `ImpresionActas.aspx?numerodoc=N` en ventana nueva.
5. **Cancelar acta** (con confirmación "Toda la información registrada se perderá") → `DeleteActaTMP`.
6. Total del acta = Σ(cantidad × precio) de su detalle (columna `totalacta` del reporte).

### 3.2 Problemas del diseño anterior que la migración CORRIGE
- Detalle temporal por combinación de personas → colisiones entre usuarios concurrentes. **Nuevo:** el borrador vive en el estado del frontend (o tabla de borrador por usuario) y se envía **un solo POST** con encabezado + líneas, en **una transacción**.
- Número de acta con `select top 1 ... order by desc` → condición de carrera. **Nuevo:** `NumeroActa` IDENTITY o SEQUENCE, devuelto por el POST.
- Al seleccionar un ítem se buscaban códigos con `LIKE '%descripcion%'` → **Nuevo:** el detalle guarda y devuelve **IDs** + descripciones.
- SQL concatenado (inyección) en Proveedores-Materiales y helpers → **Nuevo:** EF Core / parámetros.
- Contraseñas en texto plano (`validarusuario` con `@Clave`) → **Nuevo:** hash BCrypt/ASP.NET Identity PasswordHasher.
- Credenciales SQL hardcodeadas en `web.config` y **dentro de la DLL** de `ImpresionActas` (Crystal) → **Nuevo:** `appsettings` + User Secrets / variables de entorno. ⚠️ **Rotar esas credenciales en el servidor anterior**, quedaron expuestas en el código.
- Los "verificaX" de Conceptos/Construcciones/Materiales/etc. consultaban por error la tabla `arquitectos` (copy-paste). **Nuevo:** validación de nombre duplicado por cada catálogo (índice único).
- Métodos llamados `elimimarUsuario` en todos los BO → nombres correctos.
- Typos en mensajes ("Finalizao", "acutalizado") → corregir.

---

## 4. Esquema de BD reconstruido (EF Core Code-First)

> Nombres en español, PascalCase. Tipos sugeridos; ajustar si aparece un backup.

```sql
-- Seguridad / menú
CREATE TABLE TiposUsuario (Id INT IDENTITY PRIMARY KEY, Nombre NVARCHAR(50) NOT NULL UNIQUE);
CREATE TABLE Usuarios (
  Id INT IDENTITY PRIMARY KEY,
  Usuario NVARCHAR(50) NOT NULL UNIQUE,
  Nombre NVARCHAR(150) NOT NULL,
  PasswordHash NVARCHAR(200) NOT NULL,
  TipoUsuarioId INT NOT NULL REFERENCES TiposUsuario(Id),
  Activo BIT NOT NULL DEFAULT 1
);
CREATE TABLE MenuOpciones (
  Id INT IDENTITY PRIMARY KEY,
  PadreId INT NULL REFERENCES MenuOpciones(Id),
  Descripcion NVARCHAR(100) NOT NULL,
  Url NVARCHAR(200) NULL,          -- ruta React, ej. /admin/arquitectos
  Icono NVARCHAR(50) NULL,         -- ej. 'fa fa-users' → mapear a lucide/react-icons
  Orden INT NOT NULL DEFAULT 0
);
CREATE TABLE MenuOpcionTipoUsuario (
  MenuOpcionId INT REFERENCES MenuOpciones(Id),
  TipoUsuarioId INT REFERENCES TiposUsuario(Id),
  PRIMARY KEY (MenuOpcionId, TipoUsuarioId)
);

-- Catálogos (anterior: codigo, nombre/descripcion)
CREATE TABLE Arquitectos   (Id INT IDENTITY PRIMARY KEY, Nombre NVARCHAR(150) NOT NULL UNIQUE, Activo BIT NOT NULL DEFAULT 1);
CREATE TABLE Revisores     (Id INT IDENTITY PRIMARY KEY, Nombre NVARCHAR(150) NOT NULL UNIQUE, Activo BIT NOT NULL DEFAULT 1);
CREATE TABLE Contratistas  (Id INT IDENTITY PRIMARY KEY, Nombre NVARCHAR(150) NOT NULL UNIQUE, Activo BIT NOT NULL DEFAULT 1);
CREATE TABLE Proveedores   (Id INT IDENTITY PRIMARY KEY, Nombre NVARCHAR(150) NOT NULL UNIQUE, Activo BIT NOT NULL DEFAULT 1);
CREATE TABLE Construcciones(Id INT IDENTITY PRIMARY KEY, Descripcion NVARCHAR(200) NOT NULL UNIQUE, Propietario NVARCHAR(150) NULL, Activo BIT NOT NULL DEFAULT 1);
CREATE TABLE UnidadesMedida(Id INT IDENTITY PRIMARY KEY, Nombre NVARCHAR(20) NOT NULL UNIQUE, Descripcion NVARCHAR(100) NULL);
CREATE TABLE Conceptos     (Id INT IDENTITY PRIMARY KEY, Codigo NVARCHAR(30) NULL, Descripcion NVARCHAR(200) NOT NULL UNIQUE, Unidad NVARCHAR(20) NULL);
CREATE TABLE Materiales    (Id INT IDENTITY PRIMARY KEY, Codigo NVARCHAR(30) NULL, Descripcion NVARCHAR(200) NOT NULL UNIQUE, Unidad NVARCHAR(20) NULL);

-- Lista de precios por proveedor (anterior: Lista_Materiales_Proveedores(codigoproducto, valor, codigoproveedor))
CREATE TABLE ProveedorMateriales (
  ProveedorId INT NOT NULL REFERENCES Proveedores(Id),
  MaterialId  INT NOT NULL REFERENCES Materiales(Id),
  Precio DECIMAL(18,2) NOT NULL CHECK (Precio > 0),
  PRIMARY KEY (ProveedorId, MaterialId)
);

-- Actas
CREATE TABLE Actas (
  NumeroActa INT IDENTITY PRIMARY KEY,
  FechaInicial DATE NOT NULL,
  FechaFinal DATE NOT NULL,
  ArquitectoId INT NOT NULL REFERENCES Arquitectos(Id),
  RevisorId INT NOT NULL REFERENCES Revisores(Id),
  ContratistaId INT NOT NULL REFERENCES Contratistas(Id),
  Observacion NVARCHAR(1000) NULL,
  FechaRegistro DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  UsuarioRegistroId INT NULL REFERENCES Usuarios(Id),
  CONSTRAINT CK_Actas_Fechas CHECK (FechaFinal >= FechaInicial)
);
CREATE TABLE ActaDetalles (
  Id INT IDENTITY PRIMARY KEY,
  NumeroActa INT NOT NULL REFERENCES Actas(NumeroActa) ON DELETE CASCADE,
  ConstruccionId INT NOT NULL REFERENCES Construcciones(Id),
  ConceptoId INT NOT NULL REFERENCES Conceptos(Id),
  UnidadMedidaId INT NOT NULL REFERENCES UnidadesMedida(Id),
  MaterialId INT NULL REFERENCES Materiales(Id),
  Cantidad DECIMAL(18,4) NOT NULL CHECK (Cantidad > 0),
  Precio DECIMAL(18,2) NOT NULL CHECK (Precio >= 0),
  Descripcion NVARCHAR(500) NOT NULL,
  Subtotal AS (Cantidad * Precio) PERSISTED
);
CREATE INDEX IX_Actas_Fecha_Arq ON Actas(FechaInicial, FechaFinal, ArquitectoId);
```

Seed mínimo (en `HasData` o script): TipoUsuario `Administrador`, usuario `admin` (password temporal hasheado), opciones de menú de la sección 6.

---

## 5. Arquitectura destino

```
C:\Arquitectos\
├─ Codigo_anterior\            (no tocar)
├─ backend\
│  ├─ Arquitectos.sln
│  ├─ src\Arquitectos.Api\            Controllers, Program.cs, auth JWT, Swagger
│  ├─ src\Arquitectos.Application\    DTOs, validadores (FluentValidation), servicios
│  ├─ src\Arquitectos.Domain\         Entidades
│  ├─ src\Arquitectos.Infrastructure\ DbContext EF Core, Migrations, PDF (QuestPDF), Excel (ClosedXML)
│  └─ tests\Arquitectos.Tests\        xUnit
└─ frontend\
   └─ (Vite + React 18 + TypeScript)
```

**Backend:** .NET 8, EF Core 8 (SqlServer), JWT Bearer, FluentValidation, QuestPDF (reemplaza Crystal), ClosedXML (reemplaza export .xls), Serilog, Swagger. Respuestas de error con `ProblemDetails`. CORS para `http://localhost:5173`.

**Frontend:** Vite + React + TypeScript, React Router, TanStack Query, Axios (interceptor JWT), React Hook Form + Zod, UI con Bootstrap 5 (react-bootstrap) para conservar el look o shadcn/ui; SweetAlert2 para mantener los mensajes ("¡Finalizado!", "¡Error!"). Layout: sidebar con menú dinámico + header con "Salir", colores del original (fondo `#23323c`).

---

## 6. Contrato de API

| Método | Ruta | Reemplaza |
|---|---|---|
| POST | `/api/auth/login` `{usuario,password}` → `{token, nombre, tipoUsuario}` | `validarusuario` |
| GET | `/api/menu` (según tipo del JWT) → árbol | `ObtenerOpcionesMenu` |
| GET/POST/PUT/DELETE | `/api/arquitectos[/{id}]` | Admin*SelProc / Ingresar* / Actualizar* / Eliminar* |
| idem | `/api/revisores`, `/api/contratistas`, `/api/proveedores`, `/api/construcciones`, `/api/conceptos`, `/api/materiales`, `/api/unidades-medida` | idem |
| GET | `/api/proveedores/{id}/materiales` → agregados (id, descripción, precio) | `MaterialesAgregadosSelProc` |
| GET | `/api/proveedores/{id}/materiales/disponibles` | `Materiales_Sin_agregar_Selproc` |
| POST | `/api/proveedores/{id}/materiales` `[{materialId, precio}]` | INSERT Lista_Materiales_Proveedores |
| DELETE | `/api/proveedores/{id}/materiales` `[materialId]` | DELETE Lista_Materiales_Proveedores |
| POST | `/api/actas` `{fechaInicial, fechaFinal, arquitectoId, revisorId, contratistaId, observacion, detalles:[{construccionId, conceptoId, unidadMedidaId, materialId, cantidad, precio, descripcion}]}` → `{numeroActa}` | IngresaActaTMP + IngresaActa |
| GET | `/api/actas?desde=&hasta=&arquitectoId=` → `[{numeroActa, fechaInicial, fechaFinal, contratista, observacion, total}]` + total general | `ListaActasSelProc` |
| GET | `/api/actas/{numero}` (encabezado + detalle con descripciones) | `Select_registrar_acta*` |
| GET | `/api/actas/{numero}/pdf` | Crystal `InformeActa.rpt` |
| GET | `/api/actas/export?desde=&hasta=&arquitectoId=` (.xlsx) | export .xls |

DELETE de catálogos: si tiene actas asociadas → 409 con mensaje claro (o soft delete con `Activo=0`).

### Rutas React y menú semilla
| Menú | Ruta |
|---|---|
| Administración › Arquitectos | `/admin/arquitectos` |
| Administración › Revisores | `/admin/revisores` |
| Administración › Contratistas | `/admin/contratistas` |
| Administración › Proveedores | `/admin/proveedores` |
| Administración › Construcciones | `/admin/construcciones` |
| Administración › Conceptos | `/admin/conceptos` |
| Administración › Materiales | `/admin/materiales` |
| Administración › Unidades de medida | `/admin/unidades-medida` |
| Administración › Materiales por proveedor | `/admin/proveedores-materiales` |
| Actas › Toma de actas | `/actas/nueva` |
| Informes › Reporte de actas | `/informes/actas` |

---

## 7. Especificación de pantallas React

**CRUD genérico** (reutilizable para los 8 catálogos): formulario arriba (campos del catálogo) con botones Crear / Modificar / Eliminar / Cancelar; grilla paginada abajo con columna "Seleccionar". Al seleccionar fila → carga el formulario, oculta "Crear", muestra "Modificar"/"Eliminar". Eliminar pide confirmación. Mensajes SweetAlert. Construir **un componente `CatalogoCrud` configurable** (campos, endpoint, columnas) en vez de 8 pantallas copiadas.

| Catálogo | Campos | Columnas grilla |
|---|---|---|
| Arquitectos / Revisores / Contratistas / Proveedores | Nombre* | Código, Nombre |
| Construcciones | Descripción*, Propietario | Código, Descripción, Propietario |
| Conceptos / Materiales | Descripción*, Unidad | Código, Descripción, Unidad |
| Unidades de medida | Nombre*, Descripción | Código, Nombre, Descripción |

**Materiales por proveedor:** select de proveedor → dos tablas con checkbox "seleccionar todos". Izquierda "Agregados" (Código, Descripción, Precio) + botón Quitar. Derecha "Sin agregar" (Código, Descripción, input Precio) + botón Agregar. Validar precio > 0 en los marcados ("Debe seleccionar valores superiores a cero").

**Toma de actas:** título "Toma de actas – Perfil Arquitecto". Encabezado: fecha inicial, fecha final (date pickers), selects Arquitecto/Revisor/Contratista. Botón "Agregar ítems" abre modal con Construcción, Concepto, UM, Precio, Cantidad, Material, Descripción y botones Agregar / Modificar / Eliminar / Cancelar. Grilla de ítems (estado local) con subtotal y total. Campo "Comentario". Botones **Registrar** (confirm → POST → muestra N° de acta → abre `/api/actas/{n}/pdf` en pestaña nueva), **Cancelar** (confirm → limpia borrador), **Imprimir** (visible tras registrar). Guardar borrador en `localStorage` para no perderlo al recargar.

**Reporte de actas:** filtros fecha inicial (default día 1 del mes actual), fecha final (hoy), arquitecto. Botones Consultar / Exportar Excel. Tabla: N° Acta, Fecha inicial, Fecha final, Contratista, Observación, Total (formato `$ #,##0`), fila final TOTAL. Acción "Imprimir" por fila → PDF.

**PDF del acta (QuestPDF):** encabezado (N° acta, fechas, arquitecto, revisor, contratista, observación, fecha de registro), tabla de detalle agrupada por construcción (concepto, material, UM, cantidad, precio, subtotal), total general, espacio de firmas Arquitecto / Revisor / Contratista. Título "Impresión de Acta No. {n}". *(El layout exacto del .rpt no es legible: está comprimido; si se necesita fidelidad, abrir `InformeActa.rpt` en Crystal y ajustar.)*

---

## 8. Plan de trabajo para Cursor (ejecutar en orden, un prompt por fase)

1. **Backend – scaffolding**: crear solución en `C:\Arquitectos\backend` con los 4 proyectos + tests; paquetes NuGet; `appsettings.json` con `ConnectionStrings:Arquitectos` (placeholder, sin credenciales reales) y `Jwt`.
2. **Dominio + EF Core**: entidades y `ArquitectosDbContext` según sección 4 (índices únicos, decimales, cascade), seed, `dotnet ef migrations add Inicial` y `dotnet ef database update`.
3. **Auth + menú**: login JWT con PasswordHasher, endpoint `/api/menu`, política `[Authorize]` global.
4. **Catálogos**: controlador/servicio genérico para los 8 catálogos + validación de duplicados + 409 en eliminación con dependencias.
5. **Proveedor–materiales**: endpoints de la sección 6.
6. **Actas**: POST transaccional, consulta, reporte, export Excel, PDF.
7. **Tests**: xUnit para servicio de actas (total, validaciones de fechas y detalle vacío) con EF InMemory/SQLite.
8. **Frontend – scaffolding**: `npm create vite@latest frontend -- --template react-ts`, dependencias, layout, login, guard de rutas, cliente Axios.
9. **Frontend – catálogos** con `CatalogoCrud`.
10. **Frontend – proveedor-materiales, toma de actas, reporte**.
11. **Verificación**: probar flujo completo login → crear catálogos → registrar acta → PDF → reporte → Excel.

### Convenciones
- Código y nombres de dominio en **español** (Arquitecto, Acta, Construccion…); infraestructura en inglés estándar (Controller, Service, Repository).
- Nada de SQL concatenado. Nada de credenciales en el repo (`dotnet user-secrets`).
- Fechas: `DateOnly` en API, `yyyy-MM-dd` en JSON; cultura es-CO para mostrar números/moneda.
- Mensajes de usuario iguales al sistema original pero sin errores ortográficos.

---

## 9. Referencia rápida de archivos anteriores
- Lógica de datos (desensamblada): `bin/co.com.Arquitectos.dll` → `co.com.Arquitectos.BussinesObject.*BO`, `DataBase.Conexion`, `Login`.
- Code-behind de páginas: `bin/App_Web_d4eebhyk.dll` (Administración), `App_Web_wpwkx5nf.dll` (ReporteActas), `App_Web_ypghtelr.dll` (ImpresionActas), `App_Web_qvliwonv.dll` (Index, menu).
- Helpers: `bin/App_Code.dll` → `Informes`, `Culture`, `AutoComplete`.
- Markup con SPs y columnas: `Administracion/*.aspx`, `Informes/ReporteActas.aspx`.
- `ARQUITECTOS.rar` es una copia del mismo sitio publicado (no contiene fuentes adicionales). La subcarpeta `Codigo_anterior/Arquitectos/` es una versión anterior duplicada del sitio.
