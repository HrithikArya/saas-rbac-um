/** Returns the subdomain portion of the current hostname, or null on the root domain. */
export function getSubdomain(): string | null {
  if (typeof window === 'undefined') return null;
  const { hostname } = window.location;

  if (hostname === 'localhost') return null;

  // Local dev: acme.localhost
  if (hostname.endsWith('.localhost')) {
    const sub = hostname.slice(0, hostname.lastIndexOf('.localhost'));
    return sub || null;
  }

  // Production: acme.myapp.com (3+ parts)
  const parts = hostname.split('.');
  if (parts.length >= 3) return parts[0];

  return null;
}

/** Builds an absolute URL for a tenant subdomain, e.g. getTenantUrl('acme', '/dashboard'). */
export function getTenantUrl(slug: string, path = ''): string {
  if (typeof window === 'undefined') return path;
  const { protocol, hostname, port } = window.location;
  const portStr = port ? `:${port}` : '';

  if (hostname === 'localhost' || hostname.endsWith('.localhost')) {
    return `${protocol}//${slug}.localhost${portStr}${path}`;
  }

  const parts = hostname.split('.');
  const rootDomain = parts.slice(-2).join('.');
  return `${protocol}//${slug}.${rootDomain}${portStr}${path}`;
}

/** Builds an absolute URL for the root domain, e.g. getRootUrl('/select-tenant'). */
export function getRootUrl(path = ''): string {
  if (typeof window === 'undefined') return path;
  const { protocol, hostname, port } = window.location;
  const portStr = port ? `:${port}` : '';

  if (hostname === 'localhost' || hostname.endsWith('.localhost')) {
    return `${protocol}//localhost${portStr}${path}`;
  }

  const parts = hostname.split('.');
  const rootDomain = parts.slice(-2).join('.');
  return `${protocol}//${rootDomain}${portStr}${path}`;
}
