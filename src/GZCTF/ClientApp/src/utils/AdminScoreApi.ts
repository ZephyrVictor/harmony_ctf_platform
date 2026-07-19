// Admin Score Adjustment API helper
// Base path: /api/admin/games/{gameId}/score-adjustments

const BASE = `${import.meta.env.BASE_URL.replace(/\/$/, '')}/api/admin`

export interface ScoreAdjustmentDto {
  id: string
  gameId: number
  teamId: number
  delta: number
  reason: string
  category: 'Manual' | 'ZeroDayBonus'
  relatedInfo: string | null
  operatorId: string
  createdAt: string
}

export interface TeamSearchResult {
  teamId: number
  teamName: string
  teamToken: string
  members: string[]
  ndayScore: number
  zeroDayNdayScoreRequirement: number
  canReceiveZeroDayBonus: boolean
}

export interface ScoreAdjustmentListResponse {
  data: ScoreAdjustmentDto[]
  length: number
  total: number
}

export interface ScoreAdjustmentCreateModel {
  teamId: number
  delta: number
  reason: string
  category: 'Manual' | 'ZeroDayBonus'
  relatedInfo?: string | null
}

async function request<T>(url: string, options?: RequestInit): Promise<T> {
  const res = await fetch(`${BASE}${url}`, {
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

// ---- Score Adjustment ----

export const createScoreAdjustment = (gameId: number, model: ScoreAdjustmentCreateModel) =>
  request<ScoreAdjustmentDto>(`/games/${gameId}/score-adjustments`, {
    method: 'POST',
    body: JSON.stringify(model),
  })

export const listScoreAdjustments = (
  gameId: number,
  params: { page?: number; pageSize?: number; category?: 'Manual' | 'ZeroDayBonus'; teamId?: number } = {}
) => {
  const search = new URLSearchParams()
  if (params.page) search.set('page', String(params.page))
  if (params.pageSize) search.set('pageSize', String(params.pageSize))
  if (params.category) search.set('category', params.category)
  if (params.teamId) search.set('teamId', String(params.teamId))
  return request<ScoreAdjustmentListResponse>(
    `/games/${gameId}/score-adjustments?${search}`
  )
}

export const searchTeams = (gameId: number, q: string) => {
  const search = new URLSearchParams({ q })
  return request<TeamSearchResult[]>(`/games/${gameId}/teams/search?${search}`)
}
