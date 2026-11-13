# Arquitectos

Actas de obra, presupuestos y tablero. Backend en .NET 10 (`backend/global.json`, SDK 10.0.404) y frontend en React. El modelo y las reglas están en `docs/ARQUITECTURA.md`. La operación de respaldos está en `docs/OPERACION.md`.

## Requisitos

- SDK de .NET indicado en `backend/global.json`.
- Node.js 22 o superior, para el frontend.
- SQL Server, para ejecutar la API contra una base real. Las pruebas de backend usan SQLite en memoria y no necesitan el servidor.
- Herramienta global, para migraciones: `dotnet tool install --global dotnet-ef --version 10.0.15`.

La cadena de conexión y la clave JWT no van en el repositorio. En Development se leen de user-secrets, del archivo `.env` o de variables de entorno. En producción, de variables de entorno de IIS.

## Base de datos local

Copia `.env.example` a `.env` en la raíz del repositorio. Git ignora `.env` y sigue versionando el ejemplo.

| Variable | Uso |
| --- | --- |
| `ARQUITECTOS_DB_HOST` | Servidor SQL |
| `ARQUITECTOS_DB_NAME` | Base del proyecto: `ARQUITECTOS`. Los comandos de demostración exigen un nombre que termine en `_DEV`, `_E2E`, `_TEST` o `_QA` |
| `ARQUITECTOS_DB_USER` | Login de la aplicación |
| `ARQUITECTOS_DB_PASSWORD` | Clave de ese login |

Esas variables solo se leen en Development. Si también hay user-secrets o la variable `ConnectionStrings__Arquitectos`, esas ganan.

El login de la aplicación es `arquitectos_app`, con `db_datareader`, `db_datawriter` y `EXECUTE` sobre su base, sin `db_owner`. Las migraciones usan otro login, `arquitectos_migraciones`, con `db_ddladmin`. Esos logins se crean en SQL Server; no están en el repositorio.

`Sql:ConfiarCertificado` controla `TrustServerCertificate`. En Development, `appsettings.Development.json` lo deja en `true`. Fuera de Development, si la clave no está definida, el valor por defecto es `false`, salvo que la propia cadena ya lo indique.

## Ejecutar

En una terminal, la API (queda en `http://localhost:5190`):

```
dotnet run --project backend/src/Arquitectos.Api --launch-profile http
```

En otra, el frontend (queda en `http://localhost:5173` y proxifica `/api` a la API):

```
cd frontend
npm ci
npm run dev
```

## Migraciones

Se agregan con la herramienta global, no a mano:

```
dotnet-ef migrations add Nombre --project backend/src/Arquitectos.Infrastructure --startup-project backend/src/Arquitectos.Api --configuration Release
```

Para aplicarlas a la base de la cadena configurada:

```
dotnet-ef database update --project backend/src/Arquitectos.Infrastructure --startup-project backend/src/Arquitectos.Api --configuration Release
```

Ese comando modifica la base a la que apunta la cadena. No lo uses contra la base del sitio sin un script revisado. La base en uso ya tiene datos, así que las migraciones siguen siendo incrementales: no se reemplazan por una sola migración inicial.

## Datos de demostración

Solo en Development, y solo contra una base de prueba:

```
dotnet run --project backend/src/Arquitectos.Api --launch-profile http -- --seed-demo
```

Si ya hay actas, no inserta nada. `--reset --confirmar --seed-demo` vacía las tablas de negocio, restaura el usuario admin y el menú, y carga la demostración. Sin `--confirmar`, `--reset` termina con error y no toca la base. `--seed-plantilla` carga la plantilla «Vivienda unifamiliar». Los tres comandos solo corren en Development y solo si el nombre de la base termina en `_DEV`, `_E2E`, `_TEST` o `_QA` (`Demo:SufijosPermitidos`). Antes de escribir, imprimen el servidor y la base, sin usuario ni clave. `datos_demo/datos_demo.sql` está obsoleto; el motivo está en `datos_demo/README.md`.

## Pruebas

```
dotnet test backend --configuration Release
cd frontend
npm test
npm run build
npm run lint
```

`npm run test:e2e` es el recorrido completo de Playwright y corre de noche. Necesita `E2E_CLAVE` y una base de prueba creada con `--reset --confirmar --seed-demo` y `--seed-plantilla`. `npm run test:e2e:humo` (entrar, listado de actas y tablero) corre en cada pull request, con la misma clave y `--seed-demo`. No los ejecutes contra la base del sitio.

## Secretos en el servidor

`appsettings.Production.json` no se publica y no va en el repositorio. El ejemplo vacío está en `backend/src/Arquitectos.Api/appsettings.Production.example.json`.

En IIS, el grupo de aplicaciones de la API debe tener estas variables de entorno:

| Variable | Contenido |
| --- | --- |
| `ConnectionStrings__Arquitectos` | Cadena de conexión de SQL Server |
| `Jwt__Key` | Clave de firma del token, de 64 caracteres o más |

Si falta la cadena o la clave tiene menos de 32 caracteres, la API no arranca.

Hoy el sitio todavía lee el `appsettings.Production.json` que ya está en el servidor. No lo borres hasta crear esas variables: sin el archivo y sin las variables, la API se detiene.

## CORS

En Development, `Cors:Origenes` permite `http://localhost:5173`. En Production la lista queda vacía: el frontend se publica en el mismo sitio (`/Web`) y llama a `/api` en ese mismo origen, así que el navegador no exige CORS. Si el frontend pasa a otro dominio, hay que agregar ese origen a la lista.

## Licencia de QuestPDF

La API lee `QuestPdf:Licencia` en `appsettings.json`. Hoy vale `Community`. Hay que confirmar en el sitio de QuestPDF que esa licencia sigue siendo gratuita para los ingresos de Celuweb o del cliente. Si superan el umbral, cambiar el valor a `Professional` o `Enterprise` y adquirir la licencia comercial.

## Publicación en IIS

No reemplazar un `web.config` del servidor que ya tenga la cadena de conexión.

1. `dotnet publish backend/src/Arquitectos.Api -c Release`.
2. `npm run build` en `frontend`. El build usa la base `/Web/`.
3. Copiar la API y el contenido de `frontend/dist` al sitio, sin borrar lo que ya está y sin pisar los dos `web.config`.
4. Reciclar el grupo de aplicaciones con un `app_offline.htm` breve y luego borrarlo.

El sitio publicado sigue en el hosting anterior hasta que se haga este copiado a propósito. El código local posterior a ese copiado no está en el servidor.

## Pendiente de operación

- Revisar si `backend/publish`, algún `.zip` o `appsettings.Production.json` llegaron a git. Si sí, limpiar el historial y rotar los secretos.
- Rotar la contraseña de SQL Server y generar una clave JWT nueva.
- Rotar las credenciales del sistema VB.NET anterior (`Codigo_anterior`).
- Borrar `backend\publish\Api.zip` si ya no se usa.
- Configurar el respaldo diario y el registro cada 15 minutos, y probar una restauración. El procedimiento está en `docs/OPERACION.md`.
