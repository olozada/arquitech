import 'package:decimal/decimal.dart';
import 'package:material_ui/material_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../app/aplicacion.dart';
import '../../core/db/consultas.dart';
import 'detalle_acta_acciones.dart';
import '../../sincronizacion/modelos.dart';
import '../../core/decimal/dinero.dart';
import '../../core/errores/mensajes.dart';

class DetalleActaPantalla extends ConsumerStatefulWidget {
  const DetalleActaPantalla({required this.numero, super.key});

  final int numero;

  @override
  ConsumerState<DetalleActaPantalla> createState() => _EstadoDetalle();
}

class _EstadoDetalle extends ConsumerState<DetalleActaPantalla> {
  DetalleActa? _detalle;
  var _cargando = true;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _cargar());
  }

  Future<void> _cargar() async {
    final detalle = await ref
        .read(controladorProvider)
        .detalleActa(widget.numero);
    if (!mounted) {
      return;
    }
    setState(() {
      _detalle = detalle;
      _cargando = false;
    });
  }

  @override
  Widget build(BuildContext context) {
    if (_cargando) {
      return Scaffold(
        appBar: AppBar(title: Text('Acta ${widget.numero}')),
        body: const Center(child: Text('Cargando acta…')),
      );
    }
    return DetalleActaLayout(
      detalle: _detalle,
      numero: widget.numero,
      acciones: ref.read(controladorProvider),
      edicionBloqueada: ref.read(controladorProvider).edicionBloqueada,
      enLinea: ref.read(controladorProvider).enLinea,
      onPdf: ref.read(controladorProvider).enLinea
          ? ref.read(controladorProvider).abrirPdfActa
          : null,
      onActualizado: _cargar,
    );
  }
}

class DetalleActaVista extends ConsumerWidget {
  const DetalleActaVista({
    required this.detalle,
    required this.numero,
    this.onActualizado,
    super.key,
  });

  final DetalleActa? detalle;
  final int numero;
  final Future<void> Function()? onActualizado;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final controlador = ref.read(controladorProvider);
    return DetalleActaLayout(
      detalle: detalle,
      numero: numero,
      acciones: controlador,
      edicionBloqueada: controlador.edicionBloqueada,
      enLinea: controlador.enLinea,
      onPdf: controlador.enLinea ? controlador.abrirPdfActa : null,
      onActualizado: onActualizado,
    );
  }
}

/// Cuerpo del detalle (sin Riverpod), útil en pruebas de widget.
class DetalleActaLayout extends StatelessWidget {
  const DetalleActaLayout({
    required this.detalle,
    required this.numero,
    required this.acciones,
    this.edicionBloqueada = false,
    this.enLinea = false,
    this.onPdf,
    this.onActualizado,
    super.key,
  });

  final DetalleActa? detalle;
  final int numero;
  final DetalleActaAcciones acciones;
  final bool edicionBloqueada;
  final bool enLinea;
  final Future<void> Function(int numero)? onPdf;
  final Future<void> Function()? onActualizado;

  @override
  Widget build(BuildContext context) {
    final acta = detalle;
    if (acta == null) {
      return const Scaffold(
        body: Center(child: Text('No se encontró el acta.')),
      );
    }

    final grupos = <String, List<LineaDetalle>>{};
    for (final linea in acta.lineas) {
      grupos.putIfAbsent(linea.construccion, () => []).add(linea);
    }

    final puedeAgregar =
        acciones.puedeAgregarLinea(acta.cerrada) && !edicionBloqueada;
    final esRevisor = acciones.esRevisor() && !acta.cerrada;

    return Scaffold(
      appBar: AppBar(
        title: Text('Acta ${acta.numero}'),
        actions: [
          if (enLinea && onPdf != null)
            IconButton(
              tooltip: 'PDF',
              onPressed: edicionBloqueada ? null : () => onPdf!(numero),
              icon: const Icon(Icons.picture_as_pdf_outlined),
            ),
          if (esRevisor)
            TextButton(
              onPressed: edicionBloqueada
                  ? null
                  : () async {
                      await acciones.aprobarTodasLineas(numero);
                      await onActualizado?.call();
                    },
              child: const Text('Aprobar todas'),
            ),
        ],
      ),
      floatingActionButton: puedeAgregar
          ? FloatingActionButton.extended(
              onPressed: () => context.push('/actas/$numero/linea/nueva'),
              label: const Text('Agregar línea'),
              icon: const Icon(Icons.add),
            )
          : null,
      body: AbsorbPointer(
        absorbing: edicionBloqueada,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            if (edicionBloqueada) ...[
              const Text('Sincronizando…'),
              const SizedBox(height: 12),
            ],
            Text(
              '${fechaVisible(acta.fechaInicial)} – ${fechaVisible(acta.fechaFinal)}',
            ),
            const SizedBox(height: 8),
            Text('Arquitecto: ${acta.arquitecto}'),
            Text('Revisor: ${acta.revisor}'),
            Text('Contratista: ${acta.contratista}'),
            if (acta.observacion != null) ...[
              const SizedBox(height: 8),
              Text('Observación: ${acta.observacion}'),
            ],
            if (acta.cerrada) ...[
              const SizedBox(height: 12),
              Text(
                Mensajes.cerrada,
                style: TextStyle(color: Theme.of(context).colorScheme.error),
              ),
            ],
            const SizedBox(height: 12),
            Text(
              'Total aprobado: ${dinero(acta.totalAprobado)}',
              style: Theme.of(context).textTheme.titleMedium,
            ),
            for (final entrada in grupos.entries) ...[
              const SizedBox(height: 16),
              Text(entrada.key, style: Theme.of(context).textTheme.titleMedium),
              for (final linea in entrada.value)
                _Linea(
                  linea: linea,
                  actaCerrada: acta.cerrada,
                  acciones: acciones,
                  edicionBloqueada: edicionBloqueada,
                  numeroActa: numero,
                  onActualizado: onActualizado,
                ),
            ],
          ],
        ),
      ),
    );
  }
}

