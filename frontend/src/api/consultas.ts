import { QueryClient } from '@tanstack/react-query'

export const consultas = new QueryClient({
  defaultOptions: {
    queries: {
      retry: false,
      refetchOnWindowFocus: false,
    },
  },
})
