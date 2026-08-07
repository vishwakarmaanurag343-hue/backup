'use client'

import { useMemo, useState } from 'react'

interface AddCaseModalProps {
  open:      boolean
  onClose:   () => void
  onSaved?:  () => void  // ✅ NEW prop — called after successful save
}

type Priority = 'Low' | 'Medium' | 'High' | 'Urgent'
type Status   = 'Draft' | 'Active' | 'Pending' | 'Closed'

interface CaseForm {
  practiceArea:     string
  caseType:         string
  caseTitle:        string
  caseNumber:       string
  description:      string
  priority:         Priority
  status:           Status
  clientName:       string
  clientPhone:      string
  clientEmail:      string
  clientAddress:    string
  opponentName:     string
  opponentAdvocate: string
  opponentPhone:    string
  opponentAddress:  string
  court:            string
  courtLocation:    string
  judgeName:        string
  stage:            string
  filingDate:       string
  hearingDate:      string
  relief:           string
  notes:            string
}

const initialForm: CaseForm = {
  practiceArea:'', caseType:'', caseTitle:'', caseNumber:'',
  description:'', priority:'Medium', status:'Draft',
  clientName:'', clientPhone:'', clientEmail:'', clientAddress:'',
  opponentName:'', opponentAdvocate:'', opponentPhone:'', opponentAddress:'',
  court:'', courtLocation:'', judgeName:'', stage:'',
  filingDate:'', hearingDate:'', relief:'', notes:''
}

const STEPS = ['Practice Area','Case Details','Parties','Court','Documents','Review']

export default function AddCaseModal({ open, onClose, onSaved }: AddCaseModalProps) {
  const [step,    setStep]    = useState(1)
  const [form,    setForm]    = useState<CaseForm>(initialForm)
  const [saving,  setSaving]  = useState(false)  // ✅ NEW
  const [error,   setError]   = useState('')      // ✅ NEW
  const [translating,   setTranslating]   = useState(false)
  const [detectedLang,  setDetectedLang]  = useState('')

  const next     = () => { if (step < 6) setStep(step + 1) }
  const previous = () => { if (step > 1) setStep(step - 1) }

  const updateField = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    setForm({ ...form, [e.target.name]: e.target.value })
  }

  const progress = useMemo(() => ((step - 1) / 5) * 100, [step])

  // ✅ NEW — Translate client description to English
