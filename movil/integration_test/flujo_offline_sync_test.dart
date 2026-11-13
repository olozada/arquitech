import 'package:integration_test/integration_test.dart';

import '../test/flujo_integracion_sync_test.dart' as flujo;

/// Puerta de entrada para correr en dispositivo/emulador:
/// `flutter test integration_test/flujo_offline_sync_test.dart -d <device>`
void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();
  flujo.main();
}
