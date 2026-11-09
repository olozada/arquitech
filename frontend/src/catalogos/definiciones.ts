export type CampoCatalogo = {
  nombre: string
  etiqueta: string
  requerido: boolean
  maximo: number
  mensajeRequerido?: string
  mensajeMaximo: string
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

function catalogoNombre(titulo: string, recurso: string): ConfiguracionCatalogo {
  return {
    titulo,
    recurso,
    ruta: `admin/${recurso}`,
    campos: [nombre()],
    columnas: [
      { clave: 'id', titulo: 'Código' },
      { clave: 'nombre', titulo: 'Nombre' },
    ],
  }
}

export const catalogos: ConfiguracionCatalogo[] = [
  catalogoNombre('Arquitectos', 'arquitectos'),
  catalogoNombre('Revisores', 'revisores'),
  catalogoNombre('Contratistas', 'contratistas'),
  catalogoNombre('Proveedores', 'proveedores'),
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
    titulo: 'Materiales',
    recurso: 'materiales',
    ruta: 'admin/materiales',
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
