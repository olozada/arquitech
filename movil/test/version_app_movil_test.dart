import 'package:arquitectos_movil/core/api/version_app_movil.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('versionAppEsMenor ignora build y prerelease como el backend', () {
    expect(versionAppEsMenor('1.0.0+1', '1.0.0'), isFalse);
    expect(versionAppEsMenor('1.2.0-beta', '1.1.0'), isFalse);
    expect(versionAppEsMenor('0.9.0', '1.0.0'), isTrue);
    expect(versionAppEsMenor('', '1.0.0'), isTrue);
    expect(versionAppEsMenor('abc', '1.0.0'), isTrue);
  });
}
