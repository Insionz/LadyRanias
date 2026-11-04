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
