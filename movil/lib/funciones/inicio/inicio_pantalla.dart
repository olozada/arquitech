import 'package:material_ui/material_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../app/aplicacion.dart';
import '../../app/controlador.dart';
import '../../app/formulario_adaptable.dart';
import '../../app/tema.dart';
import '../../core/decimal/dinero.dart';
import '../cuenta/cerrar_sesion.dart';

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
    final tema = Theme.of(context);
    return Scaffold(
      appBar: AppBar(
        title: const Text('Inicio'),
        actions: [
          AvatarUsuario(
            nombre: controlador.usuario?.nombre,
            onPressed: () => context.push('/cuenta'),
          ),
        ],
      ),
      body: CuerpoFormulario(
        anchoMaximo: 560,
        children: [
          AvisoTexto(controlador.aviso),
          _TarjetaEstado(controlador: controlador),
          const SizedBox(height: 24),
          Padding(
            padding: const EdgeInsetsDirectional.only(start: 8, bottom: 8),
            child: Semantics(
              header: true,
              child: Text(
                'Módulos de gestión',
                style: tema.textTheme.titleSmall?.copyWith(
                  fontWeight: FontWeight.w600,
                ),
              ),
            ),
          ),
          _Modulo(
            key: const Key('ir-pendientes'),
            icono: Icons.checklist_rtl,
            titulo: 'Pendientes',
            subtitulo: 'Cambios en cola y rechazados',
            onTap: () => context.push('/pendientes'),
          ),
          _Modulo(
            key: const Key('ir-actas'),
            icono: Icons.edit_document,
            titulo: 'Actas',
            subtitulo: 'Actas de obra y sus líneas',
            onTap: () => context.push('/actas'),
          ),
          if (controlador.usuario?.esArquitecto ?? false)
            _Modulo(
              key: const Key('ir-presupuestos'),
              icono: Icons.request_quote_outlined,
              titulo: 'Presupuestos',
              subtitulo: 'Costos de obra y partidas',
              insignia: 'SOLO ARQ.',
              destacado: true,
              onTap: () => context.push('/presupuestos'),
            ),
          _Modulo(
            key: const Key('ir-cuenta'),
            icono: Icons.person_outline,
            titulo: 'Cuenta',
            subtitulo: 'Perfil y dispositivo',
            onTap: () => context.push('/cuenta'),
          ),
          const SizedBox(height: 32),
          const Divider(),
          const SizedBox(height: 24),
          OutlinedButton(
            key: const Key('cerrar-sesion-inicio'),
            onPressed: controlador.ocupado
                ? null
                : () => confirmarCerrarSesion(context, controlador),
            child: const Text('Cerrar sesión'),
          ),
        ],
      ),
    );
  }
}

class _TarjetaEstado extends StatelessWidget {
  const _TarjetaEstado({required this.controlador});

  final ControladorAplicacion controlador;

  @override
  Widget build(BuildContext context) {
    final tema = Theme.of(context);
    final esquema = tema.colorScheme;
    final marca = ColoresMarca.de(context);
    final ultima = controlador.ultimaSincronizacion;
    final progreso = controlador.progresoDescargaPaquete;
    final detalle = controlador.ocupado
        ? 'Sincronizando…'
        : ultima == null
        ? 'Sin sincronizar'
        : 'Última sincronización: ${fechaVisible(ultima)}';
    return Card(
      margin: EdgeInsets.zero,
      child: Padding(
        padding: const EdgeInsets.fromLTRB(20, 16, 16, 16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              children: [
                ExcludeSemantics(
                  child: Container(
                    width: 12,
                    height: 12,
                    decoration: BoxDecoration(
                      shape: BoxShape.circle,
                      color: controlador.enLinea
                          ? marca.enLinea
                          : esquema.onSurfaceVariant,
                    ),
                  ),
                ),
                const SizedBox(width: 16),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        controlador.enLinea ? 'En línea' : 'Sin conexión',
                        style: tema.textTheme.titleMedium?.copyWith(
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      Text(
                        detalle,
                        style: tema.textTheme.bodySmall?.copyWith(
                          color: esquema.onSurfaceVariant,
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(width: 8),
                IconButton.filled(
                  key: const Key('sincronizar'),
                  tooltip: controlador.ocupado
                      ? 'Sincronizando…'
                      : 'Sincronizar',
                  iconSize: 26,
                  style: IconButton.styleFrom(
                    minimumSize: const Size.square(52),
                  ),
                  onPressed: controlador.ocupado
                      ? null
                      : controlador.sincronizar,
                  icon: const Icon(Icons.sync),
                ),
              ],
            ),
            if (progreso != null) ...[
              const SizedBox(height: 12),
              LinearProgressIndicator(value: progreso),
              const SizedBox(height: 4),
              Text(
                'Descargando paquete… ${(progreso * 100).round()} %',
                style: tema.textTheme.bodySmall,
              ),
            ],
            const SizedBox(height: 16),
            _Contadores(
              pendientes: controlador.pendientes,
              fallidas: controlador.fallidas,
            ),
          ],
        ),
      ),
    );
  }
}

/// Pastilla con los conteos de operaciones pendientes y fallidas.
class _Contadores extends StatelessWidget {
  const _Contadores({required this.pendientes, required this.fallidas});

