import 'dart:async';
import 'dart:io';

import 'package:open_filex/open_filex.dart';

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
import '../sincronizacion/modelos.dart';
import '../core/seguridad/claves.dart';
import '../funciones/actas/detalle_acta_acciones.dart';
import '../sincronizacion/tarea_fondo.dart';
import 'dependencias.dart';
import 'ruta_notificador.dart';

enum RutaAplicacion {
  cargando,
  login,
  desbloqueo,
  cambiarContrasena,
  inicio,
  actualice,
}

class ControladorAplicacion extends ChangeNotifier
    implements DetalleActaAcciones {
  ControladorAplicacion(
    this.dependencias, {
    Logger? registro,
    RutaNotificador? rutas,
  }) : _registro = registro ?? Logger(printer: PrettyPrinter(methodCount: 0)),
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

  Future<void> entrar(
    String nombre,
    String contrasena, {
    bool reemplazarUsuario = false,
  }) async {
    final usuarioTexto = nombre.trim();
    if (usuarioTexto.isEmpty) {
      aviso = Mensajes.usuario;
      notifyListeners();
      return;
    }
    if (contrasena.isEmpty) {
      aviso = Mensajes.contrasena;
      notifyListeners();
      return;
    }
    if (dependencias.configuracion.urlApi.isEmpty) {
      aviso = Mensajes.faltaServidor;
      notifyListeners();
      return;
    }

    if (!await dependencias.desbloqueador.dispositivoSoportaBloqueo()) {
      aviso = Mensajes.requiereBloqueo;
      notifyListeners();
      return;
    }

    requiereConfirmacionCambio = false;
    final loginGuardado = await dependencias.almacen.leer(Claves.login);
    if (!reemplazarUsuario &&
        loginGuardado != null &&
        loginGuardado.toLowerCase() != usuarioTexto.toLowerCase() &&
        await _contarPendientes() > 0) {
      requiereConfirmacionCambio = true;
      notifyListeners();
      return;
    }

    await _ocupar(() async {
      if (reemplazarUsuario) {
        await _borrarDatos();
        dispositivoId = await _dispositivo();
      }
      final credenciales = await dependencias.cliente.iniciar(
        usuario: usuarioTexto,
        contrasena: contrasena,
        dispositivo: DispositivoEnvio(
          id: dispositivoId!,
          nombre: _nombreDispositivo(dispositivoId!),
          plataforma: _plataforma(),
          versionApp: dependencias.configuracion.versionApp,
        ),
      );
      await _aplicarCredenciales(credenciales, usuarioTexto);
      if (credenciales.debeCambiarContrasena) {
        _fijarRuta(RutaAplicacion.cambiarContrasena);
        return;
      }
      await _traerPaquete();
      _fijarRuta(RutaAplicacion.inicio);
    });
  }

  Future<void> desbloquear() async {
    if (!await dependencias.desbloqueador.dispositivoSoportaBloqueo()) {
      aviso = Mensajes.requiereBloqueo;
      notifyListeners();
      return;
    }
    final aceptado = await dependencias.desbloqueador.desbloquear();
    if (!aceptado) {
      aviso =
          'No se pudo desbloquear. Use la biometría o el PIN del dispositivo.';
      notifyListeners();
      return;
    }
    aviso = null;
    if (debeCambiar) {
      _fijarRuta(RutaAplicacion.cambiarContrasena);
    } else {
      _fijarRuta(RutaAplicacion.inicio);
      if (!tieneBase && enLinea) {
        await sincronizar();
        return;
      }
      await _sincronizarSiConviene();
    }
  }

  Future<void> cambiarContrasena(
    String actual,
    String nueva,
    String confirmacion,
  ) async {
    final mensaje = validarCambio(actual, nueva, confirmacion);
    if (mensaje != null) {
      aviso = mensaje;
      notifyListeners();
      return;
    }

    await _ocupar(() async {
      await _conToken(
        (token) => dependencias.cliente.cambiarContrasena(
          token: token,
          actual: actual,
          nueva: nueva,
        ),
      );
      debeCambiar = false;
      await dependencias.almacen.guardar(Claves.debeCambiar, '0');
      await _traerPaquete();
      _fijarRuta(RutaAplicacion.inicio);
      aviso = Mensajes.datosActualizados;
    });
  }

  String? validarCambio(String actual, String nueva, String confirmacion) {
    return errorCambioContrasena(
      actual: actual,
      nueva: nueva,
      confirmacion: confirmacion,
      usuario: _loginMemoria,
      comunes: dependencias.comunes,
    );
  }

  String? _loginMemoria;

  Future<void> sincronizar() async {
    enLinea = await dependencias.red.hayConexion();
    if (!enLinea) {
      aviso = Mensajes.sinConexion;
      notifyListeners();
      return;
    }
    await _ocupar(() async {
      sincronizando = true;
      notifyListeners();
      try {
        final base = await _base();
        if (base == null) {
          throw const PaqueteInvalido('No hay base local.');
        }
        final clave = await _claveSql();
        await dependencias.pendientes.abrirDb(clave);
        await dependencias.cola.recuperarEnviando(base);
        const maxVueltas = 10;
        var vueltas = 0;
        var sincronizacionIncompleta = false;
        var requierePaquete = false;
        var antes = await dependencias.cola.pendientes(base);
        while (antes > 0 && vueltas < maxVueltas) {
          vueltas++;
          requierePaquete =
              await dependencias.sincronizador.subirLote(
                base: base,
                token: await _tokenVigente(),
                dispositivoId: dispositivoId!,
              ) ||
              requierePaquete;
          final despues = await dependencias.cola.pendientes(base);
          if (despues >= antes) {
            _registro.w('La sincronización no avanzó: $despues pendientes');
            aviso = Mensajes.sincronizacionIncompleta;
            sincronizacionIncompleta = true;
            break;
          }
          antes = despues;
        }
        if (antes > 0 && vueltas >= maxVueltas && !sincronizacionIncompleta) {
          aviso = Mensajes.sincronizacionIncompleta;
          sincronizacionIncompleta = true;
        }
        pendientes = await dependencias.cola.pendientes(base);
        fallidas = await dependencias.pendientes.contar();
        if (pendientes == 0) {
          await _traerPaquete();
          if (!sincronizacionIncompleta) {
            aviso = Mensajes.datosActualizados;
          }
        } else if (!sincronizacionIncompleta) {
          aviso = Mensajes.reemplazoBloqueado;
        }
        if (requierePaquete) {
          _registro.d(
            'Servidor indicó paquete nuevo (descarga según cola vacía).',
          );
        }
      } finally {
        sincronizando = false;
      }
    });
    await refrescarConteos();
  }

  Future<void> cerrarSesion() async {
    await _ocupar(() async {
      final token = _token ?? await _renovarSiHay();
      if (token != null) {
        try {
          await dependencias.cliente.salir(
            token: token,
            dispositivoId: dispositivoId!,
          );
        } catch (error, traza) {
          _registro.w(
            'No se pudo cerrar la sesión en el servidor',
            error: error,
            stackTrace: traza,
          );
        }
      }
      await _borrarDatos();
      dispositivoId = await _dispositivo();
      _fijarRuta(RutaAplicacion.login);
      aviso = null;
    });
  }

  void alFondo() {
    if (ruta == RutaAplicacion.inicio ||
        ruta == RutaAplicacion.cambiarContrasena) {
      _fondo = dependencias.ahora();
    }
  }

  void alFrente() {
    final fondo = _fondo;
    _fondo = null;
    if (fondo != null &&
        dependencias.ahora().difference(fondo) >= const Duration(minutes: 5)) {
      _token = null;
      _expira = null;
      _fijarRuta(RutaAplicacion.desbloqueo);
      return;
    }
    unawaited(_sincronizarSiConviene());
  }

  Future<void> _sincronizarSiConviene() async {
    if (ruta != RutaAplicacion.inicio ||
        !tieneBase ||
        !enLinea ||
        edicionBloqueada) {
      return;
    }
    final marca = await hayMarcaSincronizacionPendiente(
      dependencias.directorio,
    );
    final debe = marca || pendientes > 0;
    if (!debe) {
      return;
    }
    if (marca) {
      await consumirMarcaSincronizacion(dependencias.directorio);
    }
    if (pendientes > 0) {
      await sincronizar();
    } else if (marca) {
      await sincronizar();
    }
  }

  Future<List<ActaResumen>> listarActas(FiltroActas filtro) async {
    final base = await _base();
    if (base == null) {
      return const [];
    }
    return ConsultasLocales(base).listarActas(filtro);
  }

  Future<DetalleActa?> detalleActa(int numero) async {
    final base = await _base();
    if (base == null) {
      return null;
    }
    return ConsultasLocales(base).detalleActa(numero);
  }

  Future<List<PresupuestoResumen>> listarPresupuestos() async {
    final base = await _base();
    if (base == null) {
      return const [];
    }
    return ConsultasLocales(base).presupuestos();
  }

  Future<PresupuestoPaquete?> presupuestoPorId(int id) async {
    final base = await _base();
    if (base == null) {
      return null;
    }
    return ConsultasLocales(base).presupuestoPorId(id);
  }

  Future<List<ObraOpcion>> obras() async {
    final base = await _base();
    if (base == null) {
      return const [];
    }
    return ConsultasLocales(base).obras();
  }

  Future<void> refrescarConteos() async {
    if (!tieneBase) {
      pendientes = 0;
      fallidas = 0;
      notifyListeners();
      return;
    }
    pendientes = await _contarPendientes();
    try {
      await dependencias.pendientes.abrirDb(await _claveSql());
      fallidas = await dependencias.pendientes.contar();
    } catch (_) {
      fallidas = 0;
    }
    notifyListeners();
  }

  @override
  bool puedeAgregarLinea(bool actaCerrada) {
    return !actaCerrada && usuario != null && !esRevisor();
  }

  @override
  bool puedeEditarLinea(LineaDetalle linea, bool actaCerrada) {
    if (actaCerrada || usuario == null) {
      return false;
    }
    final perfil = usuario!.tipoUsuarioId;
    if (perfil == 4) {
      return false;
    }
    return perfil == linea.tipoUsuarioId;
  }

  @override
  bool esRevisor() => usuario?.tipoUsuarioId == 4;

  Future<void> agregarLinea(int numeroActa, LineaSincronizacion linea) async {
    if (edicionBloqueada) {
      return;
    }
    final error = _validarLinea(linea);
    if (error != null) {
      aviso = error;
      notifyListeners();
      return;
    }
    final base = await _base();
    if (base == null || usuario == null) {
      return;
    }
    await dependencias.cola.agregarLinea(
      base: base,
      numeroActa: numeroActa,
      tipoUsuarioId: usuario!.tipoUsuarioId,
      linea: linea,
      creadaEn: dependencias.ahora().toUtc(),
    );
    await refrescarConteos();
  }

  Future<void> editarLinea(
    int numeroActa,
    String localId,
    LineaSincronizacion linea,
  ) async {
    if (edicionBloqueada) {
      return;
    }
    final error = _validarLinea(linea);
    if (error != null) {
      aviso = error;
      notifyListeners();
      return;
    }
    final base = await _base();
    if (base == null) {
      return;
    }
    await dependencias.cola.editarLinea(
      base: base,
      numeroActa: numeroActa,
      localId: localId,
      linea: linea,
      creadaEn: dependencias.ahora().toUtc(),
    );
    await refrescarConteos();
  }

  @override
  Future<void> eliminarLinea(int numeroActa, String localId) async {
    if (edicionBloqueada) {
      return;
    }
    final base = await _base();
    if (base == null) {
      return;
    }
    await dependencias.cola.eliminarLinea(
      base: base,
      numeroActa: numeroActa,
      localId: localId,
      creadaEn: dependencias.ahora().toUtc(),
    );
    await refrescarConteos();
  }

  @override
  Future<void> aprobarTodasLineas(int numeroActa) async {
    if (edicionBloqueada || !esRevisor()) {
      return;
    }
    final detalle = await detalleActa(numeroActa);
    if (detalle == null || detalle.cerrada) {
      return;
    }
    for (final linea in detalle.lineas) {
      if (!linea.aprobado) {
        await revisarLinea(numeroActa, linea.localId, true);
      }
    }
    await refrescarConteos();
  }

  @override
  Future<void> revisarLinea(
    int numeroActa,
    String localId,
    bool aprobado,
  ) async {
    if (edicionBloqueada) {
      return;
    }
    final base = await _base();
    if (base == null) {
      return;
    }
    await dependencias.cola.revisarLinea(
      base: base,
      numeroActa: numeroActa,
      localId: localId,
      aprobado: aprobado,
      creadaEn: dependencias.ahora().toUtc(),
    );
    await refrescarConteos();
  }

  Future<List<OperacionFallidaRegistro>> listarFallidas() async {
    await dependencias.pendientes.abrirDb(await _claveSql());
    return dependencias.pendientes.listar();
  }

  Future<void> descartarFallida(String idOperacion) async {
    await dependencias.pendientes.abrirDb(await _claveSql());
    await dependencias.pendientes.borrar(idOperacion);
    fallidas = await dependencias.pendientes.contar();
    notifyListeners();
  }

  Future<List<OpcionCatalogo>> construcciones() async {
    final base = await _base();
    if (base == null) {
      return const [];
    }
    return ConsultasLocales(base).construcciones();
  }

  Future<List<OpcionCatalogo>> conceptos() async {
    final base = await _base();
    if (base == null) {
      return const [];
    }
    return ConsultasLocales(base).conceptos();
  }

  Future<List<ItemPresupuestoOpcion>> itemsPresupuesto(
    int construccionId,
  ) async {
    final base = await _base();
    if (base == null) {
      return const [];
    }
    return ConsultasLocales(base).itemsPresupuesto(construccionId);
  }

  Future<List<OpcionCatalogo>> unidadesMedida() async {
    final base = await _base();
    if (base == null) {
      return const [];
    }
    return ConsultasLocales(base).unidadesMedida();
  }

  Future<List<InsumoOpcion>> buscarInsumos(String texto) async {
    final base = await _base();
    if (base == null) {
      return const [];
    }
    return ConsultasLocales(base).buscarInsumos(texto);
  }

  Future<List<ResumenCola>> listarCola() async {
    final base = await _base();
    if (base == null) {
      return const [];
    }
    final filas = await dependencias.cola.listarPendientes(base);
    return filas
        .map(
          (fila) => ResumenCola(
            idOperacion: fila.idOperacion,
            tipo: fila.tipo,
            numeroActa: fila.numeroActa,
            localId: fila.localId,
            secuencia: fila.secuencia,
          ),
        )
        .toList();
  }

  Future<void> abrirPdfActa(int numeroActa) async {
    if (!enLinea) {
      aviso = Mensajes.sinConexion;
      notifyListeners();
      return;
    }
    await _ocupar(() async {
      final bytes = await _conToken(
        (token) => dependencias.cliente.descargarPdfActa(
          token: token,
          numeroActa: numeroActa,
        ),
      );
      final carpeta = await dependencias.directorio();
      final archivo = File(
        '${carpeta.path}${Platform.pathSeparator}acta-$numeroActa.pdf',
      );
      await archivo.writeAsBytes(bytes, flush: true);
      await OpenFilex.open(archivo.path);
    });
  }

  Future<void> abrirPdfPresupuesto(int presupuestoId) async {
    if (!enLinea) {
      aviso = Mensajes.sinConexion;
      notifyListeners();
      return;
    }
    await _guardarYAbrir(
      () => _conToken(
        (token) => dependencias.cliente.descargarPdfPresupuesto(
          token: token,
          presupuestoId: presupuestoId,
        ),
      ),
      'presupuesto-$presupuestoId.pdf',
    );
  }

  Future<void> abrirExcelPresupuesto(int presupuestoId) async {
    if (!enLinea) {
      aviso = Mensajes.sinConexion;
      notifyListeners();
      return;
    }
    await _guardarYAbrir(
      () => _conToken(
        (token) => dependencias.cliente.descargarExcelPresupuesto(
          token: token,
          presupuestoId: presupuestoId,
        ),
      ),
      'presupuesto-$presupuestoId.xlsx',
    );
  }

  Future<void> _guardarYAbrir(
    Future<List<int>> Function() descargar,
    String nombre,
  ) async {
    await _ocupar(() async {
      final bytes = await descargar();
      final carpeta = await dependencias.directorio();
      final archivo = File('${carpeta.path}${Platform.pathSeparator}$nombre');
      await archivo.writeAsBytes(bytes, flush: true);
      await OpenFilex.open(archivo.path);
    });
  }

  String? _validarLinea(LineaSincronizacion linea) {
    if (linea.construccionId <= 0) {
      return Mensajes.construccion;
    }
    if (linea.conceptoId <= 0) {
      return Mensajes.concepto;
    }
    return errorDescripcion(linea.descripcion) ??
        errorCantidad(Decimal.tryParse(linea.cantidad)) ??
        errorPrecio(Decimal.tryParse(linea.precio));
  }

  void _fijarRuta(RutaAplicacion nueva) {
    final cambio = ruta != nueva;
    ruta = nueva;
    if (cambio) {
      rutas.actualizar();
    }
    notifyListeners();
  }

  @override
  void dispose() {
    _red?.cancel();
    super.dispose();
  }

  Future<void> _traerPaquete() async {
    final carpeta = await dependencias.directorio();
    final rutaGz = '${carpeta.path}${Platform.pathSeparator}paquete.sqlite.gz';
    progresoDescargaPaquete = 0;
    notifyListeners();
    try {
      final paquete = await _conToken(
        (token) => dependencias.cliente.descargarPaquete(
          token: token,
          dispositivoId: dispositivoId!,
          rutaArchivoGz: rutaGz,
          onProgreso: (recibidos, total) {
            if (total != null && total > 0) {
              progresoDescargaPaquete = recibidos / total;
            }
            notifyListeners();
          },
        ),
      );
      progresoDescargaPaquete = null;
      notifyListeners();
      if (paquete.esquema != dependencias.configuracion.versionEsquema) {
        throw const PaqueteInvalido(Mensajes.paqueteDistinto);
      }
      final operaciones = tieneBase ? await _contarPendientes() : 0;
      await dependencias.repositorio.cerrar();
      await dependencias.importador.importar(
        archivoGz: paquete.archivoGz,
        sha256Esperado: paquete.sha256,
        clave: await _claveSql(),
        rutaActiva: await dependencias.repositorio.rutaActiva(),
        operacionesPendientes: operaciones,
        versionEsquema: dependencias.configuracion.versionEsquema,
      );
      ultimaSincronizacion = paquete.generadoEn;
      await dependencias.almacen.guardar(Claves.ultima, paquete.generadoEn);
      tieneBase = true;
      pendientes = 0;
      await _excluirRespaldo();
    } finally {
      progresoDescargaPaquete = null;
      final residual = File(rutaGz);
      if (await residual.exists()) {
        await residual.delete();
      }
      notifyListeners();
    }
  }

  Future<void> _ocupar(Future<void> Function() accion) async {
    ocupado = true;
    aviso = null;
    notifyListeners();
    try {
      await accion();
    } on DispositivoRevocado catch (error) {
      await _borrarDatos();
      dispositivoId = await _dispositivo();
      _fijarRuta(RutaAplicacion.login);
      aviso = error.mensaje;
    } on ActualizacionRequerida catch (error) {
      _fijarRuta(RutaAplicacion.actualice);
      aviso = error.mensaje;
    } on FalloMovil catch (error) {
      aviso = error.mensaje;
    } finally {
      ocupado = false;
      notifyListeners();
    }
  }

  Future<T> _conToken<T>(Future<T> Function(String token) accion) async {
    try {
      return await accion(await _tokenVigente());
    } on ErrorServidor catch (error) {
      if (error.estado != 401) {
        rethrow;
      }
      _token = null;
      _expira = null;
      return accion(await _tokenVigente());
    }
  }

  Future<String> _tokenVigente() async {
    final token = _token;
    final expira = _expira;
    if (token != null &&
        expira != null &&
        expira.isAfter(
          dependencias.ahora().toUtc().add(const Duration(seconds: 30)),
        )) {
      return token;
    }
    final renovado = await _renovarSiHay();
    if (renovado == null) {
      throw const ErrorServidor('La sesión no es válida.', estado: 401);
    }
    return renovado;
  }

  Future<String?> _renovarSiHay() async {
    final refresh = await dependencias.almacen.leer(Claves.refresh);
    if (refresh == null || refresh.isEmpty) {
      return null;
    }
    final par = await dependencias.cliente.renovar(
      refreshToken: refresh,
      dispositivoId: dispositivoId!,
    );
    _token = par.accessToken;
    _expira = par.expiraEn;
    await dependencias.almacen.guardar(Claves.refresh, par.refreshToken);
    return par.accessToken;
  }

  Future<void> _aplicarCredenciales(
    Credenciales credenciales,
    String login,
  ) async {
    _token = credenciales.accessToken;
    _expira = credenciales.expiraEn;
    usuario = credenciales.usuario;
    debeCambiar = credenciales.debeCambiarContrasena;
    _loginMemoria = login;
    final almacen = dependencias.almacen;
    await almacen.guardar(Claves.refresh, credenciales.refreshToken);
    await almacen.guardar(Claves.usuarioId, credenciales.usuario.id.toString());
    await almacen.guardar(Claves.usuarioNombre, credenciales.usuario.nombre);
    await almacen.guardar(
      Claves.tipo,
      credenciales.usuario.tipoUsuarioId.toString(),
    );
    await almacen.guardar(Claves.login, login);
    await almacen.guardar(
      Claves.debeCambiar,
      credenciales.debeCambiarContrasena ? '1' : '0',
    );
    await _guardarOpcional(
      Claves.arquitectoId,
      credenciales.usuario.arquitectoId,
    );
    await _guardarOpcional(
      Claves.contratistaId,
      credenciales.usuario.contratistaId,
    );
    await _guardarOpcional(Claves.revisorId, credenciales.usuario.revisorId);
  }

  Future<void> _guardarOpcional(String clave, int? valor) async {
    if (valor == null) {
      await dependencias.almacen.borrar(clave);
      return;
    }
    await dependencias.almacen.guardar(clave, valor.toString());
  }

  Future<UsuarioMovil?> _usuarioGuardado() async {
    final id = int.tryParse(
      await dependencias.almacen.leer(Claves.usuarioId) ?? '',
    );
    final tipo = int.tryParse(
      await dependencias.almacen.leer(Claves.tipo) ?? '',
    );
    final nombre = await dependencias.almacen.leer(Claves.usuarioNombre);
    _loginMemoria = await dependencias.almacen.leer(Claves.login);
    if (id == null || tipo == null || nombre == null) {
      return null;
    }
    return UsuarioMovil(
      id: id,
      nombre: nombre,
      tipoUsuarioId: tipo,
      arquitectoId: int.tryParse(
        await dependencias.almacen.leer(Claves.arquitectoId) ?? '',
      ),
      contratistaId: int.tryParse(
        await dependencias.almacen.leer(Claves.contratistaId) ?? '',
      ),
      revisorId: int.tryParse(
        await dependencias.almacen.leer(Claves.revisorId) ?? '',
      ),
    );
  }

  Future<String> _dispositivo() async {
    final guardado = await dependencias.almacen.leer(Claves.dispositivo);
    if (guardado != null && guardado.isNotEmpty) {
      dependencias.identidad.id = guardado;
      return guardado;
    }
    final nuevo = _uuid.v4();
    await dependencias.almacen.guardar(Claves.dispositivo, nuevo);
    dependencias.identidad.id = nuevo;
    return nuevo;
  }

  Future<String> _claveSql() async {
    final guardada = await dependencias.almacen.leer(Claves.sql);
    if (guardada != null && RegExp(r'^[0-9a-f]{64}$').hasMatch(guardada)) {
      return guardada;
    }
    final bytes = List<int>.generate(
      32,
      (_) => dependencias.aleatorio.nextInt(256),
    );
    final clave = bytes
        .map((byte) => byte.toRadixString(16).padLeft(2, '0'))
        .join();
    await dependencias.almacen.guardar(Claves.sql, clave);
    return clave;
  }

  Future<int> _contarPendientes() async {
    if (!await dependencias.repositorio.existe()) {
      return 0;
    }
    try {
      final base = await _base();
      if (base == null) {
        return 0;
      }
      return await dependencias.cola.pendientes(base);
    } catch (error, traza) {
      _registro.w('No se pudo contar la cola', error: error, stackTrace: traza);
      return 0;
    }
  }

  Future<BaseLocal?> _base() async {
    if (!tieneBase && !await dependencias.repositorio.existe()) {
      return null;
    }
    final clave = await dependencias.almacen.leer(Claves.sql);
    return dependencias.repositorio.obtener(clave);
  }

  Future<void> _borrarDatos() async {
    await dependencias.pendientes.cerrar();
    await dependencias.repositorio.cerrar();
    final carpeta = await dependencias.directorio();
    if (await carpeta.exists()) {
      for (final nombre in [
        'arquitectos.sqlite',
        'arquitectos.sqlite-wal',
        'arquitectos.sqlite-shm',
        'arquitectos.sqlite.plano',
        'arquitectos.sqlite.nueva',
        'arquitectos.sqlite.anterior',
        'pendientes.sqlite',
      ]) {
        final archivo = File('${carpeta.path}${Platform.pathSeparator}$nombre');
        if (await archivo.exists()) {
          await archivo.delete();
        }
      }
    }
    _token = null;
    _expira = null;
    usuario = null;
    debeCambiar = false;
    tieneBase = false;
    pendientes = 0;
    fallidas = 0;
    ultimaSincronizacion = null;
    _loginMemoria = null;
    final marca = File(
      '${carpeta.path}${Platform.pathSeparator}sync_pendiente.flag',
    );
    if (await marca.exists()) {
      await marca.delete();
    }
    await dependencias.almacen.borrarTodo();
  }

  Future<void> _excluirRespaldo() async {
    if (!Platform.isIOS) {
      return;
    }
    const canal = MethodChannel('com.celuweb.arquitectos/respaldo');
    try {
      await canal.invokeMethod<void>('excluir', {
        'ruta': (await dependencias.directorio()).path,
      });
    } catch (error, traza) {
      _registro.w(
        'No se pudo excluir el respaldo',
        error: error,
        stackTrace: traza,
      );
    }
  }

  String _nombreDispositivo(String id) {
    final corto = id.replaceAll('-', '');
    final sufijo = corto.length >= 8 ? corto.substring(0, 8) : corto;
    return 'Arquitectos ${_plataforma()} $sufijo';
  }

  String _plataforma() {
    if (Platform.isAndroid) {
      return 'android';
    }
    if (Platform.isIOS) {
      return 'ios';
    }
    final nombre = Platform.operatingSystem;
    return nombre.length <= 20 ? nombre : nombre.substring(0, 20);
  }
}
