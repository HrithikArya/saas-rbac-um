'use client';

import { useEffect, useState } from 'react';
import saApi from '@/lib/sa-api';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { DollarSign, TrendingUp, Receipt } from 'lucide-react';

interface Monthly { year: number; month: number; monthLabel: string; amountCents: number; paymentCount: number; }
interface OrgEarning { orgId: string; orgName: string; totalAmountCents: number; paymentCount: number; }
interface Payment { id: string; planName: string; amountCents: number; status: string; createdAt: string; }
interface Earnings { totalRevenueCents: number; totalPayments: number; monthly: Monthly[]; byOrg: OrgEarning[]; recentPayments: Payment[]; }

function fmt(cents: number) { return `$${(cents / 100).toLocaleString('en-US', { minimumFractionDigits: 2 })}`; }

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

export default function SuperAdminEarningsPage() {
  const [data, setData] = useState<Earnings | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    saApi.get<Earnings>('/superadmin/earnings').then(r => setData(r.data)).finally(() => setLoading(false));
  }, []);

  if (loading) return <div className="text-muted-foreground text-sm">Loading…</div>;
  if (!data) return null;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold">Earnings</h1>
        <p className="text-muted-foreground text-sm">Revenue from mock payments</p>
      </div>

      <div className="grid grid-cols-3 gap-4">
        <StatCard title="Total Revenue" value={fmt(data.totalRevenueCents)} icon={DollarSign} sub="All time" />
        <StatCard title="Total Payments" value={data.totalPayments.toString()} icon={Receipt} />
        <StatCard title="Monthly Avg" value={data.monthly.length > 0 ? fmt(Math.round(data.totalRevenueCents / data.monthly.length)) : '$0.00'} icon={TrendingUp} sub="Per month" />
      </div>

      <div className="grid grid-cols-2 gap-6">
        {/* Monthly Breakdown */}
        <Card>
          <CardHeader><CardTitle className="text-base">Monthly Revenue</CardTitle></CardHeader>
          <CardContent className="p-0">
            {data.monthly.length === 0 ? (
              <p className="px-4 py-6 text-sm text-muted-foreground text-center">No payments yet</p>
            ) : (
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b bg-muted/50">
                    <th className="text-left px-4 py-2 font-medium">Month</th>
                    <th className="text-right px-4 py-2 font-medium">Revenue</th>
                    <th className="text-right px-4 py-2 font-medium">Payments</th>
                  </tr>
                </thead>
                <tbody>
                  {data.monthly.map(m => (
                    <tr key={`${m.year}-${m.month}`} className="border-b last:border-0">
                      <td className="px-4 py-2">{m.monthLabel}</td>
                      <td className="px-4 py-2 text-right font-medium">{fmt(m.amountCents)}</td>
                      <td className="px-4 py-2 text-right text-muted-foreground">{m.paymentCount}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </CardContent>
        </Card>

        {/* By Org */}
        <Card>
          <CardHeader><CardTitle className="text-base">Revenue by Organization</CardTitle></CardHeader>
          <CardContent className="p-0">
            {data.byOrg.length === 0 ? (
              <p className="px-4 py-6 text-sm text-muted-foreground text-center">No payments yet</p>
            ) : (
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b bg-muted/50">
                    <th className="text-left px-4 py-2 font-medium">Organization</th>
                    <th className="text-right px-4 py-2 font-medium">Total</th>
                  </tr>
                </thead>
                <tbody>
                  {data.byOrg.map(o => (
                    <tr key={o.orgId} className="border-b last:border-0">
                      <td className="px-4 py-2">
                        <p>{o.orgName}</p>
                        <p className="text-xs text-muted-foreground">{o.paymentCount} payment{o.paymentCount !== 1 ? 's' : ''}</p>
                      </td>
                      <td className="px-4 py-2 text-right font-medium">{fmt(o.totalAmountCents)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Recent Payments */}
      {data.recentPayments.length > 0 && (
        <Card>
          <CardHeader><CardTitle className="text-base">Recent Payments</CardTitle></CardHeader>
          <CardContent className="p-0">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-muted/50">
                  <th className="text-left px-4 py-2 font-medium">Plan</th>
                  <th className="text-left px-4 py-2 font-medium">Amount</th>
                  <th className="text-left px-4 py-2 font-medium">Date</th>
                  <th className="text-left px-4 py-2 font-medium">Status</th>
                </tr>
              </thead>
              <tbody>
                {data.recentPayments.map(p => (
                  <tr key={p.id} className="border-b last:border-0">
                    <td className="px-4 py-2">{p.planName}</td>
                    <td className="px-4 py-2 font-medium">{fmt(p.amountCents)}</td>
                    <td className="px-4 py-2 text-muted-foreground">{new Date(p.createdAt).toLocaleDateString()}</td>
                    <td className="px-4 py-2"><Badge variant="default">{p.status}</Badge></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
