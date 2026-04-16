'use client';

import { useState } from 'react';
import api from '@/lib/api';
import { useOrgStore } from '@/stores/org.store';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import axios from 'axios';

interface BillingInfo {
  plan: string;
  status: string;
  currentPeriodEnd: string | null;
  stripeCustomerId: string | null;
}

interface BillingCardProps {
  billing: BillingInfo | null;
  isLoading: boolean;
}

const statusVariant: Record<string, 'default' | 'secondary' | 'outline' | 'destructive'> = {
  Active: 'default',
  Trialing: 'secondary',
  PastDue: 'destructive',
  Canceled: 'outline',
  Incomplete: 'outline',
};

const PRICE_IDS = [
  { id: 'price_starter', label: 'Starter', description: '$9/month — Up to 5 members, basic features' },
  { id: 'price_pro', label: 'Pro', description: '$29/month — Unlimited members, advanced features' },
];

export function BillingCard({ billing, isLoading }: BillingCardProps) {
  const { currentOrgId } = useOrgStore();
  const [checkoutLoading, setCheckoutLoading] = useState<string | null>(null);
  const [portalLoading, setPortalLoading] = useState(false);
  const [error, setError] = useState('');

  const handleCheckout = async (priceId: string) => {
    if (!currentOrgId) return;
    setError('');
    setCheckoutLoading(priceId);
    try {
      const { data } = await api.post<{ url: string }>('/billing/checkout', { priceId });
      window.location.href = data.url;
    } catch (err) {
      if (axios.isAxiosError(err)) {
        setError(err.response?.data?.error ?? 'Failed to start checkout');
      } else {
        setError('Something went wrong');
      }
      setCheckoutLoading(null);
    }
  };

  const handlePortal = async () => {
    if (!currentOrgId) return;
    setError('');
    setPortalLoading(true);
    try {
      const { data } = await api.post<{ url: string }>('/billing/portal');
      window.location.href = data.url;
    } catch (err) {
      if (axios.isAxiosError(err)) {
        setError(err.response?.data?.error ?? 'Failed to open billing portal');
      } else {
        setError('Something went wrong');
      }
      setPortalLoading(false);
    }
  };

  if (isLoading) {
    return (
      <Card>
        <CardContent className="flex items-center justify-center py-12">
          <div className="h-6 w-6 animate-spin rounded-full border-4 border-primary border-t-transparent" />
        </CardContent>
      </Card>
    );
  }

  const hasActiveSubscription =
    billing?.status === 'Active' || billing?.status === 'Trialing';

  return (
    <div className="space-y-4">
      {error && (
        <p className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">{error}</p>
      )}

      {/* Current plan */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle>Current plan</CardTitle>
            {billing?.status && (
              <Badge variant={statusVariant[billing.status] ?? 'outline'}>
                {billing.status}
              </Badge>
            )}
          </div>
          <CardDescription>
            {billing?.plan ?? 'Free'} plan
            {billing?.currentPeriodEnd && (
              <span className="ml-1">
                · Renews {new Date(billing.currentPeriodEnd).toLocaleDateString()}
              </span>
            )}
          </CardDescription>
        </CardHeader>
        {hasActiveSubscription && (
          <CardFooter>
            <Button variant="outline" onClick={handlePortal} disabled={portalLoading}>
              {portalLoading ? 'Opening portal…' : 'Manage subscription'}
            </Button>
          </CardFooter>
        )}
      </Card>

      {/* Upgrade options — only shown if not active */}
      {!hasActiveSubscription && (
        <div className="space-y-3">
          <h3 className="text-sm font-semibold text-muted-foreground uppercase tracking-wider">
            Upgrade
          </h3>
          {PRICE_IDS.map(({ id, label, description }) => (
            <Card key={id}>
              <CardHeader className="pb-3">
                <CardTitle className="text-base">{label}</CardTitle>
                <CardDescription>{description}</CardDescription>
              </CardHeader>
              <CardFooter>
                <Button
                  onClick={() => handleCheckout(id)}
                  disabled={checkoutLoading === id}
                  className="w-full sm:w-auto"
                >
                  {checkoutLoading === id ? 'Redirecting…' : `Upgrade to ${label}`}
                </Button>
              </CardFooter>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
