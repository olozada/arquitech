import 'package:flutter/services.dart';
import 'package:material_ui/material_ui.dart';

import 'formulario_adaptable.dart';
import 'tema.dart';

/// Logo de Arquitech (blanco y amarillo). Solo sobre fondos oscuros.
class LogoArquitech extends StatelessWidget {
  const LogoArquitech({this.alto = 120, super.key});

  final double alto;

  @override
  Widget build(BuildContext context) {
    return Image.asset(
      'assets/imagenes/logo_arquitech.png',
      height: alto,
      fit: BoxFit.contain,
      semanticLabel: 'Arquitech. Construcción inteligente.',
    );
  }
}

/// Pantalla de acceso de las maquetas: cabecera oscura con el logo y hoja
/// blanca con esquinas redondeadas para el contenido. En ventanas expandidas
/// la cabecera pasa a un panel a la izquierda y el contenido a la derecha.
///
/// Los campos de texto dentro de la hoja usan el estilo relleno en gris
/// ([camposRellenos]).
class PantallaMarca extends StatelessWidget {
  const PantallaMarca({required this.children, super.key});

  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    final esquema = Theme.of(context).colorScheme;
    final marca = ColoresMarca.de(context);
    return AnnotatedRegion<SystemUiOverlayStyle>(
      // Íconos claros en la barra de estado, sobre la cabecera oscura.
      value: SystemUiOverlayStyle.light,
      child: Scaffold(
        backgroundColor: marca.cabecera,
        body: InputDecorationTheme(
          data: camposRellenos(esquema, marca.relleno),
          child: LayoutBuilder(
            builder: (context, restricciones) {
              final ventana = TamanoVentana.desdeAncho(restricciones.maxWidth);
              if (ventana == TamanoVentana.expandido) {
                return Row(
                  children: [
                    const Expanded(child: _Cabecera(altoLogo: 160)),
                    Expanded(
                      child: ColoredBox(
                        color: esquema.surface,
                        child: CuerpoFormulario(
                          anchoMaximo: 420,
                          centrarVertical: true,
                          children: children,
                        ),
                      ),
                    ),
                  ],
                );
              }
              return _Apilada(
                alto: restricciones.maxHeight,
                margen: ventana.margen,
                children: children,
              );
            },
          ),
        ),
      ),
    );
  }
}

/// Cabecera arriba y hoja abajo, con scroll. La hoja llega siempre hasta el
/// borde inferior para que no quede fondo oscuro debajo del contenido.
class _Apilada extends StatelessWidget {
  const _Apilada({
    required this.alto,
    required this.margen,
    required this.children,
  });

  final double alto;
  final double margen;
  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    final esquema = Theme.of(context).colorScheme;
    // Con el teclado abierto la cabecera se encoge hasta su mínimo. En la
    // maqueta la hoja empieza al 47 % del alto; aquí algo menos para que el
    // formulario quepa sin scroll en teléfonos comunes.
    final altoCabecera = (alto * 0.42).clamp(140.0, 400.0);
    final altoLogo = (altoCabecera * 0.5).clamp(64.0, 143.0);
    return SingleChildScrollView(
      keyboardDismissBehavior: ScrollViewKeyboardDismissBehavior.onDrag,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          SizedBox(
            height: altoCabecera,
            child: _Cabecera(altoLogo: altoLogo),
          ),
          ConstrainedBox(
            constraints: BoxConstraints(
              minHeight: (alto - altoCabecera).clamp(0.0, double.infinity),
            ),
            child: DecoratedBox(
              decoration: BoxDecoration(
                color: esquema.surface,
                borderRadius: const BorderRadius.vertical(
                  top: Radius.circular(28),
                ),
              ),
              child: SafeArea(
                top: false,
                child: Padding(
                  padding: EdgeInsets.fromLTRB(margen, 28, margen, margen),
                  child: Align(
                    alignment: Alignment.topCenter,
                    child: ConstrainedBox(
                      constraints: const BoxConstraints(maxWidth: 480),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        mainAxisSize: MainAxisSize.min,
                        children: children,
                      ),
                    ),
                  ),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _Cabecera extends StatelessWidget {
  const _Cabecera({required this.altoLogo});

  final double altoLogo;

  @override
  Widget build(BuildContext context) {
    // Foto de obra en escala de grises con el degradado de la maqueta: gris
    // transparente arriba y casi negro donde empieza la hoja.
    return DecoratedBox(
      decoration: const BoxDecoration(
        color: Colors.black,
        image: DecorationImage(
          image: AssetImage('assets/imagenes/cabecera_obra.jpg'),
          fit: BoxFit.cover,
          alignment: Alignment.topCenter,
        ),
      ),
      child: DecoratedBox(
        decoration: const BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topCenter,
            end: Alignment.bottomCenter,
            colors: [Color(0x006F6F6F), Color(0xD9000000)],
          ),
        ),
        child: SafeArea(
          bottom: false,
          child: Center(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: LogoArquitech(alto: altoLogo),
            ),
          ),
        ),
      ),
    );
  }
}

/// Título y subtítulo de la hoja de una [PantallaMarca].
class TituloMarca extends StatelessWidget {
  const TituloMarca({required this.titulo, this.subtitulo, super.key});

  final String titulo;
  final String? subtitulo;

  @override
  Widget build(BuildContext context) {
    final tema = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.only(bottom: 24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisSize: MainAxisSize.min,
        children: [
          Semantics(
            header: true,
            child: Text(
              titulo,
              style: tema.textTheme.headlineSmall?.copyWith(
                fontSize: 25,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
          if (subtitulo != null) ...[
            const SizedBox(height: 8),
            Text(
              subtitulo!,
              style: tema.textTheme.bodyLarge?.copyWith(
                fontSize: 15,
                fontWeight: FontWeight.w300,
                color: tema.colorScheme.onSurface,
              ),
            ),
          ],
        ],
      ),
    );
  }
}
