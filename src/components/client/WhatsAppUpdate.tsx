'use client'

import { useState } from 'react'

interface Props {
  onGenerate: (tone: string, language: string) => void
  generating: boolean
}

export default function WhatsAppUpdate({ onGenerate, generating }: Props) {
  const [language, setLanguage] = useState('Hinglish (Hindi + English)')
  const [tone, setTone] = useState('Reassuring')

  const [includeHearing, setIncludeHearing] = useState(true)
  const [includeNextDate, setIncludeNextDate] = useState(true)
  const [includeActionItem, setIncludeActionItem] = useState(false)
  const [includeFeeReminder, setIncludeFeeReminder] = useState(false)

  return (
    <div
      style={{
        background: '#ffffff',
        border: '1px solid #e2e8f0',
        borderRadius: 16,
        padding: 24,
        boxShadow: '0 2px 8px rgba(15,23,42,.04)',
        height: '100%',
        overflowY: 'auto',
      }}
    >
      {/* Header */}

      <div style={{ marginBottom: 24 }}>
        <h2
          style={{
            margin: 0,
            fontSize: 22,
            fontWeight: 700,
            color: '#0f172a',
          }}
        >
          WhatsApp Update
        </h2>

        <p
          style={{
            marginTop: 6,
            fontSize: 14,
            color: '#64748b',
          }}
        >
          Generate an AI update for your client.
        </p>
      </div>

      {/* Language */}

      <Field label="Language">
        <select
          value={language}
          onChange={(e) => setLanguage(e.target.value)}
          style={inputStyle}
        >
          <option>English</option>
          <option>Hindi</option>
          <option>Hinglish (Hindi + English)</option>
          <option>Gujarati</option>
          <option>Marathi</option>
        </select>
      </Field>

      {/* Tone */}

      <Field label="Tone">
        <select
          value={tone}
          onChange={(e) => setTone(e.target.value)}
          style={inputStyle}
        >
          <option>Professional</option>
          <option>Friendly</option>
          <option>Reassuring</option>
          <option>Formal</option>
        </select>
      </Field>

      {/* Include */}

      <div style={{ marginTop: 28 }}>
        <div
          style={{
            fontWeight: 600,
            fontSize: 15,
            color: '#334155',
            marginBottom: 16,
          }}
        >
          Include
        </div>

        <Checkbox
          checked={includeHearing}
          onChange={() => setIncludeHearing(!includeHearing)}
          label="What happened in hearing"
        />

        <Checkbox
          checked={includeNextDate}
          onChange={() => setIncludeNextDate(!includeNextDate)}
          label="Next hearing date"
        />

        <Checkbox
          checked={includeActionItem}
          onChange={() => setIncludeActionItem(!includeActionItem)}
          label="Action item for client"
        />

        <Checkbox
          checked={includeFeeReminder}
          onChange={() => setIncludeFeeReminder(!includeFeeReminder)}
          label="Fee reminder"
        />
      </div>

      {/* Generate */}

      <button
        onClick={() => onGenerate(tone, language)}
        disabled={generating}
        style={{
          width: '100%',
          marginTop: 30,
          padding: '14px',
          border: 'none',
          borderRadius: 12,
          background: generating ? '#86efac' : '#22c55e',
          color: '#ffffff',
          fontWeight: 700,
          fontSize: 15,
          cursor: generating ? 'not-allowed' : 'pointer',
          boxShadow: '0 10px 25px rgba(34,197,94,.25)',
        }}
      >
        {generating ? 'Generating...' : 'Generate for WhatsApp'}
      </button>
    </div>
  )
}

/* -------------------------------- */

function Field({
  label,
  children,
}: {
  label: string
  children: React.ReactNode
}) {
  return (
    <div style={{ marginBottom: 22 }}>
      <div
        style={{
          marginBottom: 8,
          fontWeight: 600,
          color: '#334155',
          fontSize: 14,
        }}
      >
        {label}
      </div>

      {children}
    </div>
  )
}

/* -------------------------------- */

function Checkbox({
  checked,
  onChange,
  label,
}: {
  checked: boolean
  onChange: () => void
  label: string
}) {
  return (
    <label
      style={{
        display: 'flex',
        alignItems: 'center',
        gap: 12,
        marginBottom: 16,
        cursor: 'pointer',
        fontSize: 14,
        color: '#334155',
      }}
    >
      <input
        type="checkbox"
        checked={checked}
        onChange={onChange}
        style={{
          width: 18,
          height: 18,
          cursor: 'pointer',
        }}
      />

      {label}
    </label>
  )
}

/* -------------------------------- */

const inputStyle: React.CSSProperties = {
  width: '100%',
  padding: '12px 14px',
  borderRadius: 10,
  border: '1px solid #d1d5db',
  background: '#ffffff',
  fontSize: 14,
  fontFamily: 'inherit',
  outline: 'none',
}