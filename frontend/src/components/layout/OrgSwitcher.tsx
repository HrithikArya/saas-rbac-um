'use client';

import { Building2, ChevronsUpDown, Plus } from 'lucide-react';
import { useOrgStore } from '@/stores/org.store';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { NewOrgDialog } from './NewOrgDialog';

export function OrgSwitcher() {
  const { orgs, currentOrg, setCurrentOrg } = useOrgStore();

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" className="w-full justify-between px-2 font-normal">
          <div className="flex items-center gap-2 min-w-0">
            <Building2 className="h-4 w-4 shrink-0 text-muted-foreground" />
            <span className="truncate text-sm">{currentOrg?.name ?? 'Select org'}</span>
          </div>
          <ChevronsUpDown className="h-4 w-4 shrink-0 opacity-50" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start" className="w-56">
        <DropdownMenuLabel className="text-xs text-muted-foreground">Organizations</DropdownMenuLabel>
        <DropdownMenuSeparator />
        {orgs.map((org) => (
          <DropdownMenuItem
            key={org.id}
            onClick={() => setCurrentOrg(org.id)}
            className="gap-2"
          >
            <Building2 className="h-4 w-4" />
            <span className="truncate">{org.name}</span>
            {org.id === currentOrg?.id && <span className="ml-auto text-xs text-muted-foreground">current</span>}
          </DropdownMenuItem>
        ))}
        <DropdownMenuSeparator />
        <NewOrgDialog>
          <DropdownMenuItem className="gap-2 text-muted-foreground" onSelect={(e) => e.preventDefault()}>
            <Plus className="h-4 w-4" />
            New organization
          </DropdownMenuItem>
        </NewOrgDialog>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
