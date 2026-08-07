// src/lib/api.ts
export const BASE = (process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5123').replace(/\/+$/, '').endsWith('/api')
  ? (process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5123').replace(/\/+$/, '')
  : `${(process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5123').replace(/\/+$/, '')}/api`

// Every /api/ai/* endpoint returns { result: string }. The AI is prompted to answer in
// plain prose, so `result` is not guaranteed to be JSON — try to parse it as structured
// data (optionally wrapped in a ```json fence) and fall back to null so callers can
// display the raw text instead.
// ============================================================
// REPLACE the existing parseAiJson function in src/lib/api.ts
// with this improved version
// ============================================================

export function parseAiJson<T = any>(raw: string | undefined | null): T | null {
  if (!raw) return null

  // Step 1 — Clean markdown code blocks
  let cleaned = raw.trim()
  cleaned = cleaned.replace(/^```json\s*/i, '')
  cleaned = cleaned.replace(/^```\s*/i, '')
  cleaned = cleaned.replace(/```\s*$/i, '')
  cleaned = cleaned.trim()

  // Step 2 — Try direct parse first
  try {
    return JSON.parse(cleaned) as T
  } catch {
    // continue to next strategies
  }

  // Step 3 — Find JSON object in text (AI sometimes adds preamble)
  // Look for { ... } pattern
  const objMatch = cleaned.match(/\{[\s\S]*\}/)
  if (objMatch) {
    try {
      return JSON.parse(objMatch[0]) as T
    } catch {
      // continue
    }
  }

  // Step 4 — Find JSON array in text
  // Look for [ ... ] pattern
  const arrMatch = cleaned.match(/\[[\s\S]*\]/)
  if (arrMatch) {
    try {
      return JSON.parse(arrMatch[0]) as T
    } catch {
      // continue
    }
  }

  // Step 5 — Fix common AI JSON mistakes
  // Fix trailing commas (common AI mistake)
  try {
    const fixed = cleaned
      .replace(/,\s*}/g, '}')   // trailing comma before }
      .replace(/,\s*\]/g, ']')  // trailing comma before ]
    return JSON.parse(fixed) as T
  } catch {
    // continue
  }

  // Step 6 — Extract JSON from text with preamble
  // Pattern: "Here is the analysis:\n{...}" or "Result:\n[...]"
  const afterColon = cleaned.indexOf('\n{')
  if (afterColon !== -1) {
    try {
      return JSON.parse(cleaned.substring(afterColon + 1)) as T
    } catch { }
  }

  const afterColonArr = cleaned.indexOf('\n[')
  if (afterColonArr !== -1) {
    try {
      return JSON.parse(cleaned.substring(afterColonArr + 1)) as T
    } catch { }
  }

  // All strategies failed — return null
  return null
}

function getToken(): string | null {
  if (typeof window === 'undefined') return null
  return localStorage.getItem('clausio_token')
}

function headers(): HeadersInit {
  const token = getToken()
  return {
    'Content-Type': 'application/json',
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  }
}

async function handle(res: Response, fallbackMessage: string) {
  // Capture active sliding session token if issued
  const newToken = res.headers.get('x-new-token')
  if (newToken && typeof window !== 'undefined') {
    localStorage.setItem('clausio_token', newToken)
  }

  if (res.status === 401 && typeof window !== 'undefined') {
    localStorage.removeItem('clausio_token')
    localStorage.removeItem('clausio_user')
    document.cookie = 'clausio_token=; path=/; max-age=0'
    if (window.location.pathname !== '/auth/login') {
      window.location.href = '/auth/login'
    }
    throw new Error('Security Violation / Session Expired: Access denied. Please sign in again.')
  }
  if (!res.ok) {
    let message = fallbackMessage
    try {
      const data = await res.json()
      message = data.message || data.title || fallbackMessage
    } catch {
      // response had no JSON body
    }
    throw new Error(message)
  }
  if (res.status === 204) return null
  const responseText = await res.text()
  if (!responseText || !responseText.trim()) return null
  try {
    return JSON.parse(responseText)
  } catch {
    return responseText
  }
}

function get(path: string, fallbackMessage: string) {
  return fetch(`${BASE}${path}`, { headers: headers() }).then(res => handle(res, fallbackMessage))
}

function send(method: string, path: string, data: any, fallbackMessage: string) {
  return fetch(`${BASE}${path}`, {
    method,
    headers: headers(),
    body: data !== undefined ? JSON.stringify(data) : undefined,
  }).then(res => handle(res, fallbackMessage))
}

function del(path: string, fallbackMessage: string) {
  return fetch(`${BASE}${path}`, { method: 'DELETE', headers: headers() }).then(res => handle(res, fallbackMessage))
}

export const authApi = {
  register: (data: any) => send('POST', '/auth/register', data, 'Failed to register'),

  login: async (email: string, password: string) => {
    const data = await send('POST', '/auth/login', { email, password }, 'Invalid email or password')
    localStorage.setItem('clausio_token', data.token)
    localStorage.setItem('clausio_user', JSON.stringify(data))
    // ✅ NEW — save to cookie for Next.js middleware auth guard
    if (typeof window !== 'undefined') {
      document.cookie = `clausio_token=${data.token}; path=/; max-age=${604800}`
    }
    return data
  },

  me: () => get('/auth/me', 'Failed to fetch current user'),

  logout: () => {
    localStorage.removeItem('clausio_token')
    localStorage.removeItem('clausio_user')
    // ✅ Also clear cookie on logout
    if (typeof window !== 'undefined') {
      document.cookie = 'clausio_token=; path=/; max-age=0'
    }
  },
changePassword: (data: any) => send('PUT', '/auth/change-password', data, 'Failed to change password'),
  getUser: () => {
    if (typeof window === 'undefined') return null
    const user = localStorage.getItem('clausio_user')
    return user ? JSON.parse(user) : null
  },
}

export const casesApi = {
  getAll: () => get('/cases', 'Failed to fetch cases'),
  getById: (id: string) => get(`/cases/${id}`, 'Failed to fetch case'),
  create: (data: any) => send('POST', '/cases', data, 'Failed to create case'),
  update: (id: string, data: any) => send('PUT', `/cases/${id}`, data, 'Failed to update case'),
  remove: (id: string) => del(`/cases/${id}`, 'Failed to delete case'),
}

export const aiAnalyticsApi = {
  getOverview: async () => {
    return get('/ai-analytics/overview', 'Failed to fetch AI overview')
  },
  getQuality: async () => {
    return get('/ai-analytics/quality', 'Failed to fetch AI quality metrics')
  },
  getModels: async () => {
    return get('/ai-analytics/models', 'Failed to fetch model metrics')
  }
}
export const clientsApi = {
  getAll: () => get('/clients', 'Failed to fetch clients'),
  getById: (id: string) => get(`/clients/${id}`, 'Failed to fetch client'),
  create: (data: any) => send('POST', '/clients', data, 'Failed to create client'),
  update: (id: string, data: any) => send('PUT', `/clients/${id}`, data, 'Failed to update client'),
  remove: (id: string) => del(`/clients/${id}`, 'Failed to delete client'),
}

export const hearingsApi = {
  getByCaseId: (caseId: string) => get(`/cases/${caseId}/hearings`, 'Failed to fetch hearings'),
  create: (caseId: string, data: any) => send('POST', `/cases/${caseId}/hearings`, data, 'Failed to create hearing'),
  update: (caseId: string, id: string, data: any) => send('PUT', `/cases/${caseId}/hearings/${id}`, data, 'Failed to update hearing'),
  remove: (caseId: string, id: string) => del(`/cases/${caseId}/hearings/${id}`, 'Failed to delete hearing'),
  markOrderDone: (caseId: string, hearingId: string, orderId: string) =>
    send('PUT', `/cases/${caseId}/hearings/${hearingId}/orders/${orderId}/done`, undefined, 'Failed to mark order done'),
}

export const documentsApi = {
  getByCaseId: (caseId: string) => get(`/cases/${caseId}/documents`, 'Failed to fetch documents'),

  upload: async (caseId: string, file: File, documentType: string, exhibitLabel?: string) => {
    const token = getToken()
    const formData = new FormData()
    formData.append('file', file)
    formData.append('documentType', documentType)
    if (exhibitLabel) formData.append('exhibitLabel', exhibitLabel)

    const res = await fetch(`${BASE}/cases/${caseId}/documents`, {
      method: 'POST',
      headers: { ...(token ? { Authorization: `Bearer ${token}` } : {}) },
      body: formData,
    })
    return handle(res, 'Failed to upload document')
  },

  remove: (caseId: string, id: string) => del(`/cases/${caseId}/documents/${id}`, 'Failed to delete document'),
}

export const timelineApi = {
  getByCaseId: (caseId: string) => get(`/cases/${caseId}/timeline`, 'Failed to fetch timeline'),
  create: (caseId: string, data: any) => send('POST', `/cases/${caseId}/timeline`, data, 'Failed to create timeline event'),
  update: (caseId: string, id: string, data: any) => send('PUT', `/cases/${caseId}/timeline/${id}`, data, 'Failed to update timeline event'),
  remove: (caseId: string, id: string) => del(`/cases/${caseId}/timeline/${id}`, 'Failed to delete timeline event'),
  bulkCreate: (caseId: string, data: any) => send('POST', `/cases/${caseId}/timeline/bulk`, data, 'Failed to save timeline'),
}

export const contradictionsApi = {
  getByCaseId: (caseId: string) => get(`/cases/${caseId}/contradictions`, 'Failed to fetch contradictions'),
  create: (caseId: string, data: any) => send('POST', `/cases/${caseId}/contradictions`, data, 'Failed to create contradiction'),
  update: (caseId: string, id: string, data: any) => send('PUT', `/cases/${caseId}/contradictions/${id}`, data, 'Failed to update contradiction'),
  remove: (caseId: string, id: string) => del(`/cases/${caseId}/contradictions/${id}`, 'Failed to delete contradiction'),
  summary: (caseId: string) => get(`/cases/${caseId}/contradictions/summary`, 'Failed to fetch contradictions summary'),
}

export const actionPlansApi = {
  getByCaseId: (caseId: string) => get(`/cases/${caseId}/actionplans`, 'Failed to fetch action plans'),
  create: (caseId: string, data: any) => send('POST', `/cases/${caseId}/actionplans`, data, 'Failed to create action plan item'),
  update: (caseId: string, id: string, data: any) => send('PUT', `/cases/${caseId}/actionplans/${id}`, data, 'Failed to update action plan item'),
  markDone: (caseId: string, id: string) => send('PUT', `/cases/${caseId}/actionplans/${id}/done`, undefined, 'Failed to mark item done'),
  markUndone: (caseId: string, id: string) => send('PUT', `/cases/${caseId}/actionplans/${id}/undone`, undefined, 'Failed to mark item undone'),
  remove: (caseId: string, id: string) => del(`/cases/${caseId}/actionplans/${id}`, 'Failed to delete action plan item'),
  summary: (caseId: string) => get(`/cases/${caseId}/actionplans/summary`, 'Failed to fetch action plan summary'),
}

export const researchApi = {
  getByCaseId: (caseId: string) => get(`/cases/${caseId}/research`, 'Failed to fetch research'),
  create: (caseId: string, data: any) => send('POST', `/cases/${caseId}/research`, data, 'Failed to save research item'),
  update: (caseId: string, id: string, data: any) => send('PUT', `/cases/${caseId}/research/${id}`, data, 'Failed to update research item'),
  remove: (caseId: string, id: string) => del(`/cases/${caseId}/research/${id}`, 'Failed to delete research item'),
  summary: (caseId: string) => get(`/cases/${caseId}/research/summary`, 'Failed to fetch research summary'),
}

export const readinessApi = {
  getByCaseId: (caseId: string) => get(`/cases/${caseId}/readiness`, 'Failed to fetch readiness'),
  generate: (caseId: string) => send('POST', `/cases/${caseId}/readiness/generate`, undefined, 'Failed to generate readiness report'),
  updateScore: (caseId: string, data: any) => send('PUT', `/cases/${caseId}/readiness/score`, data, 'Failed to update readiness score'),
}

export const statsApi = {
  overview: () => get('/stats/overview', 'Failed to fetch stats'),
  cases: () => get('/stats/cases', 'Failed to fetch case stats'),
  hearings: () => get('/stats/hearings', 'Failed to fetch hearing stats'),
  documents: () => get('/stats/documents', 'Failed to fetch document stats'),
  activity: () => get('/stats/activity', 'Failed to fetch activity'),
}

export const aiApi = {
  getSummary: (caseId: string) => send('POST', `/ai/summary/${caseId}`, undefined, 'Failed to generate summary'),
  getChronology: (caseId: string) => send('POST', `/ai/chronology/${caseId}`, undefined, 'Failed to generate chronology'),
  getContradictions: (caseId: string) => send('POST', `/ai/contradictions/${caseId}`, undefined, 'Failed to find contradictions'),
  getEvidence: (documentId: string) => send('POST', `/ai/evidence/${documentId}`, undefined, 'Failed to analyse evidence'),
  getLegalResearch: (caseId: string) => send('POST', `/ai/research/${caseId}`, undefined, 'Failed to fetch research'),
  getActionPlan: (caseId: string) => send('POST', `/ai/actionplan/${caseId}`, undefined, 'Failed to generate action plan'),
  getWhatsApp: (caseId: string, data: any) => send('POST', `/ai/whatsapp/${caseId}`, data, 'Failed to generate WhatsApp update'),
  getFinancial: (caseId: string) => send('POST', `/ai/financial/${caseId}`, undefined, 'Failed to analyse financials'),
  getReadiness: (caseId: string) => send('POST', `/ai/readiness/${caseId}`, undefined, 'Failed to generate readiness report'),
  getEmergency: (caseId: string, data: any) => send('POST', `/ai/emergency/${caseId}`, data, 'Failed to generate emergency response'),
  getPrep: (caseId: string) => send('POST', `/ai/prep/${caseId}`, undefined, 'Failed to generate prep notes'),
  getWitness: (caseId: string) => send('POST', `/ai/witness/${caseId}`, undefined, 'Failed to generate cross-examination questions'),
  translate: (data: any) => send('POST', '/ai/translate', data, 'Failed to translate'),
  getDraft: (caseId: string, data: any) => send('POST', `/ai/draft/${caseId}`, data, 'Failed to generate draft'),
  getCaseType: (data: any) => send('POST', '/ai/casetype', data, 'Failed to detect case type'),
  chat: (data: any) => send('POST', '/ai/chat', data, 'Failed to get AI response'),
  
  // Streaming version of chat
  chatStream: async function* (data: any): AsyncGenerator<string, void, unknown> {
    const token = getToken()
    const res = await fetch(`${BASE}/ai/chat/stream`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {})
      },
      body: JSON.stringify(data)
    })

    if (!res.ok) {
      throw new Error('Failed to get AI response stream')
    }

    if (!res.body) {
      throw new Error('No response body')
    }

    const reader = res.body.getReader()
    const decoder = new TextDecoder()
    let buffer = ''

    while (true) {
      const { value, done } = await reader.read()
      if (done) break

      buffer += decoder.decode(value, { stream: true })
      
      const lines = buffer.split('\n')
      buffer = lines.pop() || ''

      for (const line of lines) {
        if (line.startsWith('data: ')) {
          const dataStr = line.slice(6)
          if (dataStr === '[DONE]') {
            return
          }
          try {
            const parsed = JSON.parse(dataStr)
            yield typeof parsed === 'string' ? parsed : JSON.stringify(parsed)
          } catch {
            yield dataStr
          }
        }
      }
    }
  }
}
