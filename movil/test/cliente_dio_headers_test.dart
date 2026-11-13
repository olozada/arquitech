import 'package:arquitectos_movil/app/configuracion.dart';
import 'package:arquitectos_movil/core/api/cliente_dio.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('cabecerasMovil separa versión y build', () {
    const configuracion = ConfiguracionApp(
      urlApi: 'http://localhost',
      versionApp: '1.0.0',
      buildApp: '42',
    );
    final headers = cabecerasMovil(configuracion);
    expect(headers['X-Version-App'], '1.0.0');
    expect(headers['X-Build-App'], '42');
  });
}
