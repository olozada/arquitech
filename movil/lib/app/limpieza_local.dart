import 'dart:io';

/// Archivos temporales de importación y descargas en línea.
class LimpiezaLocal {
  static const nombresRestosImportacion = [
    'arquitectos.sqlite.plano',
    'arquitectos.sqlite.nueva',
    'paquete.sqlite.gz',
  ];

  static Future<Directory> carpetaDescargas(Directory raiz) async {
    final descargas = Directory(
      '${raiz.path}${Platform.pathSeparator}descargas',
    );
    if (!await descargas.exists()) {
      await descargas.create(recursive: true);
    }
    return descargas;
  }

  static Future<void> alIniciar(Directory raiz) async {
    final activa = File(
      '${raiz.path}${Platform.pathSeparator}arquitectos.sqlite',
    );
    final incluirAnterior = await activa.exists();
    for (final nombre in nombresRestosImportacion) {
      await _borrarSiExiste(
        File('${raiz.path}${Platform.pathSeparator}$nombre'),
      );
    }
    if (incluirAnterior) {
      await _borrarSiExiste(
        File(
          '${raiz.path}${Platform.pathSeparator}arquitectos.sqlite.anterior',
        ),
      );
    }
    final descargas = Directory(
      '${raiz.path}${Platform.pathSeparator}descargas',
    );
    if (!await descargas.exists()) {
      return;
    }
    final limite = DateTime.now().subtract(const Duration(hours: 24));
    await for (final entidad in descargas.list()) {
      if (entidad is! File) {
        continue;
      }
      final modificado = await entidad.lastModified();
      if (modificado.isBefore(limite)) {
        await entidad.delete();
      }
    }
  }

  static Future<void> borrarDescargas(Directory raiz) async {
    final descargas = Directory(
      '${raiz.path}${Platform.pathSeparator}descargas',
    );
    if (await descargas.exists()) {
      await descargas.delete(recursive: true);
    }
  }

  static Future<void> _borrarSiExiste(File archivo) async {
    if (await archivo.exists()) {
      await archivo.delete();
    }
  }
}
