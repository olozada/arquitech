import 'dart:io';

import 'package:path/path.dart' as p;

import 'base_local.dart';

class RepositorioLocal {
  RepositorioLocal({required this.directorio, required this.abrir});

  final Future<Directory> Function() directorio;
  final AbrirBase abrir;
  BaseLocal? _abierta;

  Future<String> rutaActiva() async {
    final carpeta = await directorio();
    await carpeta.create(recursive: true);
    return p.join(carpeta.path, 'arquitectos.sqlite');
  }

  Future<String> rutaPendientes() async {
    final carpeta = await directorio();
    return p.join(carpeta.path, 'pendientes.sqlite');
  }

  Future<bool> existe() async {
    return File(await rutaActiva()).exists();
  }

  Future<BaseLocal> obtener(String? clave) async {
    final abierta = _abierta;
    if (abierta != null) {
      return abierta;
    }
    final base = await abrir.abrir(await rutaActiva(), clave: clave);
    _abierta = base;
    return base;
  }

  Future<void> cerrar() async {
    final abierta = _abierta;
    _abierta = null;
    if (abierta != null) {
      await abierta.cerrar();
    }
  }
}
