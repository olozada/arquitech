import 'dart:convert';

import 'package:arquitectos_movil/funciones/presupuestos/presupuesto_modelos.dart';
import 'package:flutter_test/flutter_test.dart';

const detallePresupuestoPrueba = {
  'id': 1,
  'construccionId': 7,
  'construccion': 'Cimentación',
  'nombre': 'Demo',
  'version': 2,
  'estado': 'Aprobado',
  'fecha': '2026-01-15',
  'mesesProyecto': 6,
  'areaConstruidaM2': 80,
  'pctImprevistos': 5,
  'pctIncremento': 0,
  'pctAdministracion': 10,
  'pctUtilidad': 8,
  'pctIvaUtilidad': 19,
  'resumen': {
    'subtotal': 1000,
    'totalMaterial': 600,
    'totalManoObra': 400,
    'imprevistos': 50,
    'incremento': 0,
    'administracion': 100,
    'utilidad': 80,
    'ivaUtilidad': 15.2,
    'total': 1245.2,
    'valorPorM2': 15.565,
  },
  'capitulos': [
    {
      'id': 9,
      'numero': 1,
      'nombre': 'Obra gris',
      'total': 1000,
      'incidencia': 1,
      'items': [
        {
          'id': 44,
          'codigo': '1.1',
          'descripcion': 'Excavación',
          'unidad': 'm³',
          'factorConversionApu': 1,
          'avisoUnidad': false,
          'cantidad': 12.5,
          'cantidadOrigenFactor': 1,
          'valorMaterialUnitario': 40,
          'valorManoObraUnitario': 40,
          'valorUnitario': 80,
          'total': 1000,
          'desdeApu': false,
          'mediciones': [],
        },
      ],
    },
  ],
  'rowVersion': 'AAAA',
  'esPlantilla': false,
};

void main() {
  test('parsea detalle, validación y seguimiento del paquete', () {
    final detalle = PresupuestoDetalleVista.desdeJson(
      jsonEncode(detallePresupuestoPrueba),
    );
    expect(detalle.nombre, 'Demo');
    expect(detalle.resumen.total, 1245.2);
    expect(detalle.capitulos.single.items.single.codigo, '1.1');

    final validacion = parsearValidacion(
      jsonEncode([
        {
          'itemId': 44,
          'codigo': '1.1',
          'tipo': 'Error',
          'mensaje': 'Falta APU',
        },
      ]),
    );
    expect(validacion.single.mensaje, 'Falta APU');

    final seguimiento = SeguimientoPresupuestoVista.desdeJson(
      jsonEncode({
        'presupuestoId': 1,
        'construccion': 'Cimentación',
        'resumen': detallePresupuestoPrueba['resumen'],
        'ejecutado': 500,
        'proyeccion': 900,
        'noPresupuestado': 0,
        'capitulos': [],
        'items': [
          {
            'itemId': 44,
            'capituloId': 9,
            'capitulo': 'Obra gris',
            'codigo': '1.1',
            'descripcion': 'Excavación',
            'unidad': 'm³',
            'cantidadPresupuestada': 12.5,
            'cantidadEjecutada': 6,
            'avanceFisico': 0.48,
            'valorPresupuestado': 1000,
            'valorEjecutado': 480,
            'avanceFinanciero': 0.48,
            'saldo': 520,
            'precioPresupuestado': 80,
            'precioActas': 80,
            'proyeccionCierre': 1000,
          },
        ],
        'noPresupuestados': [],
        'acumuladoMensual': [
          {'anio': 2025, 'mes': 9, 'ejecutado': 200, 'acumulado': 200},
          {'anio': 2025, 'mes': 10, 'ejecutado': 300, 'acumulado': 500},
        ],
      }),
    );
    expect(seguimiento.ejecutado, 500);
    expect(seguimiento.items.single.avanceFisico, 0.48);
    expect(seguimiento.acumuladoMensual.length, 2);
    expect(seguimiento.acumuladoMensual.last.acumulado, 500);
    expect(seguimiento.acumuladoMensual.last.etiqueta, '10/2025');
  });
}
