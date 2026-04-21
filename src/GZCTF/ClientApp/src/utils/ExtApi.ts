const BASE = '/api/ext';

async function request<T>(url: string, options?: RequestInit): Promise<T> {
  const res = await fetch(`${BASE}${url}`, {
    headers: { 'Content-Type': 'application/json' },
    ...options,
  });

  if (!res.ok) {
    const body = await res.json().catch(() => ({}));
    throw new Error(body.title || body.message || `HTTP ${res.status}`);
  }

  if (res.status === 204) return undefined as T;
  return res.json();
}

// ---- Flag Management ----

export interface ChallengeFlag {
  id: string;
  challengeId: number;
  flagValue: string;
  score: number;
  sortOrder: number;
  createdAt: string;
  updatedAt: string;
}

export const getFlags = (challengeId: number) =>
  request<ChallengeFlag[]>(`/challenges/${challengeId}/flags`);

export const createFlag = (challengeId: number, data: { flagValue: string; score: number; sortOrder: number }) =>
  request<ChallengeFlag>(`/challenges/${challengeId}/flags`, {
    method: 'POST',
    body: JSON.stringify(data),
  });

export const updateFlag = (challengeId: number, flagId: string, data: { flagValue?: string; score?: number; sortOrder?: number }) =>
  request<ChallengeFlag>(`/challenges/${challengeId}/flags/${flagId}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  });

export const deleteFlag = (challengeId: number, flagId: string) =>
  request<void>(`/challenges/${challengeId}/flags/${flagId}`, { method: 'DELETE' });

// ---- Penalty Configuration ----

export interface ChallengePenalty {
  id: string;
  challengeId: number;
  penaltyScore: number;
  createdAt: string;
  updatedAt: string;
}

export const getPenalty = (challengeId: number) =>
  request<ChallengePenalty>(`/challenges/${challengeId}/penalty`);

export const updatePenalty = (challengeId: number, data: { penaltyScore: number }) =>
  request<ChallengePenalty>(`/challenges/${challengeId}/penalty`, {
    method: 'PUT',
    body: JSON.stringify(data),
  });

// ---- Competition Settings ----

export const getSettings = () =>
  request<Record<string, string>>('/settings');

export const updateSettings = (data: Record<string, unknown>) =>
  request<void>('/settings', {
    method: 'PUT',
    body: JSON.stringify(data),
  });

// ---- Submission Records ----

export interface SubmissionRecordDto {
  id: string;
  submissionId: number;
  scoreChange: number;
  isPenalty: boolean;
  createdAt: string;
  challengeName: string;
  challengeId: number;
  answer?: string;
  matchedFlagScore?: number;
}

export const getUserSubmissions = (userId: string, challengeId?: number, count = 100, skip = 0) => {
  const params = new URLSearchParams();
  if (challengeId) params.set('challengeId', String(challengeId));
  params.set('count', String(count));
  params.set('skip', String(skip));
  return request<SubmissionRecordDto[]>(`/submissions/user/${userId}?${params}`);
};

export const getMySubmissions = (challengeId?: number, count = 100, skip = 0) => {
  const params = new URLSearchParams();
  if (challengeId) params.set('challengeId', String(challengeId));
  params.set('count', String(count));
  params.set('skip', String(skip));
  return request<SubmissionRecordDto[]>(`/submissions/mine?${params}`);
};

// ---- Ticket Management ----

export interface TicketDto {
  id: string;
  description: string;
  status: string;
  reviewComment?: string;
  refundScore: number;
  bonusScore: number;
  createdAt: string;
  reviewedAt?: string;
  submissionId: number;
  userName?: string;
  reviewerName?: string;
  challengeName?: string;
  answer?: string;
  penaltyScore: number;
}

export const createTicket = (data: { submissionId: number; description: string }) =>
  request<TicketDto>('/tickets', {
    method: 'POST',
    body: JSON.stringify(data),
  });

export const getMyTickets = (count = 100, skip = 0) => {
  const params = new URLSearchParams();
  params.set('count', String(count));
  params.set('skip', String(skip));
  return request<TicketDto[]>(`/tickets/mine?${params}`);
};

export const getTicket = (ticketId: string) =>
  request<TicketDto>(`/tickets/${ticketId}`);

export const getAllTickets = (status?: string, count = 100, skip = 0) => {
  const params = new URLSearchParams();
  if (status) params.set('status', status);
  params.set('count', String(count));
  params.set('skip', String(skip));
  return request<TicketDto[]>(`/tickets?${params}`);
};

export const approveTicket = (ticketId: string, data: { refundScore: number; bonusScore: number; comment?: string }) =>
  request<TicketDto>(`/tickets/${ticketId}/approve`, {
    method: 'PUT',
    body: JSON.stringify(data),
  });

export const rejectTicket = (ticketId: string, data: { comment: string }) =>
  request<TicketDto>(`/tickets/${ticketId}/reject`, {
    method: 'PUT',
    body: JSON.stringify(data),
  });

// ---- BigScreen Events ----

export interface BigScreenEventDto {
  id: string;
  eventType: string;
  challengeId: number;
  userId?: string;
  teamName?: string;
  challengeName?: string;
  scoreChange?: number;
  message: string;
  isConsumed: boolean;
  createdAt: string;
}

export const getEvents = (unconsumedOnly = true, count = 100) => {
  const params = new URLSearchParams();
  params.set('unconsumedOnly', String(unconsumedOnly));
  params.set('count', String(count));
  return request<BigScreenEventDto[]>(`/events?${params}`);
};
