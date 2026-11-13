import 'package:arquitectos_movil/app/configuracion.dart';
import 'package:arquitectos_movil/app/controlador.dart';
import 'package:arquitectos_movil/app/dependencias.dart';
import 'package:arquitectos_movil/core/api/red.dart';
import 'package:arquitectos_movil/core/db/importador_paquete.dart';
import 'package:arquitectos_movil/core/db/pendientes_db.dart';
import 'package:arquitectos_movil/core/db/repositorio.dart';
import 'package:arquitectos_movil/core/seguridad/claves.dart';
import 'package:arquitectos_movil/core/seguridad/desbloqueador.dart';
import 'package:arquitectos_movil/sincronizacion/cola_movil.dart';
import 'package:arquitectos_movil/sincronizacion/modelos.dart';
import 'package:arquitectos_movil/sincronizacion/sincronizador_movil.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:sqflite_common_ffi/sqflite_ffi.dart';

import '../integration_test/simulador_api_movil.dart';
import 'soporte.dart';

/// Escenario MODULO §8: login → paquete → línea offline → sincronizar → paquete con id.
///
/// Misma lógica que `integration_test/` pero ejecutable con `flutter test` en VM
/// (la carpeta `integration_test/` exige dispositivo/desktop con symlinks en Windows).
void main() {
  setUpAll(() {
    TestWidgetsFlutterBinding.ensureInitialized();
    sqfliteFfiInit();
    databaseFactory = databaseFactoryFfi;
  });

  test('login, cola offline y sincronización con paquete nuevo', () async {
    final simulador = SimuladorApiMovil();
    await simulador.iniciar();
    final cliente = ClienteSimuladorApi(simulador);

    final carpeta = await carpetaPrueba('integracion');
    addTearDown(() => borrarCarpeta(carpeta));
    final abrir = AbrirFfi();
    final repositorio = RepositorioLocal(
      directorio: () async => carpeta,
      abrir: abrir,
    );
    final identidad = IdentidadDispositivo()
      ..id = '00000000-0000-4000-8000-000000000099';
    final configuracion = const ConfiguracionApp(
      urlApi: 'http://simulador.test',
    );
    final cola = ColaMovil();
    final pendientes = PendientesDb(
      abrir: abrir,
      ruta: () async => repositorio.rutaPendientes(),
    );
    final dependencias = Dependencias(
      configuracion: configuracion,
      identidad: identidad,
      cliente: cliente,
      almacen: AlmacenMemoria(),
      desbloqueador: DesbloqueadorFijo(true),
      red: RedFija(true),
      repositorio: repositorio,
      importador: ImportadorPaquete(cifrador: CifradorCopia(), abrir: abrir),
      cola: cola,
      pendientes: pendientes,
      sincronizador: SincronizadorMovil(
        cliente: cliente,
        cola: cola,
        pendientes: pendientes,
        configuracion: configuracion,
      ),
      comunes: const {},
      directorio: () async => carpeta,
    );
    final controlador = ControladorAplicacion(dependencias);
    addTearDown(controlador.dispose);

    await controlador.iniciar();
    await controlador.entrar('carlos', 'Clave12345');
    expect(controlador.tieneBase, isTrue);
    expect(controlador.usuario?.tipoUsuarioId, 3);

    const descripcionNueva = 'Línea integration test';
    await controlador.agregarLinea(
      120,
      const LineaSincronizacion(
        construccionId: 7,
        conceptoId: 1,
        cantidad: '3.0000',
        precio: '100.00',
        descripcion: descripcionNueva,
      ),
    );
    expect(controlador.pendientes, 1);

    await controlador.sincronizar();
    expect(controlador.pendientes, 0);

    final clave = await dependencias.almacen.leer(Claves.sql);
    expect(clave, isNotNull);

    final base = await repositorio.obtener(clave);
    if (base == null) {
      fail('No hay base local tras sincronizar.');
    }
    final filas = await base.consultar(
      'SELECT id, descripcion FROM acta_detalles WHERE descripcion = ?',
      [descripcionNueva],
    );
    expect(filas, hasLength(1));
    expect(filas.first['id'], 501);
    await base.cerrar();
    await repositorio.cerrar();
    await pendientes.cerrar();
  });
}
