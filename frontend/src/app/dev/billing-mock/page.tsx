'use client';

import { useState, useEffect } from 'react';
import { useSearchParams, useRouter } from 'next/navigation';
import api, { setAccessToken } from '@/lib/api';
import axios from 'axios';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { CheckCircle, CreditCard } from 'lucide-react';

const PLAN_LABELS: Record<string, { name: string; price: string }> = {
  price_starter: { name: 'Starter', price: '$9.00' },
  price_pro: { name: 'Pro', price: '$29.00' },
};

export default function BillingMockPage() {
  const params = useSearchParams();
  const router = useRouter();

  const action = params.get('action');
  const priceId = params.get('priceId') ?? '';
  const orgId = params.get('orgId') ?? '';

  const [paying, setPaying] = useState(false);
  const [paid, setPaid] = useState(false);
  const [error, setError] = useState('');
  const [ready, setReady] = useState(false);

  // Restore access token after full-page redirect
  useEffect(() => {
    const refreshToken = localStorage.getItem('refreshToken');
    if (!refreshToken) { setReady(true); return; }
    axios.post('/api/auth/refresh', { refreshToken })
      .then(({ data }) => {
        setAccessToken(data.accessToken);
        localStorage.setItem('refreshToken', data.refreshToken);
      })
      .catch(() => {})
      .finally(() => setReady(true));
  }, []);

  const isCheckout = action === 'checkout';
  const plan = PLAN_LABELS[priceId];

  const handlePay = async () => {
    setError('');
    setPaying(true);
    try {
      // orgId from URL params so the endpoint works without X-Organization-Id header
      await api.post('/billing/mock-confirm', { priceId, orgId });
      setPaid(true);
    } catch (err) {
      if (axios.isAxiosError(err)) setError(err.response?.data?.error ?? 'Payment failed');
      else setError('Something went wrong');
    } finally {
      setPaying(false);
    }
  };

  if (paid) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-muted/40">
        <Card className="w-full max-w-md text-center">
          <CardHeader>
            <CheckCircle className="h-12 w-12 text-green-500 mx-auto mb-2" />
            <CardTitle>Payment Successful!</CardTitle>
            <CardDescription>
              Your organization is now on the <strong>{plan?.name ?? priceId}</strong> plan.
            </CardDescription>
          </CardHeader>
          <CardFooter className="justify-center">
            <Button onClick={() => router.push('/settings/billing')}>Back to Billing</Button>
          </CardFooter>
        </Card>
      </div>
    );
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-muted/40">
      <Card className="w-full max-w-md">
        <CardHeader>
          <div className="flex items-center gap-2">
            <CardTitle>{isCheckout ? 'Mock Checkout' : 'Mock Billing Portal'}</CardTitle>
            <Badge variant="secondary">Dev only</Badge>
          </div>
          <CardDescription>
            {isCheckout
              ? 'In production this would redirect to Stripe Checkout.'
              : 'In production this would redirect to the Stripe Customer Portal.'}
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {isCheckout && plan && (
            <div className="rounded-lg border bg-muted/30 p-4 space-y-3">
              <p className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Order Summary</p>
              <div className="flex items-center justify-between">
                <div>
                  <p className="font-medium">{plan.name} Plan</p>
                  <p className="text-xs text-muted-foreground">Billed monthly</p>
                </div>
                <p className="text-lg font-bold">{plan.price}<span className="text-xs font-normal text-muted-foreground">/mo</span></p>
              </div>
              <div className="border-t pt-2 flex justify-between text-sm">
                <span className="font-medium">Total today</span>
                <span className="font-bold">{plan.price}</span>
              </div>
            </div>
          )}
          <div className="rounded-md border bg-muted/50 p-3 font-mono text-xs space-y-1">
            <p><span className="text-muted-foreground">action:</span> {action}</p>
            {priceId && <p><span className="text-muted-foreground">priceId:</span> {priceId}</p>}
            <p><span className="text-muted-foreground">orgId:</span> {orgId}</p>
          </div>
          <p className="text-xs text-muted-foreground">
            No <code className="bg-muted px-1 rounded">STRIPE_SECRET_KEY</code> configured.
            Clicking &ldquo;Pay Now&rdquo; activates the subscription instantly in the database.
          </p>
          {error && <p className="text-sm text-destructive">{error}</p>}
        </CardContent>
        <CardFooter className="flex gap-2">
          {isCheckout ? (
            <>
              <Button variant="outline" className="flex-1" onClick={() => router.push('/settings/billing')}>
                Cancel
              </Button>
              <Button className="flex-1" onClick={handlePay} disabled={paying || !priceId || !ready}>
                <CreditCard className="h-4 w-4 mr-2" />
                {paying ? 'Processing…' : 'Pay Now (Mock)'}
              </Button>
            </>
          ) : (
            <Button className="w-full" onClick={() => router.push('/settings/billing')}>
              Back to Billing
            </Button>
          )}
        </CardFooter>
      </Card>
    </div>
  );
}
