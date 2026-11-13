import '../../sincronizacion/modelos.dart';

class UsuarioMovil {
  const UsuarioMovil({
    required this.id,
    required this.nombre,
    required this.tipoUsuarioId,
    this.arquitectoId,
    this.contratistaId,
    this.revisorId,
  });

  final int id;
  final String nombre;
  final int tipoUsuarioId;
  final int? arquitectoId;
  final int? contratistaId;
  final int? revisorId;

  bool get esArquitecto => tipoUsuarioId == 2;
}

class Credenciales {
  const Credenciales({
    required this.accessToken,
    required this.expiraEn,
    required this.refreshToken,
    required this.usuario,
    required this.debeCambiarContrasena,
  });

  final String accessToken;
  final DateTime expiraEn;
  final String refreshToken;
  final UsuarioMovil usuario;
  final bool debeCambiarContrasena;
}

class ParTokens {
  const ParTokens({
    required this.accessToken,
    required this.expiraEn,
    required this.refreshToken,
  });

  final String accessToken;
  final DateTime expiraEn;
  final String refreshToken;
}

class PaqueteDescargado {
  const PaqueteDescargado({
    required this.gzip,
    required this.sha256,
    required this.esquema,
    required this.generadoEn,
  });

  final List<int> gzip;
  final String sha256;
  final int esquema;
  final String generadoEn;
}

class DispositivoEnvio {
  const DispositivoEnvio({
    required this.id,
    required this.nombre,
    required this.plataforma,
    required this.versionApp,
  });

  final String id;
  final String nombre;
  final String plataforma;
  final String versionApp;
}

abstract class ClienteMovil {
  Future<Credenciales> iniciar({
    required String usuario,
    required String contrasena,
    required DispositivoEnvio dispositivo,
  });

  Future<ParTokens> renovar({
    required String refreshToken,
    required String dispositivoId,
  });

  Future<void> cambiarContrasena({
    required String token,
    required String actual,
    required String nueva,
  });

  Future<void> salir({required String token, required String dispositivoId});

  Future<PaqueteDescargado> descargarPaquete({
    required String token,
    required String dispositivoId,
  });

  Future<RespuestaSincronizacion> sincronizar({
    required String token,
    required String dispositivoId,
    required String dispositivoUuid,
    required int versionEsquema,
    required List<OperacionSalida> operaciones,
  });

  Future<List<int>> descargarPdfActa({
    required String token,
    required int numeroActa,
  });

  Future<List<int>> descargarPdfPresupuesto({
    required String token,
    required int presupuestoId,
  });

  Future<List<int>> descargarExcelPresupuesto({
    required String token,
    required int presupuestoId,
  });
}
