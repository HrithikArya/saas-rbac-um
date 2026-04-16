import type { NextConfig } from 'next';

const nextConfig: NextConfig = {
  // Proxy /api/* to the backend so the frontend never hard-codes the backend URL
  async rewrites() {
    return [
      {
        source: '/api/:path*',
        destination: `${process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000'}/:path*`,
      },
    ];
  },
};

export default nextConfig;
