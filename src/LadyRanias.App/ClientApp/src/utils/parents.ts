import type { Cat } from '../types/types'

/** En forelder slik den vises: fra kullet, fra en katt i systemet, eller bare et navn (id mangler). */
export type ParentInfo = {
  id?: string | null
  name: string
  titles?: string | null
  emsCode?: string | null
  isExternal?: boolean
}

/** Fra en katt i systemet. En ekstern katt har ingen side hos oss. */
export function parentFromCat(cat: Cat): ParentInfo {
  return { id: cat.id, name: cat.name, titles: cat.titles, emsCode: cat.emsCode, isExternal: cat.role === 'External' }
}
