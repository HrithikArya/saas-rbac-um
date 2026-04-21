import type { NextConfig } from 'next';

const nextConfig: NextConfig = {
  // Proxy /api/* to the backend — uses BACKEND_URL (server-only env var).
  // This runs server-side so no NEXT_PUBLIC_ prefix needed.
  async rewrites() {
    const backendUrl = process.env.BACKEND_URL ?? 'http://localhost:49841';
    return [
      {
        source: '/api/:path*',
        destination: `${backendUrl}/:path*`,
      },
    ];
  },
};

export default nextConfig;
