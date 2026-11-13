import 'dart:io';

import 'package:arquitectos_movil/core/api/cliente_movil.dart';
import 'package:arquitectos_movil/core/decimal/dinero.dart';
import 'package:arquitectos_movil/sincronizacion/modelos.dart';
import 'package:crypto/crypto.dart';
import 'package:decimal/decimal.dart';

import '../test/soporte.dart';

/// Estado del “servidor” móvil sin HTTP (para integration_test en VM).
class SimuladorApiMovil {
  late List<int> _gzip;
  late String _sha256;
  var _generado = '2026-11-13T15:00:00.0000000Z';

  String get sha256Paquete => _sha256;

  String get generadoPaquete => _generado;
  var _requierePaqueteTrasSync = false;

  Future<void> iniciar() async {
    final carpeta = await Directory.systemTemp.createTemp('sim-movil');
    final paquete = await paqueteDePrueba(carpeta);
    _gzip = paquete.gzip;
    _sha256 = paquete.sha256;
  }

  Credenciales login(String usuario) {
    final revisor =
        usuario.toLowerCase().contains('rosa') ||
        usuario.toLowerCase() == 'revisor';
    return Credenciales(
      accessToken: 'token-prueba',
      expiraEn: DateTime.utc(2026, 11, 14, 12),
      refreshToken: 'refresh-prueba',
      debeCambiarContrasena: false,
      usuario: revisor
          ? const UsuarioMovil(
              id: 5,
              nombre: 'Rosa Revisor',
              tipoUsuarioId: 4,
              revisorId: 1,
            )
          : const UsuarioMovil(
              id: 4,
              nombre: 'Carlos Contratista',
              tipoUsuarioId: 3,
              contratistaId: 1,
            ),
    );
  }

  /// Simula en el servidor una línea ya sincronizada por otro perfil.
  Future<void> agregarLineaRemotaEnPaquete({
    required String descripcion,
    int lineaId = 777,
  }) async {
    final carpeta = await Directory.systemTemp.createTemp('sim-remota');
    final plano = File('${carpeta.path}${Platform.pathSeparator}plano.sqlite');
    final db = await baseConEsquema(plano.path);
    await sembrar(db);
    final cantidad = '1.0000';
    final precio = '200.00';
    final subtotal = subtotalLinea(
      Decimal.parse(cantidad),
      Decimal.parse(precio),
    ).toString();
    await db.ejecutar(
      '''
INSERT INTO acta_detalles (
  local_id, id, id_cliente, numero_acta, construccion_id, concepto_id, cantidad, precio, subtotal,
  descripcion, tipo_usuario_id, aprobado, row_version, estado_local
) VALUES (?, ?, ?, 120, 7, 1, ?, ?, ?, ?, 3, 1, 'REM-777', 'sincronizado')
''',
      [
        'remota-777',
        lineaId,
        'remota-777',
        cantidad,
        precio,
        subtotal,
        descripcion,
      ],
    );
    await db.cerrar();
    final bytes = await plano.readAsBytes();
    _gzip = gzip.encode(bytes);
    _sha256 = sha256.convert(bytes).toString();
    _generado = '2026-11-13T17:00:00.0000000Z';
  }

  List<int> bytesGzip() => List<int>.from(_gzip);

  Future<PaqueteDescargado> paqueteEn(String rutaArchivoGz) async {
    final archivo = File(rutaArchivoGz);
    await archivo.parent.create(recursive: true);
    await archivo.writeAsBytes(_gzip);
    return PaqueteDescargado(
      archivoGz: archivo,
      sha256: _sha256,
      esquema: 1,
      generadoEn: _generado,
    );
  }

