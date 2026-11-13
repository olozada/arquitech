import 'package:material_ui/material_ui.dart';

import '../../core/decimal/dinero.dart';
import 'presupuesto_modelos.dart';

/// Línea de ejecutado acumulado (mismo dato que la web en `acumuladoMensual`).
class GraficoAcumuladoMensual extends StatefulWidget {
  const GraficoAcumuladoMensual({required this.puntos, super.key});

  final List<PuntoEjecucionMensual> puntos;

  @override
  State<GraficoAcumuladoMensual> createState() => _EstadoGraficoAcumulado();
}

class _EstadoGraficoAcumulado extends State<GraficoAcumuladoMensual> {
  late int _seleccion;

  @override
  void initState() {
    super.initState();
    _seleccion = _indiceInicial();
  }

  @override
  void didUpdateWidget(covariant GraficoAcumuladoMensual anterior) {
    super.didUpdateWidget(anterior);
    if (anterior.puntos != widget.puntos) {
      _seleccion = _indiceInicial();
    }
  }

  int _indiceInicial() => widget.puntos.isEmpty ? 0 : widget.puntos.length - 1;

  void _tocar(TapUpDetails detalle, Size tamano) {
    final geometria = GeometriaGraficoAcumulado(tamano, widget.puntos);
    final indice = geometria.indiceCercano(detalle.localPosition);
    if (indice == null) {
      return;
    }
    setState(() => _seleccion = indice);
  }

