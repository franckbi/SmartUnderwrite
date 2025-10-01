import { apiClient } from "./apiClient";
import {
  LoginRequest,
  LoginResponse,
  RefreshTokenRequest,
  RegisterRequest,
  User,
} from "@/types/auth";

export class AuthService {
  async login(credentials: LoginRequest): Promise<LoginResponse> {
    const response = await apiClient.post<LoginResponse>(
      "/auth/login",
      credentials
    );

    // Store tokens in localStorage
    localStorage.setItem("token", response.accessToken);
    localStorage.setItem("refreshToken", response.refreshToken);
    localStorage.setItem("user", JSON.stringify(response.user));

    return response;
  }

  async register(userData: RegisterRequest): Promise<LoginResponse> {
    const response = await apiClient.post<LoginResponse>(
      "/auth/register",
      userData
    );

    // Store tokens in localStorage
    localStorage.setItem("token", response.accessToken);
    localStorage.setItem("refreshToken", response.refreshToken);
    localStorage.setItem("user", JSON.stringify(response.user));

    return response;
  }

  async refreshToken(refreshToken: string): Promise<LoginResponse> {
    const request: RefreshTokenRequest = { refreshToken };
    const response = await apiClient.post<LoginResponse>(
      "/auth/refresh",
      request
    );

    // Update stored tokens
    localStorage.setItem("token", response.accessToken);
    localStorage.setItem("refreshToken", response.refreshToken);
    localStorage.setItem("user", JSON.stringify(response.user));

    return response;
  }

  async logout(): Promise<void> {
    try {
      await apiClient.post("/auth/logout");
    } catch (error) {
      // Even if logout fails on server, clear local storage
      console.error("Logout error:", error);
    } finally {
      this.clearTokens();
    }
  }

  getCurrentUser(): User | null {
    const userStr = localStorage.getItem("user");
    if (userStr) {
      try {
        return JSON.parse(userStr);
      } catch (error) {
        console.error("Error parsing user from localStorage:", error);
        this.clearTokens();
      }
    }
    return null;
  }

  getToken(): string | null {
    return localStorage.getItem("token");
  }

  isAuthenticated(): boolean {
    const token = this.getToken();
    const user = this.getCurrentUser();
    return !!(token && user);
  }

  hasRole(role: string): boolean {
    const user = this.getCurrentUser();
    return user?.roles?.includes(role) || false;
  }

  hasAnyRole(roles: string[]): boolean {
    const user = this.getCurrentUser();
    return roles.some((role) => user?.roles?.includes(role)) || false;
  }

  clearTokens(): void {
    localStorage.removeItem("token");
    localStorage.removeItem("refreshToken");
    localStorage.removeItem("user");
  }

  isTokenExpired(): boolean {
    const token = this.getToken();
    if (!token) return true;

    try {
      const payload = JSON.parse(atob(token.split(".")[1]));
      const currentTime = Date.now() / 1000;
      return payload.exp < currentTime;
    } catch (error) {
      return true;
    }
  }
}

export const authService = new AuthService();
