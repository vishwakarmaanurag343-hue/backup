import { NextResponse } from 'next/server'
import type { NextRequest } from 'next/server'

export function middleware(request: NextRequest) {
  // Client-side authentication uses localStorage Bearer token (no cookie dependency)
  return NextResponse.next()
}

// Which routes this middleware applies to
export const config = {
  matcher: [
    '/dashboard/:path*',
    '/cases/:path*',
    '/hearings/:path*',
    '/strategy/:path*',
    '/client/:path*',
    '/financial/:path*',
    '/readiness/:path*',
    '/analysis/:path*',
    '/analytics/:path*',
    '/billing/:path*',
    '/settings/:path*',
    '/auth/:path*',
  ]
}