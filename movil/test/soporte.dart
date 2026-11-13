import 'dart:io';

import 'package:arquitectos_movil/app/configuracion.dart';
import 'package:arquitectos_movil/app/dependencias.dart';
import 'package:arquitectos_movil/core/api/cliente_movil.dart';
import 'package:arquitectos_movil/core/api/errores.dart';
import 'package:arquitectos_movil/core/api/red.dart';
import 'package:arquitectos_movil/core/db/base_local.dart';
import 'package:arquitectos_movil/core/db/esquema.dart';
import 'package:arquitectos_movil/core/db/importador_paquete.dart';
import 'package:arquitectos_movil/core/db/pendientes_db.dart';
import 'package:arquitectos_movil/core/db/repositorio.dart';
import 'package:arquitectos_movil/core/seguridad/almacen_seguro.dart';
import 'package:arquitectos_movil/core/seguridad/desbloqueador.dart';
import 'package:arquitectos_movil/sincronizacion/cola_movil.dart';
import 'package:arquitectos_movil/sincronizacion/modelos.dart';
import 'package:arquitectos_movil/sincronizacion/sincronizador_movil.dart';
import 'package:crypto/crypto.dart';
import 'package:sqflite_common_ffi/sqflite_ffi.dart';

Future<Directory> carpetaPrueba(String prefijo) async {
  final base = Directory(
    '${Directory.current.path}${Platform.pathSeparator}.test_tmp',
  );
  await base.create(recursive: true);
  final dir = Directory(
    '${base.path}${Platform.pathSeparator}$prefijo-${DateTime.now().microsecondsSinceEpoch}',
  );
  await dir.create(recursive: true);
  return dir;
}

Future<void> borrarCarpeta(Directory carpeta) async {
  if (!await carpeta.exists()) {
    return;
  }
  await for (final entidad in carpeta.list(recursive: true)) {
    if (entidad is File && entidad.path.endsWith('.sqlite')) {
      await databaseFactoryFfiNoIsolate.deleteDatabase(entidad.path);
    }
  }
  for (var intento = 0; intento < 5; intento++) {
    try {
      if (await carpeta.exists()) {
        await carpeta.delete(recursive: true);
      }
      return;
    } on FileSystemException {
      await Future<void>.delayed(Duration(milliseconds: 40 * (intento + 1)));
    }
  }
}

class AlmacenMemoria implements AlmacenSeguro {
  final Map<String, String> datos = {};

  @override
  Future<void> borrar(String clave) async {
    datos.remove(clave);
  }

  @override
  Future<void> borrarTodo() async {
    datos.clear();
  }

  @override
  Future<void> guardar(String clave, String valor) async {
    datos[clave] = valor;
  }

  @override
  Future<String?> leer(String clave) async => datos[clave];
}

class AbrirFfi implements AbrirBase {
  @override
  Future<BaseLocal> abrir(String ruta, {String? clave}) async {
    final db = await databaseFactoryFfiNoIsolate.openDatabase(ruta);
    return BaseAdaptable(db);
  }
}

class CifradorCopia implements CifradorBase {
  @override
  Future<void> exportar(String plano, String destino, String clave) async {
    await File(plano).copy(destino);
  }
}

class PaqueteListo {
  PaqueteListo({required this.gzip, required this.sha256});

  final List<int> gzip;
  final String sha256;
}

Future<PaqueteListo> paqueteDePrueba(Directory carpeta) async {
  final plano = File('${carpeta.path}${Platform.pathSeparator}plano.sqlite');
  final db = await databaseFactoryFfiNoIsolate.openDatabase(plano.path);
  final base = BaseAdaptable(db);
  await aplicarEsquema(
    base,
    File('assets/esquema_movil_v1.sql').readAsStringSync(),
  );
  await sembrar(base);
  await base.cerrar();
  final bytes = await plano.readAsBytes();
  return PaqueteListo(
    gzip: gzip.encode(bytes),
    sha256: sha256.convert(bytes).toString(),
  );
}

Future<BaseLocal> baseConEsquema(String ruta) async {
  final db = await databaseFactoryFfiNoIsolate.openDatabase(ruta);
  final base = BaseAdaptable(db);
  await aplicarEsquema(
    base,
    File('assets/esquema_movil_v1.sql').readAsStringSync(),
  );
  return base;
}

