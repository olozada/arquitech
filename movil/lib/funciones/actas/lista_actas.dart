import 'package:material_ui/material_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../app/aplicacion.dart';
import '../../app/formulario_adaptable.dart';
import '../../app/tema.dart';
import '../../core/db/consultas.dart';
import '../../core/decimal/dinero.dart';
import 'estado_acta.dart';

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
  var _verFiltros = true;

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
    return _TarjetaActa(
      key: Key('acta-${acta.numero}'),
      acta: acta,
      onTap: () async {
        await context.push('/actas/${acta.numero}');
        // Al volver, el total y «Cambios sin sincronizar» pueden haber cambiado.
        if (mounted) {
          await _cargar();
        }
      },
    );
  }

  Future<void> _elegirFechas() async {
    final rango = await showDialog<(String?, String?)>(
      context: context,
      builder: (context) => _DialogoFechas(desde: _desde, hasta: _hasta),
    );
    if (rango == null || !mounted) {
      return;
    }
    setState(() {
      _desde = rango.$1;
      _hasta = rango.$2;
    });
    await _cargar();
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

  List<Widget> _filtros() {
    final obra = _obras.where((opcion) => opcion.id == _obraId).firstOrNull;
    final fechas = _desde == null && _hasta == null
        ? 'Desde - Hasta'
        : '${_desde == null ? '…' : fechaVisible(_desde!)} - '
              '${_hasta == null ? '…' : fechaVisible(_hasta!)}';
    return [
      Row(
        children: [
          Expanded(
            child: TextField(
              key: const Key('buscar-acta'),
              controller: _texto,
              decoration: const InputDecoration(
                hintText: 'Número o nombre',
                prefixIcon: Icon(Icons.search),
              ),
              textInputAction: TextInputAction.search,
              onSubmitted: (_) => _cargar(),
            ),
          ),
          const SizedBox(width: 8),
          IconButton.outlined(
            tooltip: _verFiltros ? 'Ocultar filtros' : 'Mostrar filtros',
            style: IconButton.styleFrom(
              minimumSize: const Size.square(48),
              backgroundColor: Theme.of(context).colorScheme.surface,
              side: BorderSide(
                color: Theme.of(context).colorScheme.outlineVariant,
              ),
            ),
            onPressed: () => setState(() => _verFiltros = !_verFiltros),
            icon: const Icon(Icons.tune),
          ),
        ],
      ),
      const SizedBox(height: 12),
      _SelectorEstado(
        valor: _estado,
        onChanged: (valor) {
          setState(() => _estado = valor);
          _cargar();
        },
      ),
      if (_verFiltros) ...[
        const SizedBox(height: 12),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: [
            PopupMenuButton<OrdenActas>(
              tooltip: 'Ordenar actas',
              onSelected: (valor) {
                setState(() => _orden = valor);
                _cargar();
              },
              itemBuilder: (context) => [
                for (final (valor, texto) in const [
                  (OrdenActas.fechaInicial, 'Fecha inicial'),
                  (OrdenActas.fechaFinal, 'Fecha final'),
                  (OrdenActas.numero, 'Número'),
                ])
                  CheckedPopupMenuItem(
                    value: valor,
                    checked: _orden == valor,
                    child: Text(texto),
                  ),
              ],
              child: const _PastillaFiltro(
                texto: 'Orden',
                iconoFinal: Icons.expand_more,
              ),
            ),
            PopupMenuButton<int>(
              tooltip: 'Filtrar por obra',
              onSelected: (valor) {
                setState(() => _obraId = valor < 0 ? null : valor);
                _cargar();
              },
              itemBuilder: (context) => [
                CheckedPopupMenuItem(
                  value: -1,
                  checked: _obraId == null,
                  child: const Text('Todas'),
                ),
                for (final opcion in _obras)
                  CheckedPopupMenuItem(
                    value: opcion.id,
                    checked: _obraId == opcion.id,
                    child: Text(
                      opcion.descripcion,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
              ],
              child: _PastillaFiltro(
                texto: obra?.descripcion ?? 'Obra',
                iconoFinal: Icons.expand_more,
                activa: obra != null,
              ),
            ),
            Semantics(
              button: true,
              child: InkWell(
                customBorder: const StadiumBorder(),
                onTap: _elegirFechas,
                child: _PastillaFiltro(
                  texto: fechas,
                  icono: Icons.calendar_today_outlined,
                  activa: _desde != null || _hasta != null,
                ),
              ),
            ),
          ],
        ),
      ],
    ];
  }

  @override
  Widget build(BuildContext context) {
    final controlador = ref.watch(controladorProvider);
    return Scaffold(
      appBar: AppBar(
        title: const Text('Actas'),
        actions: [
          AvatarUsuario(
            nombre: controlador.usuario?.nombre,
            onPressed: () => context.push('/cuenta'),
          ),
        ],
      ),
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
                      children: _filtros(),
                    ),
                  ),
                ),
                const VerticalDivider(width: 1),
                Expanded(
                  child:
                      vacio ??
                      ListView.builder(
                        padding: const EdgeInsets.all(16),
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
                  16,
                  ventana.margen,
                  12,
                ),
                sliver: SliverToBoxAdapter(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: _filtros(),
                  ),
                ),
              ),
              if (vacio != null)
                SliverFillRemaining(hasScrollBody: false, child: vacio)
              else
                SliverPadding(
                  padding: EdgeInsets.fromLTRB(
                    ventana.margen,
                    0,
                    ventana.margen,
                    ventana.margen,
                  ),
                  sliver: SliverList.builder(
                    itemCount: _actas.length,
                    itemBuilder: (context, indice) => _actaTile(_actas[indice]),
                  ),
                ),
            ],
          );
        },
      ),
    );
  }
}

