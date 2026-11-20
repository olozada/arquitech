import 'package:material_ui/material_ui.dart';

import '../../app/tema.dart';

/// Número de acta como en las maquetas: «#001».
String numeroActaVisible(int numero) => '#${numero.toString().padLeft(3, '0')}';

/// Insignia «Abierta» (verde) o «Cerrada» (roja) de un acta.
class EtiquetaEstadoActa extends StatelessWidget {
  const EtiquetaEstadoActa({required this.cerrada, super.key});

  final bool cerrada;

  @override
  Widget build(BuildContext context) {
    final esquema = Theme.of(context).colorScheme;
    return EtiquetaEstado(
      texto: cerrada ? 'CERRADA' : 'ABIERTA',
      fondo: cerrada ? esquema.error : ColoresMarca.de(context).exito,
      frente: cerrada ? esquema.onError : Colors.white,
    );
  }
}

/// Insignia «Aprobado» (verde, con visto) o «Rechazado» (roja, con equis) de
/// una línea.
class EtiquetaAprobacion extends StatelessWidget {
  const EtiquetaAprobacion({required this.aprobado, super.key});

  final bool aprobado;

  @override
  Widget build(BuildContext context) {
    final esquema = Theme.of(context).colorScheme;
    return EtiquetaEstado(
      texto: aprobado ? 'Aprobado' : 'Rechazado',
      icono: aprobado ? Icons.check : Icons.close,
      fondo: aprobado ? ColoresMarca.de(context).exito : esquema.error,
      frente: aprobado ? Colors.white : esquema.onError,
    );
  }
}
