import 'package:flutter/services.dart';
import 'package:local_auth/local_auth.dart';

abstract class Desbloqueador {
  Future<bool> desbloquear();

  Future<bool> dispositivoSoportaBloqueo();
}

class DesbloqueadorLocal implements Desbloqueador {
  DesbloqueadorLocal([LocalAuthentication? autenticacion])
    : _autenticacion = autenticacion ?? LocalAuthentication();

  final LocalAuthentication _autenticacion;

  @override
  Future<bool> dispositivoSoportaBloqueo() =>
      _autenticacion.isDeviceSupported();

  @override
  Future<bool> desbloquear() async {
    try {
      return await _autenticacion.authenticate(
        localizedReason:
            'Desbloquee Arquitectos para ver las actas de la obra.',
        options: const AuthenticationOptions(
          biometricOnly: false,
          stickyAuth: true,
        ),
      );
    } on PlatformException {
      return false;
    }
  }
}

class DesbloqueadorFijo implements Desbloqueador {
  DesbloqueadorFijo(this.acepta, {this.soportaBloqueo = true});

  bool acepta;
  final bool soportaBloqueo;

  @override
  Future<bool> dispositivoSoportaBloqueo() async => soportaBloqueo;

  @override
  Future<bool> desbloquear() async => acepta;
}