async function translateDescription() {
  if (!form.description.trim()) return
  setTranslating(true)
  setDetectedLang('')

  try {
    const token = localStorage.getItem('clausio_token')
    const res   = await fetch(
      `${process.env.NEXT_PUBLIC_API_URL}/ai/translate`,
      {
        method:  'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization:  `Bearer ${token}`,
        },
        body: JSON.stringify({ text: form.description }),
      }
    )

    if (res.ok) {
      const data = await res.json()
      setForm({ ...form, description: data.translatedText })
      setDetectedLang(data.detectedLanguage)
    }
  } catch (err) {
    console.error('Translation error:', err)
  } finally {
    setTranslating(false)
  }
}

  // ✅ CHANGED: Create Case button now saves to real backend
  async function handleCreateCase() {
    if (!form.caseTitle) { setError("Case Title is required. Go back to Step 2."); return }
    if (!form.clientName) { setError("Client Name is required. Go back to Step 3."); return }
    setSaving(true)
    setError('')

    try {
      const token = localStorage.getItem('clausio_token')
      const apiBase = (process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5123/api').replace(/\/+$/, '')
      const getUrl = (endpoint: string) => apiBase.endsWith('/api') ? `${apiBase}${endpoint}` : `${apiBase}/api${endpoint}`

      // Step 1: Create client first
      const clientRes = await fetch(getUrl('/clients'), {
        method:  'POST',
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
        body: JSON.stringify({
          firstName:  form.clientName.split(' ')[0] || form.clientName,
          lastName:   form.clientName.split(' ').slice(1).join(' ') || 'Client',
          phone:      form.clientPhone || '+91 00000 00000',
          email:      form.clientEmail || 'client@example.com',
          address:    form.clientAddress || 'Address not provided',
          clientType: 'Individual',
          isVip:      false,
        }),
      })

      if (!clientRes.ok) throw new Error('Failed to create client')
      const clientData = await clientRes.json()

      // Step 2: Create case with clientId
      const caseRes = await fetch(getUrl('/cases'), {
        method:  'POST',
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
        body: JSON.stringify({
          name:          form.caseTitle,
          caseNumber:    form.caseNumber || `${form.practiceArea?.substring(0,2).toUpperCase() || 'CA'}/${Date.now()}`,
          caseType:      form.practiceArea || form.caseType,
          subType:       form.caseType || '',
          court:         form.court || 'District Court',
          courtLocation: form.courtLocation || '',
          stage:         form.stage || 'Filing',
          priority:      form.priority,
          status:        form.status,
          opposingAdv:   form.opponentAdvocate || '',
          filedOn:       form.filingDate ? new Date(form.filingDate).toISOString() : new Date().toISOString(),
          nextHearing:   form.hearingDate ? new Date(form.hearingDate).toISOString() : null,
          clientId:      clientData.id,
          description:   form.description || '',
          keyFacts:      form.description || '',
          relief:        form.relief || '',
          notes:         form.notes || '',
        }),
      })

      if (!caseRes.ok) throw new Error('Failed to create case')

      // Success — reset form and close
      setForm(initialForm)
      setStep(1)
      onSaved?.()
      onClose()

    } catch (err: any) {
      setError(err.message || 'Error creating case. Please try again.')
    } finally {
      setSaving(false)
    }
  }

  if (!open) return null

  return (
    <div style={overlay}>
      <div style={modal}>

        {/* HEADER — UNCHANGED */}
        <div style={header}>
          <div>
            <h2 style={{ margin: 0, fontSize: 30, fontWeight: 700 }}>Create New Case</h2>
            <p style={{ marginTop: 8, color: '#64748b' }}>Register a new legal matter inside Clausio.</p>
          </div>
          <button onClick={onClose} style={closeButton}>✕</button>
        </div>

        {/* PROGRESS — UNCHANGED */}
        <div style={{ padding: '20px 32px', borderBottom: '1px solid #e2e8f0' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 18 }}>
            {STEPS.map((item, index) => {
              const active = index + 1 <= step
              return (
                <div key={item} style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', flex: 1 }}>
                  <div style={{ width: 34, height: 34, borderRadius: '50%', background: active ? '#2563eb' : '#e2e8f0', color: active ? 'white' : '#64748b', display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 700 }}>
                    {index + 1}
                  </div>
                  <span style={{ marginTop: 8, fontSize: 12, color: '#64748b' }}>{item}</span>
                </div>
              )
            })}
          </div>
          <div style={{ height: 8, background: '#e2e8f0', borderRadius: 999, overflow: 'hidden' }}>
            <div style={{ width: `${progress}%`, height: '100%', background: '#2563eb', transition: '.3s' }} />
          </div>
        </div>

        {/* BODY — UNCHANGED */}
        <div style={{ padding: 32, maxHeight: '65vh', overflowY: 'auto' }}>

          {/* STEP 1 — UNCHANGED */}
          {step === 1 && (
            <>
              <h3 style={sectionTitle}>Practice Area</h3>
              <p style={sectionDescription}>Select the practice area. The remaining form adapts automatically.</p>
              <div style={grid}>
                <SelectField label="Practice Area" name="practiceArea" value={form.practiceArea} onChange={updateField} options={['Family Law','Civil','Criminal','Corporate','GST','Income Tax','NI Act','Arbitration']} />
                <SelectField label="Case Type"     name="caseType"     value={form.caseType}     onChange={updateField} options={['Petition','Appeal','Suit','Application','Execution','Review']} />
              </div>
              <div style={{ marginTop: 30, padding: 22, background: '#eff6ff', borderRadius: 14, border: '1px solid #bfdbfe' }}>
                <h4 style={{ marginTop: 0, marginBottom: 10 }}>🤖 Clausio AI</h4>
                <p style={{ margin: 0, lineHeight: 1.8, color: '#475569' }}>
                  After selecting the practice area, Clausio AI will automatically recommend:
                  • Applicable Acts & Sections • Required Documents • Similar Judgments
                  • Draft Strategy • Checklist • Initial Questions
                </p>
              </div>
            </>
          )}

          {/* STEP 2 — UNCHANGED */}
          {step === 2 && (
            <>
              <h3 style={sectionTitle}>Case Details</h3>
              <p style={sectionDescription}>Enter the basic information about this legal matter.</p>
              <div style={grid}>
                <InputField label="Case Title"    name="caseTitle"    value={form.caseTitle}    onChange={updateField} placeholder="Priya Sharma vs Rohit Sharma" required />
                <InputField label="Case Number"   name="caseNumber"   value={form.caseNumber}   onChange={updateField} placeholder="FC/245/2026" />
                <InputField label="Filing Date"   name="filingDate"   type="date" value={form.filingDate}   onChange={updateField} />
                <InputField label="Next Hearing"  name="hearingDate"  type="date" value={form.hearingDate}  onChange={updateField} />
                <SelectField label="Priority"     name="priority"     value={form.priority}     onChange={updateField} options={['Low','Medium','High','Urgent']} />
                <SelectField label="Status"       name="status"       value={form.status}       onChange={updateField} options={['Draft','Active','Pending','Closed']} />
              </div>
              <div style={{ marginTop: 24 }}>
            <div>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 }}>
                <label style={{ fontWeight: 600, fontSize: 14, color: '#334155' }}>
                  Case Description
                </label>
                <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                  {detectedLang && (
                    <span style={{ fontSize: 11, background: '#f0fdf4', color: '#15803d', padding: '2px 8px', borderRadius: 20, fontWeight: 600 }}>
                      ✓ Translated from {detectedLang}
                    </span>
                  )}
                  <button
                    type="button"
                    onClick={translateDescription}
                    disabled={translating || !form.description.trim()}
                    style={{ display: 'flex', alignItems: 'center', gap: 5, padding: '5px 12px', borderRadius: 8, border: '1px solid #bfdbfe', background: translating ? '#eff6ff' : '#fff', color: '#1d4ed8', fontSize: 12, fontWeight: 600, cursor: translating ? 'not-allowed' : 'pointer', fontFamily: 'inherit' }}
                  >
                    {translating ? (
                      <>⏳ Translating...</>
                    ) : (
                      <>🌐 Translate to English</>
                    )}
                  </button>
                </div>
              </div>
              <textarea
                name="description"
                value={form.description}
                onChange={updateField}
                rows={6}
                placeholder="Client can type in Hindi, Gujarati, Marathi or English — AI will translate automatically..."
                style={{ width: '100%', minHeight: 140, padding: 16, borderRadius: 12, border: '1px solid #dbe3ef', outline: 'none', resize: 'vertical', fontSize: 15, background: '#fff', boxSizing: 'border-box', fontFamily: 'inherit' }}
              />
              <p style={{ margin: '6px 0 0', fontSize: 12, color: '#64748b' }}>
                💡 Supported: English · हिंदी · ગુજરાતી · मराठी
              </p>
            </div>
          </div>
            </>
          )}

          {/* STEP 3 — UNCHANGED */}
          {step === 3 && (
            <>
              <h3 style={sectionTitle}>Client & Opposite Party</h3>
              <p style={sectionDescription}>Add the parties involved in this matter.</p>
              <div style={{ marginBottom: 30 }}>
                <h4 style={{ marginBottom: 18, color: '#2563eb' }}>Client Details</h4>
                <div style={grid}>
                  <InputField label="Client Name"  name="clientName"    value={form.clientName}    onChange={updateField} placeholder="Full Name" required />
                  <InputField label="Phone Number" name="clientPhone"   value={form.clientPhone}   onChange={updateField} placeholder="+91 XXXXX XXXXX" />
                  <InputField label="Email"        name="clientEmail"   value={form.clientEmail}   onChange={updateField} placeholder="client@email.com" />
                  <InputField label="Address"      name="clientAddress" value={form.clientAddress} onChange={updateField} placeholder="Full Address" />
                </div>
              </div>
              <div>
                <h4 style={{ marginBottom: 18, color: '#ef4444' }}>Opposite Party</h4>
                <div style={grid}>
                  <InputField label="Opponent Name"     name="opponentName"     value={form.opponentName}     onChange={updateField} placeholder="Full Name" />
                  <InputField label="Opponent Advocate" name="opponentAdvocate" value={form.opponentAdvocate} onChange={updateField} placeholder="Advocate Name" />
                  <InputField label="Phone"             name="opponentPhone"    value={form.opponentPhone}    onChange={updateField} placeholder="+91 XXXXX XXXXX" />
                  <InputField label="Address"           name="opponentAddress"  value={form.opponentAddress}  onChange={updateField} placeholder="Address" />
                </div>
              </div>
            </>
          )}

          {/* STEP 4 — UNCHANGED */}
          {step === 4 && (
            <>
              <h3 style={sectionTitle}>Court Information</h3>
              <p style={sectionDescription}>Provide court, hearing and judicial details.</p>
              <div style={grid}>
                <SelectField label="Court"         name="court"         value={form.court}         onChange={updateField} options={['Family Court','District Court','Sessions Court','High Court','Supreme Court','Consumer Court','Commercial Court','NCLT']} />
                <InputField  label="Court Location" name="courtLocation" value={form.courtLocation} onChange={updateField} placeholder="Mumbai" />
                <InputField  label="Judge Name"     name="judgeName"     value={form.judgeName}     onChange={updateField} placeholder="Hon. Justice..." />
                <SelectField label="Current Stage"  name="stage"         value={form.stage}         onChange={updateField} options={['Pre Filing','Filed','Notice','Written Statement','Evidence','Cross Examination','Arguments','Judgment','Execution']} />
              </div>
              <div style={{ marginTop: 28, padding: 22, background: '#f8fafc', borderRadius: 14, border: '1px solid #e2e8f0' }}>
                <h4 style={{ marginTop: 0, marginBottom: 12 }}>Upcoming Hearing</h4>
                <div style={grid}>
                  <InputField label="Hearing Date" name="hearingDate" type="date" value={form.hearingDate} onChange={updateField} />
                  <InputField label="Court Hall"   name="courtHall"  value={(form as any).courtHall || ''} onChange={updateField} placeholder="Hall No. 5" />
                </div>
              </div>
            </>
          )}

          {/* STEP 5 — UNCHANGED */}
          {step === 5 && (
            <>
              <h3 style={sectionTitle}>Documents & AI</h3>
              <p style={sectionDescription}>Upload documents and let Clausio AI analyse the matter.</p>
              <div style={{ marginTop: 20, border: '2px dashed #cbd5e1', borderRadius: 18, padding: 40, textAlign: 'center', background: '#f8fafc' }}>
                <i className="ti ti-cloud-upload" style={{ fontSize: 52, color: '#2563eb' }} />
                <h3 style={{ marginTop: 16, marginBottom: 8 }}>Upload Case Documents</h3>
                <p style={{ color: '#64748b', lineHeight: 1.8 }}>Drag & Drop or browse your files.</p>
                <input type="file" multiple style={{ marginTop: 20 }} />
              </div>
              <div style={{ marginTop: 30, background: '#eff6ff', borderRadius: 18, border: '1px solid #bfdbfe', padding: 24 }}>
                <h3 style={{ marginTop: 0, color: '#1d4ed8' }}>🤖 Clausio AI will generate</h3>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14, marginTop: 20 }}>
                  {['Case Summary','Timeline of Events','Applicable Sections','Similar Judgments','Missing Documents','Risk Analysis','Legal Strategy','Cross Examination Questions','Client Questionnaire','Draft Petition Suggestions'].map(t => (
                    <div key={t} style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '10px 14px', background: 'white', borderRadius: 12, border: '1px solid #dbeafe' }}>
                      <div style={{ width: 24, height: 24, borderRadius: '50%', background: '#2563eb', display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'white', fontSize: 12, fontWeight: 700 }}>✓</div>
                      <span>{t}</span>
                    </div>
                  ))}
                </div>
              </div>
              <div style={{ marginTop: 30 }}>
                <TextAreaField label="Relief Sought"   name="relief" value={form.relief} onChange={updateField} placeholder="Mention the relief sought from the Court..." />
                <div style={{ height: 20 }} />
                <TextAreaField label="Internal Notes"  name="notes"  value={form.notes}  onChange={updateField} placeholder="Private notes for your legal team..." />
              </div>
            </>
          )}

          {/* STEP 6 — UNCHANGED except error message added */}
          {step === 6 && (
            <>
              <h3 style={sectionTitle}>Review & Create Case</h3>
              <p style={sectionDescription}>Please verify the information before creating the case.</p>

              {/* ✅ NEW: error message */}
              {error && (
                <div style={{ padding: '10px 14px', background: '#fef2f2', border: '1px solid #fca5a5', borderRadius: 8, fontSize: 13, color: '#dc2626', marginBottom: 16 }}>
                  {error}
                </div>
              )}

              <div style={{ background: '#f8fafc', border: '1px solid #e2e8f0', borderRadius: 16, padding: 24 }}>
                <ReviewSection title="Practice" items={[['Practice Area', form.practiceArea],['Case Type', form.caseType],['Priority', form.priority],['Status', form.status]]} />
                <ReviewSection title="Case"     items={[['Case Title', form.caseTitle],['Case Number', form.caseNumber || '-'],['Filing Date', form.filingDate || '-'],['Next Hearing', form.hearingDate || '-']]} />
                <ReviewSection title="Client"   items={[['Client Name', form.clientName],['Phone', form.clientPhone],['Email', form.clientEmail || '-']]} />
                <ReviewSection title="Opponent" items={[['Opponent', form.opponentName || '-'],['Advocate', form.opponentAdvocate || '-']]} />
                <ReviewSection title="Court"    items={[['Court', form.court || '-'],['Location', form.courtLocation || '-'],['Judge', form.judgeName || '-'],['Stage', form.stage || '-']]} />
                <ReviewSection title="Notes"    items={[['Relief', form.relief || '-'],['Internal Notes', form.notes || '-']]} />
              </div>

              <div style={{ marginTop: 28, background: '#eff6ff', border: '1px solid #bfdbfe', borderRadius: 16, padding: 22 }}>
                <h4 style={{ marginTop: 0, color: '#1d4ed8' }}>🤖 Clausio AI will automatically generate</h4>
                <ul style={{ marginBottom: 0, lineHeight: 2, color: '#475569' }}>
                  <li>Case Summary</li>
                  <li>Chronology of Events</li>
                  <li>Applicable Sections</li>
                  <li>Draft Petition Suggestions</li>
                  <li>Cross Examination Questions</li>
                  <li>Relevant Judgments</li>
                  <li>Document Checklist</li>
                  <li>Legal Strategy</li>
                </ul>
              </div>
            </>
          )}

        </div>

        {/* FOOTER — UNCHANGED except Create Case button */}
        <div style={footer}>
          <button style={secondaryButton} onClick={onClose}>Cancel</button>
          <div style={{ display: 'flex', gap: 12 }}>
            {step > 1 && (
              <button style={secondaryButton} onClick={previous}>← Previous</button>
            )}
            {step < 6 ? (
              <button style={primaryButton} onClick={next}>Next →</button>
            ) : (
              // ✅ CHANGED: now calls real backend
              <button
                style={{ ...primaryButton, opacity: saving ? 0.7 : 1, cursor: saving ? 'not-allowed' : 'pointer' }}
                onClick={handleCreateCase}
                disabled={saving}
              >
                {saving ? 'Creating...' : 'Create Case'}
              </button>
            )}
          </div>
        </div>

      </div>
    </div>
  )
}

