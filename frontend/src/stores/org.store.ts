'use client';

import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import api, { setApiOrgId } from '@/lib/api';
import { getSubdomain, getTenantUrl, getRootUrl } from '@/lib/subdomain';
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
        const subdomain = getSubdomain();

        if (subdomain) {
          // Tenant subdomain — lock to the org that owns this slug
          const subdomainOrg = data.find((o) => o.slug === subdomain) ?? null;
          if (!subdomainOrg) {
            // Authenticated but not a member of this org — send to root login
            window.location.href = getRootUrl('/login');
            return;
          }
          setApiOrgId(subdomainOrg.id);
          set({ orgs: data, currentOrgId: subdomainOrg.id, currentOrg: subdomainOrg });
        } else {
          // Root domain — keep persisted selection if still valid, otherwise pick first
          const { currentOrgId } = get();
          const current =
            (currentOrgId ? data.find((o) => o.id === currentOrgId) : null) ?? data[0] ?? null;
          setApiOrgId(current?.id ?? null);
          set({ orgs: data, currentOrgId: current?.id ?? null, currentOrg: current });
        }
      },

      setCurrentOrg: (orgId) => {
        const org = get().orgs.find((o) => o.id === orgId) ?? null;
        if (!org) return;

        const subdomain = getSubdomain();

        // Navigate to the target org's subdomain whenever we're not already there
        if (!subdomain || subdomain !== org.slug) {
          window.location.href = getTenantUrl(org.slug, '/dashboard');
          return;
        }

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
      onRehydrateStorage: () => (state) => {
        if (state?.currentOrgId) {
          setApiOrgId(state.currentOrgId);
        }
      },
    }
  )
);
