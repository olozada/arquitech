import 'dart:async';

import 'package:decimal/decimal.dart';
import 'package:material_ui/material_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../app/aplicacion.dart';
import '../../app/formulario_adaptable.dart';
import '../../app/tema.dart';
import '../../core/db/consultas.dart';
import '../../core/decimal/dinero.dart';
import '../../core/errores/mensajes.dart';
import '../../sincronizacion/modelos.dart';

class FormularioLineaPantalla extends ConsumerStatefulWidget {
  const FormularioLineaPantalla({
    required this.numeroActa,
    this.localId,
    this.inicial,
    super.key,
  });

  final int numeroActa;
  final String? localId;
  final LineaSincronizacion? inicial;

  @override
  ConsumerState<FormularioLineaPantalla> createState() => _EstadoFormulario();
}

class _EstadoFormulario extends ConsumerState<FormularioLineaPantalla> {
  final _descripcion = TextEditingController();
  final _cantidad = TextEditingController();
  final _precio = TextEditingController();
  final _insumoBusqueda = TextEditingController();
  List<OpcionCatalogo> _construcciones = const [];
  List<OpcionCatalogo> _conceptos = const [];
  List<OpcionCatalogo> _unidades = const [];
  List<ItemPresupuestoOpcion> _items = const [];
  List<InsumoOpcion> _insumosEncontrados = const [];
  final _bloqueInsumo = GlobalKey();
  Timer? _relojInsumo;
  var _buscandoInsumo = false;
  var _consultaInsumo = '';
  var _generacionInsumo = 0;
  var _silenciarBusqueda = false;
  int? _construccionId;
  int? _conceptoId;
  int? _unidadMedidaId;
  int? _insumoId;
  int? _presupuestoItemId;
  var _cargando = true;
  String? _aviso;

  bool get _edicion => widget.localId != null;

  @override
  void initState() {
    super.initState();
    final inicial = widget.inicial;
    if (inicial != null) {
      _construccionId = inicial.construccionId;
      _conceptoId = inicial.conceptoId;
      _presupuestoItemId = inicial.presupuestoItemId;
      _unidadMedidaId = inicial.unidadMedidaId;
      _insumoId = inicial.insumoId;
      _descripcion.text = inicial.descripcion;
      _cantidad.text = inicial.cantidad;
      _precio.text = inicial.precio;
    }
    WidgetsBinding.instance.addPostFrameCallback((_) => _cargar());
  }

  @override
  void dispose() {
    _descripcion.dispose();
    _cantidad.dispose();
    _precio.dispose();
    _relojInsumo?.cancel();
    _insumoBusqueda.dispose();
    super.dispose();
  }

  void _actualizarSubtotal() {
    if (mounted) {
      setState(() {});
    }
  }

  String? get _subtotalVisible {
    final cantidad = Decimal.tryParse(
      _cantidad.text.trim().replaceAll(',', '.'),
    );
    final precio = Decimal.tryParse(_precio.text.trim().replaceAll(',', '.'));
    if (cantidad == null || precio == null) {
      return null;
    }
    return dinero(subtotalLinea(cantidad, precio));
  }

  Future<void> _cargar() async {
    final controlador = ref.read(controladorProvider);
    final construcciones = await controlador.construcciones();
    final conceptos = await controlador.conceptos();
    final unidades = await controlador.unidadesMedida();
    var items = const <ItemPresupuestoOpcion>[];
    if (_construccionId != null) {
      items = await controlador.itemsPresupuesto(_construccionId!);
    }
    if (!mounted) {
      return;
    }
    setState(() {
      _construcciones = construcciones;
      _conceptos = conceptos;
      _unidades = unidades;
      _items = items;
      _cargando = false;
    });
  }

  void _buscarInsumo(String texto) {
    if (_silenciarBusqueda) {
      return;
    }
    _relojInsumo?.cancel();
    final generacion = ++_generacionInsumo;
    final consulta = texto.trim();
    if (consulta.length < 2) {
      if (_insumosEncontrados.isNotEmpty ||
          _buscandoInsumo ||
          _consultaInsumo.isNotEmpty) {
        setState(() {
          _insumosEncontrados = const [];
          _buscandoInsumo = false;
          _consultaInsumo = '';
        });
      }
      return;
    }
    _relojInsumo = Timer(const Duration(milliseconds: 250), () async {
      if (!mounted || generacion != _generacionInsumo) {
        return;
      }
      setState(() {
        _buscandoInsumo = true;
        _consultaInsumo = consulta;
      });
      final lista = await ref.read(controladorProvider).buscarInsumos(consulta);
      if (!mounted || generacion != _generacionInsumo) {
        return;
      }
      setState(() {
        _insumosEncontrados = lista;
        _buscandoInsumo = false;
      });
      WidgetsBinding.instance.addPostFrameCallback((_) {
        final contexto = _bloqueInsumo.currentContext;
        if (contexto == null || !contexto.mounted) {
          return;
        }
        Scrollable.ensureVisible(
          contexto,
          alignment: 1,
          duration: const Duration(milliseconds: 200),
        );
      });
    });
  }

