import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';

export function middleware(request: NextRequest) {
  const host = request.headers.get('host') ?? '';
  const hostname = host.split(':')[0]; // strip port

  let subdomain: string | null = null;

  if (hostname !== 'localhost' && hostname.endsWith('.localhost')) {
    subdomain = hostname.slice(0, hostname.lastIndexOf('.localhost')) || null;
  } else {
    const parts = hostname.split('.');
    if (parts.length >= 3) subdomain = parts[0];
  }

  const requestHeaders = new Headers(request.headers);
  if (subdomain) {
    requestHeaders.set('x-subdomain', subdomain);
  } else {
    requestHeaders.delete('x-subdomain');
  }

  return NextResponse.next({ request: { headers: requestHeaders } });
}

export const config = {
  matcher: ['/((?!_next/static|_next/image|favicon.ico).*)'],
};
