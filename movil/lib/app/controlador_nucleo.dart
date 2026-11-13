part of 'controlador.dart';

mixin ControladorNucleo on ControladorBase {
  Future<void> _guardarYAbrir(
    Future<List<int>> Function() descargar,
    String nombre,
  ) async {
    await _ocupar(() async {
      final bytes = await descargar();
      await _archivos.abrir(bytes, nombre);
    });
  }

  void _fijarRuta(RutaAplicacion nueva) {
    final cambio = ruta != nueva;
    ruta = nueva;
    if (cambio) {
      rutas.actualizar();
    }
    notifyListeners();
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
        'paquete.sqlite.gz',
        'pendientes.sqlite',
      ]) {
        final archivo = File('${carpeta.path}${Platform.pathSeparator}$nombre');
        if (await archivo.exists()) {
          await archivo.delete();
        }
      }
      await LimpiezaLocal.borrarDescargas(carpeta);
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
