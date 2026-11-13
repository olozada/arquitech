import 'package:material_ui/material_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../app/aplicacion.dart';
import '../../core/db/consultas.dart';
import '../../core/decimal/dinero.dart';
import 'grafico_acumulado_mensual.dart';
import 'presupuesto_modelos.dart';

class DetallePresupuestoPantalla extends ConsumerStatefulWidget {
  const DetallePresupuestoPantalla({required this.id, super.key});

  final int id;

  @override
  ConsumerState<DetallePresupuestoPantalla> createState() =>
      _EstadoDetallePresupuesto();
}

class _EstadoDetallePresupuesto
    extends ConsumerState<DetallePresupuestoPantalla> {
  PresupuestoPaquete? _paquete;
  PresupuestoDetalleVista? _detalle;
  List<AvisoValidacionPresupuesto> _validacion = const [];
  SeguimientoPresupuestoVista? _seguimiento;
  var _cargando = true;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _cargar());
  }

  Future<void> _cargar() async {
    final paquete = await ref
        .read(controladorProvider)
        .presupuestoPorId(widget.id);
    if (!mounted) {
      return;
    }
    if (paquete == null) {
      setState(() {
        _cargando = false;
      });
      return;
    }
    setState(() {
      _paquete = paquete;
      _detalle = PresupuestoDetalleVista.desdeJson(paquete.detalleJson);
      _validacion = parsearValidacion(paquete.validacionJson);
      if (paquete.seguimientoJson != null &&
          paquete.seguimientoJson!.trim().isNotEmpty) {
        _seguimiento = SeguimientoPresupuestoVista.desdeJson(
          paquete.seguimientoJson!,
        );
      }
      _cargando = false;
    });
  }

  @override
  Widget build(BuildContext context) {
    final controlador = ref.watch(controladorProvider);
    final enLinea = controlador.enLinea;
    if (_cargando) {
      return Scaffold(
        appBar: AppBar(title: Text('Presupuesto ${widget.id}')),
        body: const Center(child: Text('Cargando presupuesto…')),
      );
    }
    if (_paquete == null || _detalle == null) {
      return Scaffold(
        appBar: AppBar(title: Text('Presupuesto ${widget.id}')),
        body: const Center(child: Text('No se encontró el presupuesto.')),
      );
    }
    final detalle = _detalle!;
    return DefaultTabController(
      length: 3,
      child: Scaffold(
        appBar: AppBar(
          title: Text(detalle.nombre),
          bottom: const TabBar(
            tabs: [
              Tab(text: 'Detalle'),
              Tab(text: 'Validación'),
              Tab(text: 'Seguimiento'),
            ],
          ),
          actions: [
            if (enLinea)
              Semantics(
                label: 'Descargar PDF del presupuesto',
                button: true,
                child: IconButton(
                  tooltip: 'PDF',
                  onPressed: controlador.ocupado
                      ? null
                      : () => controlador.abrirPdfPresupuesto(widget.id),
                  icon: const Icon(Icons.picture_as_pdf_outlined),
                ),
              ),
            if (enLinea)
              Semantics(
                label: 'Descargar Excel del presupuesto',
                button: true,
                child: IconButton(
                  tooltip: 'Excel',
                  onPressed: controlador.ocupado
                      ? null
                      : () => controlador.abrirExcelPresupuesto(widget.id),
                  icon: const Icon(Icons.table_chart_outlined),
                ),
              ),
          ],
        ),
        body: TabBarView(
          children: [
            _PestanaDetalle(detalle: detalle),
            _PestanaValidacion(aviso: _validacion),
            _PestanaSeguimiento(
              seguimiento: _seguimiento,
              sinJson: _seguimiento == null,
            ),
          ],
        ),
      ),
    );
  }
}

class _PestanaDetalle extends StatelessWidget {
  const _PestanaDetalle({required this.detalle});

  final PresupuestoDetalleVista detalle;

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Text(
          '${detalle.construccion} · v${detalle.version} · ${detalle.estado}',
        ),
        const SizedBox(height: 4),
        Text('Fecha ${fechaVisible(detalle.fecha)}'),
        const SizedBox(height: 16),
        _ResumenCostos(resumen: detalle.resumen),
        const SizedBox(height: 16),
        for (final capitulo in detalle.capitulos)
          _CapituloTile(capitulo: capitulo),
      ],
    );
  }
}

class _ResumenCostos extends StatelessWidget {
  const _ResumenCostos({required this.resumen});

  final ResumenCostoPresupuesto resumen;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text('Subtotal ${dineroPresupuesto(resumen.subtotal)}'),
            Text(
              'Material ${dineroPresupuesto(resumen.totalMaterial)} · '
              'Mano de obra ${dineroPresupuesto(resumen.totalManoObra)}',
            ),
            Text(
              'Imprevistos ${dineroPresupuesto(resumen.imprevistos)} · '
              'Incremento ${dineroPresupuesto(resumen.incremento)}',
            ),
            Text(
              'Administración ${dineroPresupuesto(resumen.administracion)} · '
              'Utilidad ${dineroPresupuesto(resumen.utilidad)} · '
              'IVA ${dineroPresupuesto(resumen.ivaUtilidad)}',
            ),
            const SizedBox(height: 8),
            Text(
              'Total ${dineroPresupuesto(resumen.total)}',
              style: Theme.of(context).textTheme.titleMedium,
            ),
            if (resumen.valorPorM2 != null)
              Text('Valor por m² ${dineroPresupuesto(resumen.valorPorM2!)}'),
          ],
        ),
      ),
    );
  }
}