/// Selector Todas / Abiertas / Cerradas: píldora con la opción elegida en
/// amarillo.
class _SelectorEstado extends StatelessWidget {
  const _SelectorEstado({required this.valor, required this.onChanged});

  final EstadoActaFiltro valor;
  final ValueChanged<EstadoActaFiltro> onChanged;

  @override
  Widget build(BuildContext context) {
    final tema = Theme.of(context);
    final esquema = tema.colorScheme;
    const opciones = [
      (EstadoActaFiltro.todas, 'Todas'),
      (EstadoActaFiltro.abiertas, 'Abiertas'),
      (EstadoActaFiltro.cerradas, 'Cerradas'),
    ];
    return DecoratedBox(
      decoration: ShapeDecoration(
        color: esquema.surface,
        shape: StadiumBorder(side: BorderSide(color: esquema.outlineVariant)),
      ),
      child: Padding(
        padding: const EdgeInsets.all(4),
        child: Row(
          children: [
            for (final (opcion, texto) in opciones)
              Expanded(
                child: Semantics(
                  button: true,
                  selected: opcion == valor,
                  child: Material(
                    color: opcion == valor
                        ? esquema.primary
                        : Colors.transparent,
                    shape: const StadiumBorder(),
                    clipBehavior: Clip.antiAlias,
                    child: InkWell(
                      onTap: () => onChanged(opcion),
                      child: ConstrainedBox(
                        constraints: const BoxConstraints(minHeight: 40),
                        child: Center(
                          child: Padding(
                            padding: const EdgeInsets.symmetric(horizontal: 4),
                            child: Text(
                              texto,
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                              style: tema.textTheme.bodyMedium?.copyWith(
                                color: opcion == valor
                                    ? esquema.onPrimary
                                    : esquema.onSurface,
                                fontWeight: opcion == valor
                                    ? FontWeight.w600
                                    : FontWeight.w400,
                              ),
                            ),
                          ),
                        ),
                      ),
                    ),
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }
}

/// Pastilla con borde para los filtros (Orden, Obra, fechas). Solo dibuja; el
/// toque lo maneja quien la contiene.
class _PastillaFiltro extends StatelessWidget {
  const _PastillaFiltro({
    required this.texto,
    this.icono,
    this.iconoFinal,
    this.activa = false,
  });

  final String texto;
  final IconData? icono;
  final IconData? iconoFinal;
  final bool activa;

  @override
  Widget build(BuildContext context) {
    final tema = Theme.of(context);
    final esquema = tema.colorScheme;
    return DecoratedBox(
      decoration: ShapeDecoration(
        color: activa ? esquema.primaryContainer : esquema.surface,
        shape: StadiumBorder(
          side: BorderSide(
            color: activa ? esquema.primary : esquema.outlineVariant,
          ),
        ),
      ),
      child: ConstrainedBox(
        constraints: const BoxConstraints(minHeight: 40, maxWidth: 240),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              if (icono != null) ...[
                Icon(icono, size: 16, color: esquema.onSurface),
                const SizedBox(width: 8),
              ],
              Flexible(
                child: Text(
                  texto,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: tema.textTheme.bodyMedium,
                ),
              ),
              if (iconoFinal != null) ...[
                const SizedBox(width: 8),
                Icon(iconoFinal, size: 18, color: esquema.onSurface),
              ],
            ],
          ),
        ),
      ),
    );
  }
}

class _TarjetaActa extends StatelessWidget {
  const _TarjetaActa({required this.acta, required this.onTap, super.key});

  final ActaResumen acta;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final tema = Theme.of(context);
    final esquema = tema.colorScheme;
    final secundario = tema.textTheme.bodySmall?.copyWith(
      color: esquema.onSurface,
    );
    return Card(
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              CircleAvatar(
                radius: 22,
                backgroundColor: esquema.primary,
                foregroundColor: esquema.onPrimary,
                child: const Icon(Icons.description_outlined, size: 22),
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
                          'Acta ${numeroActaVisible(acta.numero)}',
                          style: tema.textTheme.titleSmall?.copyWith(
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                        EtiquetaEstadoActa(cerrada: acta.cerrada),
                      ],
                    ),
                    const SizedBox(height: 6),
                    Text(
                      'Periodo: ${fechaVisible(acta.fechaInicial)} – '
                      '${fechaVisible(acta.fechaFinal)}',
                      style: secundario,
                    ),
                    Text(
                      'Contratista: ${acta.contratista}',
                      style: secundario,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                    Text(
                      'Total aprobado: ${dinero(acta.totalAprobado)}',
                      style: secundario?.copyWith(fontWeight: FontWeight.w600),
                    ),
                    if (acta.cambiosSinSincronizar) ...[
                      const SizedBox(height: 6),
                      Row(
                        children: [
                          Icon(Icons.sync, size: 14, color: esquema.tertiary),
                          const SizedBox(width: 4),
                          Flexible(
                            child: Text(
                              'Cambios sin sincronizar',
                              style: tema.textTheme.bodySmall?.copyWith(
                                color: esquema.tertiary,
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                          ),
                        ],
                      ),
                    ],
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

/// Diálogo con dos campos aaaa-mm-dd. La app no configura localizaciones, así
/// que no se usa `showDatePicker`. Devuelve (desde, hasta); cada uno es nulo si
/// quedó vacío o con formato inválido.
class _DialogoFechas extends StatefulWidget {
  const _DialogoFechas({required this.desde, required this.hasta});

  final String? desde;
  final String? hasta;

  @override
  State<_DialogoFechas> createState() => _EstadoDialogoFechas();
}

class _EstadoDialogoFechas extends State<_DialogoFechas> {
  late final _desde = TextEditingController(text: widget.desde ?? '');
  late final _hasta = TextEditingController(text: widget.hasta ?? '');

  @override
  void dispose() {
    _desde.dispose();
    _hasta.dispose();
    super.dispose();
  }

  static String? _valida(String texto) {
    final limpio = texto.trim();
    return RegExp(r'^\d{4}-\d{2}-\d{2}$').hasMatch(limpio) ? limpio : null;
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text('Fechas'),
      content: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            CampoEtiquetado(
              etiqueta: 'Desde',
              child: TextField(
                controller: _desde,
                decoration: const InputDecoration(hintText: 'aaaa-mm-dd'),
                keyboardType: TextInputType.datetime,
                textInputAction: TextInputAction.next,
              ),
            ),
            const SizedBox(height: 12),
            CampoEtiquetado(
              etiqueta: 'Hasta',
              child: TextField(
                controller: _hasta,
                decoration: const InputDecoration(hintText: 'aaaa-mm-dd'),
                keyboardType: TextInputType.datetime,
                textInputAction: TextInputAction.done,
              ),
            ),
          ],
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(context, (null, null)),
          child: const Text('Limpiar'),
        ),
        TextButton(
          onPressed: () => Navigator.pop(context, (
            _valida(_desde.text),
            _valida(_hasta.text),
          )),
          child: const Text('Aplicar'),
        ),
      ],
    );
  }
}
