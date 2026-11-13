import 'dart:async';
import 'dart:io';

import 'package:decimal/decimal.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/services.dart';
import 'package:logger/logger.dart';
import 'package:uuid/uuid.dart';

import '../core/api/cliente_movil.dart';
import '../core/api/errores.dart';
import '../core/db/base_local.dart';
import '../core/db/consultas.dart';
import '../core/db/pendientes_db.dart';
import '../core/errores/mensajes.dart';
import '../core/seguridad/claves.dart';
import '../funciones/actas/detalle_acta_acciones.dart';
import '../sincronizacion/modelos.dart';
import '../sincronizacion/tarea_fondo.dart';
import 'dependencias.dart';
import 'limpieza_local.dart';
import 'repositorio_archivos_movil.dart';
import 'repositorio_consultas_movil.dart';
import 'ruta_notificador.dart';

part 'controlador_nucleo.dart';
part 'controlador_sesion.dart';
part 'controlador_sincronizacion.dart';
part 'controlador_consultas.dart';
part 'controlador_actas.dart';

enum RutaAplicacion {
  cargando,
  login,
  desbloqueo,
  cambiarContrasena,
  inicio,
  actualice,
}

abstract class ControladorBase extends ChangeNotifier {
  ControladorBase(this.dependencias, {Logger? registro, RutaNotificador? rutas})
    : _registro = registro ?? Logger(printer: PrettyPrinter(methodCount: 0)),
      rutas = rutas ?? RutaNotificador();

  final Dependencias dependencias;
  final RutaNotificador rutas;
  final Logger _registro;
  final Uuid _uuid = const Uuid();

  RutaAplicacion ruta = RutaAplicacion.cargando;
  bool ocupado = false;
  String? aviso;
  bool enLinea = false;
  bool tieneBase = false;
  bool debeCambiar = false;
  bool requiereConfirmacionCambio = false;
  int pendientes = 0;
  int fallidas = 0;
  bool sincronizando = false;
  double? progresoDescargaPaquete;

  bool get edicionBloqueada => ocupado || sincronizando;
  String? ultimaSincronizacion;
  UsuarioMovil? usuario;
  String? dispositivoId;

  String? _token;
  DateTime? _expira;
  DateTime? _fondo;
  StreamSubscription<bool>? _red;
  String? _loginMemoria;

  RepositorioArchivosMovil get _archivos;

  Future<BaseLocal?> _base();
  Future<void> _ocupar(Future<void> Function() accion);
  Future<T> _conToken<T>(Future<T> Function(String token) accion);
  Future<String> _claveSql();
  Future<String> _tokenVigente();
  Future<String?> _renovarSiHay();
  Future<int> _contarPendientes();
  void _fijarRuta(RutaAplicacion nueva);
  Future<void> _traerPaquete();
  Future<void> _sincronizarSiConviene();
  Future<void> _borrarDatos();
  Future<String> _dispositivo();
  Future<void> _aplicarCredenciales(Credenciales credenciales, String login);
  Future<void> _excluirRespaldo();
  Future<void> _guardarYAbrir(
    Future<List<int>> Function() descargar,
    String nombre,
  );
  String _nombreDispositivo(String id);
  String _plataforma();

  Future<void> sincronizar();
  Future<void> refrescarConteos();
  Future<DetalleActa?> detalleActa(int numero);
}

class ControladorAplicacion extends ControladorBase
    with
        ControladorNucleo,
        ControladorSesion,
        ControladorSincronizacion,
        ControladorConsultas,
        ControladorActas
    implements DetalleActaAcciones {
  ControladorAplicacion(
    super.dependencias, {
    Logger? registro,
    RutaNotificador? rutas,
  }) : super(registro: registro, rutas: rutas);

  Future<void> iniciar() async {
    _red ??= dependencias.red.cambios.listen((valor) {
      if (enLinea == valor) {
        return;
      }
      enLinea = valor;
      notifyListeners();
      if (valor) {
        unawaited(_sincronizarSiConviene());
      }
    });
    enLinea = await dependencias.red.hayConexion();
    await LimpiezaLocal.alIniciar(await dependencias.directorio());
    dispositivoId = await _dispositivo();
    dependencias.identidad.id = dispositivoId!;
    usuario = await _usuarioGuardado();
    debeCambiar = await dependencias.almacen.leer(Claves.debeCambiar) == '1';
    ultimaSincronizacion = await dependencias.almacen.leer(Claves.ultima);
    tieneBase = await dependencias.repositorio.existe();
    if (tieneBase) {
      final base = await _base();
      if (base != null) {
        await dependencias.cola.recuperarEnviando(base);
      }
      pendientes = await _contarPendientes();
    }
    final refresh = await dependencias.almacen.leer(Claves.refresh);
    _fijarRuta(
      refresh == null ? RutaAplicacion.login : RutaAplicacion.desbloqueo,
    );
  }

  @override
  void dispose() {
    _red?.cancel();
    super.dispose();
  }
}
