import { useEffect, useState } from 'react'
import type { ImageSize } from '../types/types'

/** Feil fra API-et, med HTTP-statuskoden, så sidene kan skille «finnes ikke» (404) fra andre feil. */
export class ApiError extends Error {
  readonly status: number

  constructor(status: number) {
    super(status === 404 ? 'Fant ikke det du lette etter.' : `Serveren svarte med feil (${status}).`)
    this.status = status
  }
}

export async function getJson<T>(url: string, signal?: AbortSignal): Promise<T> {
  const response = await fetch(url, { signal, headers: { Accept: 'application/json' } })
  if (!response.ok) {
    throw new ApiError(response.status)
  }
  return (await response.json()) as T
}

export function catImageUrl(catId: string, imageId: string, size: ImageSize) {
  return `/api/public/cats/${catId}/images/${imageId}?size=${size}`
}

type State<T> = {
  url: string | null
  data: T | null
  error: string | null
  status: number | null
}

/**
 * Henter JSON fra en URL og gir tilbake data, lasting og feil.
 * Send inn null for å vente (for eksempel til en annen henting er ferdig).
 *
 * Vi husker hvilken URL svaret gjelder. Da kan «loading» regnes ut i stedet for å lagres,
 * og vi slipper å kalle setState direkte i useEffect.
 */
export function useApi<T>(url: string | null) {
  const [state, setState] = useState<State<T>>({ url: null, data: null, error: null, status: null })

  useEffect(() => {
    if (url === null) return

    // Avbryt hentingen hvis komponenten forsvinner eller URL-en endres før svaret kommer.
    const controller = new AbortController()

    getJson<T>(url, controller.signal)
      .then(data => setState({ url, data, error: null, status: 200 }))
      .catch((error: unknown) => {
        if (controller.signal.aborted) return
        setState({
          url,
          data: null,
          error: error instanceof Error ? error.message : 'Ukjent feil',
          status: error instanceof ApiError ? error.status : null,
        })
      })

    return () => controller.abort()
  }, [url])

  const isCurrent = url !== null && state.url === url

  return {
    data: isCurrent ? state.data : null,
    error: isCurrent ? state.error : null,
    notFound: isCurrent && state.status === 404,
    loading: url !== null && !isCurrent,
  }
}
