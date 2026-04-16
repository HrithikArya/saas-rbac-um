'use client';

import { useQuery } from '@tanstack/react-query';
import api from '@/lib/api';
import { useOrgStore } from '@/stores/org.store';
import { usePermission } from '@/hooks/usePermission';
import { BillingCard } from '@/components/billing/BillingCard';

interface SubscriptionInfo {
  plan: string;
  status: string;
  currentPeriodEnd: string | null;
  stripeCustomerId: string | null;
}

export default function BillingPage() {
  const { currentOrgId } = useOrgStore();
  const canManage = usePermission('billing.manage');

  const { data: billing, isLoading } = useQuery<SubscriptionInfo | null>({
    queryKey: ['billing', currentOrgId],
    queryFn: () =>
      api.get<SubscriptionInfo>(`/orgs/${currentOrgId}/subscription`).then((r) =>
        r.status === 204 ? null : r.data
      ),
    enabled: !!currentOrgId,
    staleTime: 60_000,
  });

  if (!canManage) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Billing</h1>
          <p className="text-muted-foreground">Manage your subscription</p>
        </div>
        <p className="text-sm text-muted-foreground">
          You need Owner permissions to manage billing.
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Billing</h1>
        <p className="text-muted-foreground">Manage your subscription and payment details</p>
      </div>

      <div className="max-w-2xl">
        <BillingCard billing={billing ?? null} isLoading={isLoading} />
      </div>
    </div>
  );
}
