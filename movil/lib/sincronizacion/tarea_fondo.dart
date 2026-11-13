import 'dart:io';

import 'package:path_provider/path_provider.dart';
import 'package:workmanager/workmanager.dart';

const nombreTareaSincronizar = 'arquitectos.sincronizar';

/// Android: cada ~30 min marca que conviene sincronizar al volver a primer plano.
Future<void> registrarSincronizacionPeriodica() async {
  if (!Platform.isAndroid) {
    return;
  }
  await Workmanager().initialize(callbackDispatcher);
  await Workmanager().registerPeriodicTask(
    nombreTareaSincronizar,
    nombreTareaSincronizar,
    frequency: const Duration(minutes: 30),
    existingWorkPolicy: ExistingPeriodicWorkPolicy.keep,
  );
}

@pragma('vm:entry-point')
void callbackDispatcher() {
  Workmanager().executeTask((task, inputData) async {
    try {
      final dir = await getApplicationSupportDirectory();
      final marca = File(
        '${dir.path}${Platform.pathSeparator}sync_pendiente.flag',
      );
      await marca.writeAsString('1', flush: true);
    } catch (_) {
      return false;
    }
    return true;
  });
}

Future<bool> hayMarcaSincronizacionPendiente(
  Future<Directory> Function() directorio,
) async {
  final dir = await directorio();
  final marca = File('${dir.path}${Platform.pathSeparator}sync_pendiente.flag');
  return marca.existsSync();
}

Future<void> consumirMarcaSincronizacion(
  Future<Directory> Function() directorio,
) async {
  final dir = await directorio();
  final marca = File('${dir.path}${Platform.pathSeparator}sync_pendiente.flag');
  if (await marca.exists()) {
    await marca.delete();
  }
}
