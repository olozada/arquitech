import Flutter
import LocalAuthentication
import UIKit

@main
@objc class AppDelegate: FlutterAppDelegate, FlutterImplicitEngineDelegate {
  override func application(
    _ application: UIApplication,
    didFinishLaunchingWithOptions launchOptions: [UIApplication.LaunchOptionsKey: Any]?
  ) -> Bool {
    excluirSoporteDelRespaldo()
    return super.application(application, didFinishLaunchingWithOptions: launchOptions)
  }

  private func excluirSoporteDelRespaldo() {
    let archivos = FileManager.default
    guard let soporte = archivos.urls(for: .applicationSupportDirectory, in: .userDomainMask).first else {
      return
    }
    try? archivos.createDirectory(at: soporte, withIntermediateDirectories: true)
    excluirDelRespaldo(ruta: soporte.path)
  }

  private static func excluirDelRespaldo(ruta: String) {
    var url = URL(fileURLWithPath: ruta, isDirectory: true)
    var valores = URLResourceValues()
    valores.isExcludedFromBackup = true
    try? url.setResourceValues(valores)
  }

  func didInitializeImplicitFlutterEngine(_ engineBridge: FlutterImplicitEngineBridge) {
    GeneratedPluginRegistrant.register(with: engineBridge.pluginRegistry)
    let canal = FlutterMethodChannel(
      name: "com.celuweb.arquitectos/respaldo",
      binaryMessenger: engineBridge.applicationRegistrar.messenger()
    )
    canal.setMethodCallHandler { llamada, resultado in
      if llamada.method == "excluir" {
        if let args = llamada.arguments as? [String: Any],
           let ruta = args["ruta"] as? String {
          AppDelegate.excluirDelRespaldo(ruta: ruta)
        }
        resultado(nil)
      } else {
        resultado(FlutterMethodNotImplemented)
      }
    }
    let seguridad = FlutterMethodChannel(
      name: "arquitectos/seguridad",
      binaryMessenger: engineBridge.applicationRegistrar.messenger()
    )
    seguridad.setMethodCallHandler { llamada, resultado in
      if llamada.method == "dispositivoSeguro" {
        let contexto = LAContext()
        var error: NSError?
        let configurado = contexto.canEvaluatePolicy(
          .deviceOwnerAuthentication,
          error: &error
        )
        resultado(configurado)
      } else {
        resultado(FlutterMethodNotImplemented)
      }
    }
  }
}
