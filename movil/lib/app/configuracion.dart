/// La URL llega solo con `--dart-define=API_URL=...`.
/// dev, qa y prod se distinguen por ese valor al compilar. No hay URL en el código.
class ConfiguracionApp {
  const ConfiguracionApp({
    required this.urlApi,
    this.versionApp = '1.0.0',
    this.versionEsquema = 1,
    this.maxOperacionesLote = 500,
  });

  factory ConfiguracionApp.desdeEntorno() {
    return const ConfiguracionApp(
      urlApi: String.fromEnvironment('API_URL'),
      versionApp: String.fromEnvironment('APP_VERSION', defaultValue: '1.0.0'),
      versionEsquema: int.fromEnvironment('VERSION_ESQUEMA', defaultValue: 1),
      maxOperacionesLote: int.fromEnvironment(
        'MAX_OPS_LOTE',
        defaultValue: 500,
      ),
    );
  }

  final String urlApi;
  final String versionApp;
  final int versionEsquema;
  final int maxOperacionesLote;
}
