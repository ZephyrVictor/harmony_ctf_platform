// Admin Ban Management API helper
// Base path: /api/admin/games/{gameId}/bans

const BASE = `${import.meta.env.BASE_URL.replace(/\/$/, '')}/api/admin`
const EDIT_BASE = `${import.meta.env.BASE_URL.replace(/\/$/, '')}/api/edit`

export interface BanStateDto {
  id: number
  participationId: number
  teamId: number
  challengeId: number
  trackId: string
  level: number
  banAnswer: string
  reason: string | null
  bannedBy: string | null
  isManual: boolean
  bannedAt: string
}

export interface BanCreateModel {
  teamId: number
  challengeId: number
  trackId: string
  level: number
  reason: string
}

export interface AlertNotice {
  id: number
  type: string
  values: string[] | null
  time: string
}

async function request<T>(url: string, options?: RequestInit): Promise<T> {
  const res = await fetch(url, {
    headers: { 'Content-Type': 'application/json' },
    ...options,
  })

  if (!res.ok) {
    const body = await res.json().catch(() => ({}))
    throw new Error(body.title || body.message || `HTTP ${res.status}`)
  }

  if (res.status === 204) return undefined as T
  const text = await res.text()
  return text ? JSON.parse(text) : (undefined as T)
}

export const listBans = (gameId: number, params: { teamId?: number; trackId?: string } = {}) => {
  const search = new URLSearchParams()
  if (params.teamId) search.set('teamId', String(params.teamId))
  if (params.trackId) search.set('trackId', params.trackId)
  return request<BanStateDto[]>(`${BASE}/games/${gameId}/bans?${search}`)
}

export const createBan = (gameId: number, model: BanCreateModel) =>
  request<BanStateDto>(`${BASE}/games/${gameId}/bans`, {
    method: 'POST',
    body: JSON.stringify(model),
  })

export const deleteBan = (gameId: number, banId: number) =>
  request<void>(`${BASE}/games/${gameId}/bans/${banId}`, {
    method: 'DELETE',
  })

export const listAlertNotices = (gameId: number) =>
  request<AlertNotice[]>(`${EDIT_BASE}/Games/${gameId}/AlertNotices`)
