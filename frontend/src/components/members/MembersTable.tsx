'use client';

import { useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import api from '@/lib/api';
import { useOrgStore } from '@/stores/org.store';
import { useAuthStore } from '@/stores/auth.store';
import { useMembers, usePermission } from '@/hooks/usePermission';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import type { MemberResponse, MemberRole } from '@/types/api';
import { toast } from '@/hooks/useToast';

const roleBadgeVariant: Record<MemberRole, 'default' | 'secondary' | 'outline'> = {
  Owner: 'default',
  Admin: 'secondary',
  Member: 'outline',
  Viewer: 'outline',
};

export function MembersTable() {
  const { currentOrgId } = useOrgStore();
  const { user } = useAuthStore();
  const queryClient = useQueryClient();
  const canManage = usePermission('members.manage');

  const { data: members, isLoading, isError } = useMembers();
  const [updatingId, setUpdatingId] = useState<string | null>(null);
  const [removingId, setRemovingId] = useState<string | null>(null);

  const handleRoleChange = async (member: MemberResponse, role: MemberRole) => {
    if (!currentOrgId) return;
    setUpdatingId(member.id);
    try {
      await api.patch(`/members/${member.id}/role`, { role });
      await queryClient.invalidateQueries({ queryKey: ['members', currentOrgId] });
      toast({ title: 'Role updated', description: `${member.email} is now ${role}.` });
    } finally {
      setUpdatingId(null);
    }
  };

  const handleRemove = async (member: MemberResponse) => {
    if (!currentOrgId) return;
    if (!confirm(`Remove ${member.email} from this organization?`)) return;
    setRemovingId(member.id);
    try {
      await api.delete(`/members/${member.id}`);
      await queryClient.invalidateQueries({ queryKey: ['members', currentOrgId] });
      toast({ title: 'Member removed', description: `${member.email} has been removed.` });
    } finally {
      setRemovingId(null);
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="h-6 w-6 animate-spin rounded-full border-4 border-primary border-t-transparent" />
      </div>
    );
  }

  if (isError) {
    return <p className="py-4 text-sm text-destructive">Failed to load members.</p>;
  }

  return (
    <div className="overflow-hidden rounded-md border">
      <table className="w-full text-sm">
        <thead className="bg-muted/50">
          <tr>
            <th className="px-4 py-3 text-left font-medium text-muted-foreground">Email</th>
            <th className="px-4 py-3 text-left font-medium text-muted-foreground">Role</th>
            <th className="px-4 py-3 text-left font-medium text-muted-foreground">Joined</th>
            {canManage && (
              <th className="px-4 py-3 text-right font-medium text-muted-foreground">Actions</th>
            )}
          </tr>
        </thead>
        <tbody className="divide-y">
          {members?.map((member) => {
            const isMe = member.userId === user?.id;
            const isOwner = member.role === 'Owner';
            const updating = updatingId === member.id;
            const removing = removingId === member.id;

            return (
              <tr key={member.id} className="bg-background hover:bg-muted/30 transition-colors">
                <td className="px-4 py-3">
                  <span className="font-medium">{member.email}</span>
                  {isMe && (
                    <span className="ml-2 text-xs text-muted-foreground">(you)</span>
                  )}
                </td>
                <td className="px-4 py-3">
                  {canManage && !isOwner && !isMe ? (
                    <Select
                      value={member.role}
                      onValueChange={(v) => handleRoleChange(member, v as MemberRole)}
                      disabled={updating}
                    >
                      <SelectTrigger className="h-7 w-28 text-xs">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="Admin">Admin</SelectItem>
                        <SelectItem value="Member">Member</SelectItem>
                        <SelectItem value="Viewer">Viewer</SelectItem>
                      </SelectContent>
                    </Select>
                  ) : (
                    <Badge variant={roleBadgeVariant[member.role]}>{member.role}</Badge>
                  )}
                </td>
                <td className="px-4 py-3 text-muted-foreground">
                  {new Date(member.joinedAt).toLocaleDateString()}
                </td>
                {canManage && (
                  <td className="px-4 py-3 text-right">
                    {!isOwner && !isMe && (
                      <Button
                        variant="ghost"
                        size="sm"
                        className="text-destructive hover:text-destructive"
                        onClick={() => handleRemove(member)}
                        disabled={removing}
                      >
                        {removing ? 'Removing…' : 'Remove'}
                      </Button>
                    )}
                  </td>
                )}
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
