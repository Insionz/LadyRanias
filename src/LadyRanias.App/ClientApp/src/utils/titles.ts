/**
 * Utstillingstitler etter FIFe. Kilder: FIFe show titles (2024), NRR «Titler man kan oppnå», og rasekatter.no (2010)
 * for de gamle titlene EC og EP.
 *
 * Hvor titlene står (FIFe):
 *   - Champion- og Premior-titler står foran navnet.
 *   - Vinnertitler med år står foran navnet, og foran champion-titlene: «WW'24 SC Kiara».
 *   - JW og DSW står etter navnet, og det gjør merittitlene DM, DSM og DVM også.
 * Mamma skriver titlene i ett felt, i hvilken rekkefølge hun vil. Her sorteres de på riktig plass.
 */

export type TitleKind = 'champion' | 'winner' | 'merit' | 'offspring' | 'unknown'

export type Title = {
  /** Slik den står i navnet: «GIC», «WW'24» */
  text: string
  /** Uten år: «WW» */
  code: string
  /** «'24», hvis tittelen har et år */
  year: string | null
  kind: TitleKind
  /** Champion-stigen: 1 = KCH … 6 = SC/SP/EC/EP. 0 for de andre. */
  level: number
  /** Fullt navn, til musepekeren og skjermlesere */
  name: string
  /** Står tittelen etter navnet? */
  after: boolean
}

type Known = { kind: TitleKind; level?: number; name: string; after?: boolean }

const known: Record<string, Known> = {
  KCH: { kind: 'champion', level: 1, name: 'Kitten Champion' },
  JCH: { kind: 'champion', level: 2, name: 'Junior Champion' },
  CH: { kind: 'champion', level: 3, name: 'Champion' },
  PR: { kind: 'champion', level: 3, name: 'Premior' },
  IC: { kind: 'champion', level: 4, name: 'International Champion' },
  IP: { kind: 'champion', level: 4, name: 'International Premior' },
  GIC: { kind: 'champion', level: 5, name: 'Grand International Champion' },
  GIP: { kind: 'champion', level: 5, name: 'Grand International Premior' },
  SC: { kind: 'champion', level: 6, name: 'Supreme Champion' },
  SP: { kind: 'champion', level: 6, name: 'Supreme Premior' },
  EC: { kind: 'champion', level: 6, name: 'Europa Champion (tidligere toppnivå, i dag Supreme Champion)' },
  EP: { kind: 'champion', level: 6, name: 'Europa Premior (tidligere toppnivå, i dag Supreme Premior)' },

  NW: { kind: 'winner', name: 'National Winner' },
  SW: { kind: 'winner', name: 'Scandinavian Winner' },
  NSW: { kind: 'winner', name: 'North Sea Winner' },
  MW: { kind: 'winner', name: 'Winner' },
  WW: { kind: 'winner', name: 'World Winner' },

  JW: { kind: 'merit', name: 'Junior Winner', after: true },
  DSW: { kind: 'merit', name: 'Distinguished Senior Winner', after: true },
  DSM: { kind: 'merit', name: 'Distinguished Show Merit', after: true },
  DVM: { kind: 'merit', name: 'Distinguished Variety Merit', after: true },
  DM: { kind: 'offspring', name: 'Distinguished Merit (for avkommet sitt)', after: true },
}

/** Alle kjente titler, i stigen først og så de andre. Til nedtrekkslista i admin. */
export const knownTitleCodes = Object.keys(known)

/**
 * Andre skrivemåter som betyr det samme. NRR-siden skriver KC og JC, men titlene forkortes KCH og JCH.
 * Står det KC i en katt, vises det som KCH.
 */
const aliases: Record<string, string> = { KC: 'KCH', JC: 'JCH' }

/** Rekkefølgen etter navnet: JW og DSW først, så meritter. */
const afterOrder = ['JW', 'DSW', 'DSM', 'DVM', 'DM']

/** «WW'24», «WW’24», «WW24», «WW 2024» → kode og år. */
const withYear = /^([A-Z]+)(?:['’]?(\d{2}|\d{4}))?$/

export function parseTitle(raw: string): Title {
  const upper = raw.toUpperCase()
  const match = withYear.exec(upper)
  const code = aliases[match?.[1] ?? upper] ?? match?.[1] ?? upper
  const yearDigits = match?.[2]
  const info = known[code]

  // Et år hører bare til vinnertitler. «IC24» er ikke en kjent tittel.
  if (!info || (yearDigits && info.kind !== 'winner')) {
    return { text: raw, code: raw, year: null, kind: 'unknown', level: 0, name: raw, after: false }
  }

  const year = yearDigits ? `'${yearDigits.slice(-2)}` : null
  const name = year ? `${info.name} ${yearDigits!.length === 4 ? yearDigits : `20${yearDigits}`}` : info.name
  return { text: `${code}${year ?? ''}`, code, year, kind: info.kind, level: info.level ?? 0, name, after: info.after ?? false }
}

/** Alle titlene i feltet, i den rekkefølgen de skal stå. «WW 2024» blir én tittel, ikke to. */
export function parseTitles(titles?: string | null): { before: Title[]; after: Title[] } {
  const words = (titles ?? '').split(/[\s,]+/).filter(Boolean)

  // Slå sammen «WW» + «2024» (et år skrevet med mellomrom).
  const tokens: string[] = []
  for (const word of words) {
    if (/^['’]?\d{2,4}$/.test(word) && tokens.length > 0) {
      tokens[tokens.length - 1] += word.replace(/^['’]/, "'")
    } else {
      tokens.push(word)
    }
  }

  const parsed = tokens.map(parseTitle)
  const winners = parsed.filter(t => t.kind === 'winner').sort((a, b) => (b.year ?? '').localeCompare(a.year ?? ''))
  const champions = parsed.filter(t => t.kind === 'champion').sort((a, b) => b.level - a.level)
  const unknown = parsed.filter(t => t.kind === 'unknown')
  const after = parsed.filter(t => t.after).sort((a, b) => afterOrder.indexOf(a.code) - afterOrder.indexOf(b.code))

  return { before: [...winners, ...champions, ...unknown], after }
}

/** «WW'24 SC NO*LadyRania's Kiara JW DSM», slik FIFe skriver det. */
export function fullName(name: string, titles?: string | null) {
  const { before, after } = parseTitles(titles)
  return [...before.map(t => t.text), name, ...after.map(t => t.text)].join(' ')
}

/** Den høyeste champion-tittelen, til kattekortet. Ingen champion-tittel: den første andre kjente tittelen. */
export function highestTitle(titles?: string | null): Title | null {
  const { before, after } = parseTitles(titles)
  const champion = before.filter(t => t.kind === 'champion').sort((a, b) => b.level - a.level)[0]
  return champion ?? [...before, ...after].find(t => t.kind !== 'unknown') ?? null
}
