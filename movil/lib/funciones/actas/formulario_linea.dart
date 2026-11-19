import 'dart:async';

import 'package:decimal/decimal.dart';
import 'package:material_ui/material_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../app/aplicacion.dart';
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

  @override
  Widget build(BuildContext context) {
    final controlador = ref.watch(controladorProvider);
    final bloqueado = controlador.edicionBloqueada;
    if (_cargando) {
      return Scaffold(
        appBar: AppBar(title: Text(_edicion ? 'Editar línea' : 'Nueva línea')),
        body: const Center(child: Text('Cargando catálogos…')),
      );
    }
    return Scaffold(
      appBar: AppBar(title: Text(_edicion ? 'Editar línea' : 'Nueva línea')),
      body: AbsorbPointer(
        absorbing: bloqueado,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            if (bloqueado) ...[
              const Text('Sincronizando… No edite hasta terminar.'),
              const SizedBox(height: 12),
            ],
            AvisoTexto(_aviso ?? controlador.aviso),
            DropdownButtonFormField<int>(
              initialValue: _construccionId,
              isExpanded: true,
              decoration: const InputDecoration(labelText: 'Construcción'),
              items: _construcciones
                  .map(
                    (opcion) => DropdownMenuItem(
                      value: opcion.id,
                      child: Text(
                        opcion.descripcion,
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                  )
                  .toList(),
              onChanged: bloqueado ? null : _cambiarConstruccion,
            ),
            const SizedBox(height: 12),
            if (_items.isEmpty)
              DropdownButtonFormField<int>(
                initialValue: _conceptoId,
                isExpanded: true,
                decoration: const InputDecoration(labelText: 'Concepto'),
                items: _conceptos
                    .map(
                      (opcion) => DropdownMenuItem(
                        value: opcion.id,
                        child: Text(
                          opcion.descripcion,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                    )
                    .toList(),
                onChanged: (valor) => setState(() => _conceptoId = valor),
              )
            else ...[
              DropdownButtonFormField<int>(
                initialValue: _presupuestoItemId,
                isExpanded: true,
                decoration: const InputDecoration(
                  labelText: 'Ítem de presupuesto (opcional)',
                ),
                items: [
                  const DropdownMenuItem(
                    value: null,
                    child: Text('Sin ítem de presupuesto'),
                  ),
                  ..._items.map(
                    (opcion) => DropdownMenuItem(
                      value: opcion.id,
                      child: Text(
                        opcion.descripcion,
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                  ),
                ],
                onChanged: (valor) async {
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
                },
              ),
              if (_presupuestoItemId == null) ...[
                const SizedBox(height: 12),
                DropdownButtonFormField<int>(
                  initialValue: _conceptoId,
                  isExpanded: true,
                  decoration: const InputDecoration(labelText: 'Concepto'),
                  items: _conceptos
                      .map(
                        (opcion) => DropdownMenuItem(
                          value: opcion.id,
                          child: Text(
                            opcion.descripcion,
                            overflow: TextOverflow.ellipsis,
                          ),
                        ),
                      )
                      .toList(),
                  onChanged: (valor) => setState(() => _conceptoId = valor),
                ),
              ],
            ],
            const SizedBox(height: 12),
            if (_unidades.isNotEmpty)
              DropdownButtonFormField<int>(
                initialValue: _unidadMedidaId,
                isExpanded: true,
                decoration: const InputDecoration(
                  labelText: 'Unidad de medida',
                ),
                items: [
                  const DropdownMenuItem(
                    value: null,
                    child: Text('Sin unidad'),
                  ),
                  ..._unidades.map(
                    (opcion) => DropdownMenuItem(
                      value: opcion.id,
                      child: Text(
                        opcion.descripcion,
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                  ),
                ],
                onChanged: bloqueado
                    ? null
                    : (valor) => setState(() => _unidadMedidaId = valor),
              ),
            const SizedBox(height: 12),
            Column(
              key: _bloqueInsumo,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                if (_buscandoInsumo)
                  const Padding(
                    padding: EdgeInsets.only(bottom: 8),
                    child: Text('Buscando insumos…'),
                  )
                else if (_consultaInsumo.length >= 2 &&
                    _insumosEncontrados.isEmpty)
                  Padding(
                    padding: const EdgeInsets.only(bottom: 8),
                    child: Text(
                      'No hay insumos que coincidan con «$_consultaInsumo».',
                    ),
                  )
                else if (_insumosEncontrados.isNotEmpty)
                  SizedBox(
                    height: _insumosEncontrados.length * 56.0 > 220
                        ? 220
                        : _insumosEncontrados.length * 56.0,
                    child: ListView(
                      children: [
                        for (final insumo in _insumosEncontrados)
                          ListTile(
                            title: Text(
                              insumo.etiqueta,
                              overflow: TextOverflow.ellipsis,
                            ),
                            onTap: bloqueado
                                ? null
                                : () {
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
                                  },
                          ),
                      ],
                    ),
                  ),
                TextField(
                  controller: _insumoBusqueda,
                  decoration: const InputDecoration(
                    labelText: 'Buscar insumo (opcional)',
                    helperText: 'Escriba al menos 2 letras',
                  ),
                  textInputAction: TextInputAction.search,
                  onChanged: bloqueado ? null : _buscarInsumo,
                ),
                if (_insumoId != null)
                  ListTile(
                    title: Text('Insumo seleccionado (#$_insumoId)'),
                    trailing: IconButton(
                      icon: const Icon(Icons.clear),
                      onPressed: bloqueado
                          ? null
                          : () => setState(() {
                              _relojInsumo?.cancel();
                              _insumoId = null;
                              _insumoBusqueda.clear();
                              _insumosEncontrados = const [];
                              _buscandoInsumo = false;
                              _consultaInsumo = '';
                            }),
                    ),
                  ),
              ],
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _descripcion,
              decoration: const InputDecoration(labelText: 'Descripción'),
              maxLength: 500,
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _cantidad,
              decoration: const InputDecoration(labelText: 'Cantidad'),
              keyboardType: const TextInputType.numberWithOptions(
                decimal: true,
              ),
              onChanged: (_) => _actualizarSubtotal(),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _precio,
              decoration: const InputDecoration(labelText: 'Precio'),
              keyboardType: const TextInputType.numberWithOptions(
                decimal: true,
              ),
              onChanged: (_) => _actualizarSubtotal(),
            ),
            if (_subtotalVisible != null) ...[
              const SizedBox(height: 12),
              Text(
                'Subtotal: ${_subtotalVisible!}',
                style: Theme.of(context).textTheme.titleMedium,
              ),
            ],
            const SizedBox(height: 16),
            FilledButton(
              onPressed: bloqueado ? null : _guardar,
              child: const Text('Guardar'),
            ),
          ],
        ),
      ),
    );
  }
}
