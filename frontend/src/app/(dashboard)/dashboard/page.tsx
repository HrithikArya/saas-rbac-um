'use client';

import { useAuthStore } from '@/stores/auth.store';
import { useOrgStore } from '@/stores/org.store';
import { useMembers } from '@/hooks/usePermission';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Users, Building2, ShieldCheck } from 'lucide-react';

export default function DashboardPage() {
  const { user } = useAuthStore();
  const { currentOrg, orgs } = useOrgStore();
  const { data: members, isLoading: membersLoading } = useMembers();

  const currentMember = members?.find((m) => m.userId === user?.id);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Dashboard</h1>
        <p className="text-muted-foreground">
          Welcome back, <span className="font-medium text-foreground">{user?.email}</span>
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-3">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Organization</CardTitle>
            <Building2 className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{currentOrg?.name ?? '—'}</div>
            <p className="text-xs text-muted-foreground">{orgs.length} org{orgs.length !== 1 ? 's' : ''} total</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Members</CardTitle>
            <Users className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {membersLoading ? '…' : (members?.length ?? 0)}
            </div>
            <p className="text-xs text-muted-foreground">in this organization</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Your Role</CardTitle>
            <ShieldCheck className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {membersLoading ? '…' : (currentMember?.role ?? '—')}
            </div>
            <p className="text-xs text-muted-foreground">in {currentOrg?.name ?? 'this org'}</p>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Getting started</CardTitle>
          <CardDescription>Here&apos;s what you can do with this platform</CardDescription>
        </CardHeader>
        <CardContent className="space-y-3">
          {[
            { label: 'Invite team members', description: 'Go to Settings → Members to invite colleagues by email.', done: (members?.length ?? 0) > 1 },
            { label: 'Set up billing', description: 'Go to Settings → Billing to manage your subscription.', done: false },
            { label: 'Assign roles', description: 'Owners and Admins can promote members to control access.', done: false },
          ].map((step) => (
            <div key={step.label} className="flex items-start gap-3">
              <div className={`mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full text-xs font-bold ${step.done ? 'bg-green-500 text-white' : 'border-2 border-muted-foreground/30 text-muted-foreground'}`}>
                {step.done ? '✓' : ''}
              </div>
              <div>
                <p className="text-sm font-medium">{step.label}</p>
                <p className="text-xs text-muted-foreground">{step.description}</p>
              </div>
            </div>
          ))}
        </CardContent>
      </Card>
    </div>
  );
}
