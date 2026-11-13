import 'package:arquitectos_movil/core/seguridad/desbloqueador.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  group('dispositivoSoportaBloqueo (Android)', () {
    setUp(() {
      debugDefaultTargetPlatformOverride = TargetPlatform.android;
    });
    tearDown(() {
      debugDefaultTargetPlatformOverride = null;
      TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
          .setMockMethodCallHandler(canalSeguridadDispositivo, null);
    });

    test('canal devuelve false → no hay bloqueo configurado', () async {
      TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
          .setMockMethodCallHandler(canalSeguridadDispositivo, (llamada) async {
            expect(llamada.method, 'dispositivoSeguro');
            return false;
          });
      final desbloqueador = DesbloqueadorLocal();
      expect(await desbloqueador.dispositivoSoportaBloqueo(), isFalse);
    });

    test('canal devuelve true → hay bloqueo configurado', () async {
      TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
          .setMockMethodCallHandler(canalSeguridadDispositivo, (llamada) async {
            return true;
          });
      final desbloqueador = DesbloqueadorLocal();
      expect(await desbloqueador.dispositivoSoportaBloqueo(), isTrue);
    });
  });
}
