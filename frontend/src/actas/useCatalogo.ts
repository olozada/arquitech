import { useQuery } from '@tanstack/react-query'
import { cliente } from '../api/cliente'
import type { RegistroCatalogo } from '../catalogos/definiciones'

export function useCatalogo(recurso: string) {
  return useQuery({
    queryKey: ['catalogo', recurso],
    queryFn: async () => (await cliente.get<RegistroCatalogo[]>(`/api/${recurso}`)).data,
  })
}
