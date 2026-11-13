abstract class AlmacenSeguro {
  Future<String?> leer(String clave);

  Future<void> guardar(String clave, String valor);

  Future<void> borrar(String clave);

  Future<void> borrarTodo();
}
