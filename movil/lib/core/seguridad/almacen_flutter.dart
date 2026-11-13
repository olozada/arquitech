import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import 'almacen_seguro.dart';

class AlmacenFlutter implements AlmacenSeguro {
  AlmacenFlutter([FlutterSecureStorage? caja])
    : _caja =
          caja ??
          const FlutterSecureStorage(
            aOptions: AndroidOptions(encryptedSharedPreferences: true),
            iOptions: IOSOptions(
              accessibility: KeychainAccessibility.first_unlock_this_device,
            ),
          );

  final FlutterSecureStorage _caja;

  @override
  Future<String?> leer(String clave) => _caja.read(key: clave);

  @override
  Future<void> guardar(String clave, String valor) =>
      _caja.write(key: clave, value: valor);

  @override
  Future<void> borrar(String clave) => _caja.delete(key: clave);

  @override
  Future<void> borrarTodo() => _caja.deleteAll();
}