  @override
  Widget build(BuildContext context) {
    final puntos = widget.puntos;
    if (puntos.isEmpty) {
      return const SizedBox.shrink();
    }
    final esquema = Theme.of(context).colorScheme;
    final texto = Theme.of(context).textTheme.bodySmall!;
    final punto = puntos[_seleccion.clamp(0, puntos.length - 1)];
    final leyenda =
        '${punto.etiqueta}: acumulado ${dineroPresupuesto(punto.acumulado)} · '
        'del mes ${dineroPresupuesto(punto.ejecutado)}';
    final resumen = puntos
        .map((p) => '${p.etiqueta}: ${dineroPresupuesto(p.acumulado)}')
        .join('; ');
    return Semantics(
      label: 'Ejecutado acumulado por mes. $resumen',
      value: leyenda,
      child: Card(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(12, 16, 12, 8),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                'Ejecutado acumulado',
                style: Theme.of(context).textTheme.titleSmall,
              ),
              const SizedBox(height: 4),
              Text(
                leyenda,
                key: const Key('leyenda-grafico-acumulado'),
                style: texto.copyWith(color: esquema.onSurfaceVariant),
              ),
              const SizedBox(height: 8),
              SizedBox(
                height: 200,
                child: LayoutBuilder(
                  builder: (context, restricciones) {
                    final tamano = Size(
                      restricciones.maxWidth,
                      restricciones.maxHeight,
                    );
                    return GestureDetector(
                      behavior: HitTestBehavior.opaque,
                      onTapUp: (detalle) => _tocar(detalle, tamano),
                      child: CustomPaint(
                        key: const Key('grafico-acumulado'),
                        painter: _PintorAcumulado(
                          puntos: puntos,
                          indiceSeleccionado: _seleccion,
                          colorLinea: esquema.primary,
                          colorCuadricula: esquema.outlineVariant,
                          colorTexto: esquema.onSurfaceVariant,
                          colorSuperficie: esquema.surface,
                          estiloTexto: texto,
                        ),
                        child: const SizedBox.expand(),
                      ),
                    );
                  },
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class GeometriaGraficoAcumulado {
  GeometriaGraficoAcumulado(Size size, List<PuntoEjecucionMensual> puntos)
    : coords = _coordenadas(size, puntos),
      maxY = _maximoY(puntos);

  static const margenIzq = 52.0;
  static const margenDer = 8.0;
  static const margenSup = 8.0;
  static const margenInf = 28.0;

  final List<Offset> coords;
  final double maxY;

  static double _maximoY(List<PuntoEjecucionMensual> puntos) {
    var maxY = 0.0;
    for (final punto in puntos) {
      if (punto.acumulado > maxY) {
        maxY = punto.acumulado;
      }
    }
    if (maxY <= 0) {
      return 1;
    }
    return maxY * 1.08;
  }

  static List<Offset> _coordenadas(
    Size size,
    List<PuntoEjecucionMensual> puntos,
  ) {
    final ancho = size.width - margenIzq - margenDer;
    final alto = size.height - margenSup - margenInf;
    final origen = Offset(margenIzq, margenSup);
    final maxY = _maximoY(puntos);
    final coords = <Offset>[];
    for (var i = 0; i < puntos.length; i++) {
      final x = puntos.length == 1
          ? origen.dx + ancho / 2
          : origen.dx + ancho * i / (puntos.length - 1);
      final y = origen.dy + alto * (1 - puntos[i].acumulado / maxY);
      coords.add(Offset(x, y));
    }
    return coords;
  }

  int? indiceCercano(Offset posicion, {double radio = 32}) {
    var mejor = -1;
    var distanciaMin = radio * radio;
    for (var i = 0; i < coords.length; i++) {
      final d = (coords[i] - posicion).distanceSquared;
      if (d <= distanciaMin) {
        distanciaMin = d;
        mejor = i;
      }
    }
    return mejor >= 0 ? mejor : null;
  }
}

class _PintorAcumulado extends CustomPainter {
  _PintorAcumulado({
    required this.puntos,
    required this.indiceSeleccionado,
    required this.colorLinea,
    required this.colorCuadricula,
    required this.colorTexto,
    required this.colorSuperficie,
    required this.estiloTexto,
  });

  final List<PuntoEjecucionMensual> puntos;
  final int indiceSeleccionado;
  final Color colorLinea;
  final Color colorCuadricula;
  final Color colorTexto;
  final Color colorSuperficie;
  final TextStyle estiloTexto;

  @override
  void paint(Canvas canvas, Size size) {
    final geometria = GeometriaGraficoAcumulado(size, puntos);
    final coords = geometria.coords;
    final maxY = geometria.maxY;
    final origen = const Offset(
      GeometriaGraficoAcumulado.margenIzq,
      GeometriaGraficoAcumulado.margenSup,
    );
    final ancho =
        size.width -
        GeometriaGraficoAcumulado.margenIzq -
        GeometriaGraficoAcumulado.margenDer;
    final alto =
        size.height -
        GeometriaGraficoAcumulado.margenSup -
        GeometriaGraficoAcumulado.margenInf;

    final pintarCuadricula = Paint()
      ..color = colorCuadricula
      ..strokeWidth = 1;
    const divisiones = 4;
    for (var i = 0; i <= divisiones; i++) {
      final y = origen.dy + alto * i / divisiones;
      canvas.drawLine(
        Offset(origen.dx, y),
        Offset(origen.dx + ancho, y),
        pintarCuadricula,
      );
      final valor = maxY * (1 - i / divisiones);
      _texto(canvas, _etiquetaEjeY(valor), Offset(4, y - 8), TextAlign.left);
    }

    if (indiceSeleccionado >= 0 && indiceSeleccionado < coords.length) {
      final marca = coords[indiceSeleccionado];
      canvas.drawLine(
        Offset(marca.dx, origen.dy),
        Offset(marca.dx, origen.dy + alto),
        Paint()
          ..color = colorLinea.withValues(alpha: 0.25)
          ..strokeWidth = 1.5,
      );
    }

    if (coords.length >= 2) {
      final ruta = Path()..moveTo(coords.first.dx, coords.first.dy);
      for (var i = 1; i < coords.length; i++) {
        ruta.lineTo(coords[i].dx, coords[i].dy);
      }
      canvas.drawPath(
        ruta,
        Paint()
          ..color = colorLinea
          ..strokeWidth = 2.5
          ..style = PaintingStyle.stroke
          ..strokeCap = StrokeCap.round
          ..strokeJoin = StrokeJoin.round,
      );
    }

    for (var i = 0; i < coords.length; i++) {
      final seleccionado = i == indiceSeleccionado;
      final radio = seleccionado ? 6.0 : 3.5;
      if (seleccionado) {
        canvas.drawCircle(
          coords[i],
          radio + 3,
          Paint()..color = colorLinea.withValues(alpha: 0.3),
        );
      }
      canvas.drawCircle(coords[i], radio, Paint()..color = colorLinea);
      if (seleccionado) {
        canvas.drawCircle(
          coords[i],
          radio - 2.5,
          Paint()..color = colorSuperficie,
        );
      }
    }

    final pasoEtiqueta = puntos.length > 6 ? 2 : 1;
    for (var i = 0; i < puntos.length; i++) {
      if (i % pasoEtiqueta != 0 && i != puntos.length - 1) {
        continue;
      }
      _texto(
        canvas,
        puntos[i].etiqueta,
        Offset(coords[i].dx, origen.dy + alto + 4),
        TextAlign.center,
        anchoMax: 48,
      );
    }
  }

  void _texto(
    Canvas canvas,
    String contenido,
    Offset posicion,
    TextAlign alinear, {
    double anchoMax = 44,
  }) {
    final pintor = TextPainter(
      text: TextSpan(
        text: contenido,
        style: estiloTexto.copyWith(color: colorTexto, fontSize: 10),
      ),
      textAlign: alinear,
      textDirection: TextDirection.ltr,
      maxLines: 1,
    )..layout(maxWidth: anchoMax);
    var dx = posicion.dx;
    if (alinear == TextAlign.center) {
      dx -= pintor.width / 2;
    }
    pintor.paint(canvas, Offset(dx, posicion.dy));
  }

  @override
  bool shouldRepaint(covariant _PintorAcumulado oldDelegate) {
    return oldDelegate.puntos != puntos ||
        oldDelegate.colorLinea != colorLinea ||
        oldDelegate.indiceSeleccionado != indiceSeleccionado;
  }
}

String _etiquetaEjeY(double valor) {
  final absoluto = valor.abs();
  if (absoluto >= 1000000) {
    return '${(valor / 1000000).toStringAsFixed(1)}M';
  }
  if (absoluto >= 1000) {
    return '${(valor / 1000).toStringAsFixed(0)}K';
  }
  return valor.toStringAsFixed(0);
}
