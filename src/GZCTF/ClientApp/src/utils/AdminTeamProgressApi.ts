import { AnswerResult, ChallengeCategory, ParticipationStatus } from '@Api'
import { ZeroDaySeverity } from '@Utils/AdminScoreApi'

const BASE = `${import.meta.env.BASE_URL.replace(/\/$/, '')}/api/admin`

export interface TeamProgressSummaryDto {
  participationId: number
  teamId: number
  teamName: string
  teamToken: string
  participationStatus: ParticipationStatus
  members: string[]
  totalSubmissions: number
  acceptedSubmissions: number
  wrongSubmissions: number
  penaltySubmissions: number
  solvedLevels: number
  totalLevels: number
  completedTracks: number
  totalTracks: number
  zeroDayApprovedCount: number
  zeroDayScore: number
  lastSubmissionAt?: string | number | null
}

export interface TeamProgressDetailDto extends TeamProgressSummaryDto {
  challenges: ChallengeProgressDto[]
  submissions: TeamSubmissionDto[]
  zeroDayBonuses: ZeroDayBonusBriefDto[]
}

export interface ChallengeProgressDto {
  challengeId: number
  challengeTitle: string
  category: ChallengeCategory
  tracks: TrackProgressDto[]
}

export interface TrackProgressDto {
  trackId: string
  title?: string | null
  repository?: string | null
  module?: string | null
  filePath?: string | null
  category?: string | null
  isEnabled: boolean
  levels: LevelProgressDto[]
}

export interface LevelProgressDto {
  level: number
  title?: string | null
  score: number
  status: 'Solved' | 'Unsolved'
  solvedAt?: string | number | null
  acceptedSubmissionId?: number | null
  acceptedAnswer?: string | null
  attempts: number
  wrongAttempts: number
  latestSubmissionAt?: string | number | null
}

export interface TeamSubmissionDto {
  submissionId: number
  submittedAt: string | number
  userName: string
  challengeId: number
  challengeTitle: string
  trackId?: string | null
  level?: number | null
  answer: string
  status: AnswerResult
  submissionType?: string | null
  scoreChange: number
  isPenalty: boolean
}

export interface ZeroDayBonusBriefDto {
  id: string
  severity?: ZeroDaySeverity | null
  delta: number
  reason: string
  relatedInfo?: string | null
  createdAt: string | number
}

export interface TeamProgressListResponse {
  data: TeamProgressSummaryDto[]
  length: number
  total: number
}

export interface TeamProgressListParams {
  page?: number
  pageSize?: number
  q?: string
  status?: ParticipationStatus | ''
  teamId?: number
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

  const text = await res.text()
  return text ? JSON.parse(text) : (undefined as T)
}

export const listTeamProgress = (gameId: number, params: TeamProgressListParams = {}) => {
  const search = new URLSearchParams()
  if (params.page) search.set('page', String(params.page))
  if (params.pageSize) search.set('pageSize', String(params.pageSize))
  if (params.q?.trim()) search.set('q', params.q.trim())
  if (params.status) search.set('status', params.status)
  if (params.teamId) search.set('teamId', String(params.teamId))

  return request<TeamProgressListResponse>(`/games/${gameId}/team-progress?${search}`)
}

export const getTeamProgressDetail = (gameId: number, teamId: number) =>
  request<TeamProgressDetailDto>(`/games/${gameId}/team-progress/${teamId}`)
