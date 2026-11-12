import { expect, test, type Page } from '@playwright/test'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

const usuarioAdmin = process.env.E2E_USUARIO ?? 'admin'
const claveInicial = process.env.E2E_CLAVE ?? ''
const claveNueva = process.env.E2E_CLAVE_NUEVA ?? 'Otra123456'
const claveRevisor = 'Clave12345'
let numeroActa = ''

test.describe.configure({ mode: 'serial' })

test.beforeAll(() => {
  if (!claveInicial) {
    throw new Error('Defina E2E_CLAVE con la contraseña inicial del usuario admin. No use la base real.')
  }
})

test('el administrador deja listo al revisor', async ({ page }) => {
  await ingresar(page, usuarioAdmin, claveInicial)
  if (page.url().includes('/cuenta/contrasena')) {
    await cambiarClave(page, claveInicial, claveNueva)
  }

  await page.goto('/admin/usuarios')
  await page.getByLabel('Usuario *').fill('rev.demo')
  await page.getByLabel('Nombre *').fill('Revisor demo')
  await page.getByLabel('Contraseña *').fill(claveRevisor)
  await page.getByLabel('Tipo de usuario *').selectOption({ label: 'Revisor' })
  await page.getByLabel('Persona del perfil *').selectOption({ label: 'Ing. Carlos Arango' })
  await page.getByRole('button', { name: 'Crear' }).click()
  await confirmar('¡Finalizado!', page)
})

test('crea un acta, el revisor rechaza un ítem, el administrador la cierra y abre el pdf', async ({ page }) => {
  await ingresar(page, usuarioAdmin, claveNueva)
  await page.goto('/actas/nueva')
  const hoy = new Date().toISOString().slice(0, 10)
  await page.locator('#acta-fecha-inicial').fill(hoy)
  await page.locator('#acta-fecha-final').fill(hoy)
  await page.locator('#acta-arquitecto').selectOption({ label: 'Laura Restrepo Gómez' })
  await page.locator('#acta-revisor').selectOption({ label: 'Ing. Carlos Arango' })
  await page.locator('#acta-contratista').selectOption({ label: 'Construcciones Quindío S.A.S.' })
  await page.getByRole('button', { name: 'Agregar ítems' }).click()
  await page.locator('#linea-construccion').selectOption({ label: 'Edificio Torre Café – Armenia' })
  await page.locator('#linea-concepto').selectOption({ label: 'Pañete de muros' })
  await page.locator('#linea-um').selectOption({ label: 'm2' })
  await page.locator('#linea-material').selectOption({ label: 'Cemento gris 50 kg' })
  await page.locator('#linea-cantidad').fill('2')
  await page.locator('#linea-precio').fill('1000')
  await page.locator('#linea-descripcion').fill('Ítem de prueba')
  await page.getByRole('button', { name: 'Agregar', exact: true }).click()
  await page.getByRole('button', { name: 'Registrar' }).click()
  await page.locator('.swal2-confirm').click()
  const aviso = page.locator('.swal2-html-container')
  await expect(aviso).toContainText('Acta N°')
  numeroActa = (await aviso.innerText()).replace(/\D/g, '')
  await page.getByRole('button', { name: 'OK' }).click()

  await page.getByRole('button', { name: 'Salir' }).click()
  await ingresar(page, 'rev.demo', claveRevisor)
  if (page.url().includes('/cuenta/contrasena')) {
    await cambiarClave(page, claveRevisor, claveNueva)
  }
  await page.goto(`/actas/${numeroActa}`)
  await page.getByRole('button', { name: 'Rechazar' }).click()
  await page.getByRole('button', { name: 'Guardar' }).click()
  await page.locator('.swal2-confirm').click()
  await confirmar('¡Finalizado!', page)

  await page.getByRole('button', { name: 'Salir' }).click()
  await ingresar(page, usuarioAdmin, claveNueva)
  await page.goto(`/actas/${numeroActa}`)
  await page.getByRole('button', { name: 'Cerrar acta' }).click()
  await page.locator('.swal2-confirm').click()
  await confirmar('Acta cerrada', page)
  const pdf = page.waitForResponse((respuesta) => respuesta.url().includes('/pdf') && respuesta.status() === 200)
  await page.getByRole('button', { name: 'Imprimir' }).click()
  expect((await pdf).headers()['content-type'] ?? '').toContain('pdf')
})

test('crea un presupuesto desde la plantilla, indica cantidades, lo aprueba y abre el seguimiento', async ({ page }) => {
  await ingresar(page, usuarioAdmin, claveNueva)
  await page.goto('/presupuestos')
  await page.getByRole('button', { name: 'Nuevo' }).click()
  const dialogo = page.getByRole('dialog')
  await dialogo.getByLabel('Construcción').selectOption({ label: 'Conjunto Guaduales de La Tebaida' })
  await dialogo.getByLabel('Área m²').fill('80')
  await dialogo.getByRole('button', { name: 'Continuar' }).click()
  await dialogo.getByRole('button', { name: 'Desde plantilla' }).click()
  await dialogo.locator('select').selectOption({ label: /Vivienda unifamiliar/ })
  await dialogo.getByRole('button', { name: 'Crear' }).click()
  await expect(page).toHaveURL(/\/presupuestos\/\d+$/)
  const cantidad = page.locator('table input').first()
  if (await cantidad.isEnabled()) {
    await cantidad.fill('2')
    await expect(page.getByText('Guardado')).toBeVisible()
  }
  await page.getByRole('button', { name: 'Aprobar' }).click()
  const titulo = page.locator('.swal2-title')
  await expect(titulo).toBeVisible()
  if ((await titulo.innerText()).includes('cantidades en cero')) {
    await page.locator('.swal2-confirm').click()
  }
  await confirmar('Presupuesto aprobado', page)
  await page.getByRole('link', { name: 'Seguimiento' }).click()
  await expect(page.getByRole('heading', { name: /Seguimiento/ })).toBeVisible()
})

test('importar Casa Murillo informa 116 de 122 apu', async ({ page }) => {
  await ingresar(page, usuarioAdmin, claveNueva)
  await page.goto('/presupuestos')
  await page.getByRole('button', { name: 'Importar Excel' }).click()
  const dialogo = page.getByRole('dialog')
  await dialogo.getByLabel('Construcción').selectOption({ label: 'Bodega Zona Franca Quindío' })
  await dialogo.locator('input[type="file"]').setInputFiles(
    join(dirname(fileURLToPath(import.meta.url)), '../../backend/tests/Arquitectos.Tests/Fixtures/Presupuesto_Casa_Murillo.xlsx'),
  )
  await dialogo.getByRole('button', { name: 'Importar' }).click()
  await expect(page.locator('.swal2-html-container')).toContainText('116 de 122')
})

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
  await confirmar('¡Finalizado!', page)
}

async function confirmar(titulo: string, page: Page) {
  await expect(page.locator('.swal2-title')).toHaveText(titulo)
  await page.locator('.swal2-confirm').click()
}
