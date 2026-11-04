// Speiler DTO-ene fra API-et. Datoer kommer som tekst i JSON.

export type Sex = 'Male' | 'Female' | 'Unknown'

export type Cat = {
  id: string
  name: string
  sex: Sex
  birthDate: string // «2024-06-01»
  description: string
  createdAt: string
}

export type CatImage = {
  imageId: string
  createdAt: string
}

export type ImageSize = 'thumb' | 'large'

export type LitterStatus = 'Planned' | 'Born'

export type KittenStatus = 'Available' | 'Reserved' | 'Sold' | 'Kept'

export type Kitten = {
  id: string
  litterId: string
  name: string
  sex: Sex
  status: KittenStatus
  description: string
  createdAt: string
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
}
