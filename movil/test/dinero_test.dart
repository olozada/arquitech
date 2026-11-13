import 'package:arquitectos_movil/core/decimal/dinero.dart';
import 'package:decimal/decimal.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  Decimal d(String texto) => Decimal.parse(texto);

  test('redondea lejos de cero como el servidor', () {
    expect(redondearLejosDeCero(d('1.23456'), 4), d('1.2346'));
    expect(redondearLejosDeCero(d('1.23454'), 4), d('1.2345'));
    expect(redondearLejosDeCero(d('1.23455'), 4), d('1.2346'));
    expect(redondearLejosDeCero(d('10.005'), 2), d('10.01'));
    expect(redondearLejosDeCero(d('10.004'), 2), d('10.00'));
    expect(redondearLejosDeCero(d('-1.225'), 2), d('-1.23'));
    expect(redondearLejosDeCero(d('2.5'), 0), d('3'));
    expect(redondearLejosDeCero(d('0.00004'), 4), d('0.0000'));
  });

  test('el subtotal usa cantidad a 4 y precio a 2', () {
    expect(subtotalLinea(d('1.23456'), d('10.005')), d('1.2346') * d('10.01'));
  });

  test('el dinero es-CO no muestra decimales', () {
    expect(dinero(d('1250.4')), r'$1.250');
    expect(dinero(d('-1250.5')), r'-$1.251');
  });
}
