import 'package:arquitectos_movil/app/configuracion.dart';
import 'package:arquitectos_movil/core/api/cliente_dio.dart';
import 'package:arquitectos_movil/core/api/errores.dart';
import 'package:arquitectos_movil/core/errores/mensajes.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

void main() {
  setUpAll(() {
    registerFallbackValue(Options());
  });

  Future<void> expectMensaje(DioExceptionType tipo, String mensaje) async {
    final dio = _DioPrueba(tipo);
    final cliente = ClienteDio(
      configuracion: const ConfiguracionApp(
        urlApi: 'http://localhost',
        versionApp: '1.0.0',
        buildApp: '1',
      ),
      dispositivoId: () => 'dev',
      dio: dio,
    );
    await expectLater(
      cliente.descargarPdfActa(token: 't', numeroActa: 1),
      throwsA(
        isA<ErrorServidor>().having((e) => e.mensaje, 'mensaje', mensaje),
      ),
    );
  }

  test('connectionTimeout', () async {
    await expectMensaje(
      DioExceptionType.connectionTimeout,
      Mensajes.servidorLento,
    );
  });

  test('badCertificate', () async {
    await expectMensaje(
      DioExceptionType.badCertificate,
      Mensajes.conexionInsegura,
    );
  });

  test('connectionError', () async {
    await expectMensaje(DioExceptionType.connectionError, Mensajes.sinConexion);
  });
}

class _DioPrueba implements Dio {
  _DioPrueba(this.tipo);

  final DioExceptionType tipo;

  @override
  Future<Response<T>> get<T>(
    String path, {
    Object? data,
    Map<String, dynamic>? queryParameters,
    Options? options,
    CancelToken? cancelToken,
    ProgressCallback? onReceiveProgress,
  }) async {
    throw DioException(
      requestOptions: RequestOptions(path: path),
      type: tipo,
    );
  }

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}
