import type { CatImage } from '../types/types'

const dayMs = 24 * 60 * 60 * 1000

/** Hele dager mellom to datoer på formen «2026-03-05». UTC, så sommertid ikke gir en dag for mye eller for lite. */
function daysBetween(from: string, to: string) {
  return Math.round((Date.parse(`${to}T00:00:00Z`) - Date.parse(`${from}T00:00:00Z`)) / dayMs)
}

/** Hvor mange hele uker gammel kattungen var. Negativ hvis bildet er tatt før fødselen. */
export function ageInWeeks(birthDate: string, takenAt: string) {
  return Math.floor(daysBetween(birthDate, takenAt) / 7)
}

/** «Uke 5», slik oppdrettere skriver det. Den første uka heter «Nyfødt». */
export function weekLabel(weeks: number) {
  if (weeks < 0) return 'Før fødselen'
  if (weeks === 0) return 'Nyfødt'
  return `Uke ${weeks}`
}

/** Datoen da kattungen var akkurat så mange uker. Brukes når mamma retter uka på et bilde. */
export function dateAtWeek(birthDate: string, weeks: number) {
  return new Date(Date.parse(`${birthDate}T00:00:00Z`) + weeks * 7 * dayMs).toISOString().slice(0, 10)
}

export type WeekGroup = { weeks: number; label: string; images: CatImage[] }

/**
 * Bildene gruppert per uke, nyeste uke først (bildene kommer sortert fra API-et).
 * Bilder tatt etter et halvt år er ikke kattungebilder lenger, så de havner i en egen gruppe uten ukenummer.
 */
export function groupByWeek(images: CatImage[], birthDate: string): WeekGroup[] {
  const groups: WeekGroup[] = []
  for (const image of images) {
    const weeks = Math.min(ageInWeeks(birthDate, image.takenAt), 27)
    const label = weeks > 26 ? 'Voksen' : weekLabel(weeks)
    const last = groups.at(-1)
    if (last && last.label === label) {
      last.images.push(image)
    } else {
      groups.push({ weeks, label, images: [image] })
    }
  }
  return groups
}
