import '../../core/db/consultas.dart';

/// Acciones de UI del detalle de acta (desacoplado del controlador completo).
abstract class DetalleActaAcciones {
  bool esRevisor();

  bool puedeAgregarLinea(bool actaCerrada);

  bool puedeEditarLinea(LineaDetalle linea, bool actaCerrada);

  Future<void> aprobarTodasLineas(int numeroActa);

  Future<void> revisarLinea(int numeroActa, String localId, bool aprobado);

  Future<void> eliminarLinea(int numeroActa, String localId);
}
