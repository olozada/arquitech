import 'dart:io';

import 'package:arquitectos_movil/app/controlador.dart';
import 'package:arquitectos_movil/core/api/cliente_movil.dart';
import 'package:arquitectos_movil/core/api/errores.dart';
import 'package:arquitectos_movil/core/db/consultas.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:sqflite_common_ffi/sqflite_ffi.dart';

import 'soporte.dart';

void main() {
  setUpAll(() {
    sqfliteFfiInit();
    databaseFactory = databaseFactoryFfi;
  });

  test('el login deja la base local con las actas del paquete', () async {
    final carpeta = await carpetaPrueba('controlador');
    addTearDown(() => borrarCarpeta(carpeta));
    final origen = await Directory(
      '${carpeta.path}${Platform.pathSeparator}origen',
    ).create();
    final paquete = await paqueteDePrueba(origen);
    final cliente = ClienteFalso(paquete);
    final dependencias = dependenciasPrueba(carpeta: carpeta, cliente: cliente);
    final controlador = ControladorAplicacion(dependencias);
    await controlador.iniciar();
    await controlador.entrar('ana', 'Clave12345');

    expect(cliente.descargas, 1);
    expect(controlador.tieneBase, isTrue);
    expect(controlador.ruta, RutaAplicacion.inicio);

    final actas = await controlador.listarActas(const FiltroActas());
    expect(actas.first.numero, 120);
    expect(actas.first.arquitecto, 'Ana Arquitecta');

    await dependencias.repositorio.cerrar();
    controlador.dispose();
  });

  test('login inválido deja un aviso', () async {
    final carpeta = await carpetaPrueba('login-ctrl');
    addTearDown(() => borrarCarpeta(carpeta));
    final dependencias = dependenciasPrueba(
      carpeta: carpeta,
      cliente: _ClienteError(),
    );
    final controlador = ControladorAplicacion(dependencias);
    await controlador.iniciar();
    await controlador.entrar('ana', 'mala');

    expect(controlador.aviso, 'Usuario o contraseña inválidos.');
    controlador.dispose();
  });
}

class _ClienteError extends ClienteFalso {
  _ClienteError() : super(PaqueteListo(gzip: const [], sha256: ''));

  @override
  Future<Credenciales> iniciar({
    required String usuario,
    required String contrasena,
    required DispositivoEnvio dispositivo,
  }) async {
    throw const ErrorServidor('Usuario o contraseña inválidos.', estado: 401);
  }
}
