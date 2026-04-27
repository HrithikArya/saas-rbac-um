'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useAuthStore } from '@/stores/auth.store';
import { useOrgStore } from '@/stores/org.store';
import { getSubdomain, getTenantUrl } from '@/lib/subdomain';
import { Sidebar } from '@/components/layout/Sidebar';
import { Topbar } from '@/components/layout/Topbar';

export default function DashboardLayout({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isInitializing, initialize } = useAuthStore();
  const { fetchOrgs } = useOrgStore();
  const router = useRouter();

  useEffect(() => {
    initialize().then(async () => {
      const { isAuthenticated: authed } = useAuthStore.getState();
      if (!authed) {
        router.push('/login');
        return;
      }

      await fetchOrgs().catch(() => {});

      // Root domain should not host the dashboard — redirect to the right place
      const subdomain = getSubdomain();
      if (!subdomain) {
        const { orgs } = useOrgStore.getState();
        if (orgs.length === 1) {
          window.location.href = getTenantUrl(orgs[0].slug, '/dashboard');
        } else if (orgs.length > 1) {
          router.push('/select-tenant');
        }
        // orgs.length === 0: stay and show empty state / org creation
      }
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  if (isInitializing) {
    return (
      <div className="flex h-screen items-center justify-center">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
      </div>
    );
  }

  if (!isAuthenticated) return null;

  return (
    <div className="flex h-screen overflow-hidden">
      <Sidebar />
      <div className="flex flex-1 flex-col overflow-hidden">
        <Topbar />
        <main className="flex-1 overflow-auto p-6">{children}</main>
      </div>
    </div>
  );
}