  Future<void> _cambiarConstruccion(int? id) async {
    setState(() {
      _construccionId = id;
      _presupuestoItemId = null;
      _items = const [];
    });
    if (id == null) {
      return;
    }
    final items = await ref.read(controladorProvider).itemsPresupuesto(id);
    if (!mounted) {
      return;
    }
    setState(() => _items = items);
  }

  Future<void> _guardar() async {
    final controlador = ref.read(controladorProvider);
    if (_construccionId == null || _conceptoId == null) {
      setState(() {
        _aviso = _construccionId == null
            ? Mensajes.construccion
            : Mensajes.concepto;
      });
      return;
    }
    setState(() => _aviso = null);
    ItemPresupuestoOpcion? item;
    if (_presupuestoItemId != null) {
      for (final candidato in _items) {
        if (candidato.id == _presupuestoItemId) {
          item = candidato;
          break;
        }
      }
    }
    final linea = LineaSincronizacion(
      construccionId: _construccionId!,
      conceptoId: item?.conceptoId ?? _conceptoId!,
      unidadMedidaId: item?.unidadMedidaId ?? _unidadMedidaId,
      insumoId: _insumoId,
      presupuestoItemId: item?.id,
      cantidad: _cantidad.text.trim().replaceAll(',', '.'),
      precio: _precio.text.trim().replaceAll(',', '.'),
      descripcion: _descripcion.text,
    );
    if (controlador.edicionBloqueada) {
      return;
    }
    if (_edicion) {
      await controlador.editarLinea(widget.numeroActa, widget.localId!, linea);
    } else {
      await controlador.agregarLinea(widget.numeroActa, linea);
    }
    if (!mounted) {
      return;
    }
    if (controlador.aviso == null) {
      context.pop();
    }
  }

  void _elegirInsumo(InsumoOpcion insumo) {
    _relojInsumo?.cancel();
    _generacionInsumo++;
    _silenciarBusqueda = true;
    setState(() {
      _insumoId = insumo.id;
      _insumosEncontrados = const [];
      _buscandoInsumo = false;
      _consultaInsumo = '';
      _insumoBusqueda.text = insumo.etiqueta;
    });
    _silenciarBusqueda = false;
  }

  void _quitarInsumo() {
    setState(() {
      _relojInsumo?.cancel();
      _insumoId = null;
      _insumoBusqueda.clear();
      _insumosEncontrados = const [];
      _buscandoInsumo = false;
      _consultaInsumo = '';
    });
  }

  Future<void> _elegirItem(int? valor) async {
    setState(() => _presupuestoItemId = valor);
    if (valor == null) {
      return;
    }
    ItemPresupuestoOpcion? elegido;
    for (final candidato in _items) {
      if (candidato.id == valor) {
        elegido = candidato;
        break;
      }
    }
    if (elegido == null) {
      return;
    }
    setState(() {
      _conceptoId = elegido!.conceptoId;
      _unidadMedidaId = elegido.unidadMedidaId;
      if (elegido.precio != null) {
        _precio.text = elegido.precio!;
      }
    });
    _actualizarSubtotal();
  }

