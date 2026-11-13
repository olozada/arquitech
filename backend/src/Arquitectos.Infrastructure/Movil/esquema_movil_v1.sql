CREATE TABLE meta (clave TEXT PRIMARY KEY, valor TEXT NOT NULL);
-- claves: version_esquema, generado_en, usuario_id, tipo_usuario_id, meses_historial, version_api

CREATE TABLE usuario (id INTEGER PRIMARY KEY, nombre TEXT NOT NULL, tipo_usuario_id INTEGER NOT NULL,
  arquitecto_id INTEGER, contratista_id INTEGER, revisor_id INTEGER);

CREATE TABLE tipos_usuario (id INTEGER PRIMARY KEY, nombre TEXT NOT NULL);
CREATE TABLE arquitectos  (id INTEGER PRIMARY KEY, nombre TEXT NOT NULL);
CREATE TABLE revisores    (id INTEGER PRIMARY KEY, nombre TEXT NOT NULL);
CREATE TABLE contratistas (id INTEGER PRIMARY KEY, nombre TEXT NOT NULL);
CREATE TABLE construcciones (id INTEGER PRIMARY KEY, descripcion TEXT NOT NULL);
CREATE TABLE conceptos (id INTEGER PRIMARY KEY, codigo TEXT, descripcion TEXT NOT NULL, unidad TEXT);
CREATE TABLE unidades_medida (id INTEGER PRIMARY KEY, descripcion TEXT NOT NULL);
CREATE TABLE insumos (id INTEGER PRIMARY KEY, codigo TEXT, descripcion TEXT NOT NULL, unidad TEXT);

-- Igual a GET /api/presupuestos/items-acta?construccionId= para las construcciones activas
CREATE TABLE items_presupuesto_acta (id INTEGER PRIMARY KEY, construccion_id INTEGER NOT NULL,
  codigo TEXT, descripcion TEXT NOT NULL, unidad TEXT, concepto_id INTEGER, unidad_medida_id INTEGER, precio TEXT);
CREATE INDEX ix_ipa_construccion ON items_presupuesto_acta(construccion_id);

CREATE TABLE actas (numero_acta INTEGER PRIMARY KEY, fecha_inicial TEXT NOT NULL, fecha_final TEXT NOT NULL,
  arquitecto_id INTEGER NOT NULL, revisor_id INTEGER NOT NULL, contratista_id INTEGER NOT NULL,
  observacion TEXT, cerrada INTEGER NOT NULL, fecha_registro TEXT NOT NULL, row_version TEXT NOT NULL);

CREATE TABLE acta_detalles (
  local_id TEXT PRIMARY KEY,            -- id_cliente si existe; si no, 's' || id
  id INTEGER UNIQUE,                    -- id del servidor (NULL en líneas creadas offline)
  id_cliente TEXT,
  numero_acta INTEGER NOT NULL REFERENCES actas(numero_acta),
  construccion_id INTEGER NOT NULL, concepto_id INTEGER NOT NULL,
  unidad_medida_id INTEGER, insumo_id INTEGER, presupuesto_item_id INTEGER,
  cantidad TEXT NOT NULL, precio TEXT NOT NULL, subtotal TEXT NOT NULL,
  descripcion TEXT NOT NULL, tipo_usuario_id INTEGER NOT NULL, aprobado INTEGER NOT NULL,
  row_version TEXT,
  estado_local TEXT NOT NULL DEFAULT 'sincronizado'  -- sincronizado | nuevo | modificado | eliminado
);
CREATE INDEX ix_detalles_acta ON acta_detalles(numero_acta);

-- Solo para Arquitecto: mismo resultado que la web (ListarAsync/ObtenerAsync/ValidacionAsync/SeguimientoAsync)
CREATE TABLE presupuestos (id INTEGER PRIMARY KEY, construccion_id INTEGER NOT NULL, construccion TEXT NOT NULL,
  nombre TEXT NOT NULL, version INTEGER, estado TEXT NOT NULL, total TEXT, actualizado_en TEXT,
  detalle_json TEXT NOT NULL, validacion_json TEXT, seguimiento_json TEXT);

-- Cola local (vacía al generar)
CREATE TABLE outbox (id_operacion TEXT PRIMARY KEY, secuencia INTEGER NOT NULL UNIQUE, tipo TEXT NOT NULL,
  numero_acta INTEGER NOT NULL, local_id TEXT, payload_json TEXT NOT NULL, creada_en TEXT NOT NULL,
  estado TEXT NOT NULL DEFAULT 'pendiente', intentos INTEGER NOT NULL DEFAULT 0, resultado_json TEXT);
