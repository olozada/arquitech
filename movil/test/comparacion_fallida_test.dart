import 'package:arquitectos_movil/core/db/pendientes_db.dart';
import 'package:arquitectos_movil/funciones/pendientes/comparacion_fallida.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('extrae local y servidor del JSON', () {
    const registro = OperacionFallidaRegistro(
      idOperacion: '1',
      secuencia: 1,
      tipo: 'EditarLinea',
      numeroActa: 120,
      localId: 's1',
      estado: 'Conflicto',
      mensaje: 'Conflicto',
      payloadJson: '{"linea":{"descripcion":"Local","cantidad":"2.0000","precio":"10.00"}}',
      resultadoJson: '{"lineaServidor":{"descripcion":"Servidor","cantidad":"3.0000","precio":"11.00"}}',
      registradaEn: '2026-11-13T12:00:00Z',
    );
    final datos = ComparacionFallida.desde(registro);
    expect(datos.localDescripcion, 'Local');
    expect(datos.servidorDescripcion, 'Servidor');
    expect(datos.hayServidor, isTrue);
  });
}
