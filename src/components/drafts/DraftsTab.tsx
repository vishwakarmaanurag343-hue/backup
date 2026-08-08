'use client'

import React, { useState, useEffect } from 'react'
import { motion, type Variants } from 'framer-motion'
import { MotionButton } from '@/components/ui/Motion'
import { MotionCard } from '@/components/ui/Motion'
import { useCaseStore } from '@/lib/store'
import { aiApi, casesApi, parseAiJson } from '@/lib/api'
import CaseTypeBadge from '@/components/ui/CaseTypeBadge'
import { getDraftTypesForCase, type DraftType } from '@/lib/draftTypes'

const containerVariants: Variants = {
  hidden: { opacity: 0 },
  show: { opacity: 1, transition: { staggerChildren: 0.08, delayChildren: 0.1 } }
}

const itemVariants: Variants = {
  hidden: { opacity: 0, y: 15, scale: 0.98 },
  show: { opacity: 1, y: 0, scale: 1, transition: { type: 'spring', stiffness: 300, damping: 24 } }
}

function formatLegalDraftText(raw: any): string {
  if (!raw) return ''
  let text = ''

  if (typeof raw === 'object') {
    if (raw.DraftText) text = String(raw.DraftText)
    else if (raw.draft) text = String(raw.draft)
    else if (raw.result) text = String(raw.result)
    else if (raw.seniorCounselBrief) text = String(raw.seniorCounselBrief)
    else text = JSON.stringify(raw)
  } else {
    text = String(raw).trim()
  }

  // 1. Try JSON parsing first
  try {
    const parsed = JSON.parse(text)
    if (parsed) {
      if (typeof parsed === 'string') text = parsed.trim()
      else if (parsed.DraftText) text = String(parsed.DraftText).trim()
      else if (parsed.draft) text = String(parsed.draft).trim()
      else if (parsed.result) text = String(parsed.result).trim()
      else if (parsed.seniorCounselBrief) text = String(parsed.seniorCounselBrief).trim()
    }
  } catch (e) {
    const parsed = parseAiJson<any>(text)
    if (parsed) {
      if (typeof parsed === 'string') text = parsed
      else if (parsed.DraftText) text = String(parsed.DraftText)
      else if (parsed.draft) text = String(parsed.draft)
      else if (parsed.result) text = String(parsed.result)
      else if (parsed.seniorCounselBrief) text = String(parsed.seniorCounselBrief)
    }
  }

  // 2. Clean markdown code blocks and raw JSON keys safely without truncating internal quotes
  text = text.replace(/^```markdown\s*/i, '')
    .replace(/^```json\s*/i, '')
    .replace(/^```\s*/i, '')
    .replace(/```\s*$/i, '')

  if (text.startsWith('{') && text.includes('"DraftText"')) {
    const draftTextIdx = text.indexOf('"DraftText"')
    const colonIdx = text.indexOf(':', draftTextIdx)
    const startQuoteIdx = text.indexOf('"', colonIdx)
    if (startQuoteIdx !== -1) {
      const lastQuoteIdx = text.lastIndexOf('"')
      if (lastQuoteIdx > startQuoteIdx) {
        text = text.substring(startQuoteIdx + 1, lastQuoteIdx)
      }
    }
  }

  // 3. Unescape double-escaped newlines and quotes
  text = text.replace(/\\n/g, '\n').replace(/\\"/g, '"').replace(/\\\\/g, '\\')

  // 4. Strip any UI watermark artifact lines
  text = text
    .split('\n')
    .filter(line => {
      const t = line.trim()
      if (/^CLAUSIO LEGAL AI/i.test(t)) return false
      if (/^CONFIDENTIAL LEGAL DOCUMENT/i.test(t)) return false
      if (/^Page \d+ of \d+$/i.test(t)) return false
      return true
    })
    .join('\n')

  return text.trim()
}

function renderA4LegalDocument(text: string, isEditing: boolean, onChangeText: (val: string) => void) {
  if (!text && !isEditing) return null

  if (isEditing) {
    return (
      <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', background: '#cbd5e1', padding: '24px 16px', borderRadius: 16 }}>
        <div
          className="a4-sheet"
          style={{
            width: '100%',
            maxWidth: 794,
            minHeight: 1123,
            height: 1123,
            background: '#ffffff',
            borderRadius: 2,
            border: '2px solid #3b82f6',
            boxShadow: '0 12px 36px rgba(59, 130, 246, 0.15), 0 1px 4px rgba(0,0,0,0.06)',
            padding: '40px 50px 50px 50px',
            position: 'relative',
            fontFamily: '"Times New Roman", Times, Cambria, "Book Antiqua", Georgia, serif',
            boxSizing: 'border-box',
            display: 'flex',
            flexDirection: 'column',
          }}
        >
          <div style={{ fontSize: 11, fontWeight: 700, color: '#1d4ed8', marginBottom: 12, display: 'flex', alignItems: 'center', gap: 6, fontFamily: 'system-ui, -apple-system, sans-serif', borderBottom: '1px solid #eff6ff', paddingBottom: 8, userSelect: 'none' }}>
            <i className="ti ti-edit" style={{ fontSize: 14 }} /> EDITING MODE — Type, add case numbers, or modify clauses directly on A4 paper
          </div>
          <textarea
            value={text}
            onChange={(e) => onChangeText(e.target.value)}
            placeholder="Type or edit your legal draft document here..."
            style={{
              width: '100%',
              flex: 1,
              minHeight: 920,
              border: 'none',
              outline: 'none',
              background: 'transparent',
              fontFamily: '"Times New Roman", Times, Cambria, "Book Antiqua", Georgia, serif',
              fontSize: 12.5,
              lineHeight: 1.65,
              color: '#0f172a',
              resize: 'vertical',
              boxSizing: 'border-box',
            }}
          />
        </div>
      </div>
    )
  }

  const formattedText = formatLegalDraftText(text)
  const allLines = formattedText.split('\n')
  
  // Dynamic visual line weight pagination for full, professional A4 paper fit (~36 visual lines per page)
  const MAX_VISUAL_LINES_PER_PAGE = 36
  const pages: string[][] = []
  let currentPageLines: string[] = []
  let currentVisualLinesCount = 0

  allLines.forEach((line) => {
    const trimmed = line.trim()
    let lineWeight = 1

    if (!trimmed) {
      lineWeight = 0.5
    } else {
      // Calculate wrapped lines based on ~90 characters per visual line at 12.5px font
      lineWeight = Math.max(1, Math.ceil(trimmed.length / 90))
    }

    if (currentVisualLinesCount + lineWeight > MAX_VISUAL_LINES_PER_PAGE && currentPageLines.length > 0) {
      pages.push(currentPageLines)
      currentPageLines = [line]
      currentVisualLinesCount = lineWeight
    } else {
      currentPageLines.push(line)
      currentVisualLinesCount += lineWeight
    }
  })

  if (currentPageLines.length > 0) {
    pages.push(currentPageLines)
  }

  const totalPages = pages.length

  return (
    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 32, paddingBottom: 40, background: '#cbd5e1', padding: '24px 16px', borderRadius: 16 }}>
      {pages.map((pageLines, pageIdx) => (
        <div
          key={pageIdx}
          className="a4-sheet"
          style={{
            width: '100%',
            maxWidth: 794,
            minHeight: 1000,
            background: '#ffffff',
            borderRadius: 2,
            border: '1px solid #94a3b8',
            boxShadow: '0 10px 30px rgba(15, 23, 42, 0.12), 0 1px 4px rgba(0,0,0,0.06)',
            padding: '48px 56px 48px 56px',
            position: 'relative',
            fontFamily: '"Times New Roman", Times, Cambria, "Book Antiqua", Georgia, serif',
            boxSizing: 'border-box',
            display: 'flex',
            flexDirection: 'column',
          }}
        >
          {/* Header page indicator watermark */}
          <div style={{ position: 'absolute', top: 18, right: 36, fontSize: 11, fontWeight: 600, color: '#94a3b8', fontFamily: 'system-ui, -apple-system, sans-serif', userSelect: 'none' }}>
            Page {pageIdx + 1} of {totalPages}
          </div>

          {/* Page Content */}
          <div>
            {pageLines.map((line, idx) => {
              const trimmed = line.trim()

              if (!trimmed) {
                return <div key={idx} style={{ height: '0.6em' }} />
              }

              const pureText = trimmed.replace(/^[#*\s]+/, '').replace(/[#*\s]+$/, '').trim()

              const isCourtHeader = /^(IN THE|BEFORE THE|ORIGINAL JURISDICTION|\(ORIGINAL JURISDICTION\)|WRIT PETITION|SPECIAL LEAVE PETITION|IN THE MATTER OF|MEMORANDUM OF APPEAL|MOST RESPECTFULLY SHOWETH)/i.test(pureText)
              const isSectionHeading = /^(\d+\.|\bGROUNDS\b|\bPRAYER\b|\bINTERIM RELIEF\b|\bVERIFICATION\b|\bPETITIONER\b|\bRESPONDENT\b)/i.test(pureText)

              if (isCourtHeader) {
                return (
                  <div key={idx} style={{ textAlign: 'center', fontWeight: 700, fontSize: 13.5, letterSpacing: '0.04em', textTransform: 'uppercase', color: '#0f172a', margin: '10px 0 4px 0', fontFamily: '"Times New Roman", Times, serif' }}>
                    {pureText}
                  </div>
                )
              }

              if (isSectionHeading) {
                return (
                  <div key={idx} style={{ fontWeight: 700, fontSize: 13, color: '#0f172a', marginTop: 14, marginBottom: 6, textTransform: 'uppercase', letterSpacing: '0.02em', fontFamily: '"Times New Roman", Times, serif' }}>
                    {pureText}
                  </div>
                )
              }

              const cleanLine = trimmed.replace(/^[#\s]+/, '')
              const parts = cleanLine.split(/(\*\*.*?\*\*|\*.*?\*)/g)
              return (
                <div key={idx} style={{ fontSize: 12.5, lineHeight: 1.65, color: '#1e293b', textAlign: 'justify', textJustify: 'inter-word', marginBottom: 3 }}>
                  {parts.map((part, pIdx) => {
                    if ((part.startsWith('**') && part.endsWith('**')) || (part.startsWith('*') && part.endsWith('*'))) {
                      const clean = part.replace(/^(\*\*|\*)/, '').replace(/(\*\*|\*)$/, '')
                      return <strong key={pIdx} style={{ fontWeight: 700, color: '#0f172a' }}>{clean}</strong>
                    }
                    return <span key={pIdx}>{part}</span>
                  })}
                </div>
              )
            })}
          </div>

          {/* Footer watermark */}
          <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 9.5, color: '#94a3b8', fontFamily: 'system-ui, -apple-system, sans-serif', borderTop: '1px solid #f1f5f9', paddingTop: 8, marginTop: 'auto', userSelect: 'none' }}>
            <span>CLAUSIO LEGAL AI · ADVOCATE COURT DRAFT</span>
            <span>CONFIDENTIAL LEGAL DOCUMENT</span>
          </div>
        </div>
      ))}
    </div>
  )
}

const DRAFTING_STEPS = [
  { icon: 'ti-scale', text: 'Gathering case memories, petitions & evidence...' },
  { icon: 'ti-gavel', text: 'Analyzing statutory provisions & relevant grounds...' },
  { icon: 'ti-file-code', text: 'Structuring legal grounds & statement of facts...' },
  { icon: 'ti-sparkles', text: 'Drafting high court petition & prayer clauses...' }
]

function DraftingProgressIndicator({ draftType }: { draftType: string }) {
  const [step, setStep] = useState(0)

  useEffect(() => {
    const timer = setInterval(() => {
      setStep((prev) => (prev + 1) % DRAFTING_STEPS.length)
    }, 1500)
    return () => clearInterval(timer)
  }, [])

  const current = DRAFTING_STEPS[step]

  return (
    <div
      style={{
        margin: '60px auto',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        padding: '32px 24px',
        background: '#ffffff',
        borderRadius: 24,
        boxShadow: '0 8px 30px rgba(0, 0, 0, 0.05)',
        border: '1px solid #e2e8f0',
        maxWidth: 420,
        textAlign: 'center',
      }}
    >
      <div style={{ position: 'relative', width: 48, height: 48, marginBottom: 16, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
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
        <i className={`ti ${current.icon}`} style={{ fontSize: 22, color: '#2563eb' }} />
      </div>

      <div style={{ fontWeight: 700, fontSize: 15, color: '#0f172a', marginBottom: 4 }}>
        Drafting {draftType || 'Legal Document'}
      </div>

      <div style={{ fontSize: 13, color: '#475569', fontWeight: 500, minHeight: 20 }}>
        {current.text}
      </div>
    </div>
  )
}

export default function DraftsTab() {
  const { selectedCaseId } = useCaseStore()

  const [caseType,     setCaseType]     = useState('')
  const [draftTypes,   setDraftTypes]   = useState<DraftType[]>([])
  const [draftType,    setDraftType]    = useState('')
  const [instructions, setInstructions] = useState('')
  const [draft,           setDraft]           = useState('')
  const [customDraftText, setCustomDraftText] = useState<string | null>(null)
  const [isEditing,       setIsEditing]       = useState(false)
  const [generating,      setGenerating]      = useState(false)
  const [error,           setError]           = useState('')
  const [showTypeMenu,    setShowTypeMenu]    = useState(false)

  // ✅ Load case type & auto-select first case if none selected
  useEffect(() => {
    if (!selectedCaseId) {
      casesApi.getAll()
        .then((cases: any[]) => {
          if (cases && cases.length > 0) {
            useCaseStore.getState().setSelectedCase(cases[0].id, cases[0].name || '')
          } else {
            const types = getDraftTypesForCase('')
            setDraftTypes(types)
            setDraftType(types[0]?.label ?? 'Bail Application')
          }
        })
        .catch(() => {
          const types = getDraftTypesForCase('')
          setDraftTypes(types)
          setDraftType(types[0]?.label ?? 'Bail Application')
        })
      return
    }

    casesApi.getById(selectedCaseId)
      .then(data => {
        const ct = data?.caseType ?? ''
        setCaseType(ct)
        const types = getDraftTypesForCase(ct)
        setDraftTypes(types)
        setDraftType(types[0]?.label ?? '')
      })
      .catch(() => {
        const types = getDraftTypesForCase('')
        setDraftTypes(types)
        setDraftType(types[0]?.label ?? '')
      })
  }, [selectedCaseId])

  const selectedDraftInfo = draftTypes.find(t => t.label === draftType)
  const activeText = customDraftText !== null ? customDraftText : (draft ? formatLegalDraftText(draft) : '')

  async function handleGenerate() {
    let targetCaseId = selectedCaseId
    if (!targetCaseId) {
      try {
        const cases = await casesApi.getAll()
        if (cases && cases.length > 0) {
          targetCaseId = cases[0].id
          useCaseStore.getState().setSelectedCase(cases[0].id, cases[0].name || '')
        }
      } catch (e) {}
    }

    if (!targetCaseId) {
      setError('Please create or select a case first to generate drafts.')
      return
    }

    setGenerating(true)
    setError('')
    setDraft('')
    setCustomDraftText(null)
    setIsEditing(false)
    try {
      const res = await aiApi.getDraft(targetCaseId, { draftType: draftType || 'Bail Application', instructions })
      const rawContent = res?.draft ?? res?.result ?? res
      setDraft(typeof rawContent === 'object' ? JSON.stringify(rawContent) : String(rawContent))
    } catch (err: any) {
      setError(err.message || 'Failed to generate draft. Please try again.')
    } finally {
      setGenerating(false)
    }
  }

  function handleCopy() {
    if (activeText) {
      navigator.clipboard.writeText(activeText)
    }
  }

  function handleDownloadPdf() {
    if (!activeText) return

    const formattedLines = formatLegalDraftText(activeText).split('\n')
    const documentTitle = draftType || 'Legal_Draft_Document'

    let htmlBody = ''
    formattedLines.forEach((line) => {
      const trimmed = line.trim()
      if (!trimmed) {
        htmlBody += '<div style="height: 10px;"></div>'
        return
      }

      // Strip leading markdown headers (#, ##, ###, **, etc.) for pattern matching
      const pureText = trimmed.replace(/^[#*\s]+/, '').replace(/[#*\s]+$/, '').trim()

      const isCourtHeader = /^(IN THE|BEFORE THE|ORIGINAL JURISDICTION|\(ORIGINAL JURISDICTION\)|WRIT PETITION|SPECIAL LEAVE PETITION|IN THE MATTER OF|MEMORANDUM OF APPEAL|MOST RESPECTFULLY SHOWETH)/i.test(pureText) ||
                           /^WRIT PETITION.*UNDER ARTICLE/i.test(pureText)
      const isSectionHeading = /^(\d+\.|\bGROUNDS\b|\bPRAYER\b|\bINTERIM RELIEF\b|\bVERIFICATION\b|\bPETITIONER\b|\bRESPONDENT\b)/i.test(pureText)

      if (isCourtHeader) {
        htmlBody += `<div style="text-align: center; font-weight: bold; font-size: 13.5pt; text-transform: uppercase; margin: 12px 0 6px 0; font-family: 'Times New Roman', Times, serif;">${pureText}</div>`
      } else if (isSectionHeading) {
        htmlBody += `<div style="font-weight: bold; font-size: 12.5pt; text-transform: uppercase; margin: 14px 0 6px 0; font-family: 'Times New Roman', Times, serif;">${pureText}</div>`
      } else {
        // Convert inline **bold** to <strong> and strip any leftover leading #
        let cleanLine = trimmed
          .replace(/^[#\s]+/, '')
          .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
          .replace(/\*(.*?)\*/g, '<strong>$1</strong>')
        
        htmlBody += `<p style="font-size: 12pt; line-height: 1.65; margin: 0 0 4px 0; text-align: justify; font-family: 'Times New Roman', Times, serif;">${cleanLine}</p>`
      }
    })

    // Hidden iframe method so no awkward about:blank tab pops up!
    let printFrame = document.getElementById('pdf-print-iframe') as HTMLIFrameElement
    if (!printFrame) {
      printFrame = document.createElement('iframe')
      printFrame.id = 'pdf-print-iframe'
      printFrame.style.position = 'fixed'
      printFrame.style.right = '0'
      printFrame.style.bottom = '0'
      printFrame.style.width = '0'
      printFrame.style.height = '0'
      printFrame.style.border = '0'
      document.body.appendChild(printFrame)
    }

    const frameDoc = printFrame.contentDocument || printFrame.contentWindow?.document
    if (!frameDoc) return

    frameDoc.open()
    frameDoc.write(`
      <!DOCTYPE html>
      <html>
        <head>
          <title>${documentTitle}</title>
          <style>
            @page {
              size: A4;
              margin: 20mm;
            }
            body {
              font-family: "Times New Roman", Times, Georgia, serif;
              color: #000;
              background: #fff;
              margin: 0;
              padding: 0;
            }
            p { margin: 0 0 4px 0; }
            strong { font-weight: bold; color: #000; }
          </style>
        </head>
        <body>
          <div>${htmlBody}</div>
        </body>
      </html>
    `)
    frameDoc.close()

    setTimeout(() => {
      printFrame.contentWindow?.focus()
      printFrame.contentWindow?.print()
    }, 250)
  }

  return (
    <motion.div
      variants={containerVariants}
      initial="hidden"
      animate="show"
      style={{ display: 'flex', flexDirection: 'column', height: '100%', minHeight: 600 }}
    >
      {/* Top Bar */}
      <motion.div variants={itemVariants} style={{ display: 'flex', alignItems: 'center', padding: '16px 24px', borderBottom: '1px solid rgba(0,0,0,0.05)', background: 'rgba(255,255,255,0.3)', flexShrink: 0 }}>
        <span style={{ fontSize: 18, fontWeight: 700, color: '#0f172a', letterSpacing: '-0.5px' }}>Drafting</span>
        <div style={{ marginLeft: 'auto', display: 'flex', alignItems: 'center', gap: 12 }}>

          {/* ✅ Dynamic case type badge */}
          <CaseTypeBadge />

          {/* ✅ Dynamic draft type selector */}
          <div style={{ position: 'relative' }}>
            <MotionButton
              onClick={() => setShowTypeMenu(!showTypeMenu)}
              style={{ padding: '6px 14px', borderRadius: 8, background: 'rgba(255,255,255,0.6)', border: '1px solid rgba(0,0,0,0.1)', fontSize: 12, fontWeight: 600, color: '#0f172a', cursor: 'pointer', maxWidth: 220, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}
            >
              {draftType || 'Select document type'} ▾
            </MotionButton>
            {showTypeMenu && draftTypes.length > 0 && (
              <div style={{ position: 'absolute', top: '100%', right: 0, marginTop: 4, background: '#fff', border: '1px solid #e2e8f0', borderRadius: 10, boxShadow: '0 8px 24px rgba(0,0,0,0.12)', zIndex: 100, minWidth: 280, maxHeight: 360, overflowY: 'auto' }}>
                <div style={{ padding: '8px 12px', borderBottom: '1px solid #f1f5f9', fontSize: 10, fontWeight: 700, color: '#64748b', textTransform: 'uppercase', letterSpacing: 1 }}>
                  {caseType || 'General'} Documents
                </div>
                {draftTypes.map(t => (
                  <div
                    key={t.label}
                    onClick={() => { setDraftType(t.label); setShowTypeMenu(false) }}
                    style={{ padding: '10px 14px', fontSize: 12, cursor: 'pointer', background: t.label === draftType ? '#eff6ff' : 'transparent', borderBottom: '1px solid #f8fafc', transition: 'transform 100ms ease-out, background 0.15s' }}
                    onPointerDown={e => e.currentTarget.style.transform = 'scale(0.97)'}
                    onPointerUp={e => e.currentTarget.style.transform = 'scale(1)'}
                    onPointerLeave={e => e.currentTarget.style.transform = 'scale(1)'}
                    onMouseEnter={e => { if (t.label !== draftType) (e.currentTarget.style.background = '#f8fafc') }}
                    onMouseLeave={e => { if (t.label !== draftType) (e.currentTarget.style.background = 'transparent') }}
                  >
                    <div style={{ fontWeight: 600, color: t.label === draftType ? '#1e40af' : '#0f172a' }}>{t.label}</div>
                    <div style={{ fontSize: 11, color: '#64748b', marginTop: 2 }}>{t.description}</div>
                    {t.sections.length > 0 && (
                      <div style={{ fontSize: 10, color: '#94a3b8', marginTop: 3 }}>{t.sections.slice(0,2).join(' · ')}</div>
                    )}
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Generate button — Apple Control Center Glass Pill */}
          <MotionButton
            onClick={handleGenerate}
            disabled={generating || !selectedCaseId}
            whileTap={{ scale: 0.95 }}
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: 10,
              padding: '6px 20px 6px 8px',
              borderRadius: 9999,
              background: 'linear-gradient(135deg, rgba(37, 99, 235, 0.95), rgba(29, 78, 216, 0.98))',
              backdropFilter: 'blur(20px) saturate(180%)',
              border: '1px solid rgba(255, 255, 255, 0.45)',
              boxShadow: '0 8px 24px rgba(37, 99, 235, 0.4), inset 0 1px 1.5px rgba(255, 255, 255, 0.6)',
              cursor: (generating || !selectedCaseId) ? 'not-allowed' : 'pointer',
              opacity: (generating || !selectedCaseId) ? 0.7 : 1,
              transition: 'all 0.2s cubic-bezier(0.16, 1, 0.3, 1)',
            }}
          >
            {/* Left Circle Badge */}
            <div
              style={{
                width: 34,
                height: 34,
                borderRadius: '50%',
                background: 'rgba(255, 255, 255, 0.22)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                boxShadow: 'inset 0 1px 1px rgba(255, 255, 255, 0.5), 0 2px 6px rgba(0,0,0,0.15)',
              }}
            >
              <i
                className={generating ? "ti ti-loader animate-spin" : "ti ti-sparkles"}
                style={{ fontSize: 16, color: '#ffffff' }}
              />
            </div>

            {/* Button Label */}
            <span style={{ fontSize: 14, fontWeight: 600, color: '#ffffff', letterSpacing: '-0.3px', fontFamily: 'system-ui, -apple-system, sans-serif' }}>
              {generating ? 'Generating...' : 'Generate'}
            </span>
          </MotionButton>
        </div>
      </motion.div>

      {/* Error */}
      {error && (
        <div style={{ margin: '12px 24px 0', padding: '10px 14px', background: '#fef2f2', border: '1px solid #fca5a5', borderRadius: 8, fontSize: 12, color: '#dc2626' }}>
          {error}
        </div>
      )}

      {/* Main Content */}
      <div style={{ display: 'grid', gridTemplateColumns: '35% 1fr', gap: 20, padding: '24px', flex: 1, overflow: 'hidden' }}>

        {/* Left — Editor */}
        <motion.div variants={itemVariants} style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>

          {/* Selected document info */}
          <div style={{ background: 'rgba(255,255,255,0.6)', borderRadius: 16, padding: 16, border: '1px solid rgba(0,0,0,0.05)' }}>
            <div style={{ fontSize: 12, fontWeight: 600, color: '#374151', marginBottom: 8 }}>Document Type</div>
            <div style={{ padding: '8px 12px', background: '#eff6ff', borderRadius: 8, fontSize: 13, fontWeight: 600, color: '#1e40af' }}>
              {draftType || 'Select a document type'}
            </div>
            {selectedDraftInfo?.description && (
              <div style={{ fontSize: 11, color: '#64748b', marginTop: 6 }}>{selectedDraftInfo.description}</div>
            )}
            {selectedDraftInfo && selectedDraftInfo.sections.length > 0 && (
              <div style={{ marginTop: 8, display: 'flex', flexWrap: 'wrap', gap: 4 }}>
                {selectedDraftInfo.sections.map((s, i) => (
                  <span key={i} style={{ fontSize: 9, padding: '2px 6px', background: '#f0fdf4', color: '#15803d', borderRadius: 10, fontWeight: 600, border: '1px solid #86efac' }}>{s}</span>
                ))}
              </div>
            )}
          </div>

          {/* Instructions */}
          <div style={{ background: 'rgba(255,255,255,0.6)', borderRadius: 16, padding: 16, border: '1px solid rgba(0,0,0,0.05)' }}>
            <div style={{ fontSize: 12, fontWeight: 600, color: '#374151', marginBottom: 8 }}>Special Instructions</div>
            <textarea
              value={instructions}
              onChange={e => setInstructions(e.target.value)}
              placeholder={`Add special instructions for this ${draftType}...\ne.g. Include prayer for interim relief, mention specific dates, add particular facts`}
              rows={6}
              style={{ width: '100%', padding: '8px 10px', border: '1px solid #e2e8f0', borderRadius: 8, fontSize: 12, fontFamily: 'inherit', outline: 'none', resize: 'vertical', boxSizing: 'border-box', color: '#0f172a', background: 'rgba(255,255,255,0.8)' }}
            />
          </div>

          {/* Tips specific to document type */}
          <div style={{ background: 'rgba(59, 130, 246, 0.05)', borderRadius: 16, padding: 16, border: '1px solid rgba(59, 130, 246, 0.1)' }}>
            <div style={{ fontSize: 11, fontWeight: 600, color: '#1e40af', marginBottom: 8 }}>💡 Tips for better drafts</div>
            {[
              `Clausio AI will use all case facts automatically`,
              `Add specific dates, names, and amounts in instructions`,
              `Mention any special prayers or specific relief needed`,
            ].map((tip, i) => (
              <div key={i} style={{ fontSize: 11, color: '#475569', marginBottom: 4 }}>• {tip}</div>
            ))}
          </div>
        </motion.div>

        {/* Right — Preview */}
        <motion.div variants={itemVariants} style={{ height: '100%', minHeight: 0 }}>
          <MotionCard 
            whileHover={{ y: 0, scale: 1 }}
            whileTap={{ scale: 1 }}
            style={{ display: 'flex', flexDirection: 'column', overflow: 'hidden', height: '100%', background: 'rgba(255,255,255,0.4)', borderRadius: 24, border: '1px solid rgba(0,0,0,0.05)' }}
          >
            <div style={{ padding: '20px 24px', display: 'flex', alignItems: 'center', gap: 10, borderBottom: '1px solid rgba(0,0,0,0.05)', background: 'rgba(255,255,255,0.3)' }}>
              <div style={{ width: 32, height: 32, borderRadius: 10, background: 'rgba(255,255,255,0.8)', display: 'flex', alignItems: 'center', justifyContent: 'center', boxShadow: '0 2px 6px rgba(0,0,0,0.02)' }}>
                <i className="ti ti-file-text" style={{ fontSize: 18, color: '#3b82f6' }} />
              </div>
              <span style={{ fontSize: 15, fontWeight: 700, color: '#0f172a', letterSpacing: '-0.3px', flex: 1 }}>
                {draftType || 'Generated Document'}
              </span>
              {(draft || activeText) && (
                <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                  <MotionButton
                    onClick={() => setIsEditing(!isEditing)}
                    whileTap={{ scale: 0.95 }}
                    style={{
                      padding: '6px 12px',
                      fontSize: 11,
                      fontWeight: 600,
                      borderRadius: 8,
                      border: '1px solid #cbd5e1',
                      background: isEditing ? '#eff6ff' : '#ffffff',
                      cursor: 'pointer',
                      color: isEditing ? '#1d4ed8' : '#1e293b',
                      fontFamily: 'inherit',
                      display: 'flex',
                      alignItems: 'center',
                      gap: 4,
                      boxShadow: '0 2px 4px rgba(0,0,0,0.02)'
                    }}
                  >
                    <i className={isEditing ? "ti ti-check" : "ti ti-edit"} style={{ fontSize: 13 }} />
                    {isEditing ? 'Done Editing' : 'Edit Draft'}
                  </MotionButton>

                  <MotionButton 
                    onClick={handleCopy} 
                    whileTap={{ scale: 0.95 }}
                    style={{ padding: '6px 12px', fontSize: 11, fontWeight: 600, borderRadius: 8, border: '1px solid #e2e8f0', background: '#ffffff', cursor: 'pointer', color: '#1e293b', fontFamily: 'inherit', display: 'flex', alignItems: 'center', gap: 4, boxShadow: '0 2px 4px rgba(0,0,0,0.02)' }}
                  >
                    <i className="ti ti-copy" style={{ fontSize: 13 }} />
                    Copy Document
                  </MotionButton>

                  <MotionButton 
                    onClick={handleDownloadPdf} 
                    whileTap={{ scale: 0.95 }}
                    style={{ padding: '6px 12px', fontSize: 11, fontWeight: 600, borderRadius: 8, border: '1px solid #2563eb', background: '#2563eb', cursor: 'pointer', color: '#ffffff', fontFamily: 'inherit', display: 'flex', alignItems: 'center', gap: 4, boxShadow: '0 2px 6px rgba(37,99,235,0.25)' }}
                  >
                    <i className="ti ti-file-download" style={{ fontSize: 13 }} />
                    Download PDF
                  </MotionButton>
                </div>
              )}
            </div>

            <div style={{ flex: 1, padding: '24px', overflowY: 'auto' }}>
              {generating && (
                <DraftingProgressIndicator draftType={draftType} />
              )}

              {!generating && !activeText && (
                <div style={{ background: '#ffffff', borderRadius: 16, padding: '32px 40px', minHeight: 600, fontFamily: 'serif', color: '#1e293b', fontSize: 14, lineHeight: 1.8, boxShadow: '0 4px 20px rgba(0,0,0,0.04)' }}>
                  <div style={{ textAlign: 'center', color: '#94a3b8', padding: 40 }}>
                    <i className="ti ti-file-text" style={{ fontSize: 40, display: 'block', marginBottom: 12 }} />
                    <div style={{ fontSize: 14, fontWeight: 600 }}>No draft generated yet</div>
                    <div style={{ fontSize: 12, marginTop: 8, color: '#cbd5e1' }}>
                      {!selectedCaseId
                        ? 'Select a case first, then click Generate'
                        : `Select document type and click Generate to draft your ${draftType}`
                      }
                    </div>
                  </div>
                </div>
              )}

              {!generating && activeText && (
                renderA4LegalDocument(activeText, isEditing, (newVal) => setCustomDraftText(newVal))
              )}
            </div>
          </MotionCard>
        </motion.div>
      </div>
    </motion.div>
  )
}
