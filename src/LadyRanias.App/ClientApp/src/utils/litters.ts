import type { Breed, Litter } from '../types/types'

const halfYearMs = 182 * 24 * 60 * 60 * 1000

/**
 * Et kull er «aktuelt» så lenge det er planlagt, under et halvt år gammelt, eller har kattunger som ikke har fått et hjem ennå.
 * Resten er tidligere kull: historien. Samme regel brukes på kullsiden og i admin, så de er enige om hva som er aktivt.
 */
export function isCurrent(litter: Litter, now = Date.now()) {
  if (litter.status === 'Planned') return true
  const young = litter.birthDate != null && now - Date.parse(litter.birthDate) < halfYearMs
  const open = litter.kittens.some(k => !k.deceased && (k.status === 'Available' || k.status === 'Reserved' || k.status === 'UnderEvaluation'))
  return young || open
}

/** Kullets rase er morens. Er moren slettet, brukes kattungenes. */
export function litterBreed(litter: Litter): Breed {
  return litter.mother?.breed ?? litter.kittens.find(k => k.breed !== 'Unknown')?.breed ?? 'Unknown'
}

/** Treffer søket kullnavnet, mor, far eller en av kattungene? */
export function litterMatches(litter: Litter, query: string) {
  const q = query.trim().toLowerCase()
  if (!q) return true
  const names = [litter.name, litter.mother?.name, litter.father?.name, ...litter.kittens.map(k => k.name)]
  return names.some(name => name?.toLowerCase().includes(q))
}
