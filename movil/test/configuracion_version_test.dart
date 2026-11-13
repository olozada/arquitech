import 'package:arquitectos_movil/app/configuracion.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('X-Version-App usa la versión del paquete', () {
    const configuracion = ConfiguracionApp(
      urlApi: 'http://localhost',
      versionApp: '1.0.0+1',
    );
    expect(configuracion.versionApp, '1.0.0+1');
  });
}
