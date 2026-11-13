part of 'controlador.dart';

mixin ControladorSesion on ControladorBase {
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
    if (!urlApiPermitidaEnRelease(dependencias.configuracion)) {
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
          versionApp: dependencias.configuracion.versionAppRegistro,
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
}
