'use client'

interface Props {
  activeTab: string
  onChange:  (tab: string) => void
}

const TABS = [
  { name: 'AI Chat', icon: 'ti-message-chatbot' },
  { name: 'Cross Examination', icon: 'ti-gavel' },
  { name: 'Prompt Library', icon: 'ti-library' },
  { name: 'History', icon: 'ti-history' },
  { name: 'Observability & Metrics', icon: 'ti-chart-bar' },
  { name: 'Knowledge Base', icon: 'ti-brain' },
]

export default function AnalyticsTabs({ activeTab, onChange }: Props) {
  return (
    <div style={{ display: 'flex', gap: 4, borderBottom: '1px solid #e2e8f0', marginBottom: 0, overflowX: 'auto' }}>
      {TABS.map(tab => {
        const isActive = activeTab === tab.name
        const isSoon   = tab.name === 'Knowledge Base'
        return (
          <button
            key={tab.name}
            onClick={() => onChange(tab.name)}
            style={{
              display: 'flex', alignItems: 'center', gap: 6,
              padding: '10px 16px',
              border: 'none',
              borderBottom: isActive ? '2px solid #2563eb' : '2px solid transparent',
              marginBottom: -1,
              cursor: 'pointer',
              fontFamily: 'inherit',
              fontSize: 13,
              fontWeight: 600,
              background: 'transparent',
              whiteSpace: 'nowrap',
              color: isActive ? '#1e40af' : '#64748b',
              transition: 'all 0.15s',
            }}
          >
            <i className={`ti ${tab.icon}`} style={{ fontSize: 15 }} />
            {tab.name}
            {isSoon && (
              <span style={{ fontSize: 9, padding: '1px 5px', background: '#fef3c7', color: '#d97706', borderRadius: 8, fontWeight: 700 }}>
                SOON
              </span>
            )}
          </button>
        )
      })}
    </div>
  )
}
