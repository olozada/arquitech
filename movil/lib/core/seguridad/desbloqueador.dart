import 'package:flutter/foundation.dart';
import 'package:flutter/services.dart';
import 'package:local_auth/local_auth.dart';

/// Canal nativo: `KeyguardManager.isDeviceSecure` (Android) o
/// `LAContext.deviceOwnerAuthentication` (iOS).
const canalSeguridadDispositivo = MethodChannel('arquitectos/seguridad');

abstract class Desbloqueador {
  Future<bool> desbloquear();

  Future<bool> dispositivoSoportaBloqueo();
}

class DesbloqueadorLocal implements Desbloqueador {
  DesbloqueadorLocal([
    LocalAuthentication? autenticacion,
    MethodChannel? canalSeguridad,
  ]) : _autenticacion = autenticacion ?? LocalAuthentication(),
       _canalSeguridad = canalSeguridad ?? canalSeguridadDispositivo;

  final LocalAuthentication _autenticacion;
  final MethodChannel _canalSeguridad;

  @override
  Future<bool> dispositivoSoportaBloqueo() async {
    if (await _emuladorEnDesarrollo()) {
      return true;
    }
    if (defaultTargetPlatform == TargetPlatform.android ||
        defaultTargetPlatform == TargetPlatform.iOS) {
      try {
        final seguro = await _canalSeguridad.invokeMethod<bool>(
          'dispositivoSeguro',
        );
        return seguro ?? false;
      } on PlatformException {
        return false;
      } on MissingPluginException {
        return false;
      }
    }
    return _autenticacion.isDeviceSupported();
  }

  @override
  Future<bool> desbloquear() async {
    if (await _emuladorEnDesarrollo()) {
      return true;
    }
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

  /// En debug y profile el emulador no tiene PIN. Release sigue exigiéndolo.
  Future<bool> _emuladorEnDesarrollo() async {
    if (kReleaseMode || defaultTargetPlatform != TargetPlatform.android) {
      return false;
    }
    try {
      final emulador = await _canalSeguridad.invokeMethod<bool>('esEmulador');
      return emulador ?? false;
    } on PlatformException {
      return false;
    } on MissingPluginException {
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
