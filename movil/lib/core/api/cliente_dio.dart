import 'dart:io';

import 'package:dio/dio.dart';
import 'package:logger/logger.dart';

import '../../app/configuracion.dart';
import '../../sincronizacion/modelos.dart';
import '../errores/mensajes.dart';
import 'cliente_movil.dart';
import 'errores.dart';

class ClienteDio implements ClienteMovil {
  ClienteDio({
    required ConfiguracionApp configuracion,
    required this.dispositivoId,
    Logger? registro,
    Dio? dio,
  }) : _registro = registro ?? Logger(printer: PrettyPrinter(methodCount: 0)),
       _dio =
           dio ??
           Dio(
             BaseOptions(
               baseUrl: configuracion.urlApi,
               connectTimeout: const Duration(seconds: 20),
               receiveTimeout: const Duration(minutes: 2),
               headers: {
                 'X-Requested-With': 'arquitectos',
                 'X-Version-App': configuracion.versionApp,
                 'X-Version-Esquema': '${configuracion.versionEsquema}',
               },
               validateStatus: (estado) => estado != null && estado < 600,
             ),
           );

  final Dio _dio;
  final Logger _registro;
  final String Function() dispositivoId;

  @override
  Future<Credenciales> iniciar({
    required String usuario,
    required String contrasena,
    required DispositivoEnvio dispositivo,
  }) async {
    final respuesta = await _enviar(
      () => _dio.post<Object?>(
        '/api/movil/v1/auth/login',
        data: {
          'usuario': usuario,
          'contrasena': contrasena,
          'dispositivo': {
            'id': dispositivo.id,
            'nombre': dispositivo.nombre,
            'plataforma': dispositivo.plataforma,
            'versionApp': dispositivo.versionApp,
          },
        },
        options: _opciones(),
      ),
    );
    final mapa = _mapa(respuesta.data);
    final persona = _mapa(mapa['usuario']);
    return Credenciales(
      accessToken: _texto(mapa['accessToken']),
      expiraEn: DateTime.parse(_texto(mapa['expiraEn'])).toUtc(),
      refreshToken: _texto(mapa['refreshToken']),
      debeCambiarContrasena: mapa['debeCambiarContrasena'] == true,
      usuario: UsuarioMovil(
        id: _entero(persona['id']),
        nombre: _texto(persona['nombre']),
        tipoUsuarioId: _entero(persona['tipoUsuarioId']),
        arquitectoId: _opcional(persona['arquitectoId']),
        contratistaId: _opcional(persona['contratistaId']),
        revisorId: _opcional(persona['revisorId']),
      ),
    );
  }

  @override
  Future<ParTokens> renovar({
    required String refreshToken,
    required String dispositivoId,
  }) async {
    final respuesta = await _enviar(
      () => _dio.post<Object?>(
        '/api/movil/v1/auth/renovar',
        data: {'refreshToken': refreshToken, 'dispositivoId': dispositivoId},
        options: _opciones(),
      ),
    );
    final mapa = _mapa(respuesta.data);
    return ParTokens(
      accessToken: _texto(mapa['accessToken']),
      expiraEn: DateTime.parse(_texto(mapa['expiraEn'])).toUtc(),
      refreshToken: _texto(mapa['refreshToken']),
    );
  }

  @override
  Future<void> cambiarContrasena({
    required String token,
    required String actual,
    required String nueva,
  }) async {
    await _enviar(
      () => _dio.post<Object?>(
        '/api/auth/cambiar-contrasena',
        data: {'contrasenaActual': actual, 'contrasenaNueva': nueva},
        options: _opciones(token),
      ),
    );
  }

  @override
  Future<void> salir({
    required String token,
    required String dispositivoId,
  }) async {
    await _enviar(
      () => _dio.post<Object?>(
        '/api/movil/v1/auth/salir',
        options: _opciones(token),
      ),
    );
  }

  @override
  Future<PaqueteDescargado> descargarPaquete({
    required String token,
    required String dispositivoId,
    required String rutaArchivoGz,
    void Function(int recibidos, int? total)? onProgreso,
  }) async {
    final destino = File(rutaArchivoGz);
    await destino.parent.create(recursive: true);
    if (await destino.exists()) {
      await destino.delete();
    }

    final respuesta = await _enviar(
      () => _dio.download(
        '/api/movil/v1/paquete',
        destino.path,
        options: _opciones(token),
        deleteOnError: true,
        onReceiveProgress: onProgreso,
      ),
    );
    final sha = respuesta.headers.value('x-paquete-sha256');
    final esquema = int.tryParse(
      respuesta.headers.value('x-paquete-esquema') ?? '',
    );
    final generado = respuesta.headers.value('x-paquete-generado');
    if (sha == null ||
        sha.isEmpty ||
        esquema == null ||
        generado == null ||
        generado.isEmpty) {
      if (await destino.exists()) {
        await destino.delete();
      }
      throw const PaqueteInvalido('El paquete no trae su huella.');
    }
    if (!await destino.exists() || await destino.length() == 0) {
      throw const PaqueteInvalido('El paquete llegó vacío.');
    }
    return PaqueteDescargado(
      archivoGz: destino,
      sha256: sha,
      esquema: esquema,
      generadoEn: generado,
    );
  }

