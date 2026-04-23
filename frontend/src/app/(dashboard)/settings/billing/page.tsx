'use client';

import { useEffect, useState } from 'react';
import api from '@/lib/api';
import { useOrgStore } from '@/stores/org.store';
import { usePermission } from '@/hooks/usePermission';
import axios from 'axios';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Check, Zap, Building2, Users } from 'lucide-react';
import { cn } from '@/lib/utils';

interface BillingInfo {
  plan: string;
  status: string;
  currentPeriodEnd: string | null;
  stripeCustomerId: string | null;
}

const PLANS = [
  {
    id: 'price_starter',
    name: 'Starter',
    price: '$9',
    period: '/month',
    description: 'Perfect for small teams getting started',
    icon: Zap,
    features: ['Up to 10 members', 'Basic role management', 'Email invitations', 'Audit logs', '5 GB storage'],
    highlight: false,
  },
  {
    id: 'price_pro',
    name: 'Pro',
    price: '$29',
    period: '/month',
    description: 'For growing teams with advanced needs',
    icon: Building2,
    features: ['Unlimited members', 'Advanced RBAC', 'Priority support', 'Advanced reports', 'SSO (coming soon)', '50 GB storage'],
    highlight: true,
  },
] as const;

const statusVariant: Record<string, 'default' | 'secondary' | 'outline' | 'destructive'> = {
  Active: 'default', Trialing: 'secondary', PastDue: 'destructive', Canceled: 'outline',
};

