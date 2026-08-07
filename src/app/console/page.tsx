import AIConsoleDashboard from '@/components/console/AIConsoleDashboard'

export const metadata = {
  title: 'AI Console & Model Telemetry | Clausio',
}

export default function ConsolePage() {
  return (
    <div className="glass-panel" style={{ flex: 1, overflowY: 'auto', margin: '16px', padding: 24, borderRadius: 24 }}>
      <div style={{ marginBottom: 20, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <h1 style={{ margin: 0, fontSize: 24, fontWeight: 700, color: '#0f172a', letterSpacing: '-0.5px' }}>
            AI Developer Console & Telemetry
          </h1>
          <p style={{ marginTop: 4, color: '#64748b', fontSize: 13, fontWeight: 500 }}>
            Real-time tracking of AI models, token consumption (In/Out), prompt latency, and endpoint fallback logs.
          </p>
        </div>
      </div>
      
      <AIConsoleDashboard />
    </div>
  )
}
