import 'package:flutter/foundation.dart';

/// Solo dispara al [GoRouter]; evita reconstruir toda la navegación por cambios de red.
final class RutaNotificador extends ChangeNotifier {
  void actualizar() => notifyListeners();
}
