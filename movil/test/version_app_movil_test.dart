// Casos espejo de backend/tests/Arquitectos.Tests/VersionAppTests.cs (EsMenor_evalua_metadatos_semver).
import 'package:arquitectos_movil/core/api/version_app_movil.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('versionAppEsMenor (paridad VersionApp)', () {
    test('1.0.0+1 no es menor que 1.0.0', () {
      expect(versionAppEsMenor('1.0.0+1', '1.0.0'), isFalse);
    });
    test('1.2.0-beta no es menor que 1.1.0', () {
      expect(versionAppEsMenor('1.2.0-beta', '1.1.0'), isFalse);
    });
    test('0.9.0 es menor que 1.0.0', () {
      expect(versionAppEsMenor('0.9.0', '1.0.0'), isTrue);
    });
    test('vacío cuenta como menor que 1.0.0', () {
      expect(versionAppEsMenor('', '1.0.0'), isTrue);
    });
    test('abc cuenta como menor que 1.0.0', () {
      expect(versionAppEsMenor('abc', '1.0.0'), isTrue);
    });
  });
}