class _CapituloTile extends StatelessWidget {
  const _CapituloTile({required this.capitulo});

  final CapituloPresupuestoVista capitulo;

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: ExpansionTile(
        title: Text('${capitulo.numero}. ${capitulo.nombre}'),
        subtitle: Text(dineroPresupuesto(capitulo.total)),
        children: [
          for (final item in capitulo.items)
            ListTile(
              title: Text('${item.codigo} ${item.descripcion}'),
              subtitle: Text(
                '${item.cantidad} ${item.unidad} × '
                '${dineroPresupuesto(item.valorUnitario)}',
              ),
              trailing: Text(dineroPresupuesto(item.total)),
            ),
        ],
      ),
    );
  }
}

class _PestanaValidacion extends StatelessWidget {
  const _PestanaValidacion({required this.aviso});

  final List<AvisoValidacionPresupuesto> aviso;

  @override
  Widget build(BuildContext context) {
    if (aviso.isEmpty) {
      return const Center(child: Text('Sin avisos de validación.'));
    }
    return ListView.separated(
      padding: const EdgeInsets.all(16),
      itemCount: aviso.length,
      separatorBuilder: (_, _) => const Divider(height: 1),
      itemBuilder: (context, indice) {
        final fila = aviso[indice];
        return ListTile(
          minVerticalPadding: 12,
          title: Text('${fila.codigo} · ${fila.tipo}'),
          subtitle: Text(fila.mensaje),
        );
      },
    );
  }
}

class _PestanaSeguimiento extends StatefulWidget {
  const _PestanaSeguimiento({required this.seguimiento, required this.sinJson});

  final SeguimientoPresupuestoVista? seguimiento;
  final bool sinJson;

  @override
  State<_PestanaSeguimiento> createState() => _EstadoSeguimiento();
}

class _EstadoSeguimiento extends State<_PestanaSeguimiento> {
  int _capituloId = 0;

  @override
  Widget build(BuildContext context) {
    if (widget.sinJson || widget.seguimiento == null) {
      return const Center(
        child: Text('El seguimiento no está en el paquete offline.'),
      );
    }
    final datos = widget.seguimiento!;
    final capitulos = <int, String>{};
    for (final fila in datos.items) {
      final id = fila.capituloId;
      if (id != null) {
        capitulos.putIfAbsent(id, () => fila.capitulo);
      }
    }
    final items = datos.items
        .where((fila) => _capituloId == 0 || fila.capituloId == _capituloId)
        .toList();
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text('Presupuestado ${dineroPresupuesto(datos.resumen.total)}'),
                Text(
                  'Ejecutado ${dineroPresupuesto(datos.ejecutado)} · '
                  'Proyección ${dineroPresupuesto(datos.proyeccion)}',
                ),
                Text(
                  'No presupuestado ${dineroPresupuesto(datos.noPresupuestado)}',
                ),
              ],
            ),
          ),
        ),
        if (datos.acumuladoMensual.isNotEmpty) ...[
          const SizedBox(height: 12),
          GraficoAcumuladoMensual(puntos: datos.acumuladoMensual),
        ],
        if (capitulos.isNotEmpty) ...[
          const SizedBox(height: 12),
          DropdownMenu<int>(
            initialSelection: _capituloId,
            label: const Text('Capítulo'),
            dropdownMenuEntries: [
              const DropdownMenuEntry(value: 0, label: 'Todos'),
              for (final entrada in capitulos.entries)
                DropdownMenuEntry(value: entrada.key, label: entrada.value),
            ],
            onSelected: (valor) {
              if (valor == null) {
                return;
              }
              setState(() => _capituloId = valor);
            },
          ),
        ],
        const SizedBox(height: 12),
        for (final fila in items) _FilaSeguimiento(fila: fila),
        if (datos.noPresupuestados.isNotEmpty) ...[
          const SizedBox(height: 16),
          Text(
            'Ejecutado no presupuestado',
            style: Theme.of(context).textTheme.titleSmall,
          ),
          for (final fila in datos.noPresupuestados)
            _FilaSeguimiento(fila: fila),
        ],
      ],
    );
  }
}

class _FilaSeguimiento extends StatelessWidget {
  const _FilaSeguimiento({required this.fila});

  final FilaSeguimientoPresupuesto fila;

  @override
  Widget build(BuildContext context) {
    final avance = (fila.avanceFisico * 100).clamp(0, 100);
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text('${fila.codigo} ${fila.descripcion}'),
            Text(
              '${fila.unidad} · Avance físico ${avance.toStringAsFixed(1)} %',
            ),
            LinearProgressIndicator(value: fila.avanceFisico.clamp(0, 1)),
            const SizedBox(height: 8),
            Text(
              'Presup. ${dineroPresupuesto(fila.valorPresupuestado)} · '
              'Ejec. ${dineroPresupuesto(fila.valorEjecutado)} · '
              'Saldo ${dineroPresupuesto(fila.saldo)}',
            ),
            Text(
              'Precio actas ${dineroPresupuesto(fila.precioActas)} · '
              'Proyección ${dineroPresupuesto(fila.proyeccionCierre)}',
            ),
          ],
        ),
      ),
    );
  }
}
