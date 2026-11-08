/** Siden til en katt hos NRR (MyCats). */
export function nrrCatUrl(nrrId: number, tab?: 'pedigree' | 'health') {
  const anchor = tab === 'pedigree' ? '#tabSukupuu' : tab === 'health' ? '#tabTerveystulokset' : ''
  return `https://katt.nrr.no/Katter/perusnaytto_kissa.aspx?id=${nrrId}${anchor}`
}

/** Id-en fra en NRR-lenke («…perusnaytto_kissa.aspx?id=82430#…»), eller et rent tall. null hvis det ikke er noen. */
export function parseNrrId(text: string): number | null {
  const trimmed = text.trim()
  if (/^\d+$/.test(trimmed)) return Number(trimmed)
  const match = /[?&]id=(\d+)/.exec(trimmed)
  return match ? Number(match[1]) : null
}
