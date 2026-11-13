import 'package:arquitectos_movil/core/errores/mensajes.dart';
import 'package:decimal/decimal.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  const comunes = {'password123', '123456'};

  test('la política de contraseña repite los mensajes del servidor', () {
    expect(
      errorContrasena('corta1', null, comunes),
      'La contraseña debe tener al menos 10 caracteres.',
    );
    expect(
      errorContrasena('sololetras', null, comunes),
      'La contraseña debe incluir letras y números.',
    );
    expect(
      errorContrasena('AnaUsuario1', 'anausuario1', comunes),
      'La contraseña no puede ser igual al usuario.',
    );
    expect(
      errorContrasena('password123', null, comunes),
      'La contraseña es demasiado común.',
    );
    expect(errorContrasena('Clave12345', 'ana', comunes), isNull);
  });

  test('el cambio exige confirmación y una clave distinta', () {
    expect(
      errorCambioContrasena(
        actual: 'Clave12345',
        nueva: 'Clave12345',
        confirmacion: 'Clave12345',
        usuario: 'ana',
        comunes: comunes,
      ),
      'La contraseña nueva debe ser distinta de la actual.',
    );
    expect(
      errorCambioContrasena(
        actual: 'Clave12345',
        nueva: 'Clave67890',
        confirmacion: 'otra',
        usuario: 'ana',
        comunes: comunes,
      ),
      'Las contraseñas no coinciden.',
    );
  });

  test('cantidad, precio y descripción', () {
    expect(errorCantidad(Decimal.zero), 'Debe indicar la cantidad del ítem.');
    expect(
      errorCantidad(Decimal.parse('0.00004')),
      'Debe indicar la cantidad del ítem.',
    );
    expect(errorCantidad(Decimal.parse('1.2')), isNull);
    expect(
      errorPrecio(Decimal.parse('-0.01')),
      'Debe indicar el precio del ítem.',
    );
    expect(errorPrecio(Decimal.zero), isNull);
    expect(errorDescripcion('   '), 'Debe indicar la descripción del ítem.');
    expect(
      errorDescripcion('x' * 501),
      'La descripción no puede superar 500 caracteres.',
    );
    expect(errorDescripcion('Muro'), isNull);
  });
}
