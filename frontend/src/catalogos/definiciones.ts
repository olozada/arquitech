export type CampoCatalogo = {
  nombre: string
  etiqueta: string
  requerido: boolean
  maximo: number
  mensajeRequerido?: string
  mensajeMaximo: string
  numero?: boolean
}

export type ColumnaCatalogo = {
  clave: string
  titulo: string
}

export type ConfiguracionCatalogo = {
  titulo: string
  recurso: string
  ruta: string
  campos: CampoCatalogo[]
  columnas: ColumnaCatalogo[]
}

export type RegistroCatalogo = {
  id: number
  activo: boolean
  [clave: string]: string | number | boolean | null | undefined
}

const nombre = (etiqueta = 'Nombre'): CampoCatalogo => ({
  nombre: 'nombre',
  etiqueta,
  requerido: true,
  maximo: 150,
  mensajeRequerido: 'Debe ingresar el nombre.',
  mensajeMaximo: 'El nombre no puede superar 150 caracteres.',
})

const descripcion = (maximo: number, mensajeMaximo: string, requerido = true): CampoCatalogo => ({
  nombre: 'descripcion',
  etiqueta: 'Descripción',
  requerido,
  maximo,
  mensajeRequerido: 'Debe ingresar la descripción.',
  mensajeMaximo,
})

function catalogoPersona(titulo: string, recurso: string): ConfiguracionCatalogo {
  return {
    titulo,
    recurso,
    ruta: `admin/${recurso}`,
    campos: [
      {
        nombre: 'cedula',
        etiqueta: 'Cédula',
        requerido: false,
        maximo: 20,
        mensajeMaximo: 'La cédula no puede superar 20 caracteres.',
      },
      nombre(),
    ],
    columnas: [
      { clave: 'cedula', titulo: 'Cédula' },
      { clave: 'nombre', titulo: 'Nombre' },
    ],
  }
}

export const catalogos: ConfiguracionCatalogo[] = [
  catalogoPersona('Arquitectos', 'arquitectos'),
  catalogoPersona('Revisores', 'revisores'),
  catalogoPersona('Contratistas', 'contratistas'),
  {
    titulo: 'Proveedores',
    recurso: 'proveedores',
    ruta: 'admin/proveedores',
    campos: [
      {
        nombre: 'nit',
        etiqueta: 'NIT',
        requerido: false,
        maximo: 20,
        mensajeMaximo: 'El NIT no puede superar 20 caracteres.',
      },
      nombre(),
    ],
    columnas: [
      { clave: 'nit', titulo: 'NIT' },
      { clave: 'nombre', titulo: 'Nombre' },
    ],
  },
  {
    titulo: 'Construcciones',
    recurso: 'construcciones',
    ruta: 'admin/construcciones',
    campos: [
      descripcion(200, 'La descripción no puede superar 200 caracteres.'),
      {
        nombre: 'propietario',
        etiqueta: 'Propietario',
        requerido: false,
        maximo: 150,
        mensajeMaximo: 'El propietario no puede superar 150 caracteres.',
      },
    ],
    columnas: [
      { clave: 'id', titulo: 'Código' },
      { clave: 'descripcion', titulo: 'Descripción' },
      { clave: 'propietario', titulo: 'Propietario' },
    ],
  },
  {
    titulo: 'Conceptos',
    recurso: 'conceptos',
    ruta: 'admin/conceptos',
    campos: [
      descripcion(200, 'La descripción no puede superar 200 caracteres.'),
      {
        nombre: 'unidad',
        etiqueta: 'Unidad',
        requerido: false,
        maximo: 20,
        mensajeMaximo: 'La unidad no puede superar 20 caracteres.',
      },
    ],
    columnas: [
      { clave: 'id', titulo: 'Código' },
      { clave: 'descripcion', titulo: 'Descripción' },
      { clave: 'unidad', titulo: 'Unidad' },
    ],
  },
  {
    titulo: 'Unidades de medida',
    recurso: 'unidades-medida',
    ruta: 'admin/unidades-medida',
    campos: [
      {
        nombre: 'nombre',
        etiqueta: 'Nombre',
        requerido: true,
        maximo: 20,
        mensajeRequerido: 'Debe ingresar el nombre.',
        mensajeMaximo: 'El nombre no puede superar 20 caracteres.',
      },
      descripcion(100, 'La descripción no puede superar 100 caracteres.', false),
    ],
    columnas: [
      { clave: 'id', titulo: 'Código' },
      { clave: 'nombre', titulo: 'Nombre' },
      { clave: 'descripcion', titulo: 'Descripción' },
    ],
  },
]
