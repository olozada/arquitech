import 'package:sqflite_sqlcipher/sqflite.dart' as sql;

import 'base_local.dart';

class AbrirSqlCipher implements AbrirBase {
  @override
  Future<BaseLocal> abrir(String ruta, {String? clave}) async {
    final db = await sql.openDatabase(ruta, password: clave);
    return BaseAdaptable(db);
  }
}

class CifradorSqlCipher implements CifradorBase {
  @override
  Future<void> exportar(String plano, String destino, String clave) async {
    final db = await sql.openDatabase(plano);
    try {
      final destinoSql = destino.replaceAll("'", "''");
      final claveSql = clave.replaceAll("'", "''");
      await db.execute(
        "ATTACH DATABASE '$destinoSql' AS cifrada KEY '$claveSql'",
      );
      await db.rawQuery("SELECT sqlcipher_export('cifrada')");
      await db.execute('DETACH DATABASE cifrada');
    } finally {
      await db.close();
    }
  }
}