  @override
  Future<RespuestaSincronizacion> sincronizar({
    required String token,
    required String dispositivoId,
    required String dispositivoUuid,
    required int versionEsquema,
    required List<OperacionSalida> operaciones,
  }) async {
    final respuesta = await _enviar(
      () => _dio.post<Object?>(
        '/api/movil/v1/sincronizar',
        data: {
          'dispositivoId': dispositivoUuid,
          'versionEsquema': versionEsquema,
          'operaciones': operaciones.map((op) => op.aSolicitud()).toList(),
        },
        options: _opciones(token),
      ),
    );
    final mapa = _mapa(respuesta.data);
    final resultados = (mapa['resultados'] as List? ?? const [])
        .map(
          (item) => ResultadoOperacion.desdeJson(
            Map<String, Object?>.from(item as Map),
          ),
        )
        .toList();
    return RespuestaSincronizacion(
      resultados: resultados,
      requiereNuevoPaquete: mapa['requiereNuevoPaquete'] == true,
    );
  }

  @override
  Future<List<int>> descargarPdfActa({
    required String token,
    required int numeroActa,
  }) => _descargarBytes(
    token,
    '/api/actas/$numeroActa/pdf',
    'El PDF está vacío.',
  );

  @override
  Future<List<int>> descargarPdfPresupuesto({
    required String token,
    required int presupuestoId,
  }) => _descargarBytes(
    token,
    '/api/presupuestos/$presupuestoId/pdf',
    'El PDF está vacío.',
  );

  @override
  Future<List<int>> descargarExcelPresupuesto({
    required String token,
    required int presupuestoId,
  }) => _descargarBytes(
    token,
    '/api/presupuestos/$presupuestoId/excel',
    'El Excel está vacío.',
  );

  Future<List<int>> _descargarBytes(
    String token,
    String ruta,
    String mensajeVacio,
  ) async {
    final respuesta = await _enviar(
      () => _dio.get<Object?>(
        ruta,
        options: _opciones(token).copyWith(responseType: ResponseType.bytes),
      ),
    );
    final datos = respuesta.data;
    if (datos is! List<int> || datos.isEmpty) {
      throw ErrorServidor(mensajeVacio);
    }
    return datos;
  }

  Options _opciones([String? token]) {
    return Options(
      headers: {
        'X-Dispositivo-Id': dispositivoId(),
        if (token != null) 'Authorization': 'Bearer $token',
      },
    );
  }

  Future<Response<Object?>> _enviar(
    Future<Response<Object?>> Function() llamada,
  ) async {
    final Response<Object?> respuesta;
    try {
      respuesta = await llamada();
    } on DioException catch (error) {
      _registro.w('Error de red (${error.type})', error: error);
      final mensaje = switch (error.type) {
        DioExceptionType.connectionTimeout ||
        DioExceptionType.receiveTimeout ||
        DioExceptionType.sendTimeout => Mensajes.servidorLento,
        DioExceptionType.badCertificate => Mensajes.conexionInsegura,
        DioExceptionType.connectionError => Mensajes.sinConexion,
        _ => Mensajes.sinConexion,
      };
      throw ErrorServidor(mensaje, estado: 0);
    }

    final estado = respuesta.statusCode ?? 0;
    if (estado >= 200 && estado < 300) {
      return respuesta;
    }
    throw _fallo(respuesta.data, estado);
  }

  FalloMovil _fallo(Object? data, int estado) {
    final mapa = data is Map
        ? Map<String, Object?>.from(data)
        : const <String, Object?>{};
    if (mapa['codigo'] == 'dispositivo_revocado') {
      return DispositivoRevocado(
        _detalle(mapa) ?? 'Este dispositivo fue revocado.',
      );
    }
    if (estado == 426) {
      return const ActualizacionRequerida(Mensajes.actualice);
    }
    return ErrorServidor(
      _detalle(mapa) ?? 'No se pudo completar la solicitud.',
      estado: estado,
    );
  }

  String? _detalle(Map<String, Object?> mapa) {
    final errores = mapa['errors'];
    if (errores is Map) {
      final textos = <String>[];
      for (final valor in errores.values) {
        if (valor is List) {
          textos.addAll(valor.map((item) => item.toString()));
        } else if (valor != null) {
          textos.add(valor.toString());
        }
      }
      if (textos.isNotEmpty) {
        return textos.join('\n');
      }
    }
    final detalle = mapa['detail'] ?? mapa['title'];
    if (detalle is String && detalle.isNotEmpty) {
      return detalle;
    }
    return null;
  }

  Map<String, Object?> _mapa(Object? valor) {
    if (valor is Map) {
      return Map<String, Object?>.from(valor);
    }
    throw const ErrorServidor('La respuesta del servidor no es válida.');
  }

  String _texto(Object? valor) => valor?.toString() ?? '';

  int _entero(Object? valor) =>
      valor is int ? valor : int.parse(valor.toString());

  int? _opcional(Object? valor) {
    if (valor == null) {
      return null;
    }
    return valor is int ? valor : int.tryParse(valor.toString());
  }
}
