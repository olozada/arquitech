import type { components } from './esquema'

type Esquemas = components['schemas']

export type Acta = Esquemas['ActaDto']
export type LineaActa = Esquemas['LineaActaDto']
export type ActaCreada = Esquemas['ActaCreadaDto']
export type ActaResumen = Esquemas['ActaResumenDto']
export type ConsultaActas = Esquemas['ConsultaActasDto']
export type ItemInforme = Esquemas['ItemInformeDto']
export type InformeItems = Esquemas['InformeItemsDto']
export type ItemMaterial = Esquemas['ItemMaterialInformeDto']
export type InformeMateriales = Esquemas['InformeMaterialesDto']
export type ItemPresupuestoActa = Esquemas['ItemActaDto']
