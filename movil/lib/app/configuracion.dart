import 'package:flutter/foundation.dart';

/// La URL de qa y prod llega con `--dart-define=API_URL=...`.
/// En debug y profile, si no llega, se usa el servidor de la red local.
bool urlApiPermitidaEnRelease(ConfiguracionApp configuracion) {
  if (!configuracion.esRelease || configuracion.urlApi.isEmpty) {
    return true;
  }
  return configuracion.urlApi.toLowerCase().startsWith('https://');
}

class ConfiguracionApp {
  static const urlApiDesarrollo = 'http://10.200.220.99:5190';

  const ConfiguracionApp({
    required this.urlApi,
    required this.versionApp,
    this.buildApp = '',
    this.versionEsquema = 1,
    this.maxOperacionesLote = 500,
    this.esRelease = false,
  });

  String get versionAppRegistro =>
      buildApp.isEmpty ? versionApp : '$versionApp+$buildApp';

  factory ConfiguracionApp.desdeEntorno({
    required String versionApp,
    required String buildApp,
  }) {
    const urlEntorno = String.fromEnvironment('API_URL');
    final urlApi = urlEntorno.isNotEmpty
        ? urlEntorno
        : (kReleaseMode ? '' : urlApiDesarrollo);
    return ConfiguracionApp(
      urlApi: urlApi,
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
      esRelease: kReleaseMode,
    );
  }

  final String urlApi;
  final String versionApp;
  final String buildApp;
  final int versionEsquema;
  final int maxOperacionesLote;
  final bool esRelease;
}