export default function BillingPage() {
  const { currentOrgId } = useOrgStore();
  const canManage = usePermission('billing.manage');
  const [billing, setBilling] = useState<BillingInfo | null>(null);
  const [loading, setLoading] = useState(true);
  const [checkoutLoading, setCheckoutLoading] = useState<string | null>(null);
  const [portalLoading, setPortalLoading] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!currentOrgId) return;
    api.get<BillingInfo>(`/orgs/${currentOrgId}/subscription`)
      .then(r => setBilling(r.data))
      .catch(err => { if (axios.isAxiosError(err) && err.response?.status === 204) setBilling(null); })
      .finally(() => setLoading(false));
  }, [currentOrgId]);

  const handleCheckout = async (priceId: string) => {
    setError('');
    setCheckoutLoading(priceId);
    try {
      const { data } = await api.post<{ url: string }>('/billing/checkout', { priceId });
      window.location.href = data.url;
    } catch (err) {
      if (axios.isAxiosError(err)) setError(err.response?.data?.error ?? 'Failed to start checkout');
      else setError('Something went wrong');
      setCheckoutLoading(null);
    }
  };

  const handlePortal = async () => {
    setError('');
    setPortalLoading(true);
    try {
      const { data } = await api.post<{ url: string }>('/billing/portal');
      window.location.href = data.url;
    } catch (err) {
      if (axios.isAxiosError(err)) setError(err.response?.data?.error ?? 'Failed to open portal');
      else setError('Something went wrong');
      setPortalLoading(false);
    }
  };

  if (!canManage) {
    return (
      <div className="space-y-4">
        <h1 className="text-2xl font-bold">Billing</h1>
        <p className="text-sm text-muted-foreground">You need Owner permissions to manage billing.</p>
      </div>
    );
  }

  const isActive = billing?.status === 'Active' || billing?.status === 'Trialing';
  const currentPlanId = billing?.plan === 'Starter' ? 'price_starter' : billing?.plan === 'Pro' ? 'price_pro' : null;

  return (
    <div className="space-y-8 max-w-3xl">
      <div>
        <h1 className="text-2xl font-bold">Billing</h1>
        <p className="text-muted-foreground text-sm">Manage your subscription and plan</p>
      </div>

      {error && <p className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">{error}</p>}

      {/* Active subscription banner */}
      {!loading && isActive && (
        <Card className="border-primary/30 bg-primary/5">
          <CardContent className="flex items-center justify-between py-4">
            <div className="flex items-center gap-3">
              <Users className="h-5 w-5 text-primary" />
              <div>
                <p className="text-sm font-medium">
                  You are on the <span className="font-bold">{billing?.plan}</span> plan
                </p>
                <p className="text-xs text-muted-foreground">
                  {billing?.currentPeriodEnd
                    ? `Renews ${new Date(billing.currentPeriodEnd).toLocaleDateString()}`
                    : 'Active subscription'}
                </p>
              </div>
              <Badge variant={statusVariant[billing!.status] ?? 'outline'}>{billing!.status}</Badge>
            </div>
            <Button variant="outline" size="sm" onClick={handlePortal} disabled={portalLoading}>
              {portalLoading ? 'Opening…' : 'Manage subscription'}
            </Button>
          </CardContent>
        </Card>
      )}

      {/* Pricing cards — shown if not active, or to allow switching */}
      {!loading && (
        <div>
          {!isActive && (
            <div className="mb-4">
              <h2 className="text-lg font-semibold">Choose a plan</h2>
              <p className="text-sm text-muted-foreground">No payment required in dev mode — mock payment activates the plan instantly.</p>
            </div>
          )}
          {isActive && <h2 className="text-lg font-semibold mb-4">Switch plan</h2>}

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-6">
            {PLANS.filter(p => !isActive || p.id !== currentPlanId).map(plan => {
              const Icon = plan.icon;
              const isCurrent = currentPlanId === plan.id;
              return (
                <Card key={plan.id} className={cn('relative flex flex-col', plan.highlight && 'border-primary shadow-md')}>
                  {plan.highlight && !isActive && (
                    <div className="absolute -top-3 left-1/2 -translate-x-1/2">
                      <Badge className="bg-primary text-primary-foreground text-xs px-3">Most Popular</Badge>
                    </div>
                  )}
                  <CardHeader className="pb-3">
                    <div className="flex items-center gap-2 mb-1">
                      <Icon className="h-5 w-5 text-primary" />
                      <CardTitle className="text-lg">{plan.name}</CardTitle>
                    </div>
                    <div className="flex items-baseline gap-1">
                      <span className="text-3xl font-bold">{plan.price}</span>
                      <span className="text-muted-foreground text-sm">{plan.period}</span>
                    </div>
                    <CardDescription>{plan.description}</CardDescription>
                  </CardHeader>
                  <CardContent className="flex-1">
                    <ul className="space-y-2">
                      {plan.features.map(f => (
                        <li key={f} className="flex items-center gap-2 text-sm">
                          <Check className="h-4 w-4 text-primary shrink-0" />
                          {f}
                        </li>
                      ))}
                    </ul>
                  </CardContent>
                  <CardFooter>
                    <Button
                      className="w-full"
                      variant={plan.highlight && !isActive ? 'default' : 'outline'}
                      disabled={isCurrent || checkoutLoading === plan.id}
                      onClick={() => handleCheckout(plan.id)}
                    >
                      {isCurrent ? 'Current plan' : checkoutLoading === plan.id ? 'Redirecting…' : isActive ? `Switch to ${plan.name}` : `Get ${plan.name}`}
                    </Button>
                  </CardFooter>
                </Card>
              );
            })}
          </div>

          {!isActive && (
            <Card className="mt-4 bg-muted/40">
              <CardContent className="py-3 text-sm text-muted-foreground flex items-center gap-2">
                <Check className="h-4 w-4 shrink-0" />
                Currently on <strong className="mx-1">Free</strong> — up to 3 members, basic features included.
              </CardContent>
            </Card>
          )}
        </div>
      )}

      {loading && (
        <div className="grid grid-cols-2 gap-4">
          {[1, 2].map(i => <Card key={i}><CardContent className="h-64 animate-pulse bg-muted rounded-md mt-4" /></Card>)}
        </div>
      )}
    </div>
  );
}
