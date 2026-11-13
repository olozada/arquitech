import 'dart:io';

import 'package:open_filex/open_filex.dart';

import 'limpieza_local.dart';

/// Guarda PDF y Excel en `descargas/`.
class RepositorioArchivosMovil {
  const RepositorioArchivosMovil(this._directorio);

  final Future<Directory> Function() _directorio;

  Future<File> guardar(List<int> bytes, String nombre) async {
    final descargas = await LimpiezaLocal.carpetaDescargas(await _directorio());
    final archivo = File('${descargas.path}${Platform.pathSeparator}$nombre');
    await archivo.writeAsBytes(bytes, flush: true);
    return archivo;
  }

  Future<void> abrir(List<int> bytes, String nombre) async {
    final archivo = await guardar(bytes, nombre);
    await OpenFilex.open(archivo.path);
  }
}
