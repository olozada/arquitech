import 'dart:io';

import 'package:flutter/foundation.dart';
import 'package:flutter/services.dart';
import 'package:flutter/widgets.dart';

import 'app/aplicacion.dart';
import 'app/dependencias.dart';
import 'sincronizacion/tarea_fondo.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  // La OFL de Poppins exige que la licencia acompañe a la fuente.
  LicenseRegistry.addLicense(() async* {
    final texto = await rootBundle.loadString('assets/fuentes/OFL.txt');
    yield LicenseEntryWithLineBreaks(['Poppins'], texto);
  });
  if (Platform.isAndroid) {
    await registrarSincronizacionPeriodica();
  }
  final dependencias = await Dependencias.crear();
  runApp(Aplicacion(dependencias: dependencias));
}
