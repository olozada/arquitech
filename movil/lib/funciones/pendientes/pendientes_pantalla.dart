import 'dart:convert';

import 'package:material_ui/material_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../app/aplicacion.dart';
import '../../core/db/consultas.dart';
import '../../core/db/pendientes_db.dart';
import '../../sincronizacion/modelos.dart';
import 'comparacion_fallida.dart';

class PendientesPantalla extends ConsumerStatefulWidget {
  const PendientesPantalla({super.key});

  @override
  ConsumerState<PendientesPantalla> createState() => _EstadoPendientes();
}

class _EstadoPendientes extends ConsumerState<PendientesPantalla> {
  List<OperacionFallidaRegistro> _fallidas = const [];
  List<ResumenCola> _cola = const [];
  var _cargando = true;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _cargar());
  }

  Future<void> _cargar() async {
    final controlador = ref.read(controladorProvider);
    final fallidas = await controlador.listarFallidas();
    final cola = await controlador.listarCola();
    if (!mounted) {
      return;
    }
    setState(() {
      _fallidas = fallidas;
      _cola = cola;
      _cargando = false;
    });
  }

  LineaSincronizacion? _lineaDesde(OperacionFallidaRegistro registro) {
    try {
      final mapa = jsonDecode(registro.payloadJson) as Map<String, Object?>;
      final linea = mapa['linea'];
      if (linea is Map) {
        return LineaSincronizacion.desdeJson(Map<String, Object?>.from(linea));
      }
    } catch (_) {
      return null;
    }
    return null;
  }

  Future<void> _rehacer(OperacionFallidaRegistro registro) async {
    final linea = _lineaDesde(registro);
    if (linea == null) {
      return;
    }
    final controlador = ref.read(controladorProvider);
    await controlador.descartarFallida(registro.idOperacion);
    if (!mounted) {
      return;
    }
    final localId = registro.localId;
    if (registro.tipo == 'AgregarLinea' || localId == null) {
      await context.push(
        '/actas/${registro.numeroActa}/linea/nueva',
        extra: linea,
      );
    } else {
      await context.push(
        '/actas/${registro.numeroActa}/linea/$localId',
        extra: linea,
      );
    }
    await _cargar();
  }

  @override
  Widget build(BuildContext context) {
    final controlador = ref.watch(controladorProvider);
    return Scaffold(
      appBar: AppBar(title: const Text('Pendientes')),
      body: _cargando
          ? const Center(child: Text('Cargando…'))
          : ListView(
              padding: const EdgeInsets.all(16),
              children: [
                Text('En cola: ${controlador.pendientes}'),
                const SizedBox(height: 8),
                Text('Fallidas: ${controlador.fallidas}'),
                const SizedBox(height: 16),
                if (_cola.isNotEmpty) ...[
                  Text(
                    'Por sincronizar',
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                  const SizedBox(height: 8),
                  for (final item in _cola)
                    Card(
                      child: ListTile(
                        title: Text('Acta ${item.numeroActa} · ${item.tipo}'),
                        subtitle: Text(
                          'Secuencia ${item.secuencia}'
                          '${item.localId != null ? ' · ${item.localId}' : ''}',
                        ),
                      ),
                    ),
                  const SizedBox(height: 16),
                ],
                if (_fallidas.isEmpty)
                  const Text('No hay operaciones fallidas.')
                else ...[
                  Text(
                    'Rechazadas o en conflicto',
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                  const SizedBox(height: 8),
                  for (final registro in _fallidas) ...[
                    Card(
                      child: Padding(
                        padding: const EdgeInsets.all(12),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              'Acta ${registro.numeroActa} · ${registro.tipo} · ${registro.estado}',
                              style: Theme.of(context).textTheme.titleSmall,
                            ),
                            const SizedBox(height: 4),
                            Text(registro.mensaje),
                            if (registro.localId != null)
                              Text('Línea: ${registro.localId}'),
                            const SizedBox(height: 8),
                            ComparacionFallidaVista(
                              datos: ComparacionFallida.desde(registro),
                            ),
                            const SizedBox(height: 8),
                            Row(
                              children: [
                                TextButton(
                                  onPressed: () async {
                                    await controlador.descartarFallida(
                                      registro.idOperacion,
                                    );
                                    await _cargar();
                                  },
                                  child: const Text('Descartar'),
                                ),
                                const SizedBox(width: 8),
                                if (_lineaDesde(registro) != null)
                                  FilledButton(
                                    onPressed: () => _rehacer(registro),
                                    child: const Text('Rehacer'),
                                  ),
                              ],
                            ),
                          ],
                        ),
                      ),
                    ),
                  ],
                ],
              ],
            ),
    );
  }
}
