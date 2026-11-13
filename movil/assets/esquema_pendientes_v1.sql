CREATE TABLE operaciones_fallidas (
  id_operacion TEXT PRIMARY KEY,
  secuencia INTEGER NOT NULL,
  tipo TEXT NOT NULL,
  numero_acta INTEGER NOT NULL,
  local_id TEXT,
  estado TEXT NOT NULL,
  mensaje TEXT NOT NULL,
  payload_json TEXT NOT NULL,
  resultado_json TEXT NOT NULL,
  registrada_en TEXT NOT NULL
);
CREATE INDEX ix_fallidas_acta ON operaciones_fallidas(numero_acta);
