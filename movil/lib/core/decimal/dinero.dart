import 'package:decimal/decimal.dart';
import 'package:intl/intl.dart';

/// Redondeo AwayFromZero, igual que `MidpointRounding.AwayFromZero` del servidor.
Decimal redondearLejosDeCero(Decimal valor, int escala) {
  if (escala < 0) {
    throw ArgumentError.value(escala, 'escala');
  }

  final negativo = valor < Decimal.zero;
  final absoluto = valor.abs().toString();
  final partes = absoluto.split('.');
  final entera = partes[0];
  final fraccion = partes.length > 1 ? partes[1] : '';
  if (fraccion.length <= escala) {
    if (escala == 0) {
      return negativo ? -Decimal.parse(entera) : Decimal.parse(entera);
    }
    final texto = '$entera.${fraccion.padRight(escala, '0')}';
    final exacto = Decimal.parse(texto);
    return negativo ? -exacto : exacto;
  }

  final conservar = fraccion.substring(0, escala);
  final siguiente = int.parse(fraccion[escala]);
  var digitos = escala == 0 ? entera : '$entera$conservar';
  if (siguiente >= 5) {
    digitos = (BigInt.parse(digitos) + BigInt.one).toString();
  }

  final Decimal redondeado;
  if (escala == 0) {
    redondeado = Decimal.parse(digitos);
  } else {
    final completo = digitos.length < escala
        ? digitos.padLeft(escala, '0')
        : digitos;
    final corte = completo.length - escala;
    final texto =
        '${completo.substring(0, corte)}.${completo.substring(corte)}';
    redondeado = Decimal.parse(texto);
  }

  if (!negativo || redondeado == Decimal.zero) {
    return redondeado;
  }
  return -redondeado;
}

Decimal subtotalLinea(Decimal cantidad, Decimal precio) {
  return redondearLejosDeCero(cantidad, 4) * redondearLejosDeCero(precio, 2);
}

/// Moneda es-CO sin decimales, como el total de actas en la web.
final _dineroPresupuesto = NumberFormat.currency(
  locale: 'es_CO',
  symbol: r'$',
  decimalDigits: 2,
);

/// Totales de presupuesto con dos decimales, como la web.
String dineroPresupuesto(num valor) => _dineroPresupuesto.format(valor);

String dinero(Decimal valor) {
  final redondeado = redondearLejosDeCero(valor, 0);
  final negativo = redondeado < Decimal.zero;
  final texto = redondeado.abs().toBigInt().toString();
  final buffer = StringBuffer();
  for (var i = 0; i < texto.length; i++) {
    if (i > 0 && (texto.length - i) % 3 == 0) {
      buffer.write('.');
    }
    buffer.write(texto[i]);
  }
  return '${negativo ? '-' : ''}\$$buffer';
}

String cantidadVisible(String crudo) {
  final valor = Decimal.parse(crudo);
  final texto = valor.toString();
  return texto.replaceAll('.', ',');
}

String fechaVisible(String iso) {
  final partes = iso.split('T').first.split('-');
  if (partes.length != 3) {
    return iso;
  }
  return '${partes[2]}/${partes[1]}/${partes[0]}';
}
