part of 'controlador.dart';

mixin ControladorConsultas on ControladorBase {
  RepositorioConsultasMovil get _consultas => RepositorioConsultasMovil(_base);

  RepositorioArchivosMovil get _archivos =>
      RepositorioArchivosMovil(dependencias.directorio);

  Future<List<ActaResumen>> listarActas(FiltroActas filtro) =>
      _consultas.listarActas(filtro);

  Future<DetalleActa?> detalleActa(int numero) =>
      _consultas.detalleActa(numero);

  Future<List<PresupuestoResumen>> listarPresupuestos() =>
      _consultas.listarPresupuestos();

  Future<PresupuestoPaquete?> presupuestoPorId(int id) =>
      _consultas.presupuestoPorId(id);

  Future<List<ObraOpcion>> obras() => _consultas.obras();

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

  Future<List<OpcionCatalogo>> construcciones() => _consultas.construcciones();

  Future<List<OpcionCatalogo>> conceptos() => _consultas.conceptos();

  Future<List<ItemPresupuestoOpcion>> itemsPresupuesto(int construccionId) =>
      _consultas.itemsPresupuesto(construccionId);

  Future<List<OpcionCatalogo>> unidadesMedida() => _consultas.unidadesMedida();

  Future<List<InsumoOpcion>> buscarInsumos(String texto) =>
      _consultas.buscarInsumos(texto);

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
      await _archivos.abrir(bytes, 'acta-$numeroActa.pdf');
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
}
