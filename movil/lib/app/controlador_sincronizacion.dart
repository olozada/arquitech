part of 'controlador.dart';

mixin ControladorSincronizacion on ControladorBase {
  Future<void> sincronizar() async {
    if (sincronizando || ocupado) {
      return;
    }
    sincronizando = true;
    notifyListeners();
    try {
      enLinea = await dependencias.red.hayConexion();
      if (!enLinea) {
        aviso = Mensajes.sinConexion;
        notifyListeners();
        return;
      }
      await _ocupar(() async {
        final base = await _base();
        if (base == null) {
          await _traerPaquete();
          aviso = Mensajes.datosActualizados;
          return;
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
      });
      await refrescarConteos();
    } finally {
      sincronizando = false;
      notifyListeners();
    }
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
}
