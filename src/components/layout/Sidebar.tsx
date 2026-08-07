'use client'

import Link from 'next/link'
import { usePathname } from 'next/navigation'
import { useUIStore, useCaseStore } from '@/lib/store'

const NAV = [
  {
    group: 'Workspace',
    items: [
      {
        href: '/dashboard',
        icon: 'ti-layout-dashboard',
        label: 'Dashboard',
      },
      {
        href: '/cases',
        icon: 'ti-folder',
        label: 'Cases',
      },
      {
        href: '/hearings',
        icon: 'ti-notebook',
        label: 'Hearings',
        badge: 2,
      },
      {
        href: '/strategy',
        icon: 'ti-target',
        label: 'Strategy',
      },
      {
        href: '/client',
        icon: 'ti-message-circle',
        label: 'Client',
      },
    ],
  },

  {
    group: 'AI',
    items: [
      {
        href: '/analysis',
        icon: 'ti-brain',
        label: 'Analysis',
      },
      {
        href: '/drafting',
        icon: 'ti-pencil',
        label: 'Drafting',
      },
    ],
  },

  {
    group: 'Business',
    items: [
      {
        href: '/billing',
        icon: 'ti-coin',
        label: 'Billing',
      },
      {
        href: '/analytics',
        icon: 'ti-chart-bar',
        label: 'AI Analytics',
      },
      {
        href: '/console',
        icon: 'ti-terminal-2',
        label: 'AI Console',
      },
      {
        href: '/financial',
        icon: 'ti-cash',
        label: 'Financial',
      },
      {
        href: '/readiness',
        icon: 'ti-shield-check',
        label: 'Readiness',
      },
    ],
  },
]

export default function Sidebar() {
  const pathname = usePathname()
  const { sidebarExpanded, toggleSidebar } = useUIStore()
  const { selectedCaseName } = useCaseStore()

  const expanded = sidebarExpanded

  return (
    <aside
      className="glass-sidebar"
      style={{
        width: expanded ? 220 : 64, // Slightly wider for iOS feel
        flexShrink: 0,
        overflow: 'hidden',
        transition: 'width .3s cubic-bezier(0.4, 0, 0.2, 1)', // Apple springy feel
        display: 'flex',
        flexDirection: 'column',
        margin: '16px 0 16px 16px',
        borderRadius: 24,
      }}
    >
      {/* Toggle */}
      <div style={{ padding: '16px 16px 8px 16px', display: 'flex', justifyContent: expanded ? 'flex-end' : 'center' }}>
        <button
          onClick={toggleSidebar}
          className="glass-button"
          style={{
            width: 32,
            height: 32,
            border: 'none',
            color: '#475569',
            cursor: 'pointer',
            display: 'flex',
            justifyContent: 'center',
            alignItems: 'center',
          }}
        >
          <i className="ti ti-menu-2" style={{ fontSize: 18 }} />
        </button>
      </div>

      {expanded && selectedCaseName && (
        <div style={{ padding: '0 16px 12px', display: 'flex', alignItems: 'center', gap: 8 }}>
          <div style={{ width: 6, height: 6, borderRadius: '50%', background: '#3b82f6', flexShrink: 0 }} />
          <span style={{ fontSize: 11, fontWeight: 600, color: '#334155', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
            {selectedCaseName}
          </span>
        </div>
      )}

      <nav
        style={{
          flex: 1,
          overflowY: 'auto',
          overflowX: 'hidden',
          padding: '0 12px'
        }}
      >
        {NAV.map((section) => (
          <div key={section.group} style={{ marginBottom: 16 }}>
            {expanded && (
              <div
                style={{
                  padding: '8px 12px 4px',
                  fontSize: 11,
                  letterSpacing: 0.5,
                  textTransform: 'uppercase',
                  color: '#64748b',
                  fontWeight: 600,
                }}
              >
                {section.group}
              </div>
            )}

            {section.items.map((item) => {
              const active = pathname === item.href

              return (
                <Link
                  key={item.href}
                  href={item.href}
                  title={!expanded ? item.label : undefined}
                  style={{
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: expanded ? 'flex-start' : 'center',
                    gap: expanded ? 12 : 0,
                    margin: '4px 0',
                    padding: expanded ? '0 12px' : 0,
                    height: 40,
                    borderRadius: 16,
                    position: 'relative',
                    textDecoration: 'none',
                    transition: 'all 0.2s ease',
                    background: active
                      ? 'rgba(255, 255, 255, 0.8)'
                      : 'transparent',
                    boxShadow: active ? '0 2px 8px rgba(0,0,0,0.04)' : 'none',
                  }}
                >
                  {item.label === 'Analysis' ? (
                    <div style={{ width: 20, height: 20, borderRadius: '50%', overflow: 'hidden', display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0 }}>
                      <video 
                        src="/aivideo.mp4" 
                        autoPlay 
                        loop 
                        muted 
                        playsInline 
                        style={{ width: '100%', height: '100%', objectFit: 'cover', transform: 'scale(1.8)' }} 
                      />
                    </div>
                  ) : (
                    <i
                      className={`ti ${item.icon}`}
                      style={{
                        fontSize: 20,
                        color: active ? '#0f172a' : '#64748b',
                        flexShrink: 0,
                        transition: 'color 0.2s ease',
                      }}
                    />
                  )}

                  {expanded && (
                    <span
                      style={{
                        flex: 1,
                        fontSize: 14,
                        fontWeight: active ? 600 : 500,
                        color: active ? '#0f172a' : '#475569',
                      }}
                    >
                      {item.label}
                    </span>
                  )}

                  {'badge' in item && item.badge && expanded && (
                    <span
                      style={{
                        background: '#ef4444',
                        color: '#fff',
                        fontSize: 11,
                        fontWeight: 700,
                        borderRadius: 999,
                        padding: '2px 8px',
                        boxShadow: '0 2px 4px rgba(239, 68, 68, 0.3)',
                      }}
                    >
                      {item.badge}
                    </span>
                  )}
                </Link>
              )
            })}
          </div>
        ))}
      </nav>

      <div style={{ padding: '12px', borderTop: '1px solid rgba(0,0,0,0.05)' }}>
        <Link
          href="/settings"
          style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: expanded ? 'flex-start' : 'center',
            gap: expanded ? 12 : 0,
            padding: expanded ? '0 12px' : 0,
            height: 40,
            borderRadius: 16,
            color: '#475569',
            textDecoration: 'none',
            transition: 'background 0.2s ease',
          }}
        >
          <i
            className="ti ti-settings"
            style={{
              fontSize: 20,
            }}
          />

          {expanded && (
            <span
              style={{
                fontSize: 14,
                fontWeight: 500,
              }}
            >
              Settings
            </span>
          )}
        </Link>
        <button
          onClick={() => {
            document.cookie = 'clausio_token=; path=/; max-age=0'
            localStorage.removeItem('clausio_token')
            localStorage.removeItem('clausio_user')
            localStorage.removeItem('clausio-auth')
            window.location.href = '/auth/login'
          }}
          style={{ display: 'flex', alignItems: 'center', justifyContent: expanded ? 'flex-start' : 'center', gap: expanded ? 12 : 0, padding: expanded ? '0 12px' : 0, height: 40, borderRadius: 16, color: '#ef4444', background: 'none', border: 'none', cursor: 'pointer', width: '100%', marginTop: 4, fontFamily: 'inherit' }}
        >
          <i className="ti ti-logout" style={{ fontSize: 20, flexShrink: 0 }} />
          {expanded && <span style={{ fontSize: 14, fontWeight: 500 }}>Logout</span>}
        </button>
      </div>
    </aside>
  )
}