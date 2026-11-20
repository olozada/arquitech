import 'dart:math' as math;

import 'package:material_ui/material_ui.dart';

/// Ancho desde el que la ventana deja de ser compacta (Material 3).
const double anchoMediano = 600;

/// Ancho desde el que la ventana es expandida (Material 3).
const double anchoExpandido = 840;

/// Ancho máximo de un formulario de una sola columna.
const double anchoFormulario = 640;

/// Clase de tamaño de la ventana. Se decide por el espacio disponible,
/// nunca por el tipo de dispositivo ni por la orientación.
enum TamanoVentana {
  compacto,
  mediano,
  expandido;

  static TamanoVentana desdeAncho(double ancho) {
    if (ancho < anchoMediano) {
      return TamanoVentana.compacto;
    }
    if (ancho < anchoExpandido) {
      return TamanoVentana.mediano;
    }
    return TamanoVentana.expandido;
  }

  double get margen => switch (this) {
    TamanoVentana.compacto => 16,
    TamanoVentana.mediano => 24,
    TamanoVentana.expandido => 32,
  };
}

/// Cuerpo con scroll para formularios: margen según el ancho, contenido
/// centrado y limitado a [anchoMaximo]. Con [centrarVertical] el contenido
/// queda centrado en la altura cuando cabe completo.
class CuerpoFormulario extends StatelessWidget {
  const CuerpoFormulario({
    required this.children,
    this.anchoMaximo = anchoFormulario,
    this.centrarVertical = false,
    this.controladorScroll,
    super.key,
  });

  final List<Widget> children;
  final double anchoMaximo;
  final bool centrarVertical;
  final ScrollController? controladorScroll;

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: LayoutBuilder(
        builder: (context, restricciones) {
          final margen = TamanoVentana.desdeAncho(restricciones.maxWidth)
              .margen;
          final altoMinimo = centrarVertical && restricciones.hasBoundedHeight
              ? math.max(0.0, restricciones.maxHeight - margen * 2)
              : 0.0;
          return SingleChildScrollView(
            controller: controladorScroll,
            keyboardDismissBehavior: ScrollViewKeyboardDismissBehavior.onDrag,
            padding: EdgeInsets.all(margen),
            child: ConstrainedBox(
              constraints: BoxConstraints(minHeight: altoMinimo),
              child: Align(
                alignment: centrarVertical
                    ? Alignment.center
                    : Alignment.topCenter,
                child: ConstrainedBox(
                  constraints: BoxConstraints(maxWidth: anchoMaximo),
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: children,
                  ),
                ),
              ),
            ),
          );
        },
      ),
    );
  }
}

/// Grupo de campos con título opcional. En ventanas compactas se dibuja plano
/// para no gastar ancho; desde [anchoMediano] (ancho de la ventana) se dibuja
/// como tarjeta con borde.
class SeccionFormulario extends StatelessWidget {
  const SeccionFormulario({
    required this.children,
    this.titulo,
    this.icono,
    this.espacio = 12,
    super.key,
  });

  final String? titulo;
  final IconData? icono;
  final List<Widget> children;
  final double espacio;

  @override
  Widget build(BuildContext context) {
    final tema = Theme.of(context);
    final esquema = tema.colorScheme;
    final contenido = Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      mainAxisSize: MainAxisSize.min,
      children: [
        if (titulo != null) ...[
          Semantics(
            header: true,
            child: Row(
              children: [
                if (icono != null) ...[
                  Icon(icono, size: 20, color: esquema.onSurface),
                  const SizedBox(width: 8),
                ],
                Expanded(
                  child: Text(
                    titulo!,
                    style: tema.textTheme.titleSmall?.copyWith(
                      color: esquema.onSurface,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
              ],
            ),
          ),
          SizedBox(height: espacio),
        ],
        ..._separar(children, SizedBox(height: espacio)),
      ],
    );
    final ventana = TamanoVentana.desdeAncho(MediaQuery.sizeOf(context).width);
    if (ventana == TamanoVentana.compacto) {
      return Padding(
        padding: const EdgeInsets.only(bottom: 24),
        child: contenido,
      );
    }
    return Padding(
      padding: const EdgeInsets.only(bottom: 16),
      child: DecoratedBox(
        decoration: BoxDecoration(
          color: esquema.surface,
          border: Border.all(color: esquema.outlineVariant),
          borderRadius: BorderRadius.circular(16),
        ),
        child: Padding(padding: const EdgeInsets.all(20), child: contenido),
      ),
    );
  }
}

/// Coloca los campos en fila cuando el ancho disponible llega a
/// [anchoMinimoFila]; si no, los apila en columna.
class FilaAdaptable extends StatelessWidget {
  const FilaAdaptable({
    required this.children,
    this.anchoMinimoFila = 440,
    this.espacio = 12,
    super.key,
  });

  final List<Widget> children;
  final double anchoMinimoFila;
  final double espacio;

  @override
  Widget build(BuildContext context) {
    if (children.length < 2) {
      return Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        mainAxisSize: MainAxisSize.min,
        children: children,
      );
    }
    return LayoutBuilder(
      builder: (context, restricciones) {
        if (restricciones.maxWidth < anchoMinimoFila) {
          return Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            mainAxisSize: MainAxisSize.min,
            children: _separar(children, SizedBox(height: espacio)),
          );
        }
        return Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: _separar([
            for (final hijo in children) Expanded(child: hijo),
          ], SizedBox(width: espacio)),
        );
      },
    );
  }
}

