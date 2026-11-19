part of 'consultas.dart';

String plegarTexto(String texto) {
  final plano = texto.trim().toLowerCase();
  final buffer = StringBuffer();
  for (final caracter in plano.runes) {
    buffer.write(_sinTilde(String.fromCharCode(caracter)));
  }
  return buffer.toString();
}

String sqlSinTildes(String columna) {
  var expresion = columna;
  const cambios = {
    'á': 'a',
    'Á': 'a',
    'é': 'e',
    'É': 'e',
    'í': 'i',
    'Í': 'i',
    'ó': 'o',
    'Ó': 'o',
    'ú': 'u',
    'Ú': 'u',
    'ü': 'u',
    'Ü': 'u',
    'ñ': 'n',
    'Ñ': 'n',
  };
  for (final cambio in cambios.entries) {
    expresion = "replace($expresion, '${cambio.key}', '${cambio.value}')";
  }
  return 'lower($expresion)';
}

String _sinTilde(String caracter) {
  return switch (caracter) {
    'á' || 'à' || 'ä' => 'a',
    'é' || 'è' || 'ë' => 'e',
    'í' || 'ì' || 'ï' => 'i',
    'ó' || 'ò' || 'ö' => 'o',
    'ú' || 'ù' || 'ü' => 'u',
    'ñ' => 'n',
    _ => caracter,
  };
}

mixin ConsultasCatalogo on ConsultasBase {
  Future<List<PresupuestoResumen>> presupuestos() async {
    final filas = await base.consultar('''
SELECT id, nombre, construccion, estado, total
FROM presupuestos
ORDER BY nombre
''');
    return filas
        .map(
          (fila) => PresupuestoResumen(
            id: _entero(fila['id']),
            nombre: _texto(fila['nombre']),
            construccion: _texto(fila['construccion']),
            estado: _texto(fila['estado']),
            total: fila['total']?.toString(),
          ),
        )
        .toList();
  }

  Future<PresupuestoPaquete?> presupuestoPorId(int id) async {
    final filas = await base.consultar(
      '''
SELECT id, nombre, construccion, estado, detalle_json, validacion_json, seguimiento_json
FROM presupuestos
WHERE id = ?
''',
      [id],
    );
    if (filas.isEmpty) {
      return null;
    }
    final fila = filas.first;
    return PresupuestoPaquete(
      id: _entero(fila['id']),
      nombre: _texto(fila['nombre']),
      construccion: _texto(fila['construccion']),
      estado: _texto(fila['estado']),
      detalleJson: _texto(fila['detalle_json']),
      validacionJson: fila['validacion_json']?.toString(),
      seguimientoJson: fila['seguimiento_json']?.toString(),
    );
  }

  Future<List<OpcionCatalogo>> construcciones() async {
    final filas = await base.consultar(
      'SELECT id, descripcion FROM construcciones ORDER BY descripcion',
    );
    return filas
        .map(
          (fila) => OpcionCatalogo(
            id: _entero(fila['id']),
            descripcion: _texto(fila['descripcion']),
          ),
        )
        .toList();
  }

  Future<List<OpcionCatalogo>> conceptos() async {
    final filas = await base.consultar(
      'SELECT id, descripcion FROM conceptos ORDER BY descripcion',
    );
    return filas
        .map(
          (fila) => OpcionCatalogo(
            id: _entero(fila['id']),
            descripcion: _texto(fila['descripcion']),
          ),
        )
        .toList();
  }

  Future<List<OpcionCatalogo>> unidadesMedida() async {
    final filas = await base.consultar(
      'SELECT id, descripcion FROM unidades_medida ORDER BY descripcion',
    );
    return filas
        .map(
          (fila) => OpcionCatalogo(
            id: _entero(fila['id']),
            descripcion: _texto(fila['descripcion']),
          ),
        )
        .toList();
  }

  Future<List<InsumoOpcion>> buscarInsumos(
    String texto, {
    int limite = 40,
  }) async {
    final busqueda = plegarTexto(texto);
    if (busqueda.length < 2) {
      return const [];
    }
    final patron = '%${busqueda.replaceAll('%', '').replaceAll('_', '')}%';
    final descripcion = sqlSinTildes('descripcion');
    final codigo = sqlSinTildes('codigo');
    final filas = await base.consultar(
      '''
SELECT id, codigo, descripcion FROM insumos
WHERE $descripcion LIKE ? OR $codigo LIKE ?
ORDER BY descripcion
LIMIT ?
''',
      [patron, patron, limite],
    );
    return filas
        .map(
          (fila) => InsumoOpcion(
            id: _entero(fila['id']),
            codigo: fila['codigo']?.toString(),
            descripcion: _texto(fila['descripcion']),
          ),
        )
        .toList();
  }

  Future<List<ItemPresupuestoOpcion>> itemsPresupuesto(
    int construccionId,
  ) async {
    final filas = await base.consultar(
      '''
SELECT id, descripcion, concepto_id, unidad_medida_id, precio
FROM items_presupuesto_acta
WHERE construccion_id = ?
ORDER BY descripcion
''',
      [construccionId],
    );
    return filas
        .map(
          (fila) => ItemPresupuestoOpcion(
            id: _entero(fila['id']),
            descripcion: _texto(fila['descripcion']),
            conceptoId: _entero(fila['concepto_id']),
            unidadMedidaId: _opcional(fila['unidad_medida_id']),
            precio: fila['precio']?.toString(),
          ),
        )
        .toList();
  }
}
