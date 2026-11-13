/// Paridad con `Arquitectos.Application.Movil.VersionApp` (cabecera X-Version-App).
bool versionAppEsMenor(String? anunciada, String minima) {
  final actual = _versionNormalizada(anunciada);
  if (actual == null) {
    return true;
  }
  final exigida = _versionNormalizada(minima);
  if (exigida == null) {
    return false;
  }
  return actual < exigida;
}

Version? _versionNormalizada(String? valor) {
  var texto = (valor ?? '').trim();
  final mas = texto.indexOf('+');
  final guion = texto.indexOf('-');
  var corte = -1;
  if (mas >= 0) {
    corte = mas;
  }
  if (guion >= 0 && (corte < 0 || guion < corte)) {
    corte = guion;
  }
  if (corte >= 0) {
    texto = texto.substring(0, corte);
  }
  final partes = texto.split('.').where((p) => p.isNotEmpty).toList();
  if (partes.length < 2 || partes.length > 4) {
    return null;
  }
  final numeros = <int>[];
  for (final parte in partes) {
    final n = int.tryParse(parte);
    if (n == null) {
      return null;
    }
    numeros.add(n);
  }
  while (numeros.length < 4) {
    numeros.add(0);
  }
  return Version(numeros[0], numeros[1], numeros[2], numeros[3]);
}

class Version implements Comparable<Version> {
  const Version(this.major, this.minor, this.build, this.revision);

  final int major;
  final int minor;
  final int build;
  final int revision;

  @override
  int compareTo(Version otra) {
    if (major != otra.major) {
      return major.compareTo(otra.major);
    }
    if (minor != otra.minor) {
      return minor.compareTo(otra.minor);
    }
    if (build != otra.build) {
      return build.compareTo(otra.build);
    }
    return revision.compareTo(otra.revision);
  }

  @override
  bool operator <(Version otra) => compareTo(otra) < 0;
}
