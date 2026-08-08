'use client'

import { useState, useEffect, useRef } from 'react'
import { useCaseStore } from '@/lib/store'
import { aiApi } from '@/lib/api'
import FormattedMarkdown from '@/components/common/FormattedMarkdown'

const QUICK_PROMPTS = [
  { icon: 'ti-file-text',    label: 'Case Summary',        message: 'Give me a complete summary of this case including parties, current stage, strengths and weaknesses.' },
  { icon: 'ti-scale',        label: 'Legal Research',      message: 'Find the most relevant Supreme Court and High Court judgments for this case with how to use each one.' },
  { icon: 'ti-gavel',        label: 'Cross Exam',          message: 'Generate 20 cross-examination questions for the opposing party\'s key witness based on the case facts.' },
  { icon: 'ti-target-arrow', label: 'Next Steps',          message: 'What are the 5 most critical actions I must take in the next 7 days for this case?' },
  { icon: 'ti-shield-check', label: 'Evidence Gaps',       message: 'What documents and evidence are missing from this case that could weaken our position?' },
  { icon: 'ti-currency-rupee', label: 'Maintenance Calc', message: 'Calculate the recommended maintenance amount based on Rajnesh v. Neha standard and the financial facts in this case.' },
]

interface Message {
  role: 'user' | 'assistant'
  text: string
  time: string
}

function now() {
  return new Date().toLocaleTimeString('en-IN', { hour: '2-digit', minute: '2-digit' })
}

