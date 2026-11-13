import 'dart:io';

import 'package:arquitectos_movil/app/configuracion.dart';
import 'package:arquitectos_movil/core/api/cliente_movil.dart';
import 'package:arquitectos_movil/core/db/pendientes_db.dart';
import 'package:arquitectos_movil/sincronizacion/cola_movil.dart';
import 'package:arquitectos_movil/sincronizacion/modelos.dart';
import 'package:arquitectos_movil/sincronizacion/sincronizador_movil.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:sqflite_common_ffi/sqflite_ffi.dart';
import 'package:uuid/uuid.dart';

import 'soporte.dart';

class _ClienteSincronizar implements ClienteMovil {
  _ClienteSincronizar(this.respuesta);

  final RespuestaSincronizacion respuesta;

  @override
  Future<Credenciales> iniciar({
    required String usuario,
    required String contrasena,
    required DispositivoEnvio dispositivo,
  }) async => throw UnimplementedError();

  @override
  Future<ParTokens> renovar({
    required String refreshToken,
    required String dispositivoId,
  }) async => throw UnimplementedError();

  @override
  Future<void> cambiarContrasena({
    required String token,
    required String actual,
    required String nueva,
  }) async {}

  @override
  Future<void> salir({
    required String token,
    required String dispositivoId,
  }) async {}

  @override
  Future<PaqueteDescargado> descargarPaquete({
    required String token,
    required String dispositivoId,
    required String rutaArchivoGz,
    void Function(int recibidos, int? total)? onProgreso,
  }) async => throw UnimplementedError();

  @override
  Future<RespuestaSincronizacion> sincronizar({
    required String token,
    required String dispositivoId,
    required String dispositivoUuid,
    required int versionEsquema,
    required List<OperacionSalida> operaciones,
  }) async {
    return respuesta;
  }

  @override
  Future<List<int>> descargarPdfActa({
    required String token,
    required int numeroActa,
  }) async => const [];

  @override
  Future<List<int>> descargarPdfPresupuesto({
    required String token,
    required int presupuestoId,
  }) async => const [];

  @override
  Future<List<int>> descargarExcelPresupuesto({
    required String token,
    required int presupuestoId,
  }) async => const [];
}

void main() {
  setUpAll(() {
    TestWidgetsFlutterBinding.ensureInitialized();
    sqfliteFfiInit();
    databaseFactory = databaseFactoryFfi;
  });

  test('Aplicada vacía la outbox y marca la línea sincronizada', () async {
    final carpeta = await carpetaPrueba('sync-ok');
    addTearDown(() => borrarCarpeta(carpeta));
    final ruta = '${carpeta.path}${Platform.pathSeparator}local.sqlite';
    final base = await baseConEsquema(ruta);
    await sembrar(base);
    final cola = ColaMovil(uuid: const Uuid());
    final creada = DateTime.utc(2026, 11, 13, 12);
    await cola.agregarLinea(
      base: base,
      numeroActa: 120,
      tipoUsuarioId: 3,
      linea: const LineaSincronizacion(
        construccionId: 7,
        conceptoId: 1,
        cantidad: '1.0000',
        precio: '100.00',
        descripcion: 'Nueva línea',
      ),
      creadaEn: creada,
    );
    final pendientes = await cola.listarPendientes(base);
    expect(pendientes, hasLength(1));
    final idOp = pendientes.first.idOperacion;
    final pendientesDb = PendientesDb(
      abrir: AbrirFfi(),
      ruta: () async =>
          '${carpeta.path}${Platform.pathSeparator}pendientes.sqlite',
    );
    await pendientesDb.abrirDb(null);
    final sincronizador = SincronizadorMovil(
      cliente: _ClienteSincronizar(
        RespuestaSincronizacion(
          requiereNuevoPaquete: false,
          resultados: [
            ResultadoOperacion(
              idOperacion: idOp,
              estado: 'Aplicada',
              lineaId: 500,
              idCliente: 'cli-1',
              rowVersion: 'CCCC',
            ),
          ],
        ),
      ),
      cola: cola,
      pendientes: pendientesDb,
      configuracion: const ConfiguracionApp(
        urlApi: 'http://localhost',
        versionApp: '1.0.0+1',
      ),
    );
    await sincronizador.subirLote(
      base: base,
      token: 'token',
      dispositivoId: 'd1',
    );
    expect(await cola.pendientes(base), 0);
    final linea = await base.consultar(
      "SELECT estado_local FROM acta_detalles WHERE descripcion = 'Nueva línea'",
    );
    expect(linea.first['estado_local'], 'sincronizado');
    await base.cerrar();
    await pendientesDb.cerrar();
  });

  test('Rechazada mueve a operaciones_fallidas', () async {
    final carpeta = await carpetaPrueba('sync-rech');
    addTearDown(() => borrarCarpeta(carpeta));
    final ruta = '${carpeta.path}${Platform.pathSeparator}local.sqlite';
    final base = await baseConEsquema(ruta);
    await sembrar(base);
    final cola = ColaMovil(uuid: const Uuid());
    await cola.agregarLinea(
      base: base,
      numeroActa: 120,
      tipoUsuarioId: 3,
      linea: const LineaSincronizacion(
        construccionId: 7,
        conceptoId: 1,
        cantidad: '1.0000',
        precio: '50.00',
        descripcion: 'Rechazable',
      ),
      creadaEn: DateTime.utc(2026, 11, 13, 12),
    );
    final idOp = (await cola.listarPendientes(base)).first.idOperacion;
    final pendientesDb = PendientesDb(
      abrir: AbrirFfi(),
      ruta: () async => '${carpeta.path}${Platform.pathSeparator}p.sqlite',
    );
    await pendientesDb.abrirDb(null);
    final sincronizador = SincronizadorMovil(
      cliente: _ClienteSincronizar(
        RespuestaSincronizacion(
          requiereNuevoPaquete: false,
          resultados: [
            ResultadoOperacion(
              idOperacion: idOp,
              estado: 'Rechazada',
              mensaje: 'El acta ya está cerrada.',
            ),
          ],
        ),
      ),
      cola: cola,
      pendientes: pendientesDb,
      configuracion: const ConfiguracionApp(
        urlApi: 'http://localhost',
        versionApp: '1.0.0+1',
      ),
    );
    await sincronizador.subirLote(base: base, token: 't', dispositivoId: 'd');
    expect(await cola.pendientes(base), 0);
    expect(await pendientesDb.contar(), 1);
    final lista = await pendientesDb.listar();
    expect(lista.first.mensaje, contains('cerrada'));
    await base.cerrar();
    await pendientesDb.cerrar();
  });
}
