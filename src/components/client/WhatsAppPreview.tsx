'use client'

import { useState, useEffect } from 'react'
import { aiApi, parseAiJson } from '@/lib/api'

interface Props {
  message:    string
  generating: boolean
  onRegenerate: (tone: string, language: string) => void
}

function formatMessageText(raw: string): string {
  if (!raw) return ''
  let text = raw.trim()

  // 1. Robust regex extraction for JSON fields even when unescaped control chars exist
  const jsonMatch = text.match(/"(?:DraftText|message|text|result)"\s*:\s*"([\s\S]*?)"\s*\}?\s*$/) ||
                    text.match(/"(?:DraftText|message|text|result)"\s*:\s*"([\s\S]*)"/)
  if (jsonMatch && jsonMatch[1]) {
    text = jsonMatch[1]
  } else {
    const parsed = parseAiJson<any>(text)
    if (parsed) {
      if (typeof parsed === 'string') text = parsed
      else if (parsed.DraftText) text = parsed.DraftText
      else if (parsed.message) text = parsed.message
      else if (parsed.text) text = parsed.text
      else if (parsed.result) text = parsed.result
    }
  }

  // 2. Clean markdown code blocks and raw JSON artifacts
  text = text.replace(/^```json\s*/i, '')
    .replace(/^```\s*/i, '')
    .replace(/```\s*$/i, '')
    .replace(/^\{\s*"(?:DraftText|message|text|result)"\s*:\s*"/i, '')
    .replace(/"\s*\}\s*$/i, '')
    .trim()

  // 3. Unescape double-escaped newlines and quotes
  text = text.replace(/\\n/g, '\n').replace(/\\"/g, '"').replace(/\\\\/g, '\\')

  return text.trim()
}

function renderFormattedText(text: string) {
  if (!text) return null
  const lines = text.split('\n')

  return lines.map((line, lineIdx) => {
    // Split by markdown bold markers (**text** or *text*)
    const parts = line.split(/(\*\*.*?\*\*|\*.*?\*)/g)

    return (
      <div key={lineIdx} style={{ minHeight: line.trim() ? 'auto' : '1.2em' }}>
        {parts.map((part, partIdx) => {
          if ((part.startsWith('**') && part.endsWith('**')) || (part.startsWith('*') && part.endsWith('*'))) {
            const clean = part.replace(/^(\*\*|\*)/, '').replace(/(\*\*|\*)$/, '')
            return <strong key={partIdx} style={{ fontWeight: 700, color: '#0f172a' }}>{clean}</strong>
          }
          return <span key={partIdx}>{part}</span>
        })}
      </div>
    )
  })
}

const GENERATING_STEPS = [
  { icon: 'ti-database-search', text: 'Gathering case memories & hearing records...' },
  { icon: 'ti-brain', text: 'Analyzing legal context & client intent...' },
  { icon: 'ti-message-code', text: 'Drafting structured WhatsApp update...' },
  { icon: 'ti-sparkles', text: 'Polishing language, tone & formatting...' }
]

function GeneratingIndicator() {
  const [step, setStep] = useState(0)

  useEffect(() => {
    const timer = setInterval(() => {
      setStep((prev) => (prev + 1) % GENERATING_STEPS.length)
    }, 1400)
    return () => clearInterval(timer)
  }, [])

  const current = GENERATING_STEPS[step]

  return (
    <div
      style={{
        margin: 'auto',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        padding: '24px 20px',
        background: 'rgba(255, 255, 255, 0.85)',
        backdropFilter: 'blur(16px)',
        borderRadius: 20,
        boxShadow: '0 8px 30px rgba(0, 0, 0, 0.08)',
        border: '1px solid rgba(255, 255, 255, 0.8)',
        maxWidth: 360,
        textAlign: 'center',
      }}
    >
      <div style={{ position: 'relative', width: 44, height: 44, marginBottom: 14, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
        <div 
          className="animate-spin" 
          style={{ 
            position: 'absolute', 
            inset: 0, 
            borderRadius: '50%', 
            border: '3px solid #e2e8f0', 
            borderTopColor: '#2563eb' 
          }} 
        />
        <i className={`ti ${current.icon}`} style={{ fontSize: 20, color: '#2563eb' }} />
      </div>

      <div style={{ fontWeight: 600, fontSize: 14, color: '#0f172a', marginBottom: 4, display: 'flex', alignItems: 'center', gap: 6 }}>
        <span style={{ width: 8, height: 8, borderRadius: '50%', background: '#22c55e', display: 'inline-block', boxShadow: '0 0 8px #22c55e' }} />
        AI Pipeline Active
      </div>

      <div style={{ fontSize: 13, color: '#475569', fontWeight: 500, minHeight: 20, transition: 'all 0.3s ease' }}>
        {current.text}
      </div>
    </div>
  )
}

export default function WhatsAppPreview({ message, generating, onRegenerate }: Props) {
  const [translating, setTranslating] = useState(false)
  const [copied,       setCopied]     = useState(false)
  const [translated,   setTranslated] = useState('')
  const [customText,   setCustomText] = useState<string | null>(null)
  const [isEditing,    setIsEditing]  = useState(false)

  useEffect(() => {
    setCustomText(null)
    setIsEditing(false)
  }, [message])

  // Extract base formatted text
  const baseText = formatMessageText(translated || message)
  
  // Use custom live-edited text if user modified it, else base text
  const activeText = customText !== null ? customText : baseText

  async function handleTranslate() {
    if (!activeText.trim()) return
    setTranslating(true)
    try {
      const res = await aiApi.translate({ text: activeText })
      const text = res.translatedText ?? res.result ?? ''
      setTranslated(text)
      setCustomText(null)
    } catch (err) {
      console.error(err)
    } finally {
      setTranslating(false)
    }
  }

  async function handleCopy() {
    if (!activeText.trim()) return
    // Convert **bold** to *bold* for native WhatsApp formatting on clipboard
    const waText = activeText.replace(/\*\*([^*]+)\*\*/g, '*$1*')
    await navigator.clipboard.writeText(waText)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  return (
    <div
      style={{
        background: '#ffffff',
        border: '1px solid #e2e8f0',
        borderRadius: 16,
        padding: 24,
        boxShadow: '0 2px 8px rgba(15,23,42,.04)',
        display: 'flex',
        flexDirection: 'column',
        height: '100%',
        maxHeight: '100%',
        overflow: 'hidden',
      }}
    >
      {/* Header */}

      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          marginBottom: 16,
          flexShrink: 0,
        }}
      >
        <div>
          <h2
            style={{
              margin: 0,
              fontSize: 22,
              fontWeight: 700,
              color: '#0f172a',
            }}
          >
            WhatsApp Preview
          </h2>

          <p
            style={{
              marginTop: 5,
              color: '#64748b',
              fontSize: 14,
            }}
          >
            AI generated message · Click text or edit button to customize
          </p>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          {activeText && (
            <button
              onClick={() => setIsEditing(!isEditing)}
              style={{
                padding: '6px 12px',
                borderRadius: 20,
                border: '1px solid #cbd5e1',
                background: isEditing ? '#eff6ff' : '#f8fafc',
                color: isEditing ? '#2563eb' : '#475569',
                fontWeight: 600,
                fontSize: 12,
                cursor: 'pointer',
                display: 'flex',
                alignItems: 'center',
                gap: 6,
                fontFamily: 'inherit',
              }}
            >
              <i className={isEditing ? "ti ti-check" : "ti ti-edit"} />
              {isEditing ? 'Done Editing' : 'Edit Text'}
            </button>
          )}

          <span
            style={{
              background: activeText ? '#dcfce7' : '#f1f5f9',
              color: activeText ? '#15803d' : '#64748b',
              padding: '8px 14px',
              borderRadius: 20,
              fontWeight: 600,
              fontSize: 13,
            }}
          >
            {activeText ? (customText !== null ? 'Edited' : 'Ready') : 'No message yet'}
          </span>
        </div>
      </div>

      {/* Phone */}

      <div
        style={{
          flex: 1,
          minHeight: 0,
          background: '#ece5dd',
          borderRadius: 18,
          padding: 20,
          overflowY: 'auto',
          display: 'flex',
          flexDirection: 'column',
        }}
      >
        {generating && (
          <GeneratingIndicator />
        )}
        {!generating && !activeText && (
          <div style={{ textAlign: 'center', color: '#94a3b8', fontSize: 13, padding: 20 }}>
            Configure the update on the left and click Generate for WhatsApp.
          </div>
        )}
        {!generating && activeText && (
          <div
            style={{
              background: '#dcf8c6',
              padding: 18,
              borderRadius: 14,
              width: isEditing ? '100%' : 'auto',
              maxWidth: isEditing ? '100%' : '92%',
              marginLeft: isEditing ? '0' : 'auto',
              flex: isEditing ? 1 : 'initial',
              display: 'flex',
              flexDirection: 'column',
              minHeight: isEditing ? 0 : 'initial',
              lineHeight: 1.6,
              fontSize: 14,
              color: '#111827',
              boxShadow: '0 2px 6px rgba(0,0,0,.08)',
              position: 'relative',
              transition: 'all 0.2s cubic-bezier(0.4, 0, 0.2, 1)',
            }}
          >
            {isEditing ? (
              <textarea
                value={activeText}
                onChange={(e) => setCustomText(e.target.value)}
                placeholder="Edit message here..."
                style={{
                  width: '100%',
                  height: '100%',
                  flex: 1,
                  minHeight: 0,
                  border: 'none',
                  outline: 'none',
                  background: 'transparent',
                  fontFamily: 'inherit',
                  fontSize: 14,
                  lineHeight: 1.6,
                  color: '#111827',
                  resize: 'none',
                }}
              />
            ) : (
              <div 
                onClick={() => setIsEditing(true)} 
                title="Click to edit text directly"
                style={{ cursor: 'text' }}
              >
                {renderFormattedText(activeText)}
              </div>
            )}
          </div>
        )}
      </div>

      {/* Footer */}

      <div
        style={{
          display: 'flex',
          gap: 12,
          marginTop: 16,
          flexShrink: 0,
        }}
      >
        <button
          onClick={() => onRegenerate('Reassuring', 'Hinglish (Hindi + English)')}
          disabled={generating}
          style={{ ...secondaryButton, cursor: generating ? 'not-allowed' : 'pointer' }}
        >
          <i className="ti ti-refresh" />
          Regenerate
        </button>

        <button
          onClick={handleTranslate}
          disabled={translating || !activeText}
          style={{ ...secondaryButton, cursor: (translating || !activeText) ? 'not-allowed' : 'pointer' }}
        >
          <i className="ti ti-language" />
          {translating ? 'Translating...' : 'Translate'}
        </button>

        <button
          onClick={handleCopy}
          disabled={!activeText}
          style={{ ...primaryButton, opacity: activeText ? 1 : 0.6, cursor: activeText ? 'pointer' : 'not-allowed' }}
        >
          <i className="ti ti-copy" />
          {copied ? 'Copied!' : 'Copy for WhatsApp'}
        </button>
      </div>
    </div>
  )
}

const primaryButton: React.CSSProperties = {
  flex: 1,
  background: '#22c55e',
  color: '#ffffff',
  border: 'none',
  borderRadius: 12,
  padding: '14px',
  fontSize: 14,
  fontWeight: 700,
  cursor: 'pointer',
  display: 'flex',
  justifyContent: 'center',
  alignItems: 'center',
  gap: 8,
}

const secondaryButton: React.CSSProperties = {
  background: '#f8fafc',
  color: '#334155',
  border: '1px solid #cbd5e1',
  borderRadius: 12,
  padding: '14px 18px',
  fontSize: 14,
  fontWeight: 600,
  cursor: 'pointer',
  display: 'flex',
  justifyContent: 'center',
  alignItems: 'center',
  gap: 8,
}
