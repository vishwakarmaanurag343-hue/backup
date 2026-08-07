import { useEffect, useRef } from 'react'

interface CitationPanelProps {
  isOpen: boolean
  title: string
  content: string
  onClose: () => void
}

export default function CitationPanel({ isOpen, title, content, onClose }: CitationPanelProps) {
  const panelRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (panelRef.current && !panelRef.current.contains(event.target as Node)) {
        onClose()
      }
    }
    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside)
    }
    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [isOpen, onClose])

  return (
    <div
      style={{
        position: 'fixed',
        top: 0,
        right: isOpen ? 0 : -450,
        bottom: 0,
        width: 400,
        background: '#ffffff',
        borderLeft: '1px solid rgba(0,0,0,0.1)',
        boxShadow: '-8px 0 32px rgba(0,0,0,0.05)',
        transition: 'right 0.3s cubic-bezier(0.23, 1, 0.32, 1)',
        zIndex: 100,
        display: 'flex',
        flexDirection: 'column',
      }}
      ref={panelRef}
    >
      <div style={{ padding: '20px 24px', borderBottom: '1px solid rgba(0,0,0,0.06)', display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <div style={{ width: 32, height: 32, borderRadius: 8, background: '#f1f5f9', display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#3b82f6' }}>
            <i className="ti ti-book" style={{ fontSize: 18 }} />
          </div>
          <h3 style={{ fontSize: 15, fontWeight: 600, color: '#0f172a', margin: 0 }}>{title}</h3>
        </div>
        <button
          onClick={onClose}
          style={{ background: 'none', border: 'none', cursor: 'pointer', color: '#64748b', display: 'flex', alignItems: 'center', justifyContent: 'center', padding: 4, borderRadius: '50%' }}
        >
          <i className="ti ti-x" style={{ fontSize: 18 }} />
        </button>
      </div>
      
      <div style={{ flex: 1, overflowY: 'auto', padding: '24px', fontSize: 13, lineHeight: 1.6, color: '#334155', fontFamily: 'serif' }}>
        {content.split('\\n').map((paragraph, i) => (
          <p key={i} style={{ marginBottom: 16 }}>{paragraph}</p>
        ))}
      </div>
      
      <div style={{ padding: '16px 24px', borderTop: '1px solid rgba(0,0,0,0.06)', background: '#f8fafc', fontSize: 11, color: '#64748b', display: 'flex', alignItems: 'center', gap: 8 }}>
        <i className="ti ti-shield-check" style={{ color: '#10b981', fontSize: 14 }} />
        <span>Verified exact match from case database</span>
      </div>
    </div>
  )
}
