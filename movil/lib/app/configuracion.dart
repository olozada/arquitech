import 'package:flutter/foundation.dart';

/// La URL llega solo con `--dart-define=API_URL=...`.
/// dev, qa y prod se distinguen por ese valor al compilar. No hay URL en el código.
@visibleForTesting
var simularReleaseEnPruebas = false;

bool urlApiPermitidaEnRelease(String urlApi, {bool? release}) {
  final enRelease = release ?? simularReleaseEnPruebas || kReleaseMode;
  if (!enRelease || urlApi.isEmpty) {
    return true;
  }
  return urlApi.toLowerCase().startsWith('https://');
}

class ConfiguracionApp {
  const ConfiguracionApp({
    required this.urlApi,
    required this.versionApp,
    this.buildApp = '',
    this.versionEsquema = 1,
    this.maxOperacionesLote = 500,
  });

  String get versionAppRegistro =>
      buildApp.isEmpty ? versionApp : '$versionApp+$buildApp';

  factory ConfiguracionApp.desdeEntorno({
    required String versionApp,
    required String buildApp,
  }) {
    return ConfiguracionApp(
      urlApi: const String.fromEnvironment('API_URL'),
      versionApp: versionApp,
      buildApp: buildApp,
      versionEsquema: const int.fromEnvironment(
        'VERSION_ESQUEMA',
        defaultValue: 1,
      ),
      maxOperacionesLote: const int.fromEnvironment(
        'MAX_OPS_LOTE',
        defaultValue: 500,
      ),
    );
  }

  final String urlApi;
  final String versionApp;
  final String buildApp;
  final int versionEsquema;
  final int maxOperacionesLote;
}
