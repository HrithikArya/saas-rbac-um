import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook } from '@testing-library/react';
import { usePermission, useCurrentMemberRole } from '../usePermission';

// Mock the stores and query so we can control state
vi.mock('@/stores/auth.store', () => ({
  useAuthStore: vi.fn(),
}));

vi.mock('@/stores/org.store', () => ({
  useOrgStore: vi.fn(),
}));

vi.mock('@tanstack/react-query', () => ({
  useQuery: vi.fn(),
}));

import { useAuthStore } from '@/stores/auth.store';
import { useOrgStore } from '@/stores/org.store';
import { useQuery } from '@tanstack/react-query';

const mockUseAuthStore = vi.mocked(useAuthStore);
const mockUseOrgStore = vi.mocked(useOrgStore);
const mockUseQuery = vi.mocked(useQuery);

function setupMocks(role: string | null) {
  mockUseAuthStore.mockReturnValue({ user: { id: 'user-1', email: 'test@test.com', emailVerified: true } } as any);
  mockUseOrgStore.mockReturnValue({ currentOrgId: 'org-1' } as any);

  const members = role
    ? [{ id: 'm1', userId: 'user-1', email: 'test@test.com', role, joinedAt: '' }]
    : [];

  mockUseQuery.mockReturnValue({ data: members, isLoading: false, isError: false } as any);
}

describe('useCurrentMemberRole', () => {
  beforeEach(() => vi.clearAllMocks());

  it('returns Owner when user is owner', () => {
    setupMocks('Owner');
    const { result } = renderHook(() => useCurrentMemberRole());
    expect(result.current).toBe('Owner');
  });

  it('returns Member role correctly', () => {
    setupMocks('Member');
    const { result } = renderHook(() => useCurrentMemberRole());
    expect(result.current).toBe('Member');
  });

  it('returns null when user has no membership', () => {
    setupMocks(null);
    const { result } = renderHook(() => useCurrentMemberRole());
    expect(result.current).toBeNull();
  });
});

describe('usePermission', () => {
  beforeEach(() => vi.clearAllMocks());

  it('Owner has billing.manage', () => {
    setupMocks('Owner');
    const { result } = renderHook(() => usePermission('billing.manage'));
    expect(result.current).toBe(true);
  });

  it('Admin does not have billing.manage', () => {
    setupMocks('Admin');
    const { result } = renderHook(() => usePermission('billing.manage'));
    expect(result.current).toBe(false);
  });

  it('Admin has members.manage', () => {
    setupMocks('Admin');
    const { result } = renderHook(() => usePermission('members.manage'));
    expect(result.current).toBe(true);
  });

  it('Viewer only has projects.read', () => {
    setupMocks('Viewer');
    const { result } = renderHook(() => usePermission('projects.read'));
    expect(result.current).toBe(true);
  });

  it('Viewer does not have projects.write', () => {
    setupMocks('Viewer');
    const { result } = renderHook(() => usePermission('projects.write'));
    expect(result.current).toBe(false);
  });

  it('no role means no permissions', () => {
    setupMocks(null);
    const { result } = renderHook(() => usePermission('projects.read'));
    expect(result.current).toBe(false);
  });
});
