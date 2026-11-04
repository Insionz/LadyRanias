import type { KittenStatus, Sex } from '../types/types'

export function sexLabel(sex: Sex) {
  switch (sex) {
    case 'Female':
      return 'Hunnkatt'
    case 'Male':
      return 'Hannkatt'
    default:
      return 'Ukjent kjønn'
  }
}

/** «2024-06-01» tolkes som lokal dato, ikke UTC, så datoen ikke kan forskyve seg en dag. */
function parseDate(isoDate: string) {
  const [year, month, day] = isoDate.split('-').map(Number)
  return new Date(year, month - 1, day)
}

const dateFormat = new Intl.DateTimeFormat('nb-NO', { dateStyle: 'long' })

/** «1. juni 2024» */
export function formatDate(isoDate: string) {
  return dateFormat.format(parseDate(isoDate))
}

/** «3 uker», «5 måneder», «2 år» */
export function ageLabel(isoDate: string, today: Date = new Date()) {
  const born = parseDate(isoDate)

  let months = (today.getFullYear() - born.getFullYear()) * 12 + (today.getMonth() - born.getMonth())
  if (today.getDate() < born.getDate()) months--

  if (months < 1) {
    const weeks = Math.max(0, Math.floor((today.getTime() - born.getTime()) / (7 * 24 * 60 * 60 * 1000)))
    return weeks === 1 ? '1 uke' : `${weeks} uker`
  }
  if (months < 24) {
    return months === 1 ? '1 måned' : `${months} måneder`
  }
  return `${Math.floor(months / 12)} år`
}

/** Tekst og farge på merket for en kattunge. Fargene er Mantine-farger. */
export function kittenStatusBadge(status: KittenStatus): { label: string; color: string } {
  switch (status) {
    case 'Available':
      return { label: 'Ledig', color: 'green' }
    case 'Reserved':
      return { label: 'Reservert', color: 'yellow' }
    case 'Sold':
      return { label: 'Solgt', color: 'gray' }
    case 'Kept':
      return { label: 'Blir hos oss', color: 'grape' }
  }
}

const monthFormat = new Intl.DateTimeFormat('nb-NO', { month: 'long', year: 'numeric' })

/** «desember 2026». Et planlagt kull har sjelden en nøyaktig dato. */
export function formatMonth(isoDate: string) {
  return monthFormat.format(parseDate(isoDate))
}
