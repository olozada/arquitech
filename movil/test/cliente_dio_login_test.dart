import 'dart:convert';

import 'package:arquitectos_movil/app/configuracion.dart';
import 'package:arquitectos_movil/core/api/cliente_dio.dart';
import 'package:arquitectos_movil/core/api/cliente_movil.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('login usa /api/auth/login y lee las cookies', () async {
    const exp = 1893456000;
    final acceso = _jwt({'sub': '4', 'tipoUsuarioId': '2', 'exp': exp});
    final dio = _DioLogin(
      Response<Object?>(
        requestOptions: RequestOptions(path: '/api/auth/login'),
        statusCode: 200,
        data: {
          'nombre': 'Ana',
          'tipoUsuario': 'Arquitecto',
          'debeCambiarContrasena': false,
        },
        headers: Headers.fromMap({
          'set-cookie': [
            'arquitectos.acceso=$acceso; path=/api; httponly',
            'arquitectos.renovacion=refresh-1; path=/api/auth/renovar; httponly',
          ],
        }),
      ),
    );
    final cliente = ClienteDio(
      configuracion: const ConfiguracionApp(
        urlApi: 'http://10.200.220.99:5190',
        versionApp: '1.0.0',
      ),
      dispositivoId: () => 'dev',
      dio: dio,
    );

    final credenciales = await cliente.iniciar(
      usuario: 'carango',
      contrasena: 'secreta',
      dispositivo: const DispositivoEnvio(
        id: 'dev',
        nombre: 'telefono',
        plataforma: 'android',
        versionApp: '1.0.0',
      ),
    );

    expect(dio.ruta, '/api/auth/login');
    expect(dio.cuerpo, {'usuario': 'carango', 'password': 'secreta'});
    expect(credenciales.accessToken, acceso);
    expect(credenciales.refreshToken, 'refresh-1');
    expect(credenciales.usuario.id, 4);
    expect(credenciales.usuario.nombre, 'Ana');
    expect(credenciales.usuario.tipoUsuarioId, 2);
    expect(
      credenciales.expiraEn,
      DateTime.fromMillisecondsSinceEpoch(exp * 1000, isUtc: true),
    );
  });
}

String _jwt(Map<String, Object?> claims) {
  String parte(Object valor) {
    return base64Url.encode(utf8.encode(jsonEncode(valor))).replaceAll('=', '');
  }

  return '${parte({'alg': 'none'})}.${parte(claims)}.firma';
}

class _DioLogin implements Dio {
  _DioLogin(this.respuesta);

  final Response<Object?> respuesta;
  String? ruta;
  Object? cuerpo;

  @override
  Future<Response<T>> post<T>(
    String path, {
    Object? data,
    Map<String, dynamic>? queryParameters,
    Options? options,
    CancelToken? cancelToken,
    ProgressCallback? onSendProgress,
    ProgressCallback? onReceiveProgress,
  }) async {
    ruta = path;
    cuerpo = data;
    return respuesta as Response<T>;
  }

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}
