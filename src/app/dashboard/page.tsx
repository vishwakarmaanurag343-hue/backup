'use client'

import { useState, useEffect, useCallback } from 'react'
import { useRouter } from 'next/navigation'
import { useUIStore, useCaseStore } from '@/lib/store'
import { casesApi, hearingsApi, documentsApi, actionPlansApi } from '@/lib/api'
import CaseList  from '@/components/cases/CaseList'
import AIInsights from '@/components/dashboard/AIInsights'
import {
  DocumentsTab,
  HearingsTab,
  TasksTab,
  ResearchTab,
  TimelineTab,
} from '@/components/dashboard/DashboardTabs'

const TABS = [
  { id: 'Overview',   icon: 'ti-layout-dashboard' },
  { id: 'Documents',  icon: 'ti-files'             },
  { id: 'Hearings',   icon: 'ti-gavel'             },
  { id: 'Tasks',      icon: 'ti-checklist'         },
  { id: 'Research',   icon: 'ti-books'             },
  { id: 'Timeline',   icon: 'ti-timeline'          },
]

export default function DashboardPage() {
  const router  = useRouter()
  const { caseListVisible, aiPanelVisible, aiPanelExpanded, aiPanelWidth, toggleAIPanel } = useUIStore()
  const { selectedCaseId, setSelectedCase } = useCaseStore()

  const [activeTab,  setActiveTab]  = useState('Overview')
  const [caseData,   setCaseData]   = useState<any>(null)
  const [hearings,   setHearings]   = useState<any[]>([])
  const [documents,  setDocuments]  = useState<any[]>([])
  const [tasks,      setTasks]      = useState<any[]>([])
  const [markingId,  setMarkingId]  = useState<string | null>(null)

  // Auto-select first case
  useEffect(() => {
    if (selectedCaseId) return
    const token = localStorage.getItem('clausio_token')
    const apiBase = (process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5123/api').replace(/\/+$/, '')
    const url = apiBase.endsWith('/api') ? `${apiBase}/cases` : `${apiBase}/api/cases`
    fetch(url, {
      headers: { Authorization: `Bearer ${token}` }
    })
      .then(r => r.json())
      .then(cases => {
        if (Array.isArray(cases) && cases.length > 0)
          setSelectedCase(cases[0].id, cases[0].name)
      })
      .catch(() => {})
  }, [selectedCaseId, setSelectedCase])

  const loadHearings = useCallback(() => {
    if (!selectedCaseId) return
    hearingsApi.getByCaseId(selectedCaseId)
      .then(d => setHearings(Array.isArray(d) ? d : []))
      .catch(() => {})
  }, [selectedCaseId])

  useEffect(() => {
    if (!selectedCaseId) return
    setCaseData(null)
    setHearings([])
    setDocuments([])
    setTasks([])

    casesApi.getById(selectedCaseId).then(setCaseData).catch(() => {})
    loadHearings()
    documentsApi.getByCaseId(selectedCaseId)
      .then(d => setDocuments(Array.isArray(d) ? d : [])).catch(() => {})
    actionPlansApi.getByCaseId(selectedCaseId)
      .then(d => setTasks(Array.isArray(d) ? d : [])).catch(() => {})
  }, [selectedCaseId, loadHearings])

  const allOrders    = hearings.flatMap(h => (h.orders ?? []).map((o: any) => ({ ...o, hearingId: h.id })))
  const overdueOrders = allOrders.filter(o => !o.done && o.deadline && new Date(o.deadline) < new Date())
  const pendingTasks  = tasks.filter(t => !t.done)
  const readiness     = caseData?.readinessScore ?? 0
  const lastHearing   = hearings.sort((a, b) => new Date(b.hearingDate).getTime() - new Date(a.hearingDate).getTime())[0]
  const nextHearingDate = caseData?.nextHearing ? new Date(caseData.nextHearing) : null
  const daysToHearing  = nextHearingDate ? Math.ceil((nextHearingDate.getTime() - Date.now()) / 86400000) : null

  async function markOrderDone(hearingId: string, orderId: string) {
    if (!selectedCaseId) return
    setMarkingId(orderId)
    try {
      await hearingsApi.markOrderDone(selectedCaseId, hearingId, orderId)
      loadHearings()
    } catch { } finally { setMarkingId(null) }
  }

  return (
    <div className="glass-panel" style={{ height: 'calc(100% - 32px)', margin: '16px 16px 16px 16px', display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>

      {/* ── TOP BAR ── */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 12, padding: '10px 20px', background: 'rgba(255,255,255,0.4)', borderBottom: '1px solid rgba(0,0,0,0.06)', flexShrink: 0 }}>
        {/* Case name + badges */}
        <div style={{ flex: 1, minWidth: 0 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap' }}>
            <span style={{ fontSize: 15, fontWeight: 700, color: '#0f172a', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', maxWidth: 280 }}>
              {caseData?.name ?? 'Select a case'}
            </span>
            {caseData && (
              <>
                <span style={{ fontSize: 10, padding: '2px 8px', borderRadius: 20, fontWeight: 700, background: caseData.status === 'Active' ? '#f0fdf4' : '#f1f5f9', color: caseData.status === 'Active' ? '#15803d' : '#64748b', border: `1px solid ${caseData.status === 'Active' ? '#86efac' : '#e2e8f0'}` }}>
                  {caseData.status}
                </span>
                <span style={{ fontSize: 10, padding: '2px 8px', borderRadius: 20, fontWeight: 600, background: '#eff6ff', color: '#1e40af', border: '1px solid #bfdbfe' }}>
                  {caseData.priority} Priority
                </span>
                {overdueOrders.length > 0 && (
                  <span style={{ fontSize: 10, padding: '2px 8px', borderRadius: 20, fontWeight: 700, background: '#fef2f2', color: '#dc2626', border: '1px solid #fca5a5', animation: 'pulse 2s infinite' }}>
                    ⚠ {overdueOrders.length} Overdue
                  </span>
                )}
              </>
            )}
          </div>
          {caseData && (
            <div style={{ display: 'flex', gap: 12, marginTop: 3, fontSize: 11, color: '#64748b' }}>
              <span>{caseData.court}</span>
              <span>·</span>
              <span>{caseData.caseNumber}</span>
              <span>·</span>
              <span>{caseData.caseType}</span>
              {nextHearingDate && (
                <>
                  <span>·</span>
                  <span style={{ fontWeight: 600, color: daysToHearing !== null && daysToHearing <= 7 ? '#dc2626' : '#0f172a' }}>
                    Next: {nextHearingDate.toLocaleDateString('en-IN', { day: 'numeric', month: 'short', year: 'numeric' })}
                    {daysToHearing !== null && daysToHearing >= 0 && ` (${daysToHearing}d)`}
                  </span>
                </>
              )}
            </div>
          )}
        </div>

        {/* Readiness bar */}
        {caseData && (
          <div style={{ display: 'flex', alignItems: 'center', gap: 8, flexShrink: 0 }}>
            <span style={{ fontSize: 11, color: '#64748b', whiteSpace: 'nowrap' }}>Readiness</span>
            <div style={{ width: 80, height: 6, background: '#e2e8f0', borderRadius: 3, overflow: 'hidden' }}>
              <div style={{ width: `${readiness}%`, height: '100%', background: readiness >= 70 ? '#10b981' : readiness >= 40 ? '#f59e0b' : '#ef4444', borderRadius: 3, transition: 'width 0.5s' }} />
            </div>
            <span style={{ fontSize: 11, fontWeight: 700, color: readiness >= 70 ? '#10b981' : readiness >= 40 ? '#f59e0b' : '#ef4444' }}>{readiness}%</span>
          </div>
        )}

        {/* Action buttons */}
        <div style={{ display: 'flex', gap: 8, flexShrink: 0 }}>
          <button onClick={() => router.push('/readiness')} style={{ padding: '6px 12px', background: '#fef2f2', border: '1px solid #fca5a5', borderRadius: 8, fontSize: 11, fontWeight: 700, color: '#dc2626', cursor: 'pointer', fontFamily: 'inherit', display: 'flex', alignItems: 'center', gap: 4 }}>
            <i className="ti ti-alert-triangle" style={{ fontSize: 12 }} /> Emergency
          </button>
          <button onClick={() => router.push('/drafting')} style={{ padding: '6px 12px', background: '#eff6ff', border: '1px solid #bfdbfe', borderRadius: 8, fontSize: 11, fontWeight: 600, color: '#1e40af', cursor: 'pointer', fontFamily: 'inherit', display: 'flex', alignItems: 'center', gap: 4 }}>
            <i className="ti ti-file-text" style={{ fontSize: 12 }} /> Draft
          </button>
          <button onClick={toggleAIPanel} style={{ padding: '6px 12px', background: aiPanelVisible ? '#f5f3ff' : '#f8fafc', border: `1px solid ${aiPanelVisible ? '#c4b5fd' : '#e2e8f0'}`, borderRadius: 8, fontSize: 11, fontWeight: 600, color: aiPanelVisible ? '#7c3aed' : '#64748b', cursor: 'pointer', fontFamily: 'inherit', display: 'flex', alignItems: 'center', gap: 4 }}>
            <i className="ti ti-brain" style={{ fontSize: 12 }} /> AI
          </button>
        </div>
      </div>

      {/* ── MAIN LAYOUT ── */}
      <div style={{ display: 'flex', flex: 1, overflow: 'hidden' }}>

        {/* Case list panel */}
        <div style={{ flexShrink: 0, overflow: 'hidden', transition: 'width 0.22s', width: caseListVisible ? 260 : 0 }}>
          <CaseList />
        </div>

        {/* Main content */}
        <div style={{ flex: 1, display: 'flex', flexDirection: 'column', overflow: 'hidden', minWidth: 0 }}>

          {/* Overdue alert */}
          {overdueOrders.length > 0 && (
            <div style={{ display: 'flex', alignItems: 'center', gap: 10, background: '#fef2f2', borderBottom: '1px solid #fca5a5', borderLeft: '4px solid #dc2626', padding: '8px 16px', flexShrink: 0 }}>
              <i className="ti ti-alert-triangle" style={{ color: '#dc2626', fontSize: 14 }} />
              <span style={{ fontSize: 12, fontWeight: 600, color: '#7f1d1d', flex: 1 }}>
                {overdueOrders.length} court order{overdueOrders.length > 1 ? 's' : ''} overdue — immediate action required
              </span>
              <button onClick={() => setActiveTab('Hearings')} style={{ fontSize: 11, padding: '4px 10px', border: 'none', background: '#dc2626', color: '#fff', borderRadius: 6, cursor: 'pointer', fontFamily: 'inherit', fontWeight: 600 }}>
                View Orders
              </button>
            </div>
          )}

          {/* Tabs */}
          <div style={{ display: 'flex', background: '#fff', borderBottom: '1px solid #e2e8f0', flexShrink: 0, padding: '0 4px' }}>
            {TABS.map(t => (
              <button
                key={t.id}
                onClick={() => setActiveTab(t.id)}
                style={{ display: 'flex', alignItems: 'center', gap: 5, padding: '10px 14px', fontSize: 12, cursor: 'pointer', background: 'transparent', border: 'none', borderBottom: `2px solid ${activeTab === t.id ? '#3b82f6' : 'transparent'}`, color: activeTab === t.id ? '#1e40af' : '#64748b', fontWeight: activeTab === t.id ? 600 : 400, fontFamily: 'inherit', whiteSpace: 'nowrap', flexShrink: 0, transition: 'all 0.15s' }}
              >
                <i className={`ti ${t.icon}`} style={{ fontSize: 13 }} />
                {t.id}
              </button>
            ))}
          </div>

          {/* Tab content */}
          <div style={{ flex: 1, overflowY: 'auto', padding: 20 }}>

            {/* No case */}
            {!selectedCaseId && (
              <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', height: '60%', gap: 12 }}>
                <div style={{ width: 64, height: 64, borderRadius: 16, background: '#eff6ff', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                  <i className="ti ti-folder-open" style={{ fontSize: 32, color: '#3b82f6' }} />
                </div>
                <h3 style={{ margin: 0, fontSize: 16, fontWeight: 600, color: '#0f172a' }}>No case selected</h3>
                <p style={{ margin: 0, fontSize: 13, color: '#64748b', textAlign: 'center', maxWidth: 300 }}>
                  Select a case from the left panel or create a new case to get started
                </p>
                <button onClick={() => router.push('/cases')} style={{ padding: '10px 20px', background: '#3b82f6', color: '#fff', border: 'none', borderRadius: 8, fontSize: 13, fontWeight: 600, cursor: 'pointer', fontFamily: 'inherit' }}>
                  Go to Cases →
                </button>
              </div>
            )}

            {/* ── OVERVIEW TAB ── */}
            {activeTab === 'Overview' && caseData && (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>

                {/* Metrics row */}
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 12 }}>
                  {[
                    { icon: 'ti-gavel',      label: 'Hearings',       value: hearings.length,       sub: hearings.length > 0 ? `Last: ${new Date(lastHearing?.hearingDate).toLocaleDateString('en-IN', { day: 'numeric', month: 'short' })}` : 'None recorded', color: '#3b82f6' },
                    { icon: 'ti-files',      label: 'Documents',      value: documents.length,      sub: `${documents.length} filed`,                                                                                                                                                   color: '#8b5cf6' },
                    { icon: 'ti-checklist',  label: 'Pending Tasks',  value: pendingTasks.length,   sub: pendingTasks.length > 0 ? `${pendingTasks.filter(t => t.priority === 'High' || t.priority === 'Critical').length} high priority` : 'All clear',                             color: pendingTasks.length > 0 ? '#f59e0b' : '#10b981' },
                    { icon: 'ti-alert-circle', label: 'Overdue Orders', value: overdueOrders.length, sub: overdueOrders.length > 0 ? 'Immediate action needed' : 'No overdue orders',                                                                                                color: overdueOrders.length > 0 ? '#ef4444' : '#10b981' },
                  ].map((m, i) => (
                    <div key={i} style={{ background: '#fff', borderRadius: 12, padding: '16px', border: '1px solid #e2e8f0', boxShadow: '0 1px 3px rgba(0,0,0,0.04)' }}>
                      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 12 }}>
                        <span style={{ fontSize: 12, color: '#64748b', fontWeight: 600 }}>{m.label}</span>
                        <div style={{ width: 32, height: 32, borderRadius: 8, background: `${m.color}15`, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                          <i className={`ti ${m.icon}`} style={{ fontSize: 16, color: m.color }} />
                        </div>
                      </div>
                      <div style={{ fontSize: 32, fontWeight: 700, color: '#0f172a', lineHeight: 1 }}>{m.value}</div>
                      <div style={{ fontSize: 11, color: m.color, marginTop: 6, fontWeight: 500 }}>{m.sub}</div>
                    </div>
                  ))}
                </div>

                {/* Main grid */}
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>

                  {/* Hearing Diary */}
                  <div style={{ background: '#fff', borderRadius: 12, padding: 20, border: '1px solid #e2e8f0', boxShadow: '0 1px 3px rgba(0,0,0,0.04)' }}>
                    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 16 }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                        <i className="ti ti-notebook" style={{ fontSize: 16, color: '#3b82f6' }} />
                        <span style={{ fontSize: 14, fontWeight: 700, color: '#0f172a' }}>Court Orders & Diary</span>
                      </div>
                      <button onClick={() => router.push('/hearings')} style={{ fontSize: 11, color: '#3b82f6', background: 'none', border: 'none', cursor: 'pointer', fontFamily: 'inherit', fontWeight: 600 }}>
                        Add Hearing →
                      </button>
                    </div>

                    {allOrders.length === 0 ? (
                      <div style={{ textAlign: 'center', padding: '20px 0', color: '#94a3b8' }}>
                        <i className="ti ti-clipboard" style={{ fontSize: 28, display: 'block', marginBottom: 8 }} />
                        <div style={{ fontSize: 12 }}>No court orders recorded yet</div>
                        <button onClick={() => router.push('/hearings')} style={{ marginTop: 8, fontSize: 11, padding: '4px 12px', background: '#eff6ff', color: '#1e40af', border: '1px solid #bfdbfe', borderRadius: 6, cursor: 'pointer', fontFamily: 'inherit', fontWeight: 600 }}>
                          Record Hearing
                        </button>
                      </div>
                    ) : (
                      allOrders.slice(0, 5).map((order, i) => {
                        const overdue = !order.done && order.deadline && new Date(order.deadline) < new Date()
                        return (
                          <div key={order.id} style={{ display: 'flex', alignItems: 'flex-start', gap: 10, padding: '10px 0', borderBottom: i < Math.min(allOrders.length, 5) - 1 ? '1px solid #f1f5f9' : 'none' }}>
                            <div style={{ width: 8, height: 8, borderRadius: '50%', background: order.done ? '#10b981' : overdue ? '#dc2626' : '#3b82f6', flexShrink: 0, marginTop: 5 }} />
                            <div style={{ flex: 1 }}>
                              <div style={{ fontSize: 12, color: order.done ? '#94a3b8' : '#0f172a', lineHeight: 1.4, fontWeight: 500, textDecoration: order.done ? 'line-through' : 'none' }}>
                                {order.text}
                                {overdue && <span style={{ marginLeft: 6, fontSize: 9, padding: '2px 6px', borderRadius: 10, background: '#fef2f2', color: '#dc2626', fontWeight: 700 }}>OVERDUE</span>}
                              </div>
                              {order.deadline && (
                                <div style={{ fontSize: 11, color: '#64748b', marginTop: 2 }}>
                                  Due {new Date(order.deadline).toLocaleDateString('en-IN', { day: 'numeric', month: 'short', year: 'numeric' })} · {order.responsible}
                                </div>
                              )}
                            </div>
                            {!order.done && (
                              <button onClick={() => markOrderDone(order.hearingId, order.id)} disabled={markingId === order.id} style={{ fontSize: 10, padding: '3px 8px', borderRadius: 6, border: '1px solid #e2e8f0', background: '#f8fafc', color: '#334155', cursor: 'pointer', fontFamily: 'inherit', fontWeight: 600, flexShrink: 0 }}>
                                {markingId === order.id ? '...' : '✓ Done'}
                              </button>
                            )}
                          </div>
                        )
                      })
                    )}
                  </div>

                  {/* Pending Tasks */}
                  <div style={{ background: '#fff', borderRadius: 12, padding: 20, border: '1px solid #e2e8f0', boxShadow: '0 1px 3px rgba(0,0,0,0.04)' }}>
                    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 16 }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                        <i className="ti ti-checklist" style={{ fontSize: 16, color: '#f59e0b' }} />
                        <span style={{ fontSize: 14, fontWeight: 700, color: '#0f172a' }}>Action Plan</span>
                      </div>
                      <button onClick={() => router.push('/strategy')} style={{ fontSize: 11, color: '#3b82f6', background: 'none', border: 'none', cursor: 'pointer', fontFamily: 'inherit', fontWeight: 600 }}>
                        Generate AI →
                      </button>
                    </div>

                    {pendingTasks.length === 0 ? (
                      <div style={{ textAlign: 'center', padding: '20px 0', color: '#94a3b8' }}>
                        <i className="ti ti-sparkles" style={{ fontSize: 28, display: 'block', marginBottom: 8 }} />
                        <div style={{ fontSize: 12 }}>No action items yet</div>
                        <button onClick={() => router.push('/strategy')} style={{ marginTop: 8, fontSize: 11, padding: '4px 12px', background: '#eff6ff', color: '#1e40af', border: '1px solid #bfdbfe', borderRadius: 6, cursor: 'pointer', fontFamily: 'inherit', fontWeight: 600 }}>
                          Generate Strategy
                        </button>
                      </div>
                    ) : (
                      pendingTasks.slice(0, 5).map((task, i) => {
                        const pColor = task.priority === 'Critical' || task.priority === 'High' ? '#dc2626' : task.priority === 'Medium' ? '#d97706' : '#16a34a'
                        return (
                          <div key={task.id} style={{ display: 'flex', alignItems: 'flex-start', gap: 10, padding: '10px 0', borderBottom: i < Math.min(pendingTasks.length, 5) - 1 ? '1px solid #f1f5f9' : 'none' }}>
                            <div style={{ width: 8, height: 8, borderRadius: '50%', background: pColor, flexShrink: 0, marginTop: 5 }} />
                            <div style={{ flex: 1 }}>
                              <div style={{ fontSize: 12, fontWeight: 600, color: '#0f172a' }}>{task.title}</div>
                              {task.dueBy && (
                                <div style={{ fontSize: 11, color: '#64748b', marginTop: 2 }}>
                                  Due {new Date(task.dueBy).toLocaleDateString('en-IN', { day: 'numeric', month: 'short' })} · {task.assignedTo}
                                </div>
                              )}
                            </div>
                            <span style={{ fontSize: 9, padding: '2px 6px', borderRadius: 10, background: `${pColor}15`, color: pColor, fontWeight: 700, flexShrink: 0 }}>{task.priority}</span>
                          </div>
                        )
                      })
                    )}
                  </div>
                </div>

                {/* Bottom grid */}
                <div style={{ display: 'grid', gridTemplateColumns: '2fr 1fr', gap: 16 }}>

                  {/* Recent hearings */}
                  <div style={{ background: '#fff', borderRadius: 12, padding: 20, border: '1px solid #e2e8f0', boxShadow: '0 1px 3px rgba(0,0,0,0.04)' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 16 }}>
                      <i className="ti ti-history" style={{ fontSize: 16, color: '#8b5cf6' }} />
                      <span style={{ fontSize: 14, fontWeight: 700, color: '#0f172a' }}>Recent Hearings</span>
                    </div>
                    {hearings.length === 0 ? (
                      <div style={{ fontSize: 12, color: '#94a3b8', padding: '8px 0' }}>No hearings recorded yet.</div>
                    ) : (
                      hearings.slice(0, 3).map((h, i) => (
                        <div key={h.id} style={{ padding: '10px 0', borderBottom: i < Math.min(hearings.length, 3) - 1 ? '1px solid #f1f5f9' : 'none' }}>
                          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 8 }}>
                            <div style={{ flex: 1 }}>
                              <div style={{ fontSize: 12, fontWeight: 700, color: '#0f172a', marginBottom: 4 }}>
                                {new Date(h.hearingDate).toLocaleDateString('en-IN', { day: 'numeric', month: 'long', year: 'numeric' })}
                              </div>
                              <div style={{ fontSize: 12, color: '#475569', lineHeight: 1.5 }}>{h.whatHappened}</div>
                              {h.judgeObservation && (
                                <div style={{ fontSize: 11, color: '#7c3aed', marginTop: 4, fontStyle: 'italic' }}>
                                  Judge: "{h.judgeObservation}"
                                </div>
                              )}
                            </div>
                            <span style={{ fontSize: 10, padding: '2px 8px', borderRadius: 10, background: '#f5f3ff', color: '#7c3aed', fontWeight: 600, flexShrink: 0 }}>{h.stage}</span>
                          </div>
                        </div>
                      ))
                    )}
                    {hearings.length > 3 && (
                      <button onClick={() => setActiveTab('Hearings')} style={{ marginTop: 8, fontSize: 11, color: '#3b82f6', background: 'none', border: 'none', cursor: 'pointer', fontFamily: 'inherit', fontWeight: 600, padding: 0 }}>
                        View all {hearings.length} hearings →
                      </button>
                    )}
                  </div>

                  {/* Case info */}
                  <div style={{ background: '#fff', borderRadius: 12, padding: 20, border: '1px solid #e2e8f0', boxShadow: '0 1px 3px rgba(0,0,0,0.04)' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 16 }}>
                      <i className="ti ti-info-circle" style={{ fontSize: 16, color: '#3b82f6' }} />
                      <span style={{ fontSize: 14, fontWeight: 700, color: '#0f172a' }}>Case Details</span>
                    </div>
                    {[
                      { label: 'Filed On',     value: caseData?.filedOn ? new Date(caseData.filedOn).toLocaleDateString('en-IN', { day: 'numeric', month: 'short', year: 'numeric' }) : '—' },
                      { label: 'Stage',        value: caseData?.stage ?? '—' },
                      { label: 'Opposing Adv', value: caseData?.opposingAdv || 'Not recorded' },
                      { label: 'Client',       value: caseData?.client ? `${caseData.client.firstName} ${caseData.client.lastName}` : '—' },
                      { label: 'Readiness',    value: `${readiness}%` },
                    ].map((item, i) => (
                      <div key={i} style={{ display: 'flex', justifyContent: 'space-between', padding: '7px 0', borderBottom: i < 4 ? '1px solid #f1f5f9' : 'none' }}>
                        <span style={{ fontSize: 11, color: '#64748b' }}>{item.label}</span>
                        <span style={{ fontSize: 11, fontWeight: 600, color: '#0f172a', textAlign: 'right', maxWidth: 120, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{item.value}</span>
                      </div>
                    ))}
                    <button onClick={() => router.push('/cases')} style={{ marginTop: 12, width: '100%', padding: '8px', background: '#f8fafc', border: '1px solid #e2e8f0', borderRadius: 8, fontSize: 11, fontWeight: 600, color: '#334155', cursor: 'pointer', fontFamily: 'inherit' }}>
                      Edit Case Details
                    </button>
                  </div>
                </div>

                {/* Quick actions */}
                <div style={{ background: '#fff', borderRadius: 12, padding: 16, border: '1px solid #e2e8f0' }}>
                  <div style={{ fontSize: 12, fontWeight: 700, color: '#64748b', textTransform: 'uppercase', letterSpacing: 1, marginBottom: 12 }}>Quick Actions</div>
                  <div style={{ display: 'grid', gridTemplateColumns: 'repeat(6, 1fr)', gap: 10 }}>
                    {[
                      { icon: 'ti-alert-triangle', label: 'Emergency',       route: '/readiness',  color: '#dc2626', bg: '#fef2f2' },
                      { icon: 'ti-clipboard-list', label: 'Hearing Brief',   route: '/hearings',   color: '#1e40af', bg: '#eff6ff' },
                      { icon: 'ti-message',        label: 'Client Update',   route: '/client',     color: '#15803d', bg: '#f0fdf4' },
                      { icon: 'ti-sparkles',       label: 'AI Strategy',     route: '/strategy',   color: '#7c3aed', bg: '#f5f3ff' },
                      { icon: 'ti-file-text',      label: 'Draft Document',  route: '/drafting',   color: '#0369a1', bg: '#f0f9ff' },
                      { icon: 'ti-chart-bar',      label: 'Financial',       route: '/financial',  color: '#c2410c', bg: '#fff7ed' },
                    ].map((a, i) => (
                      <button key={i} onClick={() => router.push(a.route)} style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 6, padding: '12px 8px', background: a.bg, border: `1px solid ${a.color}22`, borderRadius: 10, cursor: 'pointer', fontFamily: 'inherit', transition: 'all 0.15s' }}
                        onMouseEnter={e => { (e.currentTarget as HTMLElement).style.transform = 'translateY(-2px)'; (e.currentTarget as HTMLElement).style.boxShadow = '0 4px 12px rgba(0,0,0,0.08)' }}
                        onMouseLeave={e => { (e.currentTarget as HTMLElement).style.transform = 'translateY(0)'; (e.currentTarget as HTMLElement).style.boxShadow = 'none' }}
                      >
                        <i className={`ti ${a.icon}`} style={{ fontSize: 20, color: a.color }} />
                        <span style={{ fontSize: 10, fontWeight: 600, color: a.color, textAlign: 'center', lineHeight: 1.2 }}>{a.label}</span>
                      </button>
                    ))}
                  </div>
                </div>
              </div>
            )}

            {/* Other tabs */}
            {activeTab === 'Documents' && selectedCaseId && <DocumentsTab caseId={selectedCaseId} />}
            {activeTab === 'Hearings'  && selectedCaseId && <HearingsTab  caseId={selectedCaseId} />}
            {activeTab === 'Tasks'     && selectedCaseId && <TasksTab     caseId={selectedCaseId} />}
            {activeTab === 'Research'  && selectedCaseId && <ResearchTab  caseId={selectedCaseId} />}
            {activeTab === 'Timeline'  && selectedCaseId && <TimelineTab  caseId={selectedCaseId} />}

          </div>
        </div>

        {/* AI Insights panel - Inline layout, instant toggle (No animation) */}
        <div style={{ flexShrink: 0, overflow: 'hidden', width: aiPanelVisible ? aiPanelWidth : 0 }}>
          <AIInsights />
        </div>

      </div>
    </div>
  )
}
