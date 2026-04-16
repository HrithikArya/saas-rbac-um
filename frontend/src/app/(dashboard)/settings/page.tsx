'use client';

import { useState } from 'react';
import { useOrgStore } from '@/stores/org.store';
import api from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { usePermission } from '@/hooks/usePermission';
import axios from 'axios';

export default function SettingsPage() {
  const { currentOrg, fetchOrgs } = useOrgStore();
  const canManage = usePermission('members.manage');

  const [name, setName] = useState(currentOrg?.name ?? '');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(false);

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!currentOrg) return;
    setError('');
    setSuccess(false);
    setSaving(true);
    try {
      await api.put(`/orgs/${currentOrg.id}`, { name });
      await fetchOrgs();
      setSuccess(true);
    } catch (err) {
      if (axios.isAxiosError(err)) {
        setError(err.response?.data?.error ?? 'Failed to update');
      } else {
        setError('Something went wrong');
      }
    } finally {
      setSaving(false);
    }
  };

  if (!currentOrg) {
    return <p className="text-muted-foreground">No organization selected.</p>;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Settings</h1>
        <p className="text-muted-foreground">Manage your organization settings</p>
      </div>

      <Card className="max-w-lg">
        <form onSubmit={handleSave}>
          <CardHeader>
            <CardTitle>General</CardTitle>
            <CardDescription>Update your organization&apos;s display name</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            {error && (
              <p className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">{error}</p>
            )}
            {success && (
              <p className="rounded-md bg-green-500/10 px-3 py-2 text-sm text-green-700 dark:text-green-400">
                Organization updated successfully.
              </p>
            )}
            <div className="space-y-1">
              <Label htmlFor="name">Organization name</Label>
              <Input
                id="name"
                value={name}
                onChange={(e) => { setName(e.target.value); setSuccess(false); }}
                disabled={!canManage}
                required
              />
            </div>
            <div className="space-y-1">
              <Label>Slug</Label>
              <Input value={currentOrg.slug} disabled className="text-muted-foreground" />
              <p className="text-xs text-muted-foreground">Slug cannot be changed after creation.</p>
            </div>
          </CardContent>
          {canManage && (
            <CardFooter>
              <Button type="submit" disabled={saving}>
                {saving ? 'Saving…' : 'Save changes'}
              </Button>
            </CardFooter>
          )}
        </form>
      </Card>
    </div>
  );
}
