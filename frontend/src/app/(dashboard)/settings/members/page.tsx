'use client';

import { usePermission } from '@/hooks/usePermission';
import { Button } from '@/components/ui/button';
import { MembersTable } from '@/components/members/MembersTable';
import { InviteDialog } from '@/components/members/InviteDialog';
import { UserPlus } from 'lucide-react';

export default function MembersPage() {
  const canManage = usePermission('members.manage');

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Members</h1>
          <p className="text-muted-foreground">Manage who has access to your organization</p>
        </div>
        {canManage && (
          <InviteDialog>
            <Button>
              <UserPlus className="mr-2 h-4 w-4" />
              Invite member
            </Button>
          </InviteDialog>
        )}
      </div>

      <MembersTable />
    </div>
  );
}
