'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import saApi from '@/lib/sa-api';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog';
import { Plus, ExternalLink } from 'lucide-react';

interface OrgSummary {
  id: string;
  name: string;
  slug: string;
  ownerEmail: string;
  memberCount: number;
  planName: string;
  subscriptionStatus: string | null;
  planPriceInCents: number;
  createdAt: string;
}

const statusVariant: Record<string, 'default' | 'secondary' | 'outline' | 'destructive'> = {
  Active: 'default',
  Trialing: 'secondary',
  PastDue: 'destructive',
  Canceled: 'outline',
};

function fmt(cents: number) {
  if (cents === 0) return 'Free';
  return `$${(cents / 100).toFixed(0)}/mo`;
}

export default function SuperAdminOrgsPage() {
  const [orgs, setOrgs] = useState<OrgSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [showCreate, setShowCreate] = useState(false);
  const [orgName, setOrgName] = useState('');
  const [ownerEmail, setOwnerEmail] = useState('');
  const [creating, setCreating] = useState(false);
  const [createError, setCreateError] = useState('');

  const load = () => saApi.get<OrgSummary[]>('/superadmin/orgs').then(r => setOrgs(r.data)).finally(() => setLoading(false));

  useEffect(() => { load(); }, []);

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    setCreateError('');
    setCreating(true);
    try {
      await saApi.post('/superadmin/orgs', { orgName, ownerEmail });
      setShowCreate(false);
      setOrgName('');
      setOwnerEmail('');
      load();
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error;
      setCreateError(msg ?? 'Failed to create organization');
    } finally {
      setCreating(false);
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Organizations</h1>
          <p className="text-muted-foreground text-sm">{orgs.length} total</p>
        </div>
        <Button onClick={() => setShowCreate(true)} size="sm">
          <Plus className="h-4 w-4 mr-1" /> New Org
        </Button>
      </div>

      <Card>
        <CardContent className="p-0">
          {loading ? (
            <div className="p-8 text-center text-muted-foreground text-sm">Loading…</div>
          ) : orgs.length === 0 ? (
            <div className="p-8 text-center text-muted-foreground text-sm">No organizations yet</div>
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-muted/50">
                  <th className="text-left px-4 py-3 font-medium">Organization</th>
                  <th className="text-left px-4 py-3 font-medium">Owner</th>
                  <th className="text-left px-4 py-3 font-medium">Members</th>
                  <th className="text-left px-4 py-3 font-medium">Plan</th>
                  <th className="text-left px-4 py-3 font-medium">Status</th>
                  <th className="px-4 py-3" />
                </tr>
              </thead>
              <tbody>
                {orgs.map(org => (
                  <tr key={org.id} className="border-b last:border-0 hover:bg-muted/30">
                    <td className="px-4 py-3">
                      <p className="font-medium">{org.name}</p>
                      <p className="text-xs text-muted-foreground">{org.slug}</p>
                    </td>
                    <td className="px-4 py-3 text-muted-foreground">{org.ownerEmail}</td>
                    <td className="px-4 py-3">{org.memberCount}</td>
                    <td className="px-4 py-3">
                      <p>{org.planName}</p>
                      <p className="text-xs text-muted-foreground">{fmt(org.planPriceInCents)}</p>
                    </td>
                    <td className="px-4 py-3">
                      {org.subscriptionStatus ? (
                        <Badge variant={statusVariant[org.subscriptionStatus] ?? 'outline'}>
                          {org.subscriptionStatus}
                        </Badge>
                      ) : (
                        <span className="text-muted-foreground text-xs">—</span>
                      )}
                    </td>
                    <td className="px-4 py-3 text-right">
                      <Link href={`/superadmin/orgs/${org.id}`}>
                        <Button variant="ghost" size="icon"><ExternalLink className="h-4 w-4" /></Button>
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </CardContent>
      </Card>

      <Dialog open={showCreate} onOpenChange={setShowCreate}>
        <DialogContent>
          <DialogHeader><DialogTitle>Create Organization</DialogTitle></DialogHeader>
          <form onSubmit={handleCreate} className="space-y-4">
            {createError && <p className="text-sm text-destructive">{createError}</p>}
            <div className="space-y-1">
              <Label>Organization Name</Label>
              <Input value={orgName} onChange={e => setOrgName(e.target.value)} required />
            </div>
            <div className="space-y-1">
              <Label>Owner Email (must be a registered user)</Label>
              <Input type="email" value={ownerEmail} onChange={e => setOwnerEmail(e.target.value)} required />
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setShowCreate(false)}>Cancel</Button>
              <Button type="submit" disabled={creating}>{creating ? 'Creating…' : 'Create'}</Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
