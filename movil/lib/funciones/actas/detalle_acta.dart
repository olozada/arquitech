import 'package:decimal/decimal.dart';
import 'package:material_ui/material_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../app/aplicacion.dart';
import '../../app/tema.dart';
import '../../core/db/consultas.dart';
import 'detalle_acta_acciones.dart';
import 'estado_acta.dart';
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
  var _enPrimerPlano = false;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    final visible = ModalRoute.of(context)?.isCurrent ?? true;
    if (visible && !_enPrimerPlano) {
      _enPrimerPlano = true;
      _cargar();
    } else if (!visible) {
      _enPrimerPlano = false;
    }
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
        appBar: AppBar(title: const Text('Detalle del acta')),
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

    final puedeAgregar =
        acciones.puedeAgregarLinea(acta.cerrada) && !edicionBloqueada;
    final esRevisor = acciones.esRevisor() && !acta.cerrada;
    final tema = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Detalle del acta'),
        actions: [
          if (esRevisor)
            Padding(
              padding: const EdgeInsetsDirectional.only(end: 8),
              child: TextButton(
                onPressed: edicionBloqueada
                    ? null
                    : () async {
                        await acciones.aprobarTodasLineas(numero);
                        await onActualizado?.call();
                      },
                child: const Text('Aprobar todas'),
              ),
            ),
        ],
      ),
      body: AbsorbPointer(
        absorbing: edicionBloqueada,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            if (edicionBloqueada) ...[
              const Text('Sincronizando…'),
              const SizedBox(height: 12),
            ],
            _EncabezadoActa(
              acta: acta,
              onPdf: enLinea && onPdf != null && !edicionBloqueada
                  ? () => onPdf!(numero)
                  : null,
              mostrarPdf: enLinea && onPdf != null,
            ),
            const SizedBox(height: 20),
            Row(
              children: [
                Expanded(
                  child: Padding(
                    padding: const EdgeInsetsDirectional.only(start: 8),
                    child: Semantics(
                      header: true,
                      child: Text(
                        'Perfiles',
                        style: tema.textTheme.titleSmall?.copyWith(
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ),
                  ),
                ),
                if (puedeAgregar)
                  FilledButton.icon(
                    // El tema da ancho mínimo infinito; aquí va el del texto.
                    style: FilledButton.styleFrom(
                      minimumSize: const Size(48, 44),
                      textStyle: tema.textTheme.labelLarge?.copyWith(
                        fontSize: 15,
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                    onPressed: () async {
                      await context.push('/actas/$numero/linea/nueva');
                      await onActualizado?.call();
                    },
                    icon: const Icon(Icons.add, size: 20),
                    label: const Text('Agregar línea'),
                  ),
              ],
            ),
            const SizedBox(height: 8),
            if (acta.lineas.isEmpty)
              Padding(
                padding: const EdgeInsets.all(24),
                child: Text(
                  'El acta no tiene líneas.',
                  textAlign: TextAlign.center,
                  style: tema.textTheme.bodyMedium?.copyWith(
                    color: tema.colorScheme.onSurfaceVariant,
                  ),
                ),
              ),
            for (final linea in acta.lineas)
              _Linea(
                linea: linea,
                actaCerrada: acta.cerrada,
                acciones: acciones,
                edicionBloqueada: edicionBloqueada,
                numeroActa: numero,
                onActualizado: onActualizado,
              ),
          ],
        ),
      ),
    );
  }
}

/// Tarjeta superior: número, estado, participantes y total aprobado.
class _EncabezadoActa extends StatelessWidget {
  const _EncabezadoActa({
    required this.acta,
    required this.onPdf,
    required this.mostrarPdf,
  });

  final DetalleActa acta;
  final VoidCallback? onPdf;
  final bool mostrarPdf;

