'use client'

import React from 'react'

interface Props {
  content: string
  className?: string
  style?: React.CSSProperties
}

/**
 * Rich ChatGPT-style Markdown renderer for rendering formatted AI responses, WhatsApp previews, and legal drafts
 */
export default function FormattedMarkdown({ content, className = '', style = {} }: Props) {
  if (!content) return null

  // Process line by line for precise heading and list recognition
  const lines = content.split('\n')

  return (
    <div className={`formatted-markdown ${className}`} style={{ fontSize: 13, lineHeight: 1.6, color: '#0f172a', ...style }}>
      {lines.map((line, idx) => {
        const trimmed = line.trim()

        if (!trimmed) {
          return <div key={idx} style={{ height: '0.6em' }} />
        }

        // 1. Heading 1 (# Heading)
        if (trimmed.startsWith('# ')) {
          const text = trimmed.replace(/^#\s+/, '')
          return (
            <h1 key={idx} style={{ fontSize: 18, fontWeight: 800, color: '#0f172a', margin: '14px 0 6px 0', letterSpacing: '-0.02em', textTransform: 'uppercase' }}>
              {parseInlineMarkdown(text)}
            </h1>
          )
        }

        // 2. Heading 2 (## Heading)
        if (trimmed.startsWith('## ')) {
          const text = trimmed.replace(/^##\s+/, '')
          return (
            <h2 key={idx} style={{ fontSize: 15, fontWeight: 700, color: '#0f172a', margin: '12px 0 4px 0', letterSpacing: '-0.01em' }}>
              {parseInlineMarkdown(text)}
            </h2>
          )
        }

        // 3. Heading 3 (### Heading)
        if (trimmed.startsWith('### ')) {
          const text = trimmed.replace(/^###\s+/, '')
          return (
            <h3 key={idx} style={{ fontSize: 14, fontWeight: 700, color: '#1e293b', margin: '10px 0 4px 0' }}>
              {parseInlineMarkdown(text)}
            </h3>
          )
        }

        // 4. Unordered List (- item or * item)
        if (/^[\-\*]\s+/.test(trimmed)) {
          const itemText = trimmed.replace(/^[\-\*]\s+/, '')
          return (
            <div key={idx} style={{ display: 'flex', alignItems: 'flex-start', gap: 8, margin: '3px 0', paddingLeft: 8 }}>
              <span style={{ color: '#3b82f6', fontWeight: 700, fontSize: 14 }}>•</span>
              <span style={{ color: '#334155', flex: 1 }}>{parseInlineMarkdown(itemText)}</span>
            </div>
          )
        }

        // 5. Ordered List (1. item)
        if (/^\d+\.\s+/.test(trimmed)) {
          const match = trimmed.match(/^(\d+\.)\s+(.*)/)
          const num = match ? match[1] : '1.'
          const itemText = match ? match[2] : trimmed
          return (
            <div key={idx} style={{ display: 'flex', alignItems: 'flex-start', gap: 8, margin: '4px 0', paddingLeft: 4 }}>
              <span style={{ color: '#2563eb', fontWeight: 700, fontSize: 13, minWidth: 20 }}>{num}</span>
              <span style={{ color: '#1e293b', flex: 1 }}>{parseInlineMarkdown(itemText)}</span>
            </div>
          )
        }

        // 6. Default Paragraph Line
        return (
          <div key={idx} style={{ margin: '2px 0', color: '#0f172a' }}>
            {parseInlineMarkdown(trimmed)}
          </div>
        )
      })}
    </div>
  )
}

/**
 * Parses inline bold (**text**), italic (*text*), code (`code`), and statutory references
 */
function parseInlineMarkdown(text: string): React.ReactNode {
  if (!text) return null

  // Regex to split bold (**...**), italic (*...*), and code (`...`)
  const parts = text.split(/(\*\*.*?\*\*|\*.*?\*|`.*?`)/g)

  return parts.map((part, index) => {
    if (part.startsWith('**') && part.endsWith('**')) {
      const inner = part.slice(2, -2)
      return (
        <strong key={index} style={{ fontWeight: 700, color: '#0f172a' }}>
          {inner}
        </strong>
      )
    }

    if (part.startsWith('*') && part.endsWith('*')) {
      const inner = part.slice(1, -1)
      return (
        <em key={index} style={{ fontStyle: 'italic', color: '#334155' }}>
          {inner}
        </em>
      )
    }

    if (part.startsWith('`') && part.endsWith('`')) {
      const inner = part.slice(1, -1)
      return (
        <code key={index} style={{ background: '#f1f5f9', color: '#2563eb', padding: '1px 5px', borderRadius: 4, fontFamily: 'monospace', fontSize: 12 }}>
          {inner}
        </code>
      )
    }

    return part
  })
}