/* ============================================================
   REVIEW SECTION — UNCHANGED
============================================================ */
function ReviewSection({ title, items }: { title: string; items: [string, string][] }) {
  return (
    <div style={{ marginBottom: 28 }}>
      <h4 style={{ marginTop: 0, marginBottom: 16, color: '#1e293b', fontSize: 18 }}>{title}</h4>
      <div style={{ display: 'grid', gridTemplateColumns: '220px 1fr', gap: 12 }}>
        {items.map(([label, value]) => (
          <>
            <div key={label} style={{ color: '#64748b', fontWeight: 600 }}>{label}</div>
            <div style={{ color: '#0f172a' }}>{value || '-'}</div>
          </>
        ))}
      </div>
    </div>
  )
}

/* ============================================================
   FIELD COMPONENTS — UNCHANGED
============================================================ */
function InputField({ label, name, value, onChange, placeholder, required, type = 'text' }: { label: string; name: string; value: string; onChange: (e: React.ChangeEvent<HTMLInputElement>) => void; placeholder?: string; required?: boolean; type?: string }) {
  return (
    <div>
      <label style={labelStyle}>{label}{required && <span style={{ color: '#ef4444' }}> *</span>}</label>
      <input type={type} name={name} value={value} placeholder={placeholder} onChange={onChange} style={inputStyle} />
    </div>
  )
}

