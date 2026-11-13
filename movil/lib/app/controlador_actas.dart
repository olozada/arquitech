part of 'controlador.dart';

mixin ControladorActas on ControladorBase {
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
}
