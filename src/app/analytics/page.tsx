'use client'

import { useState } from 'react'
import AnalyticsTabs   from '@/components/analytics/AnalyticsTabs'
import AIChat           from '@/components/analytics/AIChat'
import CrossExamination from '@/components/analytics/CrossExamination'
import PromptLibrary    from '@/components/analytics/PromptLibrary'
import AIHistory        from '@/components/analytics/AIHistory'
import KnowledgeBase    from '@/components/analytics/KnowledgeBase'
import AnalyticsDashboard from '@/components/dashboard/AnalyticsDashboard'

export default function AnalyticsPage() {
  const [activeTab, setActiveTab] = useState('AI Chat')
  const [chatKey,   setChatKey]   = useState(0)

  function renderContent() {
    switch (activeTab) {
      case 'AI Chat':                 return <AIChat key={chatKey} />
      case 'Cross Examination':       return <CrossExamination />
      case 'Prompt Library':          return <PromptLibrary />
      case 'History':                 return <AIHistory />
      case 'Observability & Metrics': return <AnalyticsDashboard />
      case 'Knowledge Base':          return <KnowledgeBase />
      default:                        return <AIChat key={chatKey} />
    }
  }

  return (
    <div className="glass-panel" style={{ flex: 1, overflowY: 'auto', margin: '16px', padding: 20, borderRadius: 24 }}>

      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
        <div>
          <h1 style={{ margin: 0, fontSize: 24, fontWeight: 700, color: '#0f172a', letterSpacing: '-0.5px' }}>
            AI Analytics
          </h1>
          <p style={{ marginTop: 4, color: '#64748b', fontSize: 13, fontWeight: 500 }}>
            AI chat, cross-examination, prompt library and history.
          </p>
        </div>

        {/* New Chat button only — no fake Credits or Upload Knowledge */}
        <button
          className="glass-button"
          onClick={() => { setChatKey(k => k + 1); setActiveTab('AI Chat') }}
          style={{ height: 38, padding: '0 16px', background: '#3b82f6', color: '#fff', border: 'none', borderRadius: 10, cursor: 'pointer', fontWeight: 600, fontSize: 13, display: 'flex', alignItems: 'center', gap: 6, boxShadow: '0 4px 12px rgba(59, 130, 246, 0.3)' }}
        >
          <i className="ti ti-message-chatbot" />
          New Chat
        </button>
      </div>

      {/* Tabs */}
      <AnalyticsTabs activeTab={activeTab} onChange={setActiveTab} />

      {/* Content */}
      <div style={{ marginTop: 24 }}>
        {renderContent()}
      </div>
    </div>
  )
}
