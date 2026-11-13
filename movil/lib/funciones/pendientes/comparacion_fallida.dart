import 'dart:convert';

import 'package:decimal/decimal.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/db/pendientes_db.dart';
import '../../core/decimal/dinero.dart';

class ComparacionFallida {
  const ComparacionFallida({
    this.localDescripcion,
    this.localCantidad,
    this.localPrecio,
    this.servidorDescripcion,
    this.servidorCantidad,
    this.servidorPrecio,
  });

  final String? localDescripcion;
  final String? localCantidad;
  final String? localPrecio;
  final String? servidorDescripcion;
  final String? servidorCantidad;
  final String? servidorPrecio;

  bool get hayServidor =>
      servidorDescripcion != null ||
      servidorCantidad != null ||
      servidorPrecio != null;

  factory ComparacionFallida.desde(OperacionFallidaRegistro registro) {
    String? localDescripcion;
    String? localCantidad;
    String? localPrecio;
    try {
      final mapa = jsonDecode(registro.payloadJson) as Map<String, Object?>;
      final linea = mapa['linea'];
      if (linea is Map) {
        final datos = Map<String, Object?>.from(linea);
        localDescripcion = datos['descripcion']?.toString();
        localCantidad = datos['cantidad']?.toString();
        localPrecio = datos['precio']?.toString();
      }
    } catch (_) {}

    String? servidorDescripcion;
    String? servidorCantidad;
    String? servidorPrecio;
    try {
      final mapa = jsonDecode(registro.resultadoJson) as Map<String, Object?>;
      final linea = mapa['lineaServidor'];
      if (linea is Map) {
        final datos = Map<String, Object?>.from(linea);
        servidorDescripcion = datos['descripcion']?.toString();
        servidorCantidad = datos['cantidad']?.toString();
        servidorPrecio = datos['precio']?.toString();
      }
    } catch (_) {}

    return ComparacionFallida(
      localDescripcion: localDescripcion,
      localCantidad: localCantidad,
      localPrecio: localPrecio,
      servidorDescripcion: servidorDescripcion,
      servidorCantidad: servidorCantidad,
      servidorPrecio: servidorPrecio,
    );
  }
}

class ComparacionFallidaVista extends StatelessWidget {
  const ComparacionFallidaVista({required this.datos, super.key});

  final ComparacionFallida datos;

  @override
  Widget build(BuildContext context) {
    if (!datos.hayServidor &&
        datos.localDescripcion == null &&
        datos.localCantidad == null) {
      return const SizedBox.shrink();
    }
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Comparación',
          style: TextStyle(fontWeight: FontWeight.w600),
        ),
        const SizedBox(height: 4),
        if (datos.localDescripcion != null)
          Text('Local: ${datos.localDescripcion}'),
        if (datos.localCantidad != null || datos.localPrecio != null)
          Text(
            'Local: cant. ${datos.localCantidad ?? '—'} · '
            'precio ${_dinero(datos.localPrecio)}',
          ),
        if (datos.hayServidor) ...[
          const SizedBox(height: 4),
          if (datos.servidorDescripcion != null)
            Text('Servidor: ${datos.servidorDescripcion}'),
          if (datos.servidorCantidad != null || datos.servidorPrecio != null)
            Text(
              'Servidor: cant. ${datos.servidorCantidad ?? '—'} · '
              'precio ${_dinero(datos.servidorPrecio)}',
            ),
        ],
      ],
    );
  }

  static String _dinero(String? valor) {
    if (valor == null) {
      return '—';
    }
    final decimal = Decimal.tryParse(valor);
    if (decimal == null) {
      return valor;
    }
    return dinero(decimal);
  }
}
