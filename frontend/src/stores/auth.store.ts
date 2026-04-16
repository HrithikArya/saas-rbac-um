'use client';

import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import api, { setAccessToken } from '@/lib/api';
import type { User } from '@/types/api';

interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  isInitializing: boolean;

  initialize: () => Promise<void>;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      isAuthenticated: false,
      isInitializing: true,

      initialize: async () => {
        const refreshToken =
          typeof window !== 'undefined' ? localStorage.getItem('refreshToken') : null;

        if (!refreshToken) {
          set({ isInitializing: false });
          return;
        }

        try {
          const { data } = await api.post('/auth/refresh', { refreshToken });
          setAccessToken(data.accessToken);
          localStorage.setItem('refreshToken', data.refreshToken);
          set({ user: data.user, isAuthenticated: true });
        } catch {
          localStorage.removeItem('refreshToken');
          set({ user: null, isAuthenticated: false });
        } finally {
          set({ isInitializing: false });
        }
      },

      login: async (email, password) => {
        const { data } = await api.post('/auth/login', { email, password });
        setAccessToken(data.accessToken);
        localStorage.setItem('refreshToken', data.refreshToken);
        set({ user: data.user, isAuthenticated: true });
      },

      register: async (email, password) => {
        const { data } = await api.post('/auth/register', { email, password });
        setAccessToken(data.accessToken);
        localStorage.setItem('refreshToken', data.refreshToken);
        set({ user: data.user, isAuthenticated: true });
      },

      logout: async () => {
        const refreshToken =
          typeof window !== 'undefined' ? localStorage.getItem('refreshToken') : null;
        try {
          if (refreshToken) await api.post('/auth/logout', { refreshToken });
        } catch {
          // ignore — clean up locally regardless
        }
        setAccessToken(null);
        if (typeof window !== 'undefined') localStorage.removeItem('refreshToken');
        set({ user: null, isAuthenticated: false });
      },
    }),
    {
      name: 'auth-storage',
      // Only persist the user object — tokens are managed separately
      partialize: (state) => ({ user: state.user, isAuthenticated: state.isAuthenticated }),
    }
  )
);
