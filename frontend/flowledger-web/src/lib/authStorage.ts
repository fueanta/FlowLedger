import type { LoginResponse } from '../types'

const authStorageKey = 'flowledger.auth'

export function getStoredAuth(): LoginResponse | null {
  const raw = window.localStorage.getItem(authStorageKey)
  if (!raw) {
    return null
  }

  try {
    return JSON.parse(raw) as LoginResponse
  } catch {
    window.localStorage.removeItem(authStorageKey)
    return null
  }
}

export function setStoredAuth(auth: LoginResponse) {
  window.localStorage.setItem(authStorageKey, JSON.stringify(auth))
}

export function clearStoredAuth() {
  window.localStorage.removeItem(authStorageKey)
}
