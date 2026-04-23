'use client';

import { useEffect, useState } from 'react';
import saApi from '@/lib/sa-api';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Building2, Users, DollarSign, CreditCard } from 'lucide-react';

interface Stats {
  totalOrgs: number;
  totalUsers: number;
  totalRevenueCents: number;
  activeSubscriptions: number;
}

function StatCard({ title, value, icon: Icon, sub }: { title: string; value: string; icon: React.ElementType; sub?: string }) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">{title}</CardTitle>
        <Icon className="h-4 w-4 text-muted-foreground" />
      </CardHeader>
      <CardContent>
        <div className="text-2xl font-bold">{value}</div>
        {sub && <p className="text-xs text-muted-foreground mt-1">{sub}</p>}
      </CardContent>
    </Card>
  );
}

function fmt(cents: number) {
  return `$${(cents / 100).toLocaleString('en-US', { minimumFractionDigits: 2 })}`;
}

export default function SuperAdminDashboardPage() {
  const [stats, setStats] = useState<Stats | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    saApi.get<Stats>('/superadmin/stats').then(r => setStats(r.data)).finally(() => setLoading(false));
  }, []);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold">Dashboard</h1>
        <p className="text-muted-foreground text-sm">Platform overview</p>
      </div>

      {loading ? (
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
          {[...Array(4)].map((_, i) => (
            <Card key={i}><CardContent className="h-24 animate-pulse bg-muted rounded-md mt-4" /></Card>
          ))}
        </div>
      ) : stats ? (
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
          <StatCard title="Total Organizations" value={stats.totalOrgs.toString()} icon={Building2} />
          <StatCard title="Total Users" value={stats.totalUsers.toString()} icon={Users} />
          <StatCard title="Total Revenue" value={fmt(stats.totalRevenueCents)} icon={DollarSign} sub="All time" />
          <StatCard title="Active Subscriptions" value={stats.activeSubscriptions.toString()} icon={CreditCard} />
        </div>
      ) : null}
    </div>
  );
}
