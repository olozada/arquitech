import { spawnSync } from 'node:child_process'
import { existsSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import path from 'node:path'

const frontend = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..')
const raiz = path.resolve(frontend, '..')
const backend = path.join(raiz, 'backend')
const entorno = {
  ...process.env,
  ASPNETCORE_ENVIRONMENT: 'OpenApi',
  Jwt__Key: 'openapi-generacion-clave-local-32b',
  ConnectionStrings__Arquitectos: 'Server=localhost;Database=ARQUITECTOS_OPENAPI;Trusted_Connection=True;TrustServerCertificate=True',
}

function ejecutar(comando, args, cwd) {
  const resultado = spawnSync(comando, args, { cwd, env: entorno, stdio: 'inherit' })
  if (resultado.error) {
    console.error(resultado.error.message)
    process.exit(1)
  }
  if (resultado.status !== 0) {
    process.exit(resultado.status ?? 1)
  }
}

function npmCi(cwd) {
  const bin = path.dirname(process.execPath)
  const cli = [
    path.join(bin, 'node_modules', 'npm', 'bin', 'npm-cli.js'),
    path.join(bin, '..', 'lib', 'node_modules', 'npm', 'bin', 'npm-cli.js'),
  ].find((ruta) => existsSync(ruta))
  if (cli) {
    ejecutar(process.execPath, [cli, 'ci'], cwd)
    return
  }

  const resultado = spawnSync('npm ci', { cwd, env: entorno, stdio: 'inherit', shell: true })
  if (resultado.error) {
    console.error(resultado.error.message)
    process.exit(1)
  }
  if (resultado.status !== 0) {
    process.exit(resultado.status ?? 1)
  }
}

ejecutar('dotnet', ['tool', 'restore'], backend)
ejecutar(
  'dotnet',
  [
    'build',
    path.join(backend, 'src', 'Arquitectos.Api', 'Arquitectos.Api.csproj'),
    '--configuration',
    'Release',
    '-p:GenerateOpenApi=true',
    '--nologo',
  ],
  raiz,
)
const herramienta = path.join(frontend, 'herramientas', 'openapi')
const cli = path.join(herramienta, 'node_modules', 'openapi-typescript', 'bin', 'cli.js')
npmCi(herramienta)
ejecutar(process.execPath, [cli, path.join(frontend, 'swagger', 'v1', 'swagger.json'), '-o', path.join(frontend, 'src', 'api', 'esquema.ts')], frontend)
