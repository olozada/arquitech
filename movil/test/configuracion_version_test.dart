import 'package:arquitectos_movil/app/configuracion.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('versionApp y buildApp se conservan por separado', () {
    const configuracion = ConfiguracionApp(
      urlApi: 'http://localhost',
      versionApp: '1.0.0',
      buildApp: '1',
    );
    expect(configuracion.versionApp, '1.0.0');
    expect(configuracion.buildApp, '1');
    expect(configuracion.versionAppRegistro, '1.0.0+1');
  });
}
