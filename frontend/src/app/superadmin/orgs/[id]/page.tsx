'use client';

import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import saApi from '@/lib/sa-api';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { ArrowLeft, CheckCircle } from 'lucide-react';

interface Plan { id: string; name: string; priceInCents: number; }
interface Member { userId: string; email: string; role: string; }
interface Payment { id: string; planName: string; amountCents: number; status: string; createdAt: string; }
interface OrgSummary { id: string; name: string; slug: string; ownerEmail: string; memberCount: number; planName: string; subscriptionStatus: string | null; planId: string | null; createdAt: string; }
interface OrgDetail { summary: OrgSummary; members: Member[]; payments: Payment[]; availablePlans: Plan[]; }

function fmt(cents: number) { return `$${(cents / 100).toFixed(2)}`; }

export default function SuperAdminOrgDetailPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const [detail, setDetail] = useState<OrgDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [selectedPlan, setSelectedPlan] = useState('');
  const [changing, setChanging] = useState(false);
  const [changed, setChanged] = useState(false);

  useEffect(() => {
    saApi.get<OrgDetail>(`/superadmin/orgs/${id}`)
      .then(r => {
        setDetail(r.data);
        setSelectedPlan(r.data.summary.planId ?? '');
      })
      .finally(() => setLoading(false));
  }, [id]);

  const handlePlanChange = async () => {
    if (!selectedPlan) return;
    setChanging(true);
    try {
      await saApi.patch(`/superadmin/orgs/${id}/plan`, { planId: selectedPlan });
      setChanged(true);
      setTimeout(() => setChanged(false), 3000);
      const r = await saApi.get<OrgDetail>(`/superadmin/orgs/${id}`);
      setDetail(r.data);
    } finally {
      setChanging(false);
    }
  };

  if (loading) return <div className="text-muted-foreground text-sm">Loading…</div>;
  if (!detail) return <div className="text-destructive text-sm">Not found</div>;

  const { summary, members, payments, availablePlans } = detail;

  return (
    <div className="space-y-6 max-w-4xl">
      <div className="flex items-center gap-3">
        <Button variant="ghost" size="icon" onClick={() => router.push('/superadmin/orgs')}>
          <ArrowLeft className="h-4 w-4" />
        </Button>
        <div>
          <h1 className="text-2xl font-bold">{summary.name}</h1>
          <p className="text-muted-foreground text-sm">{summary.slug} · {summary.ownerEmail}</p>
        </div>
      </div>

      {/* Plan Management */}
      <Card>
        <CardHeader><CardTitle className="text-base">Subscription</CardTitle></CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center gap-2 text-sm">
            <span className="text-muted-foreground">Current plan:</span>
            <span className="font-medium">{summary.planName}</span>
            {summary.subscriptionStatus && (
              <Badge variant={summary.subscriptionStatus === 'Active' ? 'default' : 'outline'}>
                {summary.subscriptionStatus}
              </Badge>
            )}
          </div>
          <div className="flex items-center gap-3">
            <Select value={selectedPlan} onValueChange={setSelectedPlan}>
              <SelectTrigger className="w-48">
                <SelectValue placeholder="Select plan" />
              </SelectTrigger>
              <SelectContent>
                {availablePlans.map(p => (
                  <SelectItem key={p.id} value={p.id}>
                    {p.name} {p.priceInCents > 0 ? `— ${fmt(p.priceInCents)}/mo` : '— Free'}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <Button onClick={handlePlanChange} disabled={changing || !selectedPlan} size="sm">
              {changing ? 'Saving…' : 'Apply Plan'}
            </Button>
            {changed && <CheckCircle className="h-4 w-4 text-green-500" />}
          </div>
        </CardContent>
      </Card>

      {/* Members */}
      <Card>
        <CardHeader><CardTitle className="text-base">Members ({members.length})</CardTitle></CardHeader>
        <CardContent className="p-0">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50">
                <th className="text-left px-4 py-2 font-medium">Email</th>
                <th className="text-left px-4 py-2 font-medium">Role</th>
              </tr>
            </thead>
            <tbody>
              {members.map(m => (
                <tr key={m.userId} className="border-b last:border-0">
                  <td className="px-4 py-2">{m.email}</td>
                  <td className="px-4 py-2">
                    <Badge variant={m.role === 'Owner' ? 'default' : 'secondary'}>{m.role}</Badge>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </CardContent>
      </Card>

      {/* Payment History */}
      {payments.length > 0 && (
        <Card>
          <CardHeader><CardTitle className="text-base">Payment History</CardTitle></CardHeader>
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
                {payments.map(p => (
                  <tr key={p.id} className="border-b last:border-0">
                    <td className="px-4 py-2">{p.planName}</td>
                    <td className="px-4 py-2 font-medium">{fmt(p.amountCents)}</td>
                    <td className="px-4 py-2 text-muted-foreground">{new Date(p.createdAt).toLocaleDateString()}</td>
                    <td className="px-4 py-2">
                      <Badge variant={p.status === 'Paid' ? 'default' : 'outline'}>{p.status}</Badge>
                    </td>
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
