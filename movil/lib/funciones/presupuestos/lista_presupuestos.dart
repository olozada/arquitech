import 'package:decimal/decimal.dart';
import 'package:material_ui/material_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../app/aplicacion.dart';
import '../../core/db/consultas.dart';
import '../../core/decimal/dinero.dart';

class ListaPresupuestosPantalla extends ConsumerStatefulWidget {
  const ListaPresupuestosPantalla({super.key});

  @override
  ConsumerState<ListaPresupuestosPantalla> createState() =>
      _EstadoPresupuestos();
}

class _EstadoPresupuestos extends ConsumerState<ListaPresupuestosPantalla> {
  var _cargando = true;
  List<PresupuestoResumen> _filas = const [];

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) async {
      final filas = await ref.read(controladorProvider).listarPresupuestos();
      if (!mounted) {
        return;
      }
      setState(() {
        _filas = filas;
        _cargando = false;
      });
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Presupuestos')),
      body: _cargando
          ? const Center(child: Text('Cargando presupuestos…'))
          : _filas.isEmpty
          ? const Center(child: Text('No hay presupuestos en el paquete.'))
          : ListView.builder(
              itemCount: _filas.length,
              itemBuilder: (context, indice) {
                final fila = _filas[indice];
                final total = fila.total == null
                    ? ''
                    : dinero(Decimal.parse(fila.total!));
                return ListTile(
                  minVerticalPadding: 12,
                  title: Text(fila.nombre),
                  subtitle: Text('${fila.construccion} · ${fila.estado}'),
                  trailing: Text(total),
                  onTap: () => context.push('/presupuestos/${fila.id}'),
                );
              },
            ),
    );
  }
}
