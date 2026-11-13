import 'dart:async';

import 'package:material_ui/material_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import 'controlador.dart';
import 'dependencias.dart';
import 'enrutador.dart';
import 'tema.dart';
import 'velo_privacidad.dart';

final controladorProvider = ChangeNotifierProvider<ControladorAplicacion>(
  (ref) => throw StateError('El controlador se crea con la aplicación.'),
);

class Aplicacion extends StatefulWidget {
  const Aplicacion({required this.dependencias, this.controlador, super.key});

  final Dependencias dependencias;
  final ControladorAplicacion? controlador;

  @override
  State<Aplicacion> createState() => _EstadoAplicacion();
}

class _EstadoAplicacion extends State<Aplicacion> with WidgetsBindingObserver {
  late final ControladorAplicacion controlador;
  late final GoRouter enrutador;
  var _veloPrivacidad = false;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    controlador =
        widget.controlador ?? ControladorAplicacion(widget.dependencias);
    enrutador = crearEnrutador(controlador);
    if (widget.controlador == null) {
      unawaited(controlador.iniciar());
    }
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    enrutador.dispose();
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState estado) {
    if (estado == AppLifecycleState.inactive ||
        estado == AppLifecycleState.paused ||
        estado == AppLifecycleState.hidden) {
      if (!_veloPrivacidad) {
        setState(() => _veloPrivacidad = true);
      }
      controlador.alFondo();
    }
    if (estado == AppLifecycleState.resumed) {
      if (_veloPrivacidad) {
        setState(() => _veloPrivacidad = false);
      }
      controlador.alFrente();
    }
  }

  @override
  Widget build(BuildContext context) {
    return ProviderScope(
      overrides: [controladorProvider.overrideWith((ref) => controlador)],
      child: Stack(
        children: [
          MaterialApp.router(
            title: 'Arquitectos',
            debugShowCheckedModeBanner: false,
            theme: temaArquitectos(),
            routerConfig: enrutador,
          ),
          if (_veloPrivacidad)
            Positioned.fill(
              child: Theme(
                data: temaArquitectos(),
                child: const VeloPrivacidad(),
              ),
            ),
        ],
      ),
    );
  }
}
