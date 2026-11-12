import {
  BadgeDollarSign,
  BrickWall,
  Building2,
  Calculator,
  ChartColumn,
  ChevronDown,
  ChevronUp,
  ClipboardCheck,
  FilePenLine,
  FileSpreadsheet,
  Hammer,
  HardHat,
  KeyRound,
  LayoutDashboard,
  ListTree,
  Menu,
  Package,
  PencilRuler,
  Ruler,
  Settings,
  Table,
  Truck,
  Users,
  Wallet,
  type LucideIcon,
} from 'lucide-react'

const iconos: Record<string, LucideIcon> = {
  'layout-dashboard': LayoutDashboard,
  settings: Settings,
  'pencil-ruler': PencilRuler,
  'clipboard-check': ClipboardCheck,
  'hard-hat': HardHat,
  truck: Truck,
  'building-2': Building2,
  'list-tree': ListTree,
  package: Package,
  ruler: Ruler,
  'badge-dollar-sign': BadgeDollarSign,
  users: Users,
  'key-round': KeyRound,
  'file-pen-line': FilePenLine,
  'chart-column': ChartColumn,
  table: Table,
  'brick-wall': BrickWall,
  wallet: Wallet,
  'file-spreadsheet': FileSpreadsheet,
  hammer: Hammer,
  calculator: Calculator,
}

const clasesAnteriores: Record<string, string> = {
  'fa fa-tachometer': 'layout-dashboard',
  'fa fa-cogs': 'settings',
  'fa fa-user': 'pencil-ruler',
  'fa fa-eye': 'clipboard-check',
  'fa fa-briefcase': 'hard-hat',
  'fa fa-truck': 'truck',
  'fa fa-building': 'building-2',
  'fa fa-list': 'list-tree',
  'fa fa-cubes': 'package',
  'fa fa-balance-scale': 'ruler',
  'fa fa-tags': 'badge-dollar-sign',
  'fa fa-users': 'users',
  'fa fa-key': 'key-round',
  'fa fa-file-text': 'file-pen-line',
  'fa fa-bar-chart': 'chart-column',
  'fa fa-table': 'table',
  'fa fa-calculator': 'wallet',
  'fa fa-file-text-o': 'file-spreadsheet',
  'fa fa-list-alt': 'calculator',
}

export function iconoDeMenu(nombre: string | null, id?: number): LucideIcon | null {
  if (!nombre) {
    return null
  }

  if (nombre === 'fa fa-cubes' && id === 23) {
    return BrickWall
  }

  if (nombre === 'fa fa-users' && id === 21) {
    return Hammer
  }

  const canonico = clasesAnteriores[nombre] ?? nombre
  return iconos[canonico] ?? null
}

export { ChevronDown, ChevronUp, Menu }
