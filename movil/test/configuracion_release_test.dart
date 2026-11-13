import 'package:arquitectos_movil/app/configuracion.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('en release solo permite API_URL con https', () {
    expect(
      urlApiPermitidaEnRelease('http://api.ejemplo', release: true),
      isFalse,
    );
    expect(
      urlApiPermitidaEnRelease('https://api.ejemplo', release: true),
      isTrue,
    );
    expect(
      urlApiPermitidaEnRelease('http://api.ejemplo', release: false),
      isTrue,
    );
  });

  test('entrar rechaza http en release simulado', () async {
    expect(
      urlApiPermitidaEnRelease(
        const ConfiguracionApp(urlApi: 'http://x', versionApp: '1.0.0').urlApi,
        release: true,
      ),
      isFalse,
    );
  });
}
