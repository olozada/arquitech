import 'package:arquitectos_movil/app/controlador.dart';
import 'package:flutter_test/flutter_test.dart';

import 'soporte.dart';

void main() {
  test('edicionBloqueada refleja ocupado o sincronizando', () async {
    final carpeta = await carpetaPrueba('ctrl-ed');
    addTearDown(() => borrarCarpeta(carpeta));
    final controlador = ControladorAplicacion(
      dependenciasPrueba(
        carpeta: carpeta,
        cliente: ClienteFalso(PaqueteListo(gzip: const [], sha256: '')),
      ),
    );
    expect(controlador.edicionBloqueada, isFalse);
    controlador.ocupado = true;
    expect(controlador.edicionBloqueada, isTrue);
    controlador.ocupado = false;
    controlador.sincronizando = true;
    expect(controlador.edicionBloqueada, isTrue);
    controlador.dispose();
  });
}
