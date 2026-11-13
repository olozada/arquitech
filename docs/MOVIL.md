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
- **Sync:** `POST /api/movil/v1/sincronizar` por lotes. El campo `requiereNuevoPaquete` es informativo; el cliente **siempre** descarga un paquete nuevo al terminar si la outbox y los envíos quedaron en cero (aunque el servidor no haya aplicado cambios locales o todas las operaciones salgan rechazadas). Si aún hay operaciones en cola, no reemplaza la base y muestra el aviso correspondiente. `sincronizar()` ignora llamadas concurrentes (`sincronizando`/`ocupado`) y marca `sincronizando` antes del primer `await`.

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
| Bloqueo | Login y reapertura exigen bloqueo real del SO: canal `arquitectos/seguridad` / `dispositivoSeguro` (`KeyguardManager.isDeviceSecure` en Android, `LAContext.deviceOwnerAuthentication` en iOS), no solo `local_auth.isDeviceSupported()`. Tras **5 minutos** en segundo plano vuelve la pantalla de desbloqueo (`local_auth`). |
| Privacidad visual | Android release: `FLAG_SECURE` en `MainActivity` (no capturas ni recientes). iOS/Android: velo `VeloPrivacidad` en `inactive`/`paused`. |
| API en release | Login rechaza `API_URL` sin `https://` (mismo aviso que URL vacía). |
| UI Material | Dependencia `material_ui` en pub.dev: paquete oficial del equipo Flutter (`flutter/packages`, descripción “official Flutter Material UI Library”); se mantiene en lugar de `package:flutter/material.dart`. |
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
| _(automático)_ | `X-Version-App` = `package_info.version`; `X-Build-App` = `buildNumber`. El login guarda `version+build` solo en el registro del dispositivo. El servidor compara versiones sin metadatos (`+`/`-`). |
| `VERSION_ESQUEMA` | Debe coincidir con `Movil:VersionEsquema` del servidor. |

Prueba de integración en VM:

```bash
flutter test test/flujo_integracion_sync_test.dart
```

En dispositivo (carpeta `integration_test/`) hace falta plataforma desktop o symlinks en Windows.

## Pruebas en dispositivo

Completar en campo (fecha, modelo, SO, build). Marcar OK / fallo / N/A.

| # | Escenario | Esperado | Resultado | Notas |
| --- | --- | --- | --- | --- |
| 1 | Android, AAB **release** firmado (`bundletool install-apks`), con PIN | Login → paquete → acta → línea → sync → cerrar/reabrir → desbloquear. Sin cierres. | | |
| 2 | Android **sin** PIN/patrón/huella (con lector de huella) | Mensaje «Configure un PIN, patrón o huella…» | | |
| 3 | Mismo equipo del #2 tras configurar PIN | Login funciona | | |
| 4 | Android release | Recientes sin contenido; captura bloqueada | | |
| 5 | iOS (TestFlight o desarrollo) | Flujo #1; velo al pasar a segundo plano | | |
| 6 | Avión → 3 líneas → red → Sincronizar | 3 líneas en web; paquete renovado | | |
| 7 | Admin cierra acta en web; móvil offline con cambios | Sync: pendientes rechazados; acta cerrada en móvil | | |
| 8 | Revisor sin cambios pulsa Sincronizar | Ve líneas nuevas del contratista | | |

## Revocar un dispositivo

1. En la **web**, usuario administrador → **Usuarios** → pestaña dispositivos del usuario.
2. **Revocar** el dispositivo afectado.
3. En el móvil, el siguiente intento de sincronizar o renovar token recibe `dispositivo_revocado`, borra datos locales y muestra login.

Escenario de aceptación **6** del módulo móvil.

## Publicación (resumen)

- **CI:** workflow `.github/workflows/movil.yml` — lee `movil/.flutter-version` (LF) a `FLUTTER_VERSION`, `flutter --version` en log, luego `format`, `analyze`, `test --coverage`, `apk --debug`.
- **Android release:** crear `movil/android/key.properties` (no versionado) con:
  - `storeFile` — ruta al `.jks` o `.keystore`
  - `storePassword`, `keyAlias`, `keyPassword`
- Generar keystore: `keytool -genkey -v -keystore arquitectos-release.jks -keyalg RSA -keysize 2048 -validity 10000 -alias arquitectos`
- Sin `key.properties`, `flutter build appbundle --release` falla con mensaje explícito (no usa la firma debug).
- Secretos de GitHub para CI release: `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASSWORD`, `ANDROID_KEY_ALIAS`, `ANDROID_KEY_PASSWORD`.
- **Android:** AAB firmado con keystore en secretos de CI; `allowBackup` desactivado; release con `isMinifyEnabled` y reglas en `android/app/proguard-rules.pro` para `net.zetetic` (SQLCipher), `com.davidmartos96.sqflite_sqlcipher`, `com.tekartik.sqflite` y `flutter_secure_storage`.
- **Aceptación release en dispositivo:** tabla en [Pruebas en dispositivo](#pruebas-en-dispositivo). ProGuard: 2026-11-13 (`sqflite_sqlcipher` 3.4.x / `net.zetetic:sqlcipher-android`).
- **iOS:** certificados y provisioning en secretos; distribución TestFlight o interna.
- No commitear keystores, `.env` ni URLs de producción.

## Sincronización en segundo plano

- **Android:** la tarea de `workmanager` cada ~30 min **solo crea** `sync_pendiente.flag`; no abre la base ni sincroniza en el isolate de fondo (evita acceso concurrente a SQLCipher). La sincronización real ocurre al volver a **primer plano** en `ControladorAplicacion.alFrente()` si hay red y la marca o la cola lo exigen.
- **iOS:** solo en primer plano (limitación del sistema).

Detalle histórico del diseño: `docs/historico/MODULO_MOVIL_SEGUNDO_PLANO.md`.
