export interface User {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
  affiliateId?: number;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  refreshToken: string;
  user: User;
  expiresAt: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  affiliateId?: number;
}

export interface AuthState {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
}

export const UserRole = {
  Admin: "Admin",
  Underwriter: "Underwriter",
  Affiliate: "Affiliate",
} as const;

export type UserRoleType = (typeof UserRole)[keyof typeof UserRole];
