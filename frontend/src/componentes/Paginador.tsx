import { Pagination } from 'react-bootstrap'

export function Paginador({
  pagina,
  total,
  tamano,
  alCambiar,
}: {
  pagina: number
  total: number
  tamano: number
  alCambiar: (pagina: number) => void
}) {
  const paginas = Math.max(1, Math.ceil(total / (tamano || 1)))
  if (paginas <= 1) {
    return null
  }

  return (
    <Pagination className="catalogo-paginacion mt-3 mb-0">
      <Pagination.Prev disabled={pagina <= 1} onClick={() => alCambiar(pagina - 1)} />
      <Pagination.Item active>
        {pagina} / {paginas}
      </Pagination.Item>
      <Pagination.Next disabled={pagina >= paginas} onClick={() => alCambiar(pagina + 1)} />
    </Pagination>
  )
}
