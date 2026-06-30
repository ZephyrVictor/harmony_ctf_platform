const BASE = `${import.meta.env.BASE_URL.replace(/\/$/, '')}/api/ext`

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

async function requestForm<T>(url: string, formData: FormData): Promise<T> {
  const res = await fetch(`${BASE}${url}`, {
    method: 'POST',
    body: formData,
  })

  if (!res.ok) {
    const body = await res.json().catch(() => ({}))
    throw new Error(body.title || body.message || `HTTP ${res.status}`)
  }

  if (res.status === 204) return undefined as T
  const text = await res.text()
  return text ? JSON.parse(text) : (undefined as T)
}

// ---- Flag Management ----
// Flag CRUD helpers 已迁移到 Api.ts (api.ext.extGetFlags / extCreateFlag / extUpdateFlag / extDeleteFlag)
// ChallengeFlag / ChallengeFlagAnswerType / ChallengeFlagCreateModel / ChallengeFlagUpdateModel 接口也由 Api.ts 提供

// ---- Penalty Configuration ----

export interface ChallengePenalty {
  id: string
  challengeId: number
  penaltyScore: number
  createdAt: string
  updatedAt: string
}

export const getPenalty = (challengeId: number) => request<ChallengePenalty>(`/challenges/${challengeId}/penalty`)

export const updatePenalty = (challengeId: number, data: { penaltyScore: number }) =>
  request<ChallengePenalty>(`/challenges/${challengeId}/penalty`, {
    method: 'PUT',
    body: JSON.stringify(data),
  })

// ---- Competition Settings ----

export const getSettings = () => request<Record<string, string>>('/settings')

export const updateSettings = (data: Record<string, unknown>) =>
  request<void>('/settings', {
    method: 'PUT',
    body: JSON.stringify(data),
  })

// ---- Submission Records ----

export interface SubmissionRecordDto {
  id: string
  submissionId: number
  scoreChange: number
  isPenalty: boolean
  createdAt: string
  challengeName: string
  challengeId: number
  gameId: number
  answer?: string
  matchedFlagId?: string
  matchedFlagScore?: number
}

export const getUserSubmissions = (userId: string, challengeId?: number, count = 100, skip = 0, gameId?: number) => {
  const params = new URLSearchParams()
  if (challengeId) params.set('challengeId', String(challengeId))
  if (gameId) params.set('gameId', String(gameId))
  params.set('count', String(count))
  params.set('skip', String(skip))
  return request<SubmissionRecordDto[]>(`/submissions/user/${userId}?${params}`)
}

export const getMySubmissions = (challengeId?: number, count = 100, skip = 0, gameId?: number) => {
  const params = new URLSearchParams()
  if (challengeId) params.set('challengeId', String(challengeId))
  if (gameId) params.set('gameId', String(gameId))
  params.set('count', String(count))
  params.set('skip', String(skip))
  return request<SubmissionRecordDto[]>(`/submissions/mine?${params}`)
}

// ---- Ticket Management ----

export interface TicketDto {
  id: string
  description: string
  status: string
  reviewComment?: string
  refundScore: number
  bonusScore: number
  createdAt: string
  reviewedAt?: string
  submissionId: number
  gameId: number
  userName?: string
  reviewerName?: string
  challengeName?: string
  answer?: string
  penaltyScore: number
  attachments?: TicketAttachmentDto[]
}

export interface TicketAttachmentDto {
  id: string
  url: string
  fileName?: string
  filename?: string
  contentType?: string
  fileSize?: number
  sizeBytes?: number
  size?: number
  createdAt?: string
}

export const createTicket = (data: { submissionId: number; description: string; gameId?: number }) =>
  request<TicketDto>('/tickets', {
    method: 'POST',
    body: JSON.stringify(data),
  })

export const getMyTickets = (count = 100, skip = 0, gameId?: number) => {
  const params = new URLSearchParams()
  if (gameId) params.set('gameId', String(gameId))
  params.set('count', String(count))
  params.set('skip', String(skip))
  return request<TicketDto[]>(`/tickets/mine?${params}`)
}

export const getTicket = (ticketId: string) => request<TicketDto>(`/tickets/${ticketId}`)

export const getAllTickets = (status?: string, count = 100, skip = 0, gameId?: number) => {
  const params = new URLSearchParams()
  if (status) params.set('status', status)
  if (gameId) params.set('gameId', String(gameId))
  params.set('count', String(count))
  params.set('skip', String(skip))
  return request<TicketDto[]>(`/tickets?${params}`)
}

export const approveTicket = (ticketId: string, data: { refundScore: number; bonusScore: number; comment?: string }) =>
  request<TicketDto>(`/tickets/${ticketId}/approve`, {
    method: 'PUT',
    body: JSON.stringify(data),
  })

export const rejectTicket = (ticketId: string, data: { comment: string }) =>
  request<TicketDto>(`/tickets/${ticketId}/reject`, {
    method: 'PUT',
    body: JSON.stringify(data),
  })

export const uploadTicketAttachments = (ticketId: string, files: File[]) => {
  const formData = new FormData()
  files.forEach((file) => formData.append('files', file))
  return requestForm<TicketAttachmentDto[]>(`/tickets/${ticketId}/attachments`, formData)
}

// ---- BigScreen Events ----

export interface BigScreenEventDto {
  id: string
  eventType: string
  challengeId: number
  userId?: string
  teamName?: string
  challengeName?: string
  scoreChange?: number
  message: string
  isConsumed: boolean
  createdAt: string
}

export const getEvents = (unconsumedOnly = true, count = 100) => {
  const params = new URLSearchParams()
  params.set('unconsumedOnly', String(unconsumedOnly))
  params.set('count', String(count))
  return request<BigScreenEventDto[]>(`/events?${params}`)
}
