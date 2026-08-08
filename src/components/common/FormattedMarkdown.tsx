'use client'

import React from 'react'

interface Props {
  content: string
  className?: string
  style?: React.CSSProperties
}

/**
 * Rich ChatGPT-style Markdown renderer for rendering formatted AI responses
 */
export default function FormattedMarkdown({ content, className = '', style = {} }: Props) {
  if (!content) return null

  // Split into blocks by double newlines
  const blocks = content.split(/\n\n+/)

  return (
    <div className={`formatted-markdown ${className}`} style={{ fontSize: 13, lineHeight: 1.6, color: '#0f172a', ...style }}>
      {blocks.map((block, bIdx) => {
        const trimmed = block.trim()

        // 1. Heading 2 (## Heading)
        if (trimmed.startsWith('## ')) {
          const text = trimmed.replace(/^##\s+/, '')
          return (
            <h2 key={bIdx} style={{ fontSize: 16, fontWeight: 700, color: '#0f172a', margin: '14px 0 6px 0', letterSpacing: '-0.01em' }}>
              {parseInlineMarkdown(text)}
            </h2>
          )
        }

        // 2. Heading 3 (### Heading)
        if (trimmed.startsWith('### ')) {
          const text = trimmed.replace(/^###\s+/, '')
          return (
            <h3 key={bIdx} style={{ fontSize: 14, fontWeight: 700, color: '#1e293b', margin: '12px 0 4px 0' }}>
              {parseInlineMarkdown(text)}
            </h3>
          )
        }

        // 3. Unordered List (- item or * item)
        if (/^[\-\*]\s+/m.test(trimmed)) {
          const lines = trimmed.split('\n').filter(l => l.trim().length > 0)
          return (
            <ul key={bIdx} style={{ margin: '6px 0 10px 0', paddingLeft: 20, display: 'flex', flexDirection: 'column', gap: 4 }}>
              {lines.map((line, lIdx) => {
                const itemText = line.replace(/^[\-\*]\s+/, '')
                return (
                  <li key={lIdx} style={{ listStyleType: 'disc', color: '#334155' }}>
                    {parseInlineMarkdown(itemText)}
                  </li>
                )
              })}
            </ul>
          )
        }

        // 4. Ordered List (1. item)
        if (/^\d+\.\s+/m.test(trimmed)) {
          const lines = trimmed.split('\n').filter(l => l.trim().length > 0)
          return (
            <ol key={bIdx} style={{ margin: '6px 0 10px 0', paddingLeft: 20, display: 'flex', flexDirection: 'column', gap: 4 }}>
              {lines.map((line, lIdx) => {
                const itemText = line.replace(/^\d+\.\s+/, '')
                return (
                  <li key={lIdx} style={{ listStyleType: 'decimal', color: '#334155' }}>
                    {parseInlineMarkdown(itemText)}
                  </li>
                )
              })}
            </ol>
          )
        }

        // 5. Normal Paragraph with potential internal newlines
        const paragraphLines = trimmed.split('\n')
        return (
          <p key={bIdx} style={{ margin: '0 0 10px 0', color: '#0f172a' }}>
            {paragraphLines.map((line, lIdx) => (
              <React.Fragment key={lIdx}>
                {parseInlineMarkdown(line)}
                {lIdx < paragraphLines.length - 1 && <br />}
              </React.Fragment>
            ))}
          </p>
        )
      })}
    </div>
  )
}

/**
 * Parses inline bold (**text**), italic (*text*), code (`code`), and statutory citations
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
