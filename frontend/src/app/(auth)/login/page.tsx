'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { useAuthStore } from '@/stores/auth.store';
import { useOrgStore } from '@/stores/org.store';
import { getSubdomain, getTenantUrl } from '@/lib/subdomain';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import api from '@/lib/api';
import axios from 'axios';
import type { OrgResponse } from '@/types/api';

export default function LoginPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [tenantOrg, setTenantOrg] = useState<OrgResponse | null>(null);

  const { login } = useAuthStore();
  const { fetchOrgs } = useOrgStore();
  const router = useRouter();

  // Fetch org info for tenant login branding (e.g. "Sign in to Acme Corp")
  useEffect(() => {
    const subdomain = getSubdomain();
    if (!subdomain) return;
    api.get<OrgResponse>(`/orgs/slug/${subdomain}`)
      .then((res) => setTenantOrg(res.data))
      .catch(() => {});
  }, []);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      await login(email, password);
      await fetchOrgs();

      const subdomain = getSubdomain();
      const { orgs } = useOrgStore.getState();

      if (subdomain) {
        // Stay on this tenant's subdomain
        router.push('/dashboard');
      } else if (orgs.length === 1) {
        // Single org — go straight to its subdomain
        window.location.href = getTenantUrl(orgs[0].slug, '/dashboard');
      } else {
        // Multiple orgs (or zero) — let user pick
        router.push('/select-tenant');
      }
    } catch (err) {
      if (axios.isAxiosError(err)) {
        setError(err.response?.data?.error ?? 'Invalid credentials');
      } else {
        setError('Something went wrong');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>
          {tenantOrg ? `Sign in to ${tenantOrg.name}` : 'Welcome back'}
        </CardTitle>
        <CardDescription>
          {tenantOrg
            ? `Enter your credentials to access ${tenantOrg.name}`
            : 'Sign in to your account'}
        </CardDescription>
      </CardHeader>
      <form onSubmit={handleSubmit}>
        <CardContent className="space-y-4">
          {error && (
            <p className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">{error}</p>
          )}
          <div className="space-y-1">
            <Label htmlFor="email">Email</Label>
            <Input
              id="email"
              type="email"
              placeholder="you@example.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />
          </div>
          <div className="space-y-1">
            <Label htmlFor="password">Password</Label>
            <Input
              id="password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />
          </div>
          <div className="text-right">
            <Link href="/forgot-password" className="text-xs text-muted-foreground hover:underline">
              Forgot password?
            </Link>
          </div>
        </CardContent>
        <CardFooter className="flex-col gap-3">
          <Button type="submit" className="w-full" disabled={loading}>
            {loading ? 'Signing in…' : 'Sign in'}
          </Button>
          <p className="text-center text-sm text-muted-foreground">
            No account?{' '}
            <Link href="/register" className="font-medium text-primary hover:underline">
              Register
            </Link>
          </p>
        </CardFooter>
      </form>
    </Card>
  );
}
