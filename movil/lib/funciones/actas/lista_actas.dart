import 'package:material_ui/material_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../app/aplicacion.dart';
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

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Actas')),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
            child: TextField(
              key: const Key('buscar-acta'),
              controller: _texto,
              decoration: const InputDecoration(labelText: 'Número o nombre'),
              onSubmitted: (_) => _cargar(),
            ),
          ),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
            child: SegmentedButton<EstadoActaFiltro>(
              segments: const [
                ButtonSegment(
                  value: EstadoActaFiltro.todas,
                  label: Text('Todas'),
                ),
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
          ),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: Row(
              children: [
                Expanded(
                  child: DropdownButtonFormField<OrdenActas>(
                    initialValue: _orden,
                    decoration: const InputDecoration(labelText: 'Orden'),
                    items: const [
                      DropdownMenuItem(
                        value: OrdenActas.fechaInicial,
                        child: Text('Fecha inicial'),
                      ),
                      DropdownMenuItem(
                        value: OrdenActas.fechaFinal,
                        child: Text('Fecha final'),
                      ),
                      DropdownMenuItem(
                        value: OrdenActas.numero,
                        child: Text('Número'),
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
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: DropdownButtonFormField<int?>(
                    initialValue: _obraId,
                    decoration: const InputDecoration(labelText: 'Obra'),
                    items: [
                      const DropdownMenuItem<int?>(
                        value: null,
                        child: Text('Todas'),
                      ),
                      for (final obra in _obras)
                        DropdownMenuItem(
                          value: obra.id,
                          child: Text(obra.descripcion),
                        ),
                    ],
                    onChanged: (valor) {
                      setState(() => _obraId = valor);
                      _cargar();
                    },
                  ),
                ),
              ],
            ),
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
            child: Row(
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
                IconButton(
                  tooltip: 'Aplicar fechas',
                  onPressed: _cargar,
                  icon: const Icon(Icons.search),
                ),
              ],
            ),
          ),
          Expanded(
            child: _cargando
                ? const Center(child: Text('Cargando actas…'))
                : _actas.isEmpty
                ? const Center(child: Text('No hay actas con ese filtro.'))
                : ListView.builder(
                    itemCount: _actas.length,
                    itemBuilder: (context, indice) {
                      final acta = _actas[indice];
                      return ListTile(
                        key: Key('acta-${acta.numero}'),
                        minVerticalPadding: 12,
                        title: Text('Acta ${acta.numero}'),
                        subtitle: Text(
                          '${fechaVisible(acta.fechaInicial)} – ${fechaVisible(acta.fechaFinal)}\n'
                          '${acta.arquitecto} · ${acta.contratista} · ${acta.revisor}'
                          '${acta.cerrada ? '\nCerrada' : ''}'
                          '${acta.cambiosSinSincronizar ? '\nCambios sin sincronizar' : ''}',
                        ),
                        isThreeLine: true,
                        trailing: Text(dinero(acta.totalAprobado)),
                        onTap: () => context.push('/actas/${acta.numero}'),
                      );
                    },
                  ),
          ),
        ],
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
      onChanged: (valor) {
        final texto = valor.trim();
        onChanged(
          RegExp(r'^\d{4}-\d{2}-\d{2}$').hasMatch(texto) ? texto : null,
        );
      },
    );
  }
}
