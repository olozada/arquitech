import 'dart:io';

import 'package:arquitectos_movil/app/controlador.dart';
import 'package:arquitectos_movil/core/api/cliente_movil.dart';
import 'package:arquitectos_movil/core/errores/mensajes.dart';
import 'package:arquitectos_movil/core/seguridad/claves.dart';
import 'package:arquitectos_movil/sincronizacion/modelos.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:sqflite_common_ffi/sqflite_ffi.dart';

import '../integration_test/simulador_api_movil.dart';
import 'soporte.dart';

void main() {
  setUpAll(() {
    TestWidgetsFlutterBinding.ensureInitialized();
    sqfliteFfiInit();
    databaseFactory = databaseFactoryFfi;
  });

  test('desbloquear sin base local descarga el paquete', () async {
    final carpeta = await carpetaPrueba('sync-sin-base');
    addTearDown(() => borrarCarpeta(carpeta));
    final origen = await Directory(
      '${carpeta.path}${Platform.pathSeparator}origen',
    ).create();
    final paquete = await paqueteDePrueba(origen);
    final cliente = ClienteFalso(paquete);
    final almacen = AlmacenMemoria();
    await almacen.guardar(Claves.refresh, 'refresh-valido');
    await almacen.guardar(Claves.usuarioId, '4');
    await almacen.guardar(Claves.tipo, '2');
    await almacen.guardar(Claves.usuarioNombre, 'Ana');
    await almacen.guardar(Claves.login, 'ana');
    await almacen.guardar(
      Claves.dispositivo,
      '00000000-0000-4000-8000-000000000099',
    );
    final dependencias = dependenciasPrueba(
      carpeta: carpeta,
      cliente: cliente,
      almacen: almacen,
    );
    final controlador = ControladorAplicacion(dependencias);
    addTearDown(controlador.dispose);

    await controlador.iniciar();
    expect(controlador.tieneBase, isFalse);
    expect(controlador.ruta, RutaAplicacion.desbloqueo);

    await controlador.desbloquear();

    expect(cliente.descargas, 1);
    expect(controlador.tieneBase, isTrue);
  });

  test('sincronizar sin base local descarga el paquete', () async {
    final carpeta = await carpetaPrueba('sync-null-base');
    addTearDown(() => borrarCarpeta(carpeta));
    final origen = await Directory(
      '${carpeta.path}${Platform.pathSeparator}origen',
    ).create();
    final paquete = await paqueteDePrueba(origen);
    final cliente = ClienteFalso(paquete);
    final almacen = AlmacenMemoria();
    await almacen.guardar(Claves.refresh, 'refresh-valido');
    await almacen.guardar(Claves.sql, 'a' * 64);
    await almacen.guardar(Claves.dispositivo, '00000000-0000-4000-8000-000000000001');
    final dependencias = dependenciasPrueba(
      carpeta: carpeta,
      cliente: cliente,
      almacen: almacen,
    );
    final controlador = ControladorAplicacion(dependencias);
    addTearDown(controlador.dispose);
    await controlador.iniciar();
    controlador
      ..ruta = RutaAplicacion.inicio
      ..dispositivoId = '00000000-0000-4000-8000-000000000001'
      ..usuario = const UsuarioMovil(id: 4, nombre: 'Ana', tipoUsuarioId: 2);

    await controlador.sincronizar();

    expect(cliente.descargas, 1);
    expect(controlador.tieneBase, isTrue);
    expect(controlador.aviso, Mensajes.datosActualizados);
  });

  test('con outbox vacía, sincronizar descarga el paquete', () async {
    final carpeta = await carpetaPrueba('sync-paq-vacio');
    addTearDown(() => borrarCarpeta(carpeta));
    final origen = await Directory(
      '${carpeta.path}${Platform.pathSeparator}origen',
    ).create();
    final paquete = await paqueteDePrueba(origen);
    final cliente = ClienteFalso(paquete);
    final dependencias = dependenciasPrueba(carpeta: carpeta, cliente: cliente);
    final controlador = ControladorAplicacion(dependencias);
    addTearDown(controlador.dispose);

    await controlador.iniciar();
    await controlador.entrar('ana', 'Clave12345');
    expect(cliente.descargas, 1);

    await controlador.sincronizar();

    expect(cliente.descargas, 2);
    expect(controlador.aviso, Mensajes.datosActualizados);
  });

  test('lote solo con rechazadas termina descargando el paquete', () async {
    final carpeta = await carpetaPrueba('sync-rech-paq');
    addTearDown(() => borrarCarpeta(carpeta));
    final origen = await Directory(
      '${carpeta.path}${Platform.pathSeparator}origen',
    ).create();
    final paquete = await paqueteDePrueba(origen);
    final cliente = _ClienteRechazaLote(paquete);
    final dependencias = dependenciasPrueba(carpeta: carpeta, cliente: cliente);
    final controlador = ControladorAplicacion(dependencias);
    addTearDown(controlador.dispose);

    await controlador.iniciar();
    await controlador.entrar('ana', 'Clave12345');
    final descargasTrasLogin = cliente.descargas;
    await controlador.agregarLinea(
      120,
      const LineaSincronizacion(
        construccionId: 7,
        conceptoId: 1,
        cantidad: '1.0000',
        precio: '10.00',
        descripcion: 'Será rechazada',
      ),
    );
    expect(controlador.pendientes, 1);

    await controlador.sincronizar();

    expect(controlador.pendientes, 0);
    expect(controlador.fallidas, 1);
    expect(cliente.descargas, descargasTrasLogin + 1);
    expect(controlador.aviso, Mensajes.datosActualizados);
  });

  test(
    'servidor sin resultado corta el bucle y deja la operación pendiente',
    () async {
      final carpeta = await carpetaPrueba('sync-estanca');
      addTearDown(() => borrarCarpeta(carpeta));
      final origen = await Directory(
        '${carpeta.path}${Platform.pathSeparator}origen',
      ).create();
      final paquete = await paqueteDePrueba(origen);
      final cliente = _ClienteSinResultados(paquete);
      final dependencias = dependenciasPrueba(
        carpeta: carpeta,
        cliente: cliente,
      );
      final controlador = ControladorAplicacion(dependencias);
      addTearDown(controlador.dispose);

      await controlador.iniciar();
      await controlador.entrar('ana', 'Clave12345');
      await controlador.agregarLinea(
        120,
        const LineaSincronizacion(
          construccionId: 7,
          conceptoId: 1,
          cantidad: '1.0000',
          precio: '20.00',
          descripcion: 'Sin respuesta servidor',
        ),
      );

      await controlador.sincronizar();

      expect(cliente.llamadasSincronizar, lessThanOrEqualTo(2));
      expect(cliente.llamadasSincronizar, greaterThanOrEqualTo(1));
      expect(controlador.pendientes, greaterThan(0));
      expect(controlador.aviso, Mensajes.sincronizacionIncompleta);
    },
  );

  test('revisor sin pendientes ve una línea nueva del contratista', () async {
    final simulador = SimuladorApiMovil();
    await simulador.iniciar();
    final cliente = ClienteSimuladorApi(simulador);
    const descripcionRemota = 'Línea del contratista en servidor';

    final carpeta = await carpetaPrueba('sync-revisor-remota');
    addTearDown(() => borrarCarpeta(carpeta));
    final dependencias = dependenciasPrueba(carpeta: carpeta, cliente: cliente);
    final controlador = ControladorAplicacion(dependencias);
    addTearDown(controlador.dispose);

    await controlador.iniciar();
    await controlador.entrar('rosa', 'Clave12345');
    expect(controlador.usuario?.tipoUsuarioId, 4);
    expect(controlador.pendientes, 0);

    await simulador.agregarLineaRemotaEnPaquete(descripcion: descripcionRemota);
    await controlador.sincronizar();

    final clave = await dependencias.almacen.leer(Claves.sql);
    final base = await dependencias.repositorio.obtener(clave);
    if (base == null) {
      fail('Se esperaba base local tras sincronizar.');
    }
    final filas = await base.consultar(
      'SELECT descripcion FROM acta_detalles WHERE descripcion = ?',
      [descripcionRemota],
    );
    expect(filas, hasLength(1));
    await base.cerrar();
    await dependencias.repositorio.cerrar();
  });
}

class _ClienteRechazaLote extends ClienteFalso {
  _ClienteRechazaLote(super.paquete);

  @override
  Future<RespuestaSincronizacion> sincronizar({
    required String token,
    required String dispositivoId,
    required String dispositivoUuid,
    required int versionEsquema,
    required List<OperacionSalida> operaciones,
  }) async {
    return RespuestaSincronizacion(
      requiereNuevoPaquete: false,
      resultados: [
        for (final op in operaciones)
          ResultadoOperacion(
            idOperacion: op.idOperacion,
            estado: 'Rechazada',
            mensaje: 'El acta ya está cerrada.',
          ),
      ],
    );
  }
}

class _ClienteSinResultados extends ClienteFalso {
  _ClienteSinResultados(super.paquete);

  var llamadasSincronizar = 0;

  @override
  Future<RespuestaSincronizacion> sincronizar({
    required String token,
    required String dispositivoId,
    required String dispositivoUuid,
    required int versionEsquema,
    required List<OperacionSalida> operaciones,
  }) async {
    llamadasSincronizar += 1;
    return const RespuestaSincronizacion(
      requiereNuevoPaquete: false,
      resultados: [],
    );
  }
}
