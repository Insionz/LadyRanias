import type { KittenStatus } from '../types/types'

/**
 * Samme tabell som KittenStatusRules i backend. Brukes bare til å vise lovlige valg i adminsiden.
 * Det er backend som bestemmer: prøver noen noe annet, svarer API-et 409 uansett.
 */
const allowed: Record<KittenStatus, KittenStatus[]> = {
  Available: ['Reserved', 'Sold', 'Kept', 'UnderEvaluation'],
  Reserved: ['Available', 'Sold'],
  Sold: [],
  Kept: ['Available', 'UnderEvaluation'],
  UnderEvaluation: ['Available', 'Kept'],
}

export function nextKittenStatuses(from: KittenStatus): KittenStatus[] {
  return allowed[from]
}
