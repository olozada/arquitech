import 'package:arquitectos_movil/app/configuracion.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('en release solo permite API_URL con https', () {
    expect(
      urlApiPermitidaEnRelease(
        const ConfiguracionApp(
          urlApi: 'http://api.ejemplo',
          versionApp: '1.0.0',
          esRelease: true,
        ),
      ),
      isFalse,
    );
    expect(
      urlApiPermitidaEnRelease(
        const ConfiguracionApp(
          urlApi: 'https://api.ejemplo',
          versionApp: '1.0.0',
          esRelease: true,
        ),
      ),
      isTrue,
    );
    expect(
      urlApiPermitidaEnRelease(
        const ConfiguracionApp(
          urlApi: 'http://api.ejemplo',
          versionApp: '1.0.0',
          esRelease: false,
        ),
      ),
      isTrue,
    );
  });
}
