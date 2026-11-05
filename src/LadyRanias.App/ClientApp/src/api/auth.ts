import { useApi } from './api'
import type { Me } from '../types/types'

export type AuthState =
  | { kind: 'loading' }
  | { kind: 'anonymous' }
  | { kind: 'forbidden'; me: Me }
  | { kind: 'admin'; me: Me }
  | { kind: 'error'; message: string }

/** Hvem er innlogget? api/auth/me svarer 401 hvis ingen er det. */
export function useAuth(): AuthState {
  const me = useApi<Me>('/api/auth/me')

  if (me.loading) return { kind: 'loading' }
  if (me.status === 401) return { kind: 'anonymous' }
  if (me.error || !me.data) return { kind: 'error', message: me.error ?? 'Ukjent feil' }
  return me.data.isAdmin ? { kind: 'admin', me: me.data } : { kind: 'forbidden', me: me.data }
}

/**
 * Innlogging er en vanlig sidenavigasjon, ikke fetch: nettleseren må følge omdirigeringen til Entra og tilbake.
 */
export function login(returnUrl = '/admin') {
  window.location.href = `/api/auth/login?returnUrl=${encodeURIComponent(returnUrl)}`
}
