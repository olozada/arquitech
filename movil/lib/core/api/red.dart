import 'package:connectivity_plus/connectivity_plus.dart';

abstract class Red {
  Future<bool> hayConexion();

  Stream<bool> get cambios;
}

class RedConectividad implements Red {
  RedConectividad([Connectivity? conectividad])
    : _conectividad = conectividad ?? Connectivity();

  final Connectivity _conectividad;

  @override
  Future<bool> hayConexion() async {
    final resultados = await _conectividad.checkConnectivity();
    return _enLinea(resultados);
  }

  @override
  Stream<bool> get cambios => _conectividad.onConnectivityChanged.map(_enLinea);

  bool _enLinea(List<ConnectivityResult> resultados) {
    return resultados.any((resultado) => resultado != ConnectivityResult.none);
  }
}

class RedFija implements Red {
  RedFija(this.valor);

  bool valor;

  @override
  Future<bool> hayConexion() async => valor;

  @override
  Stream<bool> get cambios => const Stream.empty();
}
