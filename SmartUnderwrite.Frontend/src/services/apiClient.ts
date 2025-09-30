import axios, { AxiosInstance, AxiosResponse, AxiosError } from "axios";
import { ApiError, ApiClient } from "@/types/api";

class ApiClientImpl implements ApiClient {
  private client: AxiosInstance;

  constructor() {
    this.client = axios.create({
      baseURL: "/api",
      headers: {
        "Content-Type": "application/json",
      },
    });

    this.setupInterceptors();
  }

  private setupInterceptors() {
    // Request interceptor to add auth token
    this.client.interceptors.request.use(
      (config) => {
        const token = localStorage.getItem("token");
        if (token) {
          config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
      },
      (error) => Promise.reject(error)
    );

    // Response interceptor to handle errors
    this.client.interceptors.response.use(
      (response: AxiosResponse) => response,
      async (error: AxiosError) => {
        if (error.response?.status === 401) {
          // Try to refresh token
          const refreshToken = localStorage.getItem("refreshToken");
          if (refreshToken) {
            try {
              const response = await this.post<any>("/auth/refresh", {
                refreshToken,
              });
              const { token, refreshToken: newRefreshToken } = response;

              localStorage.setItem("token", token);
              localStorage.setItem("refreshToken", newRefreshToken);

              // Retry the original request
              if (error.config) {
                error.config.headers.Authorization = `Bearer ${token}`;
                return this.client.request(error.config);
              }
            } catch (refreshError) {
              // Refresh failed, redirect to login
              localStorage.removeItem("token");
              localStorage.removeItem("refreshToken");
              window.location.href = "/login";
            }
          } else {
            // No refresh token, redirect to login
            window.location.href = "/login";
          }
        }

        const apiError: ApiError = {
          message:
            (error.response?.data as any)?.message ||
            error.message ||
            "An error occurred",
          errors: (error.response?.data as any)?.errors,
          statusCode: error.response?.status || 500,
        };

        return Promise.reject(apiError);
      }
    );
  }

  async get<T>(url: string): Promise<T> {
    const response = await this.client.get<T>(url);
    return response.data;
  }

  async post<T>(url: string, data?: any): Promise<T> {
    const response = await this.client.post<T>(url, data);
    return response.data;
  }

  async put<T>(url: string, data?: any): Promise<T> {
    const response = await this.client.put<T>(url, data);
    return response.data;
  }

  async delete<T>(url: string): Promise<T> {
    const response = await this.client.delete<T>(url);
    return response.data;
  }
}

export const apiClient = new ApiClientImpl();
