'use client';

import { useSearchParams, useRouter } from 'next/navigation';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';

export default function BillingMockPage() {
  const params = useSearchParams();
  const router = useRouter();

  const action = params.get('action');
  const priceId = params.get('priceId');
  const orgId = params.get('orgId');

  const isCheckout = action === 'checkout';

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
              ? 'In production this would redirect to a Stripe Checkout page.'
              : 'In production this would redirect to the Stripe Customer Portal.'}
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-3 text-sm">
          <div className="rounded-md border bg-muted/50 p-3 font-mono text-xs space-y-1">
            <p><span className="text-muted-foreground">action:</span> {action}</p>
            {priceId && <p><span className="text-muted-foreground">priceId:</span> {priceId}</p>}
            <p><span className="text-muted-foreground">orgId:</span> {orgId}</p>
          </div>
          <p className="text-muted-foreground">
            No <code className="text-xs bg-muted px-1 rounded">STRIPE_SECRET_KEY</code> is configured.
            Set one in <code className="text-xs bg-muted px-1 rounded">appsettings.Development.json</code> to
            enable real Stripe sessions.
          </p>
        </CardContent>
        <CardFooter>
          <Button className="w-full" onClick={() => router.push('/settings/billing')}>
            Back to billing
          </Button>
        </CardFooter>
      </Card>
    </div>
  );
}
