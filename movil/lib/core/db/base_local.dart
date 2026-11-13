abstract class BaseLocal {
  Future<List<Map<String, Object?>>> consultar(
    String sentencia, [
    List<Object?> argumentos = const [],
  ]);

  Future<void> ejecutar(
    String sentencia, [
    List<Object?> argumentos = const [],
  ]);

  Future<void> cerrar();
}

abstract class AbrirBase {
  Future<BaseLocal> abrir(String ruta, {String? clave});
}

abstract class CifradorBase {
  /// Copia [plano] a [destino] cifrado con [clave].
  Future<void> exportar(String plano, String destino, String clave);
}

class BaseAdaptable implements BaseLocal {
  BaseAdaptable(this._db);

  final dynamic _db;

  @override
  Future<List<Map<String, Object?>>> consultar(
    String sentencia, [
    List<Object?> argumentos = const [],
  ]) async {
    final filas = await _db.rawQuery(sentencia, argumentos) as List<dynamic>;
    return filas.map((fila) => Map<String, Object?>.from(fila as Map)).toList();
  }

  @override
  Future<void> ejecutar(
    String sentencia, [
    List<Object?> argumentos = const [],
  ]) async {
    final verbo = sentencia.trimLeft().toLowerCase();
    if (verbo.startsWith('insert')) {
      await _db.rawInsert(sentencia, argumentos);
      return;
    }
    if (argumentos.isEmpty) {
      await _db.execute(sentencia);
      return;
    }
    await _db.rawUpdate(sentencia, argumentos);
  }

  @override
  Future<void> cerrar() async {
    await _db.close();
  }
}