class _Linea extends StatelessWidget {
  const _Linea({
    required this.linea,
    required this.actaCerrada,
    required this.acciones,
    required this.edicionBloqueada,
    required this.numeroActa,
    this.onActualizado,
  });

  final LineaDetalle linea;
  final bool actaCerrada;
  final DetalleActaAcciones acciones;
  final bool edicionBloqueada;
  final int numeroActa;
  final Future<void> Function()? onActualizado;

  @override
  Widget build(BuildContext context) {
    final estado = linea.aprobado ? 'Aprobado' : 'Rechazado';
    final local = linea.estadoLocal == 'sincronizado'
        ? ''
        : ' · ${linea.estadoLocal}';
    final puedeEditar =
        acciones.puedeEditarLinea(linea, actaCerrada) && !edicionBloqueada;
    final esRevisor = acciones.esRevisor() && !actaCerrada;

    final tarjeta = Card(
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(linea.descripcion),
            const SizedBox(height: 4),
            Text('${linea.perfil} · $estado$local'),
            const SizedBox(height: 4),
            Text(
              '${cantidadVisible(linea.cantidad)} × ${dinero(Decimal.parse(linea.precio))} · ${dinero(Decimal.parse(linea.subtotal))}',
            ),
            if (esRevisor) ...[
              const SizedBox(height: 8),
              Row(
                children: [
                  const Text('Aprobar'),
                  Switch(
                    value: linea.aprobado,
                    onChanged: edicionBloqueada
                        ? null
                        : (valor) async {
                            await acciones.revisarLinea(
                              numeroActa,
                              linea.localId,
                              valor,
                            );
                            await onActualizado?.call();
                          },
                  ),
                ],
              ),
            ],
            if (puedeEditar) ...[
              const SizedBox(height: 8),
              const Text(
                'Deslice a la derecha para editar o a la izquierda para eliminar.',
                style: TextStyle(fontSize: 12),
              ),
              const SizedBox(height: 8),
              Row(
                children: [
                  TextButton(
                    onPressed: () => context.push(
                      '/actas/$numeroActa/linea/${linea.localId}',
                      extra: LineaSincronizacion(
                        construccionId: linea.construccionId,
                        conceptoId: linea.conceptoId,
                        unidadMedidaId: linea.unidadMedidaId,
                        insumoId: linea.insumoId,
                        presupuestoItemId: linea.presupuestoItemId,
                        cantidad: linea.cantidad,
                        precio: linea.precio,
                        descripcion: linea.descripcion,
                      ),
                    ),
                    child: const Text('Editar'),
                  ),
                  TextButton(
                    onPressed: () async {
                      await acciones.eliminarLinea(numeroActa, linea.localId);
                      await onActualizado?.call();
                    },
                    child: const Text('Eliminar'),
                  ),
                ],
              ),
            ],
          ],
        ),
      ),
    );

    if (!puedeEditar) {
      return tarjeta;
    }
    return Dismissible(
      key: Key('linea-${linea.localId}'),
      direction: DismissDirection.horizontal,
      background: Container(
        color: Theme.of(context).colorScheme.primaryContainer,
        alignment: Alignment.centerLeft,
        padding: const EdgeInsets.only(left: 16),
        child: const Text('Editar'),
      ),
      secondaryBackground: Container(
        color: Theme.of(context).colorScheme.errorContainer,
        alignment: Alignment.centerRight,
        padding: const EdgeInsets.only(right: 16),
        child: const Text('Eliminar'),
      ),
      confirmDismiss: (direccion) async {
        if (direccion == DismissDirection.startToEnd) {
          await context.push(
            '/actas/$numeroActa/linea/${linea.localId}',
            extra: LineaSincronizacion(
              construccionId: linea.construccionId,
              conceptoId: linea.conceptoId,
              unidadMedidaId: linea.unidadMedidaId,
              insumoId: linea.insumoId,
              presupuestoItemId: linea.presupuestoItemId,
              cantidad: linea.cantidad,
              precio: linea.precio,
              descripcion: linea.descripcion,
            ),
          );
          await onActualizado?.call();
          return false;
        }
        if (direccion == DismissDirection.endToStart) {
          await acciones.eliminarLinea(numeroActa, linea.localId);
          await onActualizado?.call();
          return true;
        }
        return false;
      },
      child: tarjeta,
    );
  }
}