  @override
  Widget build(BuildContext context) {
    final controlador = ref.watch(controladorProvider);
    final bloqueado = controlador.edicionBloqueada;
    final titulo = Text(_edicion ? 'Editar línea' : 'Nueva línea');
    if (_cargando) {
      return Scaffold(
        appBar: AppBar(title: titulo),
        body: const Center(child: Text('Cargando catálogos…')),
      );
    }
    final concepto = _Desplegable<int>(
      etiqueta: 'Concepto',
      valor: _conceptoId,
      opciones: [
        for (final opcion in _conceptos) (opcion.id, opcion.descripcion),
      ],
      onChanged: (valor) => setState(() => _conceptoId = valor),
    );
    final obra = SeccionFormulario(
      titulo: 'Obra y actividad',
      icono: Icons.apartment_outlined,
      children: [
        FilaAdaptable(
          children: [
            _Desplegable<int>(
              etiqueta: 'Construcción',
              valor: _construccionId,
              opciones: [
                for (final opcion in _construcciones)
                  (opcion.id, opcion.descripcion),
              ],
              onChanged: bloqueado ? null : _cambiarConstruccion,
            ),
            if (_items.isEmpty)
              concepto
            else
              _Desplegable<int>(
                etiqueta: 'Ítem de presupuesto (opcional)',
                valor: _presupuestoItemId,
                vacio: 'Sin ítem de presupuesto',
                opciones: [
                  for (final opcion in _items) (opcion.id, opcion.descripcion),
                ],
                onChanged: _elegirItem,
              ),
          ],
        ),
        if (_items.isNotEmpty && _presupuestoItemId == null) concepto,
      ],
    );
    final insumo = SeccionFormulario(
      titulo: 'Insumo',
      icono: Icons.inventory_2_outlined,
      children: [
        Column(
          key: _bloqueInsumo,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            _ResultadosInsumo(
              buscando: _buscandoInsumo,
              consulta: _consultaInsumo,
              encontrados: _insumosEncontrados,
              onElegir: bloqueado ? null : _elegirInsumo,
            ),
            TextField(
              controller: _insumoBusqueda,
              decoration: const InputDecoration(
                labelText: 'Buscar insumo (opcional)',
                helperText: 'Escriba al menos 2 letras',
                prefixIcon: Icon(Icons.search),
              ),
              textInputAction: TextInputAction.search,
              onChanged: bloqueado ? null : _buscarInsumo,
            ),
            if (_insumoId != null)
              Padding(
                padding: const EdgeInsets.only(top: 8),
                child: Align(
                  alignment: AlignmentDirectional.centerStart,
                  child: InputChip(
                    avatar: const Icon(Icons.check_circle_outline),
                    label: Text('Insumo seleccionado (#$_insumoId)'),
                    deleteButtonTooltipMessage: 'Quitar insumo',
                    onDeleted: bloqueado ? null : _quitarInsumo,
                  ),
                ),
              ),
          ],
        ),
      ],
    );
    final subtotal = _subtotalVisible;
    final valores = SeccionFormulario(
      titulo: 'Cantidad y precio',
      icono: Icons.calculate_outlined,
      children: [
        if (_unidades.isNotEmpty)
          _Desplegable<int>(
            etiqueta: 'Unidad de medida',
            valor: _unidadMedidaId,
            vacio: 'Sin unidad',
            opciones: [
              for (final opcion in _unidades) (opcion.id, opcion.descripcion),
            ],
            onChanged: bloqueado
                ? null
                : (valor) => setState(() => _unidadMedidaId = valor),
          ),
        FilaAdaptable(
          anchoMinimoFila: 320,
          children: [
            _CampoNumero(
              controller: _cantidad,
              etiqueta: 'Cantidad',
              onChanged: (_) => _actualizarSubtotal(),
            ),
            _CampoNumero(
              controller: _precio,
              etiqueta: 'Precio',
              onChanged: (_) => _actualizarSubtotal(),
            ),
          ],
        ),
        if (subtotal != null) _Subtotal(subtotal),
      ],
    );
    final descripcion = SeccionFormulario(
      titulo: 'Descripción',
      icono: Icons.notes,
      children: [
        TextField(
          controller: _descripcion,
          decoration: const InputDecoration(
            labelText: 'Descripción',
            alignLabelWithHint: true,
          ),
          minLines: 2,
          maxLines: 5,
          maxLength: 500,
          keyboardType: TextInputType.multiline,
          textCapitalization: TextCapitalization.sentences,
        ),
      ],
    );
    return Scaffold(
      appBar: AppBar(title: titulo),
      body: AbsorbPointer(
        absorbing: bloqueado,
        child: CuerpoFormulario(
          anchoMaximo: 1080,
          children: [
            if (bloqueado) const _AvisoSincronizando(),
            AvisoTexto(_aviso ?? controlador.aviso),
            ColumnasAdaptables(
              izquierda: [obra, insumo],
              derecha: [valores, descripcion],
            ),
            const SizedBox(height: 8),
            AccionesFormulario(
              child: FilledButton.icon(
                onPressed: bloqueado ? null : _guardar,
                icon: const Icon(Icons.save_outlined),
                label: const Text('Guardar'),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

/// Desplegable con una opción vacía opcional ([vacio]) y textos que se
/// cortan con puntos suspensivos en lugar de desbordar.
class _Desplegable<T> extends StatelessWidget {
  const _Desplegable({
    required this.etiqueta,
    required this.valor,
    required this.opciones,
    required this.onChanged,
    this.vacio,
  });

  final String etiqueta;
  final T? valor;
  final String? vacio;
  final List<(T, String)> opciones;
  final ValueChanged<T?>? onChanged;

  @override
  Widget build(BuildContext context) {
    return DropdownButtonFormField<T>(
      initialValue: valor,
      isExpanded: true,
      decoration: InputDecoration(labelText: etiqueta),
      items: [
        if (vacio != null)
          DropdownMenuItem<T>(
            value: null,
            child: Text(vacio!, overflow: TextOverflow.ellipsis),
          ),
        for (final (id, descripcion) in opciones)
          DropdownMenuItem<T>(
            value: id,
            child: Text(descripcion, overflow: TextOverflow.ellipsis),
          ),
      ],
      onChanged: onChanged,
    );
  }
}

class _CampoNumero extends StatelessWidget {
  const _CampoNumero({
    required this.controller,
    required this.etiqueta,
    required this.onChanged,
  });

  final TextEditingController controller;
  final String etiqueta;
  final ValueChanged<String> onChanged;

  @override
  Widget build(BuildContext context) {
    return TextField(
      controller: controller,
      decoration: InputDecoration(labelText: etiqueta),
      keyboardType: const TextInputType.numberWithOptions(decimal: true),
      textInputAction: TextInputAction.next,
      textAlign: TextAlign.end,
      onChanged: onChanged,
    );
  }
}

class _ResultadosInsumo extends StatelessWidget {
  const _ResultadosInsumo({
    required this.buscando,
    required this.consulta,
    required this.encontrados,
    required this.onElegir,
  });

  final bool buscando;
  final String consulta;
  final List<InsumoOpcion> encontrados;
  final ValueChanged<InsumoOpcion>? onElegir;

  @override
  Widget build(BuildContext context) {
    final esquema = Theme.of(context).colorScheme;
    final secundario = Theme.of(context).textTheme.bodyMedium
        ?.copyWith(color: esquema.onSurfaceVariant);
    if (buscando) {
      return Padding(
        padding: const EdgeInsets.only(bottom: 8),
        child: Text('Buscando insumos…', style: secundario),
      );
    }
    if (consulta.length >= 2 && encontrados.isEmpty) {
      return Padding(
        padding: const EdgeInsets.only(bottom: 8),
        child: Text(
          'No hay insumos que coincidan con «$consulta».',
          style: secundario,
        ),
      );
    }
    if (encontrados.isEmpty) {
      return const SizedBox.shrink();
    }
    final alto = encontrados.length * 56.0 > 220
        ? 220.0
        : encontrados.length * 56.0;
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: DecoratedBox(
        decoration: BoxDecoration(
          border: Border.all(color: esquema.outlineVariant),
          borderRadius: BorderRadius.circular(12),
        ),
        child: ClipRRect(
          borderRadius: BorderRadius.circular(12),
          child: SizedBox(
            height: alto,
            child: ListView.builder(
              padding: EdgeInsets.zero,
              itemCount: encontrados.length,
              itemBuilder: (context, indice) {
                final insumo = encontrados[indice];
                return ListTile(
                  leading: const Icon(Icons.inventory_2_outlined),
                  title: Text(insumo.etiqueta, overflow: TextOverflow.ellipsis),
                  onTap: onElegir == null ? null : () => onElegir!(insumo),
                );
              },
            ),
          ),
        ),
      ),
    );
  }
}

class _Subtotal extends StatelessWidget {
  const _Subtotal(this.valor);

  final String valor;

  @override
  Widget build(BuildContext context) {
    final tema = Theme.of(context);
    final esquema = tema.colorScheme;
    return Semantics(
      label: 'Subtotal: $valor',
      excludeSemantics: true,
      child: DecoratedBox(
        decoration: BoxDecoration(
          color: esquema.primaryContainer,
          borderRadius: BorderRadius.circular(12),
        ),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
          child: Row(
            children: [
              Text(
                'Subtotal',
                style: tema.textTheme.titleSmall?.copyWith(
                  color: esquema.onPrimaryContainer,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Text(
                  valor,
                  textAlign: TextAlign.end,
                  style: tema.textTheme.titleLarge?.copyWith(
                    color: esquema.onPrimaryContainer,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _AvisoSincronizando extends StatelessWidget {
  const _AvisoSincronizando();

  @override
  Widget build(BuildContext context) {
    final esquema = Theme.of(context).colorScheme;
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: DecoratedBox(
        decoration: BoxDecoration(
          color: esquema.secondaryContainer,
          borderRadius: BorderRadius.circular(12),
        ),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
          child: Row(
            children: [
              Icon(Icons.sync, size: 20, color: esquema.onSecondaryContainer),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  'Sincronizando… No edite hasta terminar.',
                  style: TextStyle(color: esquema.onSecondaryContainer),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
