# Datos de demostración

`datos_demo.sql` quedó obsoleto. No lo ejecutes: el esquema ya no tiene la tabla de materiales (ahora son insumos) y el archivo se conserva solo como referencia.

La fuente que usa la aplicación es `SemillaDemo`, con los mismos casos que `generar_datos_demo.py`. En Development:

```
dotnet run --project backend/src/Arquitectos.Api --launch-profile http -- --seed-demo
```

Ese comando usa la cadena de conexión de Development. La base tiene que terminar en `_DEV`, `_E2E`, `_TEST` o `_QA`. `--reset` además exige `--confirmar` e imprime solo el servidor y la base. No lo apuntes a la base del sitio.

`recrear_tablas.sql` y `recrear_pruebas.sql` hacen DROP y CREATE fuera de EF. No están en git. No los ejecutes contra la base del sitio.
