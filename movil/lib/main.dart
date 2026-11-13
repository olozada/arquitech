import 'dart:io';

import 'package:flutter/widgets.dart';

import 'app/aplicacion.dart';
import 'app/dependencias.dart';
import 'sincronizacion/tarea_fondo.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  if (Platform.isAndroid) {
    await registrarSincronizacionPeriodica();
  }
  final dependencias = await Dependencias.crear();
  runApp(Aplicacion(dependencias: dependencias));
}
