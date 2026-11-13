import '../core/db/base_local.dart';
import '../core/db/consultas.dart';

/// Lecturas de la base local usadas por el controlador de pantalla.
class RepositorioConsultasMovil {
  const RepositorioConsultasMovil(this._base);

  final Future<BaseLocal?> Function() _base;

  ConsultasLocales? _consultas(BaseLocal? base) =>
      base == null ? null : ConsultasLocales(base);

  Future<List<ActaResumen>> listarActas(FiltroActas filtro) async {
    final consultas = _consultas(await _base());
    return consultas == null ? const [] : consultas.listarActas(filtro);
  }

  Future<DetalleActa?> detalleActa(int numero) async {
    final consultas = _consultas(await _base());
    return consultas?.detalleActa(numero);
  }

  Future<List<PresupuestoResumen>> listarPresupuestos() async {
    final consultas = _consultas(await _base());
    return consultas == null ? const [] : consultas.presupuestos();
  }

  Future<PresupuestoPaquete?> presupuestoPorId(int id) async {
    final consultas = _consultas(await _base());
    return consultas?.presupuestoPorId(id);
  }

  Future<List<ObraOpcion>> obras() async {
    final consultas = _consultas(await _base());
    return consultas == null ? const [] : consultas.obras();
  }

  Future<List<OpcionCatalogo>> construcciones() async {
    final consultas = _consultas(await _base());
    return consultas == null ? const [] : consultas.construcciones();
  }

  Future<List<OpcionCatalogo>> conceptos() async {
    final consultas = _consultas(await _base());
    return consultas == null ? const [] : consultas.conceptos();
  }

  Future<List<ItemPresupuestoOpcion>> itemsPresupuesto(
    int construccionId,
  ) async {
    final consultas = _consultas(await _base());
    return consultas == null
        ? const []
        : consultas.itemsPresupuesto(construccionId);
  }

  Future<List<OpcionCatalogo>> unidadesMedida() async {
    final consultas = _consultas(await _base());
    return consultas == null ? const [] : consultas.unidadesMedida();
  }

  Future<List<InsumoOpcion>> buscarInsumos(String texto) async {
    final consultas = _consultas(await _base());
    return consultas == null ? const [] : consultas.buscarInsumos(texto);
  }
}
