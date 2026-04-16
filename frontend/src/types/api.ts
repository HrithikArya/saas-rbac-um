export interface User {
  id: string;
  email: string;
  emailVerified: boolean;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: User;
}

export interface OrgResponse {
  id: string;
  name: string;
  slug: string;
  createdAt: string;
  memberCount: number;
}

export type MemberRole = 'Owner' | 'Admin' | 'Member' | 'Viewer';

export interface MemberResponse {
  id: string;
  userId: string;
  email: string;
  role: MemberRole;
  joinedAt: string;
}

export interface ApiError {
  error: string;
}
