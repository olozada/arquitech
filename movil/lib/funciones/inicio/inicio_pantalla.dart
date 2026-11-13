import 'package:material_ui/material_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../app/aplicacion.dart';
import '../../app/tema.dart';
import '../../core/decimal/dinero.dart';

class InicioPantalla extends ConsumerStatefulWidget {
  const InicioPantalla({super.key});

  @override
  ConsumerState<InicioPantalla> createState() => _EstadoInicio();
}

class _EstadoInicio extends ConsumerState<InicioPantalla> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      ref.read(controladorProvider).refrescarConteos();
    });
  }

  @override
  Widget build(BuildContext context) {
    final controlador = ref.watch(controladorProvider);
    final ultima = controlador.ultimaSincronizacion;
    return Scaffold(
      appBar: AppBar(title: const Text('Inicio')),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(24),
          children: [
            Text(controlador.enLinea ? 'En línea' : 'Sin conexión'),
            const SizedBox(height: 8),
            Text(
              ultima == null
                  ? 'Sin sincronizar'
                  : 'Última sincronización: ${fechaVisible(ultima)}',
            ),
            const SizedBox(height: 8),
            Text('Pendientes: ${controlador.pendientes}'),
            Text('Fallidas: ${controlador.fallidas}'),
            const SizedBox(height: 16),
            AvisoTexto(controlador.aviso),
            if (controlador.progresoDescargaPaquete != null) ...[
              const SizedBox(height: 8),
              LinearProgressIndicator(
                value: controlador.progresoDescargaPaquete,
              ),
              const SizedBox(height: 8),
              Text(
                'Descargando paquete… ${((controlador.progresoDescargaPaquete ?? 0) * 100).round()} %',
              ),
            ],
            FilledButton(
              key: const Key('sincronizar'),
              onPressed: controlador.ocupado ? null : controlador.sincronizar,
              child: Text(
                controlador.ocupado ? 'Sincronizando…' : 'Sincronizar',
              ),
            ),
            const SizedBox(height: 12),
            OutlinedButton(
              onPressed: () => context.push('/pendientes'),
              child: const Text('Pendientes'),
            ),
            const SizedBox(height: 12),
            OutlinedButton(
              key: const Key('ir-actas'),
              onPressed: () => context.push('/actas'),
              child: const Text('Actas'),
            ),
            if (controlador.usuario?.esArquitecto ?? false) ...[
              const SizedBox(height: 12),
              OutlinedButton(
                key: const Key('ir-presupuestos'),
                onPressed: () => context.push('/presupuestos'),
                child: const Text('Presupuestos'),
              ),
            ],
            const SizedBox(height: 12),
            OutlinedButton(
              onPressed: () => context.push('/cuenta'),
              child: const Text('Cuenta'),
            ),
          ],
        ),
      ),
    );
  }
}