  @override
  Widget build(BuildContext context) {
    final tema = Theme.of(context);
    final esquema = tema.colorScheme;
    final total = tema.textTheme.titleMedium?.copyWith(
      fontWeight: FontWeight.w600,
    );
    return Card(
      margin: EdgeInsets.zero,
      child: Padding(
        padding: const EdgeInsets.fromLTRB(20, 16, 12, 16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    'Acta ${numeroActaVisible(acta.numero)}',
                    style: tema.textTheme.titleLarge,
                  ),
                ),
                if (mostrarPdf)
                  BotonBarra(
                    texto: 'PDF',
                    icono: Icons.description_outlined,
                    descripcion: 'Descargar PDF del acta',
                    onPressed: onPdf,
                  ),
              ],
            ),
            const SizedBox(height: 8),
            Align(
              alignment: AlignmentDirectional.centerStart,
              child: EtiquetaEstadoActa(cerrada: acta.cerrada),
            ),
            const SizedBox(height: 12),
            Padding(
              padding: const EdgeInsetsDirectional.only(end: 8),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text(
                    '${fechaVisible(acta.fechaInicial)} – ${fechaVisible(acta.fechaFinal)}',
                    style: tema.textTheme.bodySmall?.copyWith(
                      color: esquema.onSurfaceVariant,
                    ),
                  ),
                  const SizedBox(height: 8),
                  _Dato(etiqueta: 'Arquitecto:', valor: acta.arquitecto),
                  _Dato(etiqueta: 'Revisor:', valor: acta.revisor),
                  _Dato(etiqueta: 'Contratista:', valor: acta.contratista),
                  if (acta.observacion != null)
                    _Dato(etiqueta: 'Observación:', valor: acta.observacion!),
                  if (acta.cerrada) ...[
                    const SizedBox(height: 12),
                    Text(
                      Mensajes.cerrada,
                      style: TextStyle(color: esquema.error),
                    ),
                  ],
                  const Divider(height: 28),
                  MergeSemantics(
                    child: Row(
                      children: [
                        Expanded(child: Text('Total aprobado:', style: total)),
                        const SizedBox(width: 12),
                        Text(dinero(acta.totalAprobado), style: total),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _Dato extends StatelessWidget {
  const _Dato({required this.etiqueta, required this.valor});

  final String etiqueta;
  final String valor;

  @override
  Widget build(BuildContext context) {
    final estilo = Theme.of(context).textTheme.bodyMedium;
    return MergeSemantics(
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 2),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(etiqueta, style: estilo),
            const SizedBox(width: 12),
            Expanded(
              child: Text(valor, style: estilo, textAlign: TextAlign.end),
            ),
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

  Future<void> _editar(BuildContext context) async {
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
  }

  @override
  Widget build(BuildContext context) {
    final tema = Theme.of(context);
    final esquema = tema.colorScheme;
    final sinSincronizar = linea.estadoLocal != 'sincronizado';
    final sinDescripcion = linea.descripcion.trim().isEmpty;
    final puedeEditar =
        acciones.puedeEditarLinea(linea, actaCerrada) && !edicionBloqueada;
    final esRevisor = acciones.esRevisor() && !actaCerrada;

    final tarjeta = Card(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(20, 16, 20, 16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              linea.construccion,
              style: tema.textTheme.titleMedium?.copyWith(
                fontWeight: FontWeight.w700,
              ),
            ),
            const SizedBox(height: 6),
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(
                  child: Wrap(
                    spacing: 8,
                    runSpacing: 4,
                    crossAxisAlignment: WrapCrossAlignment.center,
                    children: [
                      Text(linea.perfil, style: tema.textTheme.bodyLarge),
                      EtiquetaAprobacion(aprobado: linea.aprobado),
                    ],
                  ),
                ),
                const SizedBox(width: 12),
                Text(
                  dinero(Decimal.parse(linea.subtotal)),
                  style: tema.textTheme.bodyLarge,
                ),
              ],
            ),
            if (sinSincronizar) ...[
              const SizedBox(height: 6),
              Row(
                children: [
                  Icon(Icons.sync, size: 14, color: esquema.tertiary),
                  const SizedBox(width: 4),
                  Text(
                    'Sin sincronizar',
                    style: tema.textTheme.bodySmall?.copyWith(
                      color: esquema.tertiary,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ],
              ),
            ],
            const SizedBox(height: 12),
            Text(
              sinDescripcion ? 'Sin descripción' : linea.descripcion,
              style: sinDescripcion
                  ? TextStyle(
                      color: esquema.onSurfaceVariant,
                      fontStyle: FontStyle.italic,
                    )
                  : tema.textTheme.bodyMedium,
            ),
            const SizedBox(height: 4),
            Text(
              '${cantidadVisible(linea.cantidad)} × ${dinero(Decimal.parse(linea.precio))}',
              style: tema.textTheme.bodySmall?.copyWith(
                color: esquema.onSurfaceVariant,
              ),
            ),
            if (esRevisor) ...[
              const SizedBox(height: 8),
              // Un solo nodo de accesibilidad: el lector dice «Aprobar» y el
              // estado del interruptor.
              MergeSemantics(
                child: Row(
                  children: [
                    Text('Aprobar', style: tema.textTheme.bodyLarge),
                    const SizedBox(width: 8),
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
              ),
            ],
            if (puedeEditar) ...[
              const SizedBox(height: 16),
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton.icon(
                      onPressed: () => _editar(context),
                      icon: const Icon(Icons.edit_outlined, size: 18),
                      label: const Text('Editar'),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: OutlinedButton.icon(
                      onPressed: () async {
                        await acciones.eliminarLinea(numeroActa, linea.localId);
                        await onActualizado?.call();
                      },
                      icon: const Icon(Icons.delete_outline, size: 18),
                      label: const Text('Eliminar'),
                    ),
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
        margin: const EdgeInsets.symmetric(vertical: 6),
        decoration: BoxDecoration(
          color: Theme.of(context).colorScheme.primaryContainer,
          borderRadius: radioTarjeta,
        ),
        alignment: Alignment.centerLeft,
        padding: const EdgeInsets.only(left: 16),
        child: const Text('Editar'),
      ),
      secondaryBackground: Container(
        margin: const EdgeInsets.symmetric(vertical: 6),
        decoration: BoxDecoration(
          color: Theme.of(context).colorScheme.errorContainer,
          borderRadius: radioTarjeta,
        ),
        alignment: Alignment.centerRight,
        padding: const EdgeInsets.only(right: 16),
        child: const Text('Eliminar'),
      ),
      confirmDismiss: (direccion) async {
        if (direccion == DismissDirection.startToEnd) {
          await _editar(context);
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
