import { useQuery } from '@tanstack/react-query';
import api from '@/lib/api';
import { useAuthStore } from '@/stores/auth.store';
import { useOrgStore } from '@/stores/org.store';
import type { MemberResponse, MemberRole } from '@/types/api';

const ROLE_PERMISSIONS: Record<MemberRole, string[]> = {
  Owner:  ['projects.read', 'projects.write', 'members.manage', 'billing.manage'],
  Admin:  ['projects.read', 'projects.write', 'members.manage'],
  Member: ['projects.read', 'projects.write'],
  Viewer: ['projects.read'],
};

export function useMembers() {
  const { currentOrgId } = useOrgStore();

  return useQuery<MemberResponse[]>({
    queryKey: ['members', currentOrgId],
    queryFn: () =>
      api
        .get<MemberResponse[]>(`/orgs/${currentOrgId}/members`)
        .then((r) => r.data),
    enabled: !!currentOrgId,
    staleTime: 60_000,
  });
}

export function useCurrentMemberRole(): MemberRole | null {
  const { user } = useAuthStore();
  const { data: members } = useMembers();

  if (!user || !members) return null;
  return members.find((m) => m.userId === user.id)?.role ?? null;
}

export function usePermission(permission: string): boolean {
  const role = useCurrentMemberRole();
  if (!role) return false;
  return ROLE_PERMISSIONS[role]?.includes(permission) ?? false;
}
