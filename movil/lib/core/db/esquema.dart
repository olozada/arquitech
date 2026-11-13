import 'base_local.dart';

Future<void> aplicarEsquema(BaseLocal base, String sql) async {
  final sinComentarios = sql
      .split('\n')
      .map((linea) {
        final comentario = linea.indexOf('--');
        return comentario < 0 ? linea : linea.substring(0, comentario);
      })
      .join('\n');
  for (final parte in sinComentarios.split(';')) {
    final sentencia = parte.trim();
    if (sentencia.isEmpty) {
      continue;
    }
    await base.ejecutar(sentencia);
  }
}
