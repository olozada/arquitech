import { expect, test, type Page } from '@playwright/test'

const usuarioAdmin = process.env.E2E_USUARIO ?? 'admin'
const claveInicial = process.env.E2E_CLAVE ?? ''
const claveNueva = process.env.E2E_CLAVE_NUEVA ?? 'Otra123456'

test.beforeAll(() => {
  if (!claveInicial) {
    throw new Error('Defina E2E_CLAVE con la contraseña inicial del usuario admin. No use la base real.')
  }
})

test('entra, abre el listado de actas y el tablero', async ({ page }) => {
  await ingresar(page, usuarioAdmin, claveInicial)
  if (page.url().includes('/cuenta/contrasena')) {
    await cambiarClave(page, claveInicial, claveNueva)
  }

  const listado = page.waitForResponse(respuestaDe('/api/actas'))
  await page.goto('/actas')
  await listado
  await expect(page.getByRole('heading', { name: 'Actas' })).toBeVisible()

  const tablero = page.waitForResponse(respuestaDe('/api/dashboard'))
  await page.goto('/')
  await tablero
  await expect(page.getByRole('heading', { name: 'Tablero' })).toBeVisible()
})

function respuestaDe(ruta: string) {
  return (respuesta: { url: () => string; status: () => number }) => {
    const url = new URL(respuesta.url())
    return url.pathname === ruta && respuesta.status() === 200
  }
}

async function ingresar(page: Page, usuario: string, clave: string) {
  await page.goto('/login')
  await page.getByLabel('Usuario').fill(usuario)
  await page.getByLabel('Contraseña', { exact: true }).fill(clave)
  await page.getByRole('button', { name: 'Ingresar' }).click()
  await page.waitForURL((url) => !url.pathname.endsWith('/login'))
}

async function cambiarClave(page: Page, actual: string, nueva: string) {
  await page.getByLabel('Contraseña actual *').fill(actual)
  await page.getByLabel('Contraseña nueva *').fill(nueva)
  await page.getByLabel('Confirmar contraseña *').fill(nueva)
  await page.getByRole('button', { name: 'Guardar' }).click()
  await expect(page.locator('.swal2-title')).toHaveText('¡Finalizado!')
  await page.locator('.swal2-confirm').click()
}