  final int pendientes;
  final int fallidas;

  @override
  Widget build(BuildContext context) {
    final esquema = Theme.of(context).colorScheme;
    return DecoratedBox(
      decoration: ShapeDecoration(
        shape: StadiumBorder(side: BorderSide(color: esquema.outlineVariant)),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 8),
        child: IntrinsicHeight(
          child: Row(
            children: [
              Expanded(
                child: _Conteo(
                  etiqueta: 'Pendiente',
                  valor: pendientes,
                  subrayado: esquema.primary,
                ),
              ),
              VerticalDivider(
                width: 32,
                thickness: 1.5,
                color: esquema.onSurfaceVariant,
              ),
              Expanded(
                child: _Conteo(
                  etiqueta: 'Fallidas',
                  valor: fallidas,
                  subrayado: esquema.error,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _Conteo extends StatelessWidget {
  const _Conteo({
    required this.etiqueta,
    required this.valor,
    required this.subrayado,
  });

  final String etiqueta;
  final int valor;
  final Color subrayado;

  @override
  Widget build(BuildContext context) {
    final tema = Theme.of(context);
    return MergeSemantics(
      child: Row(
        children: [
          Expanded(
            child: Text(
              etiqueta,
              style: tema.textTheme.bodyMedium,
              overflow: TextOverflow.ellipsis,
            ),
          ),
          const SizedBox(width: 8),
          Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                valor.toString().padLeft(2, '0'),
                style: tema.textTheme.titleMedium?.copyWith(
                  fontWeight: FontWeight.w700,
                ),
              ),
              ExcludeSemantics(
                child: Container(
                  width: 20,
                  height: 3,
                  decoration: BoxDecoration(
                    color: subrayado,
                    borderRadius: BorderRadius.circular(2),
                  ),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _Modulo extends StatelessWidget {
  const _Modulo({
    required this.icono,
    required this.titulo,
    required this.subtitulo,
    required this.onTap,
    this.insignia,
    this.destacado = false,
    super.key,
  });

  final IconData icono;
  final String titulo;
  final String subtitulo;
  final VoidCallback onTap;
  final String? insignia;
  final bool destacado;

  @override
  Widget build(BuildContext context) {
    final tema = Theme.of(context);
    final esquema = tema.colorScheme;
    final marca = ColoresMarca.de(context);
    return Card(
      color: destacado ? esquema.primaryContainer : null,
      shape: destacado
          ? RoundedRectangleBorder(
              borderRadius: radioTarjeta,
              side: BorderSide(color: esquema.primary.withValues(alpha: 0.5)),
            )
          : null,
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
          child: Row(
            children: [
              CircleAvatar(
                radius: 22,
                backgroundColor: destacado ? esquema.primary : marca.relleno,
                foregroundColor: destacado
                    ? esquema.onPrimary
                    : esquema.onSurface,
                child: Icon(icono, size: 22),
              ),
              const SizedBox(width: 16),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Wrap(
                      spacing: 8,
                      runSpacing: 4,
                      crossAxisAlignment: WrapCrossAlignment.center,
                      children: [
                        Text(
                          titulo,
                          style: tema.textTheme.titleMedium?.copyWith(
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                        if (insignia != null)
                          EtiquetaEstado(
                            texto: insignia!,
                            fondo: esquema.primary,
                            frente: esquema.onPrimary,
                          ),
                      ],
                    ),
                    const SizedBox(height: 2),
                    Text(
                      subtitulo,
                      style: tema.textTheme.bodyMedium?.copyWith(
                        color: esquema.onSurfaceVariant,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
