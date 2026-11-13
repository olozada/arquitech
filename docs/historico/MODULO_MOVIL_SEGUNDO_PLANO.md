# Módulo móvil — segundo plano (§6.3)

## Decisión

En Android, `workmanager` **no** ejecuta la sincronización completa ni abre SQLite cifrado desde un isolate de fondo. Solo marca `sync_pendiente.flag` en el directorio de la app.

## Flujo

1. Cada ~30 minutos la tarea en segundo plano escribe la marca.
2. Al volver a primer plano, `ControladorAplicacion.alFrente()` consume la marca y, si hay red, llama `sincronizar()` cuando corresponde (cola pendiente o marca).
3. En iOS no hay tarea equivalente; la sincronización ocurre solo en primer plano.

## Motivo

Evitar dos procesos abriendo la misma base SQLCipher y la outbox en paralelo, y respetar las limitaciones de `workmanager` en Flutter.