Future<void> sembrar(BaseLocal base) async {
  await base.ejecutar(
    "INSERT INTO meta (clave, valor) VALUES ('version_esquema', '1')",
  );
  await base.ejecutar(
    "INSERT INTO tipos_usuario (id, nombre) VALUES (2, 'Arquitecto'), (3, 'Contratista')",
  );
  await base.ejecutar(
    "INSERT INTO arquitectos (id, nombre) VALUES (1, 'Ana Arquitecta')",
  );
  await base.ejecutar(
    "INSERT INTO revisores (id, nombre) VALUES (1, 'Rosa Revisor')",
  );
  await base.ejecutar(
    "INSERT INTO contratistas (id, nombre) VALUES (1, 'Carlos Contratista')",
  );
  await base.ejecutar(
    "INSERT INTO construcciones (id, descripcion) VALUES (7, 'Cimentación')",
  );
  await base.ejecutar(
    "INSERT INTO conceptos (id, descripcion) VALUES (1, 'Excavación')",
  );
  await base.ejecutar(
    "INSERT INTO unidades_medida (id, descripcion) VALUES (1, 'm³')",
  );
  await base.ejecutar(
    "INSERT INTO insumos (id, codigo, descripcion, unidad) VALUES (1, 'CEM', 'Cemento', 'bulto')",
  );
  await base.ejecutar('''
INSERT INTO actas (
  numero_acta, fecha_inicial, fecha_final, arquitecto_id, revisor_id, contratista_id,
  observacion, cerrada, fecha_registro, row_version
) VALUES (
  120, '2026-11-01', '2026-11-30', 1, 1, 1, 'Obra norte', 0, '2026-11-01T00:00:00Z', 'AAAA'
)
''');
  await base.ejecutar('''
INSERT INTO actas (
  numero_acta, fecha_inicial, fecha_final, arquitecto_id, revisor_id, contratista_id,
  cerrada, fecha_registro, row_version
) VALUES (
  80, '2026-01-01', '2026-01-31', 1, 1, 1, 1, '2026-01-01T00:00:00Z', 'BBBB'
)
''');
  await base.ejecutar('''
INSERT INTO acta_detalles (
  local_id, id, numero_acta, construccion_id, concepto_id, cantidad, precio, subtotal,
  descripcion, tipo_usuario_id, aprobado, estado_local
) VALUES (
  's1', 1, 120, 7, 1, '2.0000', '750.00', '1500.00', 'Excavación manual', 3, 1, 'sincronizado'
)
''');
  await base.ejecutar('''
INSERT INTO acta_detalles (
  local_id, id, numero_acta, construccion_id, concepto_id, cantidad, precio, subtotal,
  descripcion, tipo_usuario_id, aprobado, estado_local
) VALUES (
  's2', 2, 120, 7, 1, '1.0000', '999.00', '999.00', 'Rechazada', 3, 0, 'sincronizado'
)
''');
}

class ClienteFalso implements ClienteMovil {
  ClienteFalso(this.paquete);

  final PaqueteListo paquete;
  String? errorLogin;
  var descargas = 0;
  var revocarEnSincronizar = false;
  var revocarEnRenovar = false;

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
  }) async {
    descargas += 1;
    final archivo = File(rutaArchivoGz);
    await archivo.parent.create(recursive: true);
    await archivo.writeAsBytes(paquete.gzip);
    onProgreso?.call(paquete.gzip.length, paquete.gzip.length);
    return PaqueteDescargado(
      archivoGz: archivo,
      sha256: paquete.sha256,
      esquema: 1,
      generadoEn: '2026-11-13T15:00:00Z',
    );
  }

  @override
  Future<Credenciales> iniciar({
    required String usuario,
    required String contrasena,
    required DispositivoEnvio dispositivo,
  }) async {
    if (errorLogin != null) {
      throw Exception(errorLogin);
    }
    return Credenciales(
      accessToken: 'acceso',
      expiraEn: DateTime.utc(2026, 11, 13, 16),
      refreshToken: 'refresco',
      debeCambiarContrasena: false,
      usuario: const UsuarioMovil(
        id: 4,
        nombre: 'Ana Arquitecta',
        tipoUsuarioId: 2,
        arquitectoId: 1,
      ),
    );
  }

  @override
  Future<ParTokens> renovar({
    required String refreshToken,
    required String dispositivoId,
  }) async {
    if (revocarEnRenovar) {
      throw const DispositivoRevocado('Este dispositivo fue revocado.');
    }
    return ParTokens(
      accessToken: 'acceso',
      expiraEn: DateTime.utc(2026, 11, 13, 16),
      refreshToken: 'refresco-2',
    );
  }

  @override
  Future<void> salir({
    required String token,
    required String dispositivoId,
  }) async {}

  @override
  Future<List<int>> descargarPdfActa({
    required String token,
    required int numeroActa,
  }) async {
    return const [37, 80, 68, 70];
  }

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

  @override
  Future<RespuestaSincronizacion> sincronizar({
    required String token,
    required String dispositivoId,
    required String dispositivoUuid,
    required int versionEsquema,
    required List<OperacionSalida> operaciones,
  }) async {
    if (revocarEnSincronizar) {
      throw const DispositivoRevocado('Este dispositivo fue revocado.');
    }
    return const RespuestaSincronizacion(
      resultados: [],
      requiereNuevoPaquete: false,
    );
  }
}

Dependencias dependenciasPrueba({
  required Directory carpeta,
  required ClienteMovil cliente,
  ConfiguracionApp configuracion = const ConfiguracionApp(
    urlApi: 'http://localhost:5190',
  ),
  AlmacenSeguro? almacen,
  Set<String> comunes = const {},
  DateTime Function()? ahora,
}) {
  final abrir = AbrirFfi();
  final repositorio = RepositorioLocal(
    directorio: () async => carpeta,
    abrir: abrir,
  );
  final cola = ColaMovil();
  final pendientes = PendientesDb(
    abrir: abrir,
    ruta: () async => repositorio.rutaPendientes(),
  );
  return Dependencias(
    configuracion: configuracion,
    cliente: cliente,
    almacen: almacen ?? AlmacenMemoria(),
    desbloqueador: DesbloqueadorFijo(true),
    red: RedFija(true),
    repositorio: repositorio,
    importador: ImportadorPaquete(cifrador: CifradorCopia(), abrir: abrir),
    cola: cola,
    pendientes: pendientes,
    sincronizador: SincronizadorMovil(
      cliente: cliente,
      cola: cola,
      pendientes: pendientes,
      configuracion: configuracion,
      ahora: ahora,
    ),
    comunes: comunes,
    directorio: () async => carpeta,
    ahora: ahora,
  );
}
