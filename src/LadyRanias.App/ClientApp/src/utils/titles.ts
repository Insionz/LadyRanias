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
  /** Hvilken stige champion-tittelen hører til. Bare den høyeste i hver stige vises. */
  track?: Track
}

/**
 * Kattunge-, junior- og voksentitlene er hver sin stige (UR § 4.1–4.5). En katt beholder titlene sine, men i hver
 * stige vises bare den høyeste: en katt som er CH, IC og GIC, er «GIC». Hel og kastrert katt har hver sin voksenstige,
 * så en katt som ble GIC og så kastrert og IP, har begge.
 */
type Track = 'kitten' | 'junior' | 'entire' | 'neutered'

/** Rekkefølgen stigene står i foran navnet: voksentitlene først, så junior, så kattunge («GIC JCH KCH»). */
const trackOrder: Track[] = ['entire', 'neutered', 'junior', 'kitten']

type Known = { kind: TitleKind; level?: number; name: string; after?: boolean; track?: Track }

const known: Record<string, Known> = {
  KCH: { kind: 'champion', level: 1, track: 'kitten', name: 'Kitten Champion' },
  JCH: { kind: 'champion', level: 2, track: 'junior', name: 'Junior Champion' },
  CH: { kind: 'champion', level: 3, track: 'entire', name: 'Champion' },
  PR: { kind: 'champion', level: 3, track: 'neutered', name: 'Premior' },
  IC: { kind: 'champion', level: 4, track: 'entire', name: 'International Champion' },
  IP: { kind: 'champion', level: 4, track: 'neutered', name: 'International Premior' },
  GIC: { kind: 'champion', level: 5, track: 'entire', name: 'Grand International Champion' },
  GIP: { kind: 'champion', level: 5, track: 'neutered', name: 'Grand International Premior' },
  SC: { kind: 'champion', level: 6, track: 'entire', name: 'Supreme Champion' },
  SP: { kind: 'champion', level: 6, track: 'neutered', name: 'Supreme Premior' },
  EC: { kind: 'champion', level: 6, track: 'entire', name: 'Europa Champion (tidligere toppnivå, i dag Supreme Champion)' },
  EP: { kind: 'champion', level: 6, track: 'neutered', name: 'Europa Premior (tidligere toppnivå, i dag Supreme Premior)' },

  // Vinnertitler med år, foran navnet (UR § 2.4, 2.6.5, 4.17, 4.19)
  NW: { kind: 'winner', name: 'National Winner' },
  IW: { kind: 'winner', name: 'International Winner' },
  AW: { kind: 'winner', name: 'American Winner' },
  BW: { kind: 'winner', name: 'Baltic Winner' },
  BSW: { kind: 'winner', name: 'Black Sea Winner' },
  CEW: { kind: 'winner', name: 'Central European Winner' },
  EAW: { kind: 'winner', name: 'Eurasian Winner' },
  MW: { kind: 'winner', name: 'Mediterranean Winner' },
  NSW: { kind: 'winner', name: 'North Sea Winner' },
  SW: { kind: 'winner', name: 'Scandinavian Winner' },
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
  return {
    text: `${code}${year ?? ''}`,
    code,
    year,
    kind: info.kind,
    level: info.level ?? 0,
    name,
    after: info.after ?? false,
    track: info.track,
  }
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
  // Bare den høyeste tittelen i hver stige, i rekkefølgen voksen, junior, kattunge.
  const champions = trackOrder
    .map(track => parsed.filter(t => t.track === track).sort((a, b) => b.level - a.level)[0])
    .filter((t): t is Title => t !== undefined)
  const unknown = parsed.filter(t => t.kind === 'unknown')
  const after = parsed.filter(t => t.after).sort((a, b) => afterOrder.indexOf(a.code) - afterOrder.indexOf(b.code))

  return { before: [...winners, ...champions, ...unknown], after }
}

/** «WW'24 SC NO*LadyRania's Kiara JW DSM», slik FIFe skriver det. */
export function fullName(name: string, titles?: string | null) {
  const { before, after } = parseTitles(titles)
  return [...before.map(t => t.text), name, ...after.map(t => t.text)].join(' ')
}

/** Den høyeste champion-tittelen (voksen før junior før kattunge), til kattekortet. Ellers den første andre kjente. */
export function highestTitle(titles?: string | null): Title | null {
  const { before, after } = parseTitles(titles)
  const champion = before.filter(t => t.kind === 'champion').sort((a, b) => b.level - a.level)[0]
  return champion ?? [...before, ...after].find(t => t.kind !== 'unknown') ?? null
}