/// Dos columnas de secciones cuando hay espacio de sobra; una sola columna
/// (primero [izquierda], luego [derecha]) en el resto de ventanas.
class ColumnasAdaptables extends StatelessWidget {
  const ColumnasAdaptables({
    required this.izquierda,
    required this.derecha,
    this.anchoMinimo = 880,
    this.espacio = 24,
    super.key,
  });

  final List<Widget> izquierda;
  final List<Widget> derecha;
  final double anchoMinimo;
  final double espacio;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, restricciones) {
        if (restricciones.maxWidth < anchoMinimo) {
          return Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            mainAxisSize: MainAxisSize.min,
            children: [...izquierda, ...derecha],
          );
        }
        return Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: izquierda,
              ),
            ),
            SizedBox(width: espacio),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: derecha,
              ),
            ),
          ],
        );
      },
    );
  }
}

/// Botón principal de un formulario: a todo el ancho en ventanas angostas,
/// alineado a la derecha con ancho fijo en las anchas.
class AccionesFormulario extends StatelessWidget {
  const AccionesFormulario({
    required this.child,
    this.anchoBoton = 280,
    super.key,
  });

  final Widget child;
  final double anchoBoton;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, restricciones) {
        if (restricciones.maxWidth < anchoMediano) {
          return child;
        }
        return Align(
          alignment: AlignmentDirectional.centerEnd,
          child: SizedBox(width: anchoBoton, child: child),
        );
      },
    );
  }
}

/// Campo con la etiqueta encima, como en las maquetas. El campo lleva solo el
/// texto de ayuda ([InputDecoration.hintText]).
///
/// Con [fusionarSemantica] el lector de pantalla anuncia etiqueta y campo como
/// un solo elemento. Desactivarlo cuando el campo tiene botones propios (por
/// ejemplo, mostrar u ocultar la contraseña), que se perderían al fusionar.
class CampoEtiquetado extends StatelessWidget {
  const CampoEtiquetado({
    required this.etiqueta,
    required this.child,
    this.fusionarSemantica = true,
    super.key,
  });

  final String etiqueta;
  final Widget child;
  final bool fusionarSemantica;

  @override
  Widget build(BuildContext context) {
    final tema = Theme.of(context);
    final columna = Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      mainAxisSize: MainAxisSize.min,
      children: [
        Padding(
          padding: const EdgeInsetsDirectional.only(start: 16, bottom: 6),
          child: Text(
            etiqueta,
            style: tema.textTheme.bodyMedium?.copyWith(
              color: tema.colorScheme.onSurface,
              fontSize: 13,
            ),
          ),
        ),
        child,
      ],
    );
    return fusionarSemantica ? MergeSemantics(child: columna) : columna;
  }
}

/// Campo de contraseña con botón para mostrar u ocultar el texto. Lleva
/// [etiqueta] dentro del campo, o solo [pista] cuando la etiqueta va encima
/// ([CampoEtiquetado]).
class CampoContrasena extends StatefulWidget {
  const CampoContrasena({
    required this.controller,
    this.etiqueta,
    this.pista,
    this.autofillHints,
    this.textInputAction,
    this.onSubmitted,
    this.habilitado = true,
    super.key,
  });

  final TextEditingController controller;
  final String? etiqueta;
  final String? pista;
  final Iterable<String>? autofillHints;
  final TextInputAction? textInputAction;
  final ValueChanged<String>? onSubmitted;
  final bool habilitado;

  @override
  State<CampoContrasena> createState() => _EstadoCampoContrasena();
}

class _EstadoCampoContrasena extends State<CampoContrasena> {
  var _oculta = true;

  @override
  Widget build(BuildContext context) {
    return TextField(
      controller: widget.controller,
      enabled: widget.habilitado,
      obscureText: _oculta,
      autocorrect: false,
      enableSuggestions: false,
      autofillHints: widget.autofillHints,
      textInputAction: widget.textInputAction,
      onSubmitted: widget.onSubmitted,
      decoration: InputDecoration(
        labelText: widget.etiqueta,
        hintText: widget.pista,
        suffixIcon: IconButton(
          tooltip: _oculta ? 'Mostrar contraseña' : 'Ocultar contraseña',
          icon: Icon(
            // Como en la maqueta: ojo tachado mientras está oculta.
            _oculta ? Icons.visibility_off_outlined : Icons.visibility_outlined,
          ),
          onPressed: () => setState(() => _oculta = !_oculta),
        ),
      ),
    );
  }
}

List<Widget> _separar(List<Widget> hijos, Widget separador) {
  return [
    for (var i = 0; i < hijos.length; i++) ...[if (i > 0) separador, hijos[i]],
  ];
}
