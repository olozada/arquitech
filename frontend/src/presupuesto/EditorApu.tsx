import { useEditorApu, type PropsEditorApu } from './useEditorApu'
import { VistaEditorApu } from './VistaEditorApu'

export function EditorApu(props: PropsEditorApu) {
  const modelo = useEditorApu(props)
  if (modelo.cargando) {
    return <p>Cargando APU…</p>
  }

  return <VistaEditorApu {...modelo} />
}