export default function AIChat() {
  const { selectedCaseId } = useCaseStore()
  const [messages, setMessages] = useState<Message[]>([])
  const [input,    setInput]    = useState('')
  const [loading,  setLoading]  = useState(false)
  const [error,    setError]    = useState('')
  const [copied,   setCopied]   = useState<number | null>(null)
  const [isListening, setIsListening] = useState(false)
  const bottomRef = useRef<HTMLDivElement>(null)
  
  const mediaRecorderRef = useRef<MediaRecorder | null>(null)
  const streamRef = useRef<MediaStream | null>(null)
  const audioCtxRef = useRef<AudioContext | null>(null)
  const processorRef = useRef<ScriptProcessorNode | null>(null)
  const pcmBufferRef = useRef<Float32Array[]>([])
  const chunkIntervalRef = useRef<ReturnType<typeof setInterval> | null>(null)

  useEffect(() => {
    return () => {
      if (chunkIntervalRef.current) clearInterval(chunkIntervalRef.current)
      processorRef.current?.disconnect()
      audioCtxRef.current?.close()
      streamRef.current?.getTracks().forEach(t => t.stop())
    }
  }, [])

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages, loading])

  async function send(text: string) {
    if (!text.trim() || loading) return
    setError('')
    const history = messages.map(m => m.text)
    setMessages(prev => [...prev, { role: 'user', text, time: now() }])
    setInput('')
    setLoading(true)
    try {
      const res = await aiApi.chat({ message: text, caseId: selectedCaseId || undefined, history })
      const reply = res.response ?? res.result ?? ''
      setMessages(prev => [...prev, { role: 'assistant', text: reply, time: now() }])
      // Save to history
      const stored = JSON.parse(localStorage.getItem('clausio_ai_history') || '[]')
      stored.unshift({ query: text, response: reply, time: new Date().toISOString(), caseId: selectedCaseId })
      localStorage.setItem('clausio_ai_history', JSON.stringify(stored.slice(0, 100)))
    } catch (err: any) {
      setError(err.message || 'Failed to get AI response. Please try again.')
    } finally {
      setLoading(false)
    }
  }

  function copyMessage(idx: number, text: string) {
    navigator.clipboard.writeText(text)
    setCopied(idx)
    setTimeout(() => setCopied(null), 2000)
  }

  function clearChat() {
    setMessages([])
    setInput('')
    setError('')
  }

  const handleVoiceInput = async () => {
    if (isListening) {
      mediaRecorderRef.current?.stop()
      streamRef.current?.getTracks().forEach(t => t.stop())
      mediaRecorderRef.current = null
      streamRef.current = null
      setIsListening(false)
      return
    }

    try {
      await navigator.mediaDevices.getUserMedia({ audio: true })
      const devices = await navigator.mediaDevices.enumerateDevices()
      const audioInputs = devices.filter(d => d.kind === 'audioinput')
      const realMic = audioInputs.find(d => 
        !d.label.toLowerCase().includes('zoom') && 
        !d.label.toLowerCase().includes('blackhole') &&
        d.deviceId !== 'default' &&
        d.deviceId !== 'communications'
      )

      const stream = await navigator.mediaDevices.getUserMedia({
        audio: realMic ? { deviceId: { exact: realMic.deviceId } } : true
      })
      
      streamRef.current = stream
      const audioTrack = stream.getAudioTracks()[0]
      console.log(`[voice] Using microphone: ${audioTrack?.label || 'Unknown'}`)
      
      const originalText = input.trim()
      const mimeType = MediaRecorder.isTypeSupported('audio/webm;codecs=opus')
        ? 'audio/webm;codecs=opus'
        : 'audio/webm'

      const recorder = new MediaRecorder(stream, { mimeType })
      mediaRecorderRef.current = recorder
      const audioChunks: Blob[] = []

      recorder.ondataavailable = async (e) => {
        if (e.data.size > 0) {
          audioChunks.push(e.data)
          const fullBlob = new Blob(audioChunks, { type: mimeType })
          try {
            const fd = new FormData()
            fd.append('file', fullBlob, 'audio.webm')
            const res = await fetch('http://127.0.0.1:8000/api/voice', { method: 'POST', body: fd })
            if (res.ok) {
              const data = await res.json()
              if (data.text?.trim()) {
                setInput(originalText + (originalText ? ' ' : '') + data.text.trim())
              }
            }
          } catch (err) {
            console.error('Voice API error', err)
          }
        }
      }

      recorder.onstop = () => setIsListening(false)
      recorder.start(1500)
      setIsListening(true)
    } catch (e) {
      console.error(e)
      alert('Microphone access denied. Please allow microphone access and try again.')
      setIsListening(false)
    }
  }

  return (
    <div>
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
        <div>
          <h2 style={{ margin: 0, fontSize: 20, fontWeight: 700, color: '#0f172a' }}>AI Chat</h2>
          <p style={{ marginTop: 4, color: '#64748b', fontSize: 13 }}>
            Ask Clausio anything about your case, law, or court procedure.
            {selectedCaseId && <span style={{ color: '#2563eb', fontWeight: 600 }}> · Case loaded</span>}
          </p>
        </div>
        <button onClick={clearChat} style={{ height: 36, padding: '0 14px', background: '#f1f5f9', border: '1px solid #e2e8f0', borderRadius: 8, cursor: 'pointer', fontSize: 13, fontWeight: 600, color: '#475569', fontFamily: 'inherit', display: 'flex', alignItems: 'center', gap: 6 }}>
          <i className="ti ti-plus" /> New Chat
        </button>
      </div>

      {/* Quick prompts */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 10, marginBottom: 20 }}>
        {QUICK_PROMPTS.map(p => (
          <button key={p.label} onClick={() => send(p.message)} disabled={loading}
            style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '10px 14px', background: '#fff', border: '1px solid #e2e8f0', borderRadius: 10, cursor: loading ? 'not-allowed' : 'pointer', fontFamily: 'inherit', textAlign: 'left', transition: 'all 0.15s' }}
            onMouseEnter={e => { e.currentTarget.style.borderColor = '#3b82f6'; e.currentTarget.style.background = '#eff6ff' }}
            onMouseLeave={e => { e.currentTarget.style.borderColor = '#e2e8f0'; e.currentTarget.style.background = '#fff' }}>
            <i className={`ti ${p.icon}`} style={{ fontSize: 18, color: '#3b82f6', flexShrink: 0 }} />
            <span style={{ fontSize: 12, fontWeight: 600, color: '#0f172a' }}>{p.label}</span>
          </button>
        ))}
      </div>

      {/* Chat window */}
      <div style={{ background: '#fff', border: '1px solid #e2e8f0', borderRadius: 16, overflow: 'hidden' }}>
        <div style={{ padding: 20, minHeight: 380, maxHeight: 480, overflowY: 'auto' }}>

          {/* Welcome */}
          {messages.length === 0 && (
            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', minHeight: 340, gap: 16 }}>
              <div style={{ width: 64, height: 64, borderRadius: '50%', overflow: 'hidden', display: 'flex', alignItems: 'center', justifyContent: 'center', boxShadow: '0 8px 32px rgba(56, 189, 248, 0.4)', border: '1px solid rgba(255,255,255,0.8)' }}>
                <video src="/aivideo.mp4" autoPlay loop muted playsInline style={{ width: '100%', height: '100%', objectFit: 'cover', transform: 'scale(1.8)' }} />
              </div>
              <h3 style={{ fontSize: 20, fontWeight: 600, color: '#0f172a', margin: 0, letterSpacing: '-0.01em' }}>Ask Clausio anything</h3>
              <p style={{ color: '#64748b', fontSize: 13, textAlign: 'center', maxWidth: 400, lineHeight: 1.6 }}>
                {selectedCaseId
                    ? 'I have your selected case loaded. Ask me anything about it or use the quick prompts above.'
                    : 'Select a case from the dashboard for case-specific answers, or ask any general legal question.'}
              </p>
            </div>
          )}

          {/* Messages */}
          {messages.map((m, i) => (
            <div key={i} style={{ marginBottom: 20 }}>
              {m.role === 'user' ? (
                <div style={{ display: 'flex', justifyContent: 'flex-end', padding: '0 4px' }}>
                  <div style={{ 
                    maxWidth: '85%', 
                    background: '#ffffff', 
                    color: '#0f172a',
                    border: '1px solid rgba(0,0,0,0.06)',
                    boxShadow: '0 4px 16px rgba(0,0,0,0.04)',
                    borderRadius: 18, 
                    borderBottomRightRadius: 4,
                    padding: '12px 16px',
                    position: 'relative'
                  }}>
                    <p style={{ fontSize: 13, lineHeight: 1.5, margin: 0, fontWeight: 500, whiteSpace: 'pre-wrap' }}>{m.text}</p>
                    <span style={{ position: 'absolute', bottom: -18, right: 4, fontSize: 10, color: '#94a3b8' }}>{m.time}</span>
                  </div>
                </div>
              ) : (
                <div style={{ display: 'flex', alignItems: 'flex-start', gap: 12, padding: '0 4px', maxWidth: '90%' }}>
                  <div style={{ width: 28, height: 28, borderRadius: '50%', overflow: 'hidden', display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0, border: '1px solid rgba(0,0,0,0.1)', marginTop: 2 }}>
                    <video src="/aivideo.mp4" autoPlay loop muted playsInline style={{ width: '100%', height: '100%', objectFit: 'cover', transform: 'scale(1.8)' }} />
                  </div>
                  <div style={{ flex: 1, position: 'relative', paddingRight: 32 }}>
                    <FormattedMarkdown content={m.text} />
                    <button onClick={() => copyMessage(i, m.text)}
                      style={{ position: 'absolute', top: -4, right: 0, background: 'none', border: 'none', cursor: 'pointer', color: '#94a3b8', fontSize: 14, padding: 4, transition: 'color 0.2s' }}
                      onMouseEnter={e => e.currentTarget.style.color = '#3b82f6'}
                      onMouseLeave={e => e.currentTarget.style.color = '#94a3b8'}
                    >
                      <i className={`ti ${copied === i ? 'ti-check' : 'ti-copy'}`} style={{ color: copied === i ? '#22c55e' : 'inherit' }} />
                    </button>
                    <span style={{ display: 'block', marginTop: 8, fontSize: 10, color: '#94a3b8' }}>{m.time}</span>
                  </div>
                </div>
              )}
            </div>
          ))}

          {loading && (
            <div style={{ display: 'flex', alignItems: 'flex-start', gap: 12, padding: '0 4px', maxWidth: '90%' }}>
              <div style={{ width: 28, height: 28, borderRadius: '50%', overflow: 'hidden', display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0, border: '1px solid rgba(0,0,0,0.1)', marginTop: 2 }}>
                <video src="/aivideo.mp4" autoPlay loop muted playsInline style={{ width: '100%', height: '100%', objectFit: 'cover', transform: 'scale(1.8)' }} />
              </div>
              <div style={{ flex: 1 }}>
                <p style={{ fontSize: 13, color: '#64748b', lineHeight: 1.6, margin: 0, fontWeight: 500, display: 'flex', alignItems: 'center', gap: 8 }}>
                  <i className="ti ti-loader animate-spin" style={{ fontSize: 16 }} />
                  Thinking...
                </p>
              </div>
            </div>
          )}

          {error && (
            <div style={{ padding: '10px 14px', background: '#fef2f2', border: '1px solid #fca5a5', borderRadius: 8, fontSize: 13, color: '#dc2626', margin: '8px 0' }}>
              {error}
            </div>
          )}

          <div ref={bottomRef} />
        </div>

        {/* Input */}
        <div style={{ padding: '12px 14px', background: 'transparent', flexShrink: 0, borderTop: 'none', position: 'relative' }}>
          {/* subtle fade up to mask text scrolling under input */}
          <div style={{ position: 'absolute', top: -32, left: 0, right: 0, height: 32, background: 'linear-gradient(to top, #fff, transparent)', pointerEvents: 'none' }} />
          <div className="apple-intelligence-chat-pill">
            <textarea
              rows={1}
              className="apple-intelligence-input-text"
              value={input}
              onChange={e => setInput(e.target.value)}
              onKeyDown={e => {
                if (e.key === 'Enter' && !e.shiftKey) {
                  e.preventDefault()
                  send(input)
                }
              }}
              placeholder={isListening ? "Listening..." : "Ask Clausio about this case..."}
              style={{ resize: 'none', maxHeight: 120, paddingTop: 6, paddingBottom: 6 }}
            />
            <button
              onClick={handleVoiceInput}
              title="Voice Typing"
              style={{ 
                background: 'none', 
                border: 'none', 
                cursor: 'pointer',
                color: isListening ? '#ef4444' : '#64748b',
                marginRight: 8,
                transition: 'transform 0.2s cubic-bezier(0.23, 1, 0.32, 1), color 0.2s',
                transform: isListening ? 'scale(1.15)' : 'scale(1)'
              }}
            >
              <i className="ti ti-microphone" style={{ fontSize: 18 }} />
            </button>
            <button
              onClick={() => send(input)}
              disabled={loading || !input.trim()}
              className="apple-intelligence-send-btn"
              title="Send message"
              style={{ opacity: loading || !input.trim() ? 0.5 : 1 }}
            >
              {loading ? <i className="ti ti-loader animate-spin" /> : <i className="ti ti-arrow-up" style={{ fontSize: 16 }} />}
            </button>
          </div>
          <div style={{ display: 'flex', justifyContent: 'space-between', padding: '6px 8px 0', fontSize: 10, color: '#64748b' }}>
            <span>Press Enter to send · Shift + Enter for new line</span>
          </div>
        </div>
      </div>
    </div>
  )
}