  Future<RespuestaSincronizacion> sincronizar(
    List<OperacionSalida> operaciones,
  ) async {
    final resultados = <ResultadoOperacion>[];
    for (final op in operaciones) {
      if (op.tipo == 'AgregarLinea') {
        final idCliente = op.idCliente ?? 'cli-srv';
        resultados.add(
          ResultadoOperacion(
            idOperacion: op.idOperacion,
            estado: 'Aplicada',
            lineaId: 501,
            idCliente: idCliente,
            rowVersion: 'SYN-501',
          ),
        );
        _requierePaqueteTrasSync = true;
        final linea = op.linea;
        if (linea != null) {
          await _regenerarPaqueteConLinea(idCliente, linea);
        }
      } else {
        resultados.add(
          ResultadoOperacion(idOperacion: op.idOperacion, estado: 'Aplicada'),
        );
      }
    }
    final requiere = _requierePaqueteTrasSync;
    _requierePaqueteTrasSync = false;
    return RespuestaSincronizacion(
      resultados: resultados,
      requiereNuevoPaquete: requiere,
    );
  }

  Future<void> _regenerarPaqueteConLinea(
    String idCliente,
    LineaSincronizacion linea,
  ) async {
    final carpeta = await Directory.systemTemp.createTemp('sim-paq');
    final plano = File('${carpeta.path}${Platform.pathSeparator}plano.sqlite');
    final db = await baseConEsquema(plano.path);
    await sembrar(db);
    final cantidad = Decimal.parse(linea.cantidad);
    final precio = Decimal.parse(linea.precio);
    final subtotal = subtotalLinea(cantidad, precio).toString();
    await db.ejecutar(
      '''
INSERT INTO acta_detalles (
  local_id, id, id_cliente, numero_acta, construccion_id, concepto_id, cantidad, precio, subtotal,
  descripcion, tipo_usuario_id, aprobado, row_version, estado_local
) VALUES (?, 501, ?, 120, ?, ?, ?, ?, ?, ?, 3, 1, 'SYN-501', 'sincronizado')
''',
      [
        idCliente,
        idCliente,
        linea.construccionId,
        linea.conceptoId,
        linea.cantidad,
        linea.precio,
        subtotal,
        linea.descripcion,
      ],
    );
    await db.cerrar();
    final bytes = await plano.readAsBytes();
    _gzip = gzip.encode(bytes);
    _sha256 = sha256.convert(bytes).toString();
    _generado = '2026-11-13T16:00:00.0000000Z';
  }
}

class ClienteSimuladorApi implements ClienteMovil {
  ClienteSimuladorApi(this.simulador);

  final SimuladorApiMovil simulador;

  @override
  Future<Credenciales> iniciar({
    required String usuario,
    required String contrasena,
    required DispositivoEnvio dispositivo,
  }) async => simulador.login(usuario);

  @override
  Future<ParTokens> renovar({
    required String refreshToken,
    required String dispositivoId,
  }) async {
    return ParTokens(
      accessToken: 'token-prueba-2',
      expiraEn: DateTime.utc(2026, 11, 14, 14),
      refreshToken: 'refresh-prueba-2',
    );
  }

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
  }) async {
    final paq = await simulador.paqueteEn(rutaArchivoGz);
    onProgreso?.call(_tamanoGz(paq.archivoGz), _tamanoGz(paq.archivoGz));
    return paq;
  }

  int _tamanoGz(File archivo) => archivo.lengthSync();

  @override
  Future<RespuestaSincronizacion> sincronizar({
    required String token,
    required String dispositivoId,
    required String dispositivoUuid,
    required int versionEsquema,
    required List<OperacionSalida> operaciones,
  }) async => await simulador.sincronizar(operaciones);

  @override
  Future<List<int>> descargarPdfActa({
    required String token,
    required int numeroActa,
  }) async => const [37, 80, 68, 70];

  @override
  Future<List<int>> descargarPdfPresupuesto({
    required String token,
    required int presupuestoId,
  }) async => const [37, 80, 68, 70];

  @override
  Future<List<int>> descargarExcelPresupuesto({
    required String token,
    required int presupuestoId,
  }) async => const [80, 75, 76, 88];
}
