import 'package:material_ui/material_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../app/aplicacion.dart';
import '../../app/formulario_adaptable.dart';
import '../../core/db/consultas.dart';
import '../../core/decimal/dinero.dart';

class ListaActasPantalla extends ConsumerStatefulWidget {
  const ListaActasPantalla({super.key});

  @override
  ConsumerState<ListaActasPantalla> createState() => _EstadoLista();
}

class _EstadoLista extends ConsumerState<ListaActasPantalla> {
  final _texto = TextEditingController();
  EstadoActaFiltro _estado = EstadoActaFiltro.todas;
  OrdenActas _orden = OrdenActas.fechaInicial;
  int? _obraId;
  String? _desde;
  String? _hasta;
  List<ObraOpcion> _obras = const [];
  List<ActaResumen> _actas = const [];
  var _cargando = true;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _cargar());
  }

  @override
  void dispose() {
    _texto.dispose();
    super.dispose();
  }

  Future<void> _cargar() async {
    final controlador = ref.read(controladorProvider);
    final actas = await controlador.listarActas(
      FiltroActas(
        texto: _texto.text,
        estado: _estado,
        desde: _desde,
        hasta: _hasta,
        obraId: _obraId,
        orden: _orden,
      ),
    );
    final obras = await controlador.obras();
    if (!mounted) {
      return;
    }
    setState(() {
      _actas = actas;
      _obras = obras;
      _cargando = false;
    });
  }

  String _mensajeVacio() {
    final texto = _texto.text.trim();
    final estado = switch (_estado) {
      EstadoActaFiltro.abiertas => 'abiertas',
      EstadoActaFiltro.cerradas => 'cerradas',
      EstadoActaFiltro.todas => '',
    };
    if (texto.isNotEmpty) {
      final cuales = estado.isEmpty ? 'actas' : 'actas $estado';
      return 'No hay $cuales que coincidan con «$texto». Borre la búsqueda para ver el resto.';
    }
    if (estado.isEmpty) {
      return 'No hay actas descargadas.';
    }
    return 'No hay actas $estado.';
  }

  Widget _actaTile(ActaResumen acta) {
    final esquema = Theme.of(context).colorScheme;
    return ListTile(
      key: Key('acta-${acta.numero}'),
      minVerticalPadding: 12,
      title: Text('Acta ${acta.numero}'),
      subtitle: Text.rich(
        TextSpan(
          children: [
            TextSpan(
              text:
                  '${fechaVisible(acta.fechaInicial)} – ${fechaVisible(acta.fechaFinal)}\n'
                  '${acta.arquitecto} · ${acta.contratista} · ${acta.revisor}',
            ),
            if (acta.cerrada) const TextSpan(text: '\nCerrada'),
            if (acta.cambiosSinSincronizar)
              TextSpan(
                text: '\nCambios sin sincronizar',
                style: TextStyle(
                  color: esquema.primary,
                  fontWeight: FontWeight.w600,
                ),
              ),
          ],
        ),
      ),
      isThreeLine: true,
      trailing: Text(dinero(acta.totalAprobado)),
      onTap: () async {
        await context.push('/actas/${acta.numero}');
        // Al volver, el total y «Cambios sin sincronizar» pueden haber cambiado.
        if (mounted) {
          await _cargar();
        }
      },
    );
  }

  Widget? _estadoVacio() {
    if (_cargando) {
      return const Center(child: Text('Cargando actas…'));
    }
    if (_actas.isEmpty) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Text(_mensajeVacio(), textAlign: TextAlign.center),
        ),
      );
    }
    return null;
  }

  List<Widget> _filtros({required bool panel}) {
    return [
      TextField(
        key: const Key('buscar-acta'),
        controller: _texto,
        decoration: const InputDecoration(
          labelText: 'Número o nombre',
          prefixIcon: Icon(Icons.search),
        ),
        textInputAction: TextInputAction.search,
        onSubmitted: (_) => _cargar(),
      ),
      const SizedBox(height: 12),
      SegmentedButton<EstadoActaFiltro>(
        expandedInsets: EdgeInsets.zero,
        showSelectedIcon: false,
        segments: const [
          ButtonSegment(value: EstadoActaFiltro.todas, label: Text('Todas')),
          ButtonSegment(
            value: EstadoActaFiltro.abiertas,
            label: Text('Abiertas'),
          ),
          ButtonSegment(
            value: EstadoActaFiltro.cerradas,
            label: Text('Cerradas'),
          ),
        ],
        selected: {_estado},
        onSelectionChanged: (seleccion) {
          setState(() => _estado = seleccion.first);
          _cargar();
        },
      ),
      const SizedBox(height: 12),
      FilaAdaptable(
        anchoMinimoFila: panel ? double.infinity : 300,
        children: [
          DropdownButtonFormField<OrdenActas>(
            initialValue: _orden,
            isExpanded: true,
            decoration: const InputDecoration(labelText: 'Orden'),
            items: const [
              DropdownMenuItem(
                value: OrdenActas.fechaInicial,
                child: Text('Fecha inicial', overflow: TextOverflow.ellipsis),
              ),
              DropdownMenuItem(
                value: OrdenActas.fechaFinal,
                child: Text('Fecha final', overflow: TextOverflow.ellipsis),
              ),
              DropdownMenuItem(
                value: OrdenActas.numero,
                child: Text('Número', overflow: TextOverflow.ellipsis),
              ),
            ],
            onChanged: (valor) {
              if (valor == null) {
                return;
              }
              setState(() => _orden = valor);
              _cargar();
            },
          ),
          DropdownButtonFormField<int?>(
            initialValue: _obraId,
            isExpanded: true,
            decoration: const InputDecoration(labelText: 'Obra'),
            items: [
              const DropdownMenuItem<int?>(
                value: null,
                child: Text('Todas', overflow: TextOverflow.ellipsis),
              ),
              for (final obra in _obras)
                DropdownMenuItem(
                  value: obra.id,
                  child: Text(
                    obra.descripcion,
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
            ],
            onChanged: (valor) {
              setState(() => _obraId = valor);
              _cargar();
            },
          ),
        ],
      ),
      const SizedBox(height: 12),
      Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            child: _FechaCampo(
              etiqueta: 'Desde',
              onChanged: (valor) => _desde = valor,
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: _FechaCampo(
              etiqueta: 'Hasta',
              onChanged: (valor) => _hasta = valor,
            ),
          ),
          Padding(
            padding: const EdgeInsets.only(top: 4),
            child: IconButton.filledTonal(
              tooltip: 'Aplicar fechas',
              onPressed: _cargar,
              icon: const Icon(Icons.search),
            ),
          ),
        ],
      ),
    ];
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Actas')),
      body: LayoutBuilder(
        builder: (context, restricciones) {
          final ventana = TamanoVentana.desdeAncho(restricciones.maxWidth);
          final vacio = _estadoVacio();
          if (ventana == TamanoVentana.expandido) {
            return Row(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                SizedBox(
                  width: 360,
                  child: SingleChildScrollView(
                    keyboardDismissBehavior:
                        ScrollViewKeyboardDismissBehavior.onDrag,
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: _filtros(panel: true),
                    ),
                  ),
                ),
                const VerticalDivider(width: 1),
                Expanded(
                  child:
                      vacio ??
                      ListView.builder(
                        itemCount: _actas.length,
                        itemBuilder: (context, indice) =>
                            _actaTile(_actas[indice]),
                      ),
                ),
              ],
            );
          }
          return CustomScrollView(
            keyboardDismissBehavior: ScrollViewKeyboardDismissBehavior.onDrag,
            slivers: [
              SliverPadding(
                padding: EdgeInsets.fromLTRB(
                  ventana.margen,
                  8,
                  ventana.margen,
                  8,
                ),
                sliver: SliverToBoxAdapter(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: _filtros(panel: false),
                  ),
                ),
              ),
              if (vacio != null)
                SliverFillRemaining(hasScrollBody: false, child: vacio)
              else
                SliverList.builder(
                  itemCount: _actas.length,
                  itemBuilder: (context, indice) => _actaTile(_actas[indice]),
                ),
            ],
          );
        },
      ),
    );
  }
}

class _FechaCampo extends StatelessWidget {
  const _FechaCampo({required this.etiqueta, required this.onChanged});

  final String etiqueta;
  final ValueChanged<String?> onChanged;

  @override
  Widget build(BuildContext context) {
    return TextField(
      decoration: InputDecoration(labelText: etiqueta, hintText: 'aaaa-mm-dd'),
      keyboardType: TextInputType.datetime,
      onChanged: (valor) {
        final texto = valor.trim();
        onChanged(
          RegExp(r'^\d{4}-\d{2}-\d{2}$').hasMatch(texto) ? texto : null,
        );
      },
    );
  }
}
