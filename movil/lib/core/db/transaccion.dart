import 'base_local.dart';

Future<T> transaccion<T>(BaseLocal base, Future<T> Function() trabajo) async {
  await base.ejecutar('BEGIN IMMEDIATE');
  try {
    final resultado = await trabajo();
    await base.ejecutar('COMMIT');
    return resultado;
  } catch (error) {
    await base.ejecutar('ROLLBACK');
    rethrow;
  }
}
