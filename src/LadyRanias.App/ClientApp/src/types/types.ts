// Speiler DTO-ene fra API-et. Datoer kommer som tekst i JSON.

export type Sex = 'Male' | 'Female' | 'Unknown'

/** Unknown = katten ble lagt inn før rasen fantes, og mangler den. */
export type Breed = 'Unknown' | 'MaineCoon' | 'SacredBirman'

/** Kitten lages bare via kullet. External = fra et annet oppdrett, for eksempel en lånt hannkatt. */
export type CatRole = 'Breeding' | 'Retired' | 'Kitten' | 'External'

export type Cat = {
  id: string
  name: string
  sex: Sex
  birthDate: string // «2024-06-01»
  description: string
  createdAt: string
  breed: Breed
  role: CatRole
  emsCode?: string | null // «MCO n 22»
  color?: string | null
  neutered: boolean
  registrationNumber?: string | null
  titles?: string | null
  deceased: boolean // død, med eller uten dato
  deathDate?: string | null // ofte ukjent for de gamle kattene
  nrrId?: number | null // id-en hos NRR (katt.nrr.no), for lenke til stamtavle og helseresultater
  // Mor og far for en kjøpt katt: en katt i systemet, eller bare et navn. Født her: se kullet.
  motherId?: string | null
  motherName?: string | null
  fatherId?: string | null
  fatherName?: string | null
  litterId?: string | null // satt = født her, i dette kullet
  saleStatus?: KittenStatus | null // bare for kattunger
}

/** Hvilket bilde forsiden viser. Newest = det nyeste bildet av den nyeste katten (som før). */
export type HeroMode = 'Newest' | 'Fixed' | 'Random' | 'Slideshow'

/** Det mamma styrer selv: velkomstteksten, «Om oss» og forsidebildet. */
export type SiteSettings = {
  welcomeText: string
  aboutText: string
  heroMode: HeroMode
  heroImageId?: string | null
}

/** Hva et innlegg i nyhetene handler om. */
export type PostKind = 'Other' | 'Show' | 'NewCat' | 'Title' | 'Litter'

export type Post = {
  id: string
  title: string
  body: string // vanlig tekst, tom linje = nytt avsnitt
  date: string // dagen det skjedde
  kind: PostKind
  catIds: string[]
  published: boolean
  createdAt: string
}

export type CatImage = {
  imageId: string
  createdAt: string
  takenAt: string // «2026-03-05». Fra kameraet, ellers opplastingsdagen. Nyeste først fra API-et.
}

export type ImageSize = 'thumb' | 'large'

export type LitterStatus = 'Planned' | 'Born'

/** UnderEvaluation: ikke bestemt ennå om den beholdes eller selges. */
export type KittenStatus = 'Available' | 'Reserved' | 'Sold' | 'Kept' | 'UnderEvaluation'

export type Kitten = {
  id: string
  litterId: string
  name: string
  sex: Sex
  status: KittenStatus
  description: string
  createdAt: string
  breed: Breed
  emsCode?: string | null
  color?: string | null
  /** Kitten, eller Breeding/Retired for en beholdt kattunge som nå er voksen (koblet til kullet). */
  role: CatRole
  deceased: boolean
  deathDate?: string | null
}

/** Mor eller far, slik kullet viser dem. Eksterne katter (lånte hannkatter) lenkes ikke til. */
export type Parent = {
  id: string
  name: string
  breed: Breed
  emsCode?: string | null
  titles?: string | null
  isExternal: boolean
}

export type Litter = {
  id: string
  name: string
  motherId: string
  fatherId: string
  status: LitterStatus
  expectedDate: string | null
  birthDate: string | null
  description: string
  createdAt: string
  kittens: Kitten[]
  mother?: Parent | null // null hvis katten er slettet
  father?: Parent | null
}

/** Svar fra api/auth/me */
export type Me = {
  name: string | null
  email: string | null
  isAdmin: boolean
}

export type ContactStatus = 'New' | 'Read' | 'Answered'

export type ContactMessage = {
  id: string
  name: string
  email: string
  phone: string | null
  subject: string | null
  message: string
  status: ContactStatus
  emailSentAt: string | null
  createdAt: string
}
