# App móvil (`movil/`)

Flutter (Android e iOS) para perfiles **Arquitecto, Contratista y Revisor**. La app trabaja **offline** contra una base SQLite cifrada; el servidor es autoritativo tras cada sincronización.

## Arquitectura

```
movil/lib/
  app/              Configuración, router, controlador de sesión
  core/             API (Dio), DB (SQLCipher), errores, dinero
  sincronizacion/   Cola (outbox), sincronizador, workmanager (Android)
  funciones/        Pantallas: auth, actas, presupuestos, pendientes, cuenta
assets/
  esquema_movil_v1.sql       Copia del generador de paquete en el backend
  esquema_pendientes_v1.sql  Operaciones fallidas (sobreviven al reemplazo del paquete)
```

- **Paquete:** `GET /api/movil/v1/paquete` → gzip + SHA-256 → SQLite cifrado en el dispositivo.
- **Cola:** comandos de negocio (`AgregarLinea`, `EditarLinea`, …) en `outbox`, con compactación local.
- **Sync:** `POST /api/movil/v1/sincronizar` por lotes. El campo `requiereNuevoPaquete` es informativo; el cliente **siempre** descarga un paquete nuevo al terminar si la outbox y los envíos quedaron en cero (aunque el servidor no haya aplicado cambios locales o todas las operaciones salgan rechazadas). Si aún hay operaciones en cola, no reemplaza la base y muestra el aviso correspondiente.

Detalle de reglas y API: `MODULO_MOVIL_FLUTTER.md` en la raíz del repo (no versionado si así lo indica el equipo).

## Esquema local

Versión `user_version = 1`. Tablas principales: `actas`, `acta_detalles`, `outbox`, `presupuestos` (solo arquitecto), catálogos de apoyo. El backend valida que `movil/assets/esquema_movil_v1.sql` coincida con su recurso embebido.

## Seguridad en el dispositivo

| Tema | Implementación |
| --- | --- |
| Secretos | Refresh token, clave SQLCipher e id de dispositivo en `flutter_secure_storage`. Access token solo en memoria. |
| Ubicación | `getApplicationSupportDirectory()`. |
| Android | `android:allowBackup="false"` y exclusiones en `res/xml/`. |
| iOS | `NSURLIsExcludedFromBackupKey` en Application Support al arranque y tras importar paquete (`MethodChannel` `com.celuweb.arquitectos/respaldo`). |
| Bloqueo | Biometría o PIN (`local_auth`) al abrir; tras **5 minutos** en segundo plano vuelve la pantalla de desbloqueo. |
| Revocación | Respuesta `dispositivo_revocado` → borrado de bases, outbox, pendientes y claves; pantalla de login. |
| Cierre de sesión | Opcional en servidor; siempre borra datos locales (con aviso si hay cola pendiente). |

## Desarrollo y QA

Requisitos: Flutter estable, SDK Dart del `pubspec.yaml`.

```bash
cd movil
flutter pub get
flutter test --concurrency=1
```

Contra API local o QA (ajustar URL):

```bash
flutter run --dart-define=API_URL=http://10.0.2.2:5190   # emulador Android → localhost
```

Variables útiles:

| Define | Uso |
| --- | --- |
| `API_URL` | Base del backend (obligatoria en builds reales). |
| `APP_VERSION` | Cabecera `X-Version-App` (default `1.0.0`). |
| `VERSION_ESQUEMA` | Debe coincidir con `Movil:VersionEsquema` del servidor. |

Prueba de integración en VM:

```bash
flutter test test/flujo_integracion_sync_test.dart
```

En dispositivo (carpeta `integration_test/`) hace falta plataforma desktop o symlinks en Windows.

## Revocar un dispositivo

1. En la **web**, usuario administrador → **Usuarios** → pestaña dispositivos del usuario.
2. **Revocar** el dispositivo afectado.
3. En el móvil, el siguiente intento de sincronizar o renovar token recibe `dispositivo_revocado`, borra datos locales y muestra login.

Escenario de aceptación **6** del módulo móvil.

## Publicación (resumen)

- **CI:** workflow `.github/workflows/movil.yml` — `format`, `analyze`, `test --coverage`, `apk --debug`.
- **Android:** AAB firmado con keystore en secretos de CI; `allowBackup` desactivado.
- **iOS:** certificados y provisioning en secretos; distribución TestFlight o interna.
- No commitear keystores, `.env` ni URLs de producción.

## Sincronización en segundo plano

- **Android:** `workmanager` marca `sync_pendiente.flag` cada ~30 min; al volver a primer plano la app sincroniza si hay red y pendientes.
- **iOS:** solo en primer plano (limitación del sistema).
