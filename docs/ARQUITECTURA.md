# Arquitectura

Aplicación de actas de obra, presupuestos y tablero. El estado de los documentos anteriores está en `docs/historico/`.

## Capas

| Proyecto | Responsabilidad |
| --- | --- |
| `Arquitectos.Domain` | Entidades y enumeraciones. No referencia a EF ni a ASP.NET. |
| `Arquitectos.Application` | Contratos, filtros y perfiles. |
| `Arquitectos.Infrastructure` | EF Core, SQL Server, servicios, semilla y reglas del tablero. |
| `Arquitectos.Api` | Controladores, autenticación, cabeceras y salud. |
| `frontend` | React, Vite y React Query. En desarrollo llama a `http://localhost:5190` por el proxy. En el sitio publicado la base es `/Web/` y la API es `/api`. |
| `movil` | Flutter offline-first para arquitecto, contratista y revisor. Ver `docs/MOVIL.md`. |

## Módulos

- **Catálogos.** Arquitectos, revisores, contratistas, proveedores, construcciones, conceptos, unidades e insumos.
- **Actas.** Cabecera, líneas, revisión, cierre y PDF. Cada línea puede ligar un insumo. La cabecera lleva `RowVersion` para rechazar un guardado con una versión vieja.
- **Presupuesto.** Capítulos, ítems, APU, plantilla, importación de Excel, aprobación y seguimiento contra las actas.
- **Tablero.** KPIs, rankings y alertas A1–A5 sobre las actas del rango pedido.
- **Usuarios.** Login, sesión en cookie, cambio obligatorio de contraseña y auditoría de cambios.

## Modelo vigente

El insumo reemplaza al material. `TipoInsumo.Material` es 1. El precio de lista vive en `ProveedorMaterial` con clave `(ProveedorId, InsumoId)`. La ruta `/api/proveedores/{id}/materiales` sigue existiendo; `/api/materiales` no.

Un concepto describe la actividad (excavación, pañete, suministro). No se mezcla con el insumo.

`ActaDetalle.InsumoId` puede ser nulo: es una línea de mano de obra u otra actividad sin material. `Subtotal` es `Cantidad * Precio`. Una línea rechazada conserva la fila y su subtotal de tablero queda en cero.

`EstadoPresupuesto`: Borrador 1, Aprobado 2, Cerrado 3.

Las migraciones siguen siendo incrementales. La base del sitio ya tiene datos, así que no se aplasta el historial en una sola migración inicial.

## Perfiles

| Id | Perfil |
| --- | --- |
| 1 | Administrador |
| 2 | Arquitecto |
| 3 | Contratista |
| 4 | Revisor |

El administrador administra usuarios y catálogos. Arquitecto, contratista y revisor son perfiles operativos y cada usuario de ese tipo queda ligado a una persona del catálogo. Los permisos de menú salen de `MenuOpcionTipoUsuario`.

## Cálculo del APU

Cada línea usa un modo:

- **Desperdicio:** cantidad × precio × (1 + factor).
- **Rendimiento:** cantidad × precio × factor.
- **Divisor:** (cantidad × precio) / factor. Si el factor es 0, la línea vale 0.
- **Herramienta menor:** factor × la mano de obra ya sumada en el grupo.

Un divisor de grupo o un divisor general en 0 se toma como 1. El precio unitario de un insumo es `precioBase × (1 + iva) / cantidadPorUnidad`. Un sub-APU se calcula recursivo; si hay un ciclo, el cálculo se rechaza.

El presupuesto parte del subtotal directo. Imprevistos, incremento, administración y utilidad son ese subtotal por su fracción (0,10 es 10 %). El IVA se aplica solo sobre la utilidad. El total suma esas partes. Si hay área, el valor por m² es el total dividido por el área.

## Tablero

El rango por defecto de los últimos 12 meses empieza el día 1 del mismo mes del año anterior. Las alertas comparan el precio de la línea con el promedio de los 180 días previos (A1), con el menor precio de lista del insumo (A2), detectan dos cobros del mismo contratista, obra y concepto que se cruzan en fechas (A3), obras activas sin acta en 30 días (A4) y actas registradas más de 15 días después del fin del periodo (A5). Los umbrales están en la sección `Dashboard` de la configuración. La lista publicada se corta en `MaxAlertas` (100).

## Versiones que se dejan quietas

Se suben menores y parches. Estas se quedan porque la siguiente publicación cambia de generación:

- Paquetes de ASP.NET, EF Core y `Microsoft.Extensions.*` en 10.0.15. La 11.0.0 pide el marco de .NET 11 y la solución sigue en `net10.0`.
- `Swashbuckle.AspNetCore` queda en la línea 10 (ahora 10.3.1). La 11 acompaña a ASP.NET 11.
- `Microsoft.OpenApi` 2.13.0. La 3.x cambia el contrato y Swashbuckle 10 usa la 2.x.
- `System.IdentityModel.Tokens.Jwt` 7.7.3, la última de la 7. La 8 no coincide con el paquete de JWT de ASP.NET 10.
- `Microsoft.NET.Test.Sdk` 17.14.2, `coverlet.collector` 6.0.4 y `xunit.runner.visualstudio` 2.8.2. Las líneas 18, 8/10 y 3/4 son otra generación del ejecutor de pruebas.
- `@types/node` 24, la misma línea de Node que usa el proyecto. La 26 es otra generación.
