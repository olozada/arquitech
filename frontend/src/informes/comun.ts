import { useQuery } from '@tanstack/react-query'
import Swal from 'sweetalert2'
import { cliente } from '../api/cliente'
import { fechaConsulta } from '../actas/formato'
import type { RegistroCatalogo } from '../catalogos/definiciones'

export function avisar(texto: string) {
  return Swal.fire({ icon: 'error', title: '¡Error!', text: texto, confirmButtonColor: '#1abb9c' })
}

export function useCatalogo(recurso: string) {
  return useQuery({
    queryKey: ['catalogo', recurso],
    queryFn: async () => (await cliente.get<RegistroCatalogo[]>(`/api/${recurso}`)).data,
  })
}

export function fechasInvalidas(usarFechas: boolean, desde: string, hasta: string) {
  return usarFechas && Boolean(fechaConsulta(true, desde)) && Boolean(fechaConsulta(true, hasta)) && hasta < desde
}
