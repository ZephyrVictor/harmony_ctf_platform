import { ChallengeCategory, ParticipationStatus } from '@Api'

const BASE = `${import.meta.env.BASE_URL.replace(/\/$/, '')}/api/admin`

export type SubmissionControlLimitMode = 'InheritDefault' | 'Custom' | 'Unlimited'

export interface SubmissionControlLimit {
  mode: SubmissionControlLimitMode
  customValue: number
  defaultValue: number
  effectiveValue: number
}

export interface SubmissionControlGameConfig {
  gameId: number
  title: string
  dailyTeamSubmissionLimit: number
  cooldownMinutes: number
  challengeCooldownWrongAnswerLimitFactor: number
  challengeDailyTeamSubmissionLimitFactor: number
  challengeTotalTeamSubmissionLimitFactor: number
}

export interface SubmissionControlChallengeConfig {
  challengeId: number
  title: string
  category: ChallengeCategory
  isEnabled: boolean
  trackCount: number
  effectiveWrongAnswerLimit: number
  cooldownWrongAnswerLimitOverride: number
  dailyTeamSubmissionLimit: number
  totalTeamSubmissionLimit: number
  cooldownWrongAnswerLimit: SubmissionControlLimit
  dailyTeamSubmission: SubmissionControlLimit
  totalTeamSubmission: SubmissionControlLimit
}

export interface SubmissionControlTeamSummary {
  participationId: number
  teamId: number
  teamName: string
  participationStatus: ParticipationStatus
  todaySubmissions: number
  totalSubmissions: number
  acceptedSubmissions: number
  wrongSubmissions: number
  gameDailyLimit: number
  gameDailyRemaining: number
  gameDailyExceeded: boolean
  coolingChallenges: number
  blockedChallenges: number
  lastSubmissionAt?: string | number | null
}

export interface SubmissionControlTeamChallengeStatus {
  challengeId: number
  title: string
  category: ChallengeCategory
  todayTeamUsed: number
  totalTeamUsed: number
  dailyTeamLimit: number
  dailyTeamRemaining: number
  dailyTeamExceeded: boolean
  totalTeamLimit: number
  totalTeamRemaining: number
  totalTeamExceeded: boolean
  wrongCount: number
  effectiveWrongAnswerLimit: number
  cooldownWrongAnswerLimitOverride: number
  cooldownMinutes: number
  cooldownUntil?: string | number | null
  isCoolingDown: boolean
}

export interface SubmissionControlTeamDetail extends SubmissionControlTeamSummary {
  challenges: SubmissionControlTeamChallengeStatus[]
}

export interface SubmissionControlOverview {
  game: SubmissionControlGameConfig
  challenges: SubmissionControlChallengeConfig[]
  teams: SubmissionControlTeamSummary[]
  dailyResetAt: string | number
}

export interface SubmissionControlChallengeUpdate {
  challengeId: number
  dailyTeamSubmissionLimit: number
  totalTeamSubmissionLimit: number
  cooldownWrongAnswerLimitOverride: number
  cooldownWrongAnswerLimitMode: SubmissionControlLimitMode
  dailyTeamSubmissionLimitMode: SubmissionControlLimitMode
  totalTeamSubmissionLimitMode: SubmissionControlLimitMode
}

export interface SubmissionControlUpdate {
  dailyTeamSubmissionLimit: number
  cooldownMinutes: number
  challengeCooldownWrongAnswerLimitFactor: number
  challengeDailyTeamSubmissionLimitFactor: number
  challengeTotalTeamSubmissionLimitFactor: number
  applyDefaultsToAllChallenges: boolean
  challenges: SubmissionControlChallengeUpdate[]
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

export const getSubmissionControl = (gameId: number) =>
  request<SubmissionControlOverview>(`/games/${gameId}/submission-control`)

export const updateSubmissionControl = (gameId: number, model: SubmissionControlUpdate) =>
  request<SubmissionControlOverview>(`/games/${gameId}/submission-control`, {
    method: 'PUT',
    body: JSON.stringify(model),
  })

export const getSubmissionControlTeamDetail = (gameId: number, teamId: number) =>
  request<SubmissionControlTeamDetail>(`/games/${gameId}/submission-control/teams/${teamId}`)
