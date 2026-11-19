allprojects {
    repositories {
        google()
        mavenCentral()
    }
}

// workmanager_android omite kotlin-android en AGP 9 y espera Kotlin integrado.
// El template de Flutter deja android.builtInKotlin=false, así que la clase
// WorkmanagerPlugin no se genera. Se aplica el plugin solo en ese módulo.
subprojects {
    if (name == "workmanager_android") {
        pluginManager.apply("org.jetbrains.kotlin.android")
        tasks.withType<org.jetbrains.kotlin.gradle.tasks.KotlinCompile>().configureEach {
            compilerOptions.jvmTarget.set(org.jetbrains.kotlin.gradle.dsl.JvmTarget.JVM_1_8)
        }
    }
}

val newBuildDir: Directory =
    rootProject.layout.buildDirectory
        .dir("../../build")
        .get()
rootProject.layout.buildDirectory.value(newBuildDir)

subprojects {
    val newSubprojectBuildDir: Directory = newBuildDir.dir(project.name)
    project.layout.buildDirectory.value(newSubprojectBuildDir)
}
subprojects {
    project.evaluationDependsOn(":app")
}

tasks.register<Delete>("clean") {
    delete(rootProject.layout.buildDirectory)
}
