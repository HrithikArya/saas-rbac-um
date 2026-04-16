'use client';

import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import api, { setApiOrgId } from '@/lib/api';
import type { OrgResponse } from '@/types/api';

interface OrgState {
  orgs: OrgResponse[];
  currentOrgId: string | null;
  currentOrg: OrgResponse | null;

  fetchOrgs: () => Promise<void>;
  setCurrentOrg: (orgId: string) => void;
  addOrg: (org: OrgResponse) => void;
}

export const useOrgStore = create<OrgState>()(
  persist(
    (set, get) => ({
      orgs: [],
      currentOrgId: null,
      currentOrg: null,

      fetchOrgs: async () => {
        const { data } = await api.get<OrgResponse[]>('/orgs');
        const { currentOrgId } = get();

        // Keep selected org if still valid, otherwise pick first
        const current =
          (currentOrgId ? data.find((o) => o.id === currentOrgId) : null) ?? data[0] ?? null;

        setApiOrgId(current?.id ?? null);
        set({ orgs: data, currentOrgId: current?.id ?? null, currentOrg: current });
      },

      setCurrentOrg: (orgId) => {
        const org = get().orgs.find((o) => o.id === orgId) ?? null;
        setApiOrgId(orgId);
        set({ currentOrgId: orgId, currentOrg: org });
      },

      addOrg: (org) => {
        set((s) => ({ orgs: [...s.orgs, org] }));
      },
    }),
    {
      name: 'org-storage',
      partialize: (state) => ({ currentOrgId: state.currentOrgId }),
    }
  )
);
