'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { Building2 } from 'lucide-react';
import { useAuthStore } from '@/stores/auth.store';
import { useOrgStore } from '@/stores/org.store';
import { getTenantUrl } from '@/lib/subdomain';
import { Card, CardContent } from '@/components/ui/card';

export default function SelectTenantPage() {
  const { isAuthenticated, isInitializing, initialize } = useAuthStore();
  const { orgs, fetchOrgs } = useOrgStore();
  const router = useRouter();

  useEffect(() => {
    initialize().then(async () => {
      const { isAuthenticated: authed } = useAuthStore.getState();
      if (!authed) {
        router.push('/login');
        return;
      }
      await fetchOrgs().catch(() => {});
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
    <div className="flex min-h-screen flex-col items-center justify-center bg-muted/40 p-4">
      <div className="w-full max-w-md space-y-6">
        <div className="text-center">
          <h1 className="text-2xl font-bold">Choose a workspace</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Select the organization you want to access
          </p>
        </div>

        {orgs.length === 0 ? (
          <Card>
            <CardContent className="py-12 text-center text-sm text-muted-foreground">
              You don&apos;t belong to any organization yet.
            </CardContent>
          </Card>
        ) : (
          <div className="space-y-2">
            {orgs.map((org) => (
              <button
                key={org.id}
                className="flex w-full items-center gap-4 rounded-lg border bg-background px-4 py-4 text-left transition-colors hover:bg-accent"
                onClick={() => {
                  window.location.href = getTenantUrl(org.slug, '/dashboard');
                }}
              >
                <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-md bg-primary/10">
                  <Building2 className="h-5 w-5 text-primary" />
                </div>
                <div className="min-w-0 flex-1">
                  <p className="truncate font-medium">{org.name}</p>
                  <p className="text-xs text-muted-foreground">
                    {org.memberCount} member{org.memberCount !== 1 ? 's' : ''}
                    {' · '}
                    {org.slug}.localhost
                  </p>
                </div>
              </button>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