function SelectField({ label, name, value, options, onChange }: { label: string; name: string; value: string; options: string[]; onChange: (e: React.ChangeEvent<HTMLSelectElement>) => void }) {
  return (
    <div>
      <label style={labelStyle}>{label}</label>
      <select name={name} value={value} onChange={onChange} style={inputStyle}>
        <option value="">Select</option>
        {options.map(option => <option key={option} value={option}>{option}</option>)}
      </select>
    </div>
  )
}

function TextAreaField({ label, name, value, placeholder, onChange }: { label: string; name: string; value: string; placeholder?: string; onChange: (e: React.ChangeEvent<HTMLTextAreaElement>) => void }) {
  return (
    <div>
      <label style={labelStyle}>{label}</label>
      <textarea name={name} value={value} placeholder={placeholder} onChange={onChange} rows={6} style={textareaStyle} />
    </div>
  )
}

/* ============================================================
   STYLES — UNCHANGED
============================================================ */
const overlay:        React.CSSProperties = { position: 'fixed', inset: 0, background: 'rgba(15,23,42,.55)', display: 'flex', justifyContent: 'center', alignItems: 'center', padding: 30, zIndex: 9999 }
const modal:          React.CSSProperties = { width: '100%', maxWidth: 1150, maxHeight: '92vh', background: '#fff', borderRadius: 24, overflow: 'hidden', display: 'flex', flexDirection: 'column', boxShadow: '0 25px 60px rgba(15,23,42,.18)' }
const header:         React.CSSProperties = { display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '26px 32px', borderBottom: '1px solid #e2e8f0', background: '#ffffff' }
const footer:         React.CSSProperties = { display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '24px 32px', borderTop: '1px solid #e2e8f0', background: '#ffffff' }
const grid:           React.CSSProperties = { display: 'grid', gridTemplateColumns: 'repeat(2,1fr)', gap: 22 }
const sectionTitle:   React.CSSProperties = { fontSize: 26, fontWeight: 700, margin: 0, marginBottom: 10, color: '#0f172a' }
const sectionDescription: React.CSSProperties = { marginTop: 0, marginBottom: 28, color: '#64748b', lineHeight: 1.7, fontSize: 15 }
const labelStyle:     React.CSSProperties = { display: 'block', marginBottom: 8, fontWeight: 600, color: '#334155', fontSize: 14 }
const inputStyle:     React.CSSProperties = { width: '100%', height: 52, padding: '0 16px', borderRadius: 12, border: '1px solid #dbe3ef', outline: 'none', fontSize: 15, background: '#fff', boxSizing: 'border-box' }
const textareaStyle:  React.CSSProperties = { width: '100%', minHeight: 140, padding: 16, borderRadius: 12, border: '1px solid #dbe3ef', outline: 'none', resize: 'vertical', fontSize: 15, background: '#fff', boxSizing: 'border-box', fontFamily: 'inherit' }
const primaryButton:  React.CSSProperties = { background: '#2563eb', color: '#fff', border: 'none', borderRadius: 12, padding: '12px 24px', cursor: 'pointer', fontWeight: 600, fontSize: 15, transition: '.25s' }
const secondaryButton:React.CSSProperties = { background: '#fff', color: '#334155', border: '1px solid #dbe3ef', borderRadius: 12, padding: '12px 24px', cursor: 'pointer', fontWeight: 600, fontSize: 15, transition: '.25s' }
const closeButton:    React.CSSProperties = { width: 42, height: 42, borderRadius: 12, border: 'none', background: '#f1f5f9', color: '#334155', fontSize: 18, cursor: 'pointer', display: 'flex', justifyContent: 'center', alignItems: 'center' }
