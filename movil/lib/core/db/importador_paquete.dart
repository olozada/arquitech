import 'dart:io';

import 'package:crypto/crypto.dart';

import '../errores/mensajes.dart';
import '../api/errores.dart';
import 'base_local.dart';

class ImportadorPaquete {
  ImportadorPaquete({required this.cifrador, required this.abrir});

  final CifradorBase cifrador;
  final AbrirBase abrir;

  Future<void> importar({
    required List<int> contenidoGzip,
    required String sha256Esperado,
    required String clave,
    required String rutaActiva,
    required int operacionesPendientes,
    required int versionEsquema,
  }) async {
    if (operacionesPendientes > 0) {
      throw const ReemplazoBloqueado(Mensajes.reemplazoBloqueado);
    }

    final bytes = gzip.decode(contenidoGzip);
    final resumen = sha256.convert(bytes).toString();
    if (resumen != sha256Esperado.trim().toLowerCase()) {
      throw const PaqueteInvalido(Mensajes.paqueteCorrupto);
    }

    final plano = File('$rutaActiva.plano');
    final nueva = File('$rutaActiva.nueva');
    final activa = File(rutaActiva);
    final anterior = File('$rutaActiva.anterior');
    await _borrarSiExiste(plano);
    await _borrarSiExiste(nueva);

    try {
      await plano.writeAsBytes(bytes, flush: true);
      await cifrador.exportar(plano.path, nueva.path, clave);
      await _validar(nueva.path, clave, versionEsquema);
      await _borrarSiExiste(plano);

      if (await anterior.exists()) {
        await anterior.delete();
      }
      if (await activa.exists()) {
        await activa.rename(anterior.path);
      }
      try {
        await nueva.rename(activa.path);
      } catch (_) {
        if (await anterior.exists() && !await activa.exists()) {
          await anterior.rename(activa.path);
        }
        rethrow;
      }
      await _borrarSiExiste(anterior);
    } catch (error) {
      await _borrarSiExiste(plano);
      await _borrarSiExiste(nueva);
      if (error is FalloMovil) {
        rethrow;
      }
      throw const PaqueteInvalido('No se pudo importar el paquete.');
    }
  }

  Future<void> _validar(String ruta, String clave, int versionEsquema) async {
    final base = await abrir.abrir(ruta, clave: clave);
    try {
      final version = await base.consultar(
        "SELECT valor FROM meta WHERE clave = 'version_esquema'",
      );
      final valor = version.isEmpty ? null : version.first['valor']?.toString();
      if (valor != versionEsquema.toString()) {
        throw const PaqueteInvalido(Mensajes.paqueteDistinto);
      }

      final integridad = await base.consultar('PRAGMA integrity_check');
      final sana = integridad.any(
        (fila) => fila.values.any((valorFila) => valorFila?.toString() == 'ok'),
      );
      if (!sana) {
        throw const PaqueteInvalido(
          'El paquete no pasó la verificación de integridad.',
        );
      }
    } finally {
      await base.cerrar();
    }
  }

  Future<void> _borrarSiExiste(File archivo) async {
    if (await archivo.exists()) {
      await archivo.delete();
    }
  }
}
