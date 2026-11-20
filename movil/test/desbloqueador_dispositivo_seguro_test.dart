import 'package:arquitectos_movil/core/seguridad/desbloqueador.dart';
import 'package:flutter/foundation.dart';
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
            expect(llamada.method, isIn(['esEmulador', 'dispositivoSeguro']));
            return false;
          });
      final desbloqueador = DesbloqueadorLocal();
      expect(await desbloqueador.dispositivoSoportaBloqueo(), isFalse);
    });

    test('sin canal registrado → false (MissingPluginException)', () async {
      final desbloqueador = DesbloqueadorLocal();
      expect(await desbloqueador.dispositivoSoportaBloqueo(), isFalse);
    });

    test('canal devuelve true → hay bloqueo configurado', () async {
      TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
          .setMockMethodCallHandler(canalSeguridadDispositivo, (llamada) async {
            return llamada.method == 'dispositivoSeguro';
          });
      final desbloqueador = DesbloqueadorLocal();
      expect(await desbloqueador.dispositivoSoportaBloqueo(), isTrue);
    });

    test('emulador en desarrollo no exige PIN', () async {
      TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
          .setMockMethodCallHandler(canalSeguridadDispositivo, (llamada) async {
            return llamada.method == 'esEmulador';
          });
      final desbloqueador = DesbloqueadorLocal();
      expect(await desbloqueador.dispositivoSoportaBloqueo(), isTrue);
      expect(await desbloqueador.desbloquear(), isTrue);
    });
  });
}
