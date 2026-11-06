import { useCallback, useEffect, useState } from 'react'
import type { ImageSize } from '../types/types'

/** Feil fra API-et, med HTTP-statuskoden, så sidene kan skille 401, 403, 404 og andre feil. */
export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message?: string) {
    super(message ?? defaultMessage(status))
    this.status = status
  }
}

function defaultMessage(status: number) {
  switch (status) {
    case 401:
      return 'Du er ikke logget inn.'
    case 403:
      return 'Du har ikke tilgang til dette.'
    case 404:
      return 'Fant ikke det du lette etter.'
    case 429:
      return 'Du har sendt mange meldinger på kort tid. Prøv igjen om en time.'
    default:
      return `Serveren svarte med feil (${status}).`
  }
}

/**
 * Feilsvar fra ASP.NET Core er «ProblemDetails»: { title, detail, errors: { Felt: ["melding"] } }.
 * Vi plukker ut den mest nyttige teksten, så brukeren ser hva som var galt.
 */
async function errorFrom(response: Response): Promise<ApiError> {
  try {
    const problem = (await response.json()) as {
      title?: string
      detail?: string
      errors?: Record<string, string[]>
    }
    const fieldErrors = problem.errors ? Object.values(problem.errors).flat().join(' ') : ''
    return new ApiError(response.status, problem.detail || fieldErrors || problem.title || undefined)
  } catch {
    return new ApiError(response.status)
  }
}

export async function getJson<T>(url: string, signal?: AbortSignal): Promise<T> {
  const response = await fetch(url, { signal, headers: { Accept: 'application/json' } })
  if (!response.ok) {
    throw await errorFrom(response)
  }
  return (await response.json()) as T
}

/** POST, PUT og DELETE. Gir tilbake JSON-svaret, eller null hvis svaret er tomt (for eksempel 202 eller 204). */
export async function send<T = void>(method: 'POST' | 'PUT' | 'DELETE', url: string, body?: unknown): Promise<T | null> {
  const response = await fetch(url, {
    method,
    headers: body === undefined ? { Accept: 'application/json' } : { Accept: 'application/json', 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  })
  if (!response.ok) {
    throw await errorFrom(response)
  }
  // Les som tekst først: flere svar har ingen kropp (202 Accepted, 204 No Content),
  // og response.json() feiler på en tom kropp.
  const text = await response.text()
  return text ? (JSON.parse(text) as T) : null
}

/** Laster opp en fil som multipart/form-data. Feltnavnet må være «formFile», som i controlleren. */
export async function uploadFile<T>(url: string, file: File): Promise<T> {
  const form = new FormData()
  form.append('formFile', file)

  // Ikke sett Content-Type selv: nettleseren legger til «boundary» som skiller delene i skjemaet.
  const response = await fetch(url, { method: 'POST', body: form })
  if (!response.ok) {
    throw await errorFrom(response)
  }
  return (await response.json()) as T
}

/** Hvem bildet tilhører: en katt (også kattunger) eller et helt kull. Adressene er ellers like. */
export type ImageOwner = 'cats' | 'litters'

export function imageUrl(owner: ImageOwner, ownerId: string, imageId: string, size: ImageSize) {
  return `/api/public/${owner}/${ownerId}/images/${imageId}?size=${size}`
}

export function catImageUrl(catId: string, imageId: string, size: ImageSize) {
  return imageUrl('cats', catId, imageId, size)
}

type State<T> = {
  key: string | null
  data: T | null
  error: string | null
  status: number | null
}

/**
 * Henter JSON fra en URL og gir tilbake data, lasting og feil.
 * Send inn null for å vente (for eksempel til en annen henting er ferdig).
 * reload() henter på nytt, for eksempel etter at noe er lagret.
 *
 * Vi husker hvilken URL (og hvilken «runde») svaret gjelder. Da kan «loading» regnes ut i stedet for å lagres,
 * og vi slipper å kalle setState direkte i useEffect.
 */
export function useApi<T>(url: string | null) {
  const [version, setVersion] = useState(0)
  const [state, setState] = useState<State<T>>({ key: null, data: null, error: null, status: null })

  const key = url === null ? null : `${url}#${version}`

  useEffect(() => {
    if (url === null || key === null) return

    // Avbryt hentingen hvis komponenten forsvinner eller URL-en endres før svaret kommer.
    const controller = new AbortController()

    getJson<T>(url, controller.signal)
      .then(data => setState({ key, data, error: null, status: 200 }))
      .catch((error: unknown) => {
        if (controller.signal.aborted) return
        setState({
          key,
          data: null,
          error: error instanceof Error ? error.message : 'Ukjent feil',
          status: error instanceof ApiError ? error.status : null,
        })
      })

    return () => controller.abort()
  }, [url, key])

  const reload = useCallback(() => setVersion(v => v + 1), [])

  const isCurrent = key !== null && state.key === key
  // Under en reload viser vi forrige data i stedet for å blinke til «laster».
  const hasPrevious = state.key !== null && state.key.startsWith(`${url}#`)

  return {
    data: isCurrent || hasPrevious ? state.data : null,
    error: isCurrent ? state.error : null,
    status: isCurrent ? state.status : null,
    notFound: isCurrent && state.status === 404,
    loading: key !== null && !isCurrent && !hasPrevious,
    reload,
  }
}
