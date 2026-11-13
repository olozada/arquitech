import 'dart:io';

import 'package:arquitectos_movil/app/configuracion.dart';
import 'package:arquitectos_movil/core/api/cliente_movil.dart';
import 'package:arquitectos_movil/core/db/base_local.dart';
import 'package:arquitectos_movil/core/db/pendientes_db.dart';
import 'package:arquitectos_movil/sincronizacion/cola_movil.dart';
import 'package:arquitectos_movil/sincronizacion/modelos.dart';
import 'package:arquitectos_movil/sincronizacion/sincronizador_movil.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:sqflite_common_ffi/sqflite_ffi.dart';
import 'package:uuid/uuid.dart';

import 'soporte.dart';

class ColaMovilConCorte extends ColaMovil {
  ColaMovilConCorte({super.uuid});

  var cortarQuitar = true;

  @override
  Future<void> quitarOperacion(BaseLocal base, String idOperacion) async {
    if (cortarQuitar) {
      cortarQuitar = false;
      throw Exception('Simula caída antes de vaciar outbox');
    }
    return super.quitarOperacion(base, idOperacion);
  }
}

void main() {
  setUpAll(() {
    TestWidgetsFlutterBinding.ensureInitialized();
    sqfliteFfiInit();
    databaseFactory = databaseFactoryFfi;
  });

  test(
    'rechazo idempotente: un solo registro en fallidas tras reintento',
    () async {
      final carpeta = await carpetaPrueba('sync-idem');
      addTearDown(() => borrarCarpeta(carpeta));
      final ruta = '${carpeta.path}${Platform.pathSeparator}local.sqlite';
      final base = await baseConEsquema(ruta);
      await sembrar(base);
      final cola = ColaMovilConCorte(uuid: const Uuid());
      await cola.agregarLinea(
        base: base,
        numeroActa: 120,
        tipoUsuarioId: 3,
        linea: const LineaSincronizacion(
          construccionId: 7,
          conceptoId: 1,
          cantidad: '1.0000',
          precio: '50.00',
          descripcion: 'Caída',
        ),
        creadaEn: DateTime.utc(2026, 11, 13, 12),
      );
      final idOp = (await cola.listarPendientes(base)).first.idOperacion;
      final pendientesDb = PendientesDb(
        abrir: AbrirFfi(),
        ruta: () async => '${carpeta.path}${Platform.pathSeparator}p.sqlite',
      );
      await pendientesDb.abrirDb(null);
      final cliente = _ClienteRechazo(idOp);
      final sincronizador = SincronizadorMovil(
        cliente: cliente,
        cola: cola,
        pendientes: pendientesDb,
        configuracion: const ConfiguracionApp(urlApi: 'http://localhost'),
      );

      await sincronizador.subirLote(base: base, token: 't', dispositivoId: 'd');
      expect(await cola.pendientes(base), 0);
      expect(await pendientesDb.contar(), 1);

      await sincronizador.subirLote(base: base, token: 't', dispositivoId: 'd');
      expect(await pendientesDb.contar(), 1);

      await base.cerrar();
      await pendientesDb.cerrar();
    },
  );
}

class _ClienteRechazo implements ClienteMovil {
  _ClienteRechazo(this.idOp);

  final String idOp;

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
        ResultadoOperacion(
          idOperacion: idOp,
          estado: 'Rechazada',
          mensaje: 'El acta ya está cerrada.',
        ),
      ],
    );
  }

  @override
  Future<void> cambiarContrasena({
    required String token,
    required String actual,
    required String nueva,
  }) async {}

  @override
  Future<PaqueteDescargado> descargarPaquete({
    required String token,
    required String dispositivoId,
    required String rutaArchivoGz,
    void Function(int recibidos, int? total)? onProgreso,
  }) async => throw UnimplementedError();

  @override
  Future<List<int>> descargarExcelPresupuesto({
    required String token,
    required int presupuestoId,
  }) async => const [];

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
  Future<void> salir({
    required String token,
    required String dispositivoId,
  }) async {}
}
