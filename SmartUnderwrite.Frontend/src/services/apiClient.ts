import axios, {
  AxiosInstance,
  AxiosResponse,
  AxiosError,
  AxiosRequestConfig,
} from "axios";
import { ApiError, ApiClient } from "@/types/api";

// Extend AxiosRequestConfig to include _retry flag
interface ExtendedAxiosRequestConfig extends AxiosRequestConfig {
  _retry?: boolean;
}

class ApiClientImpl implements ApiClient {
  private client: AxiosInstance;
  private isRefreshing = false;
  private failedQueue: Array<{
    resolve: (value?: any) => void;
    reject: (error?: any) => void;
  }> = [];

  constructor() {
    this.client = axios.create({
      baseURL: "/api",
      headers: {
        "Content-Type": "application/json",
      },
    });

    this.setupInterceptors();
  }

  private processQueue(error: any, token: string | null = null) {
    this.failedQueue.forEach(({ resolve, reject }) => {
      if (error) {
        reject(error);
      } else {
        resolve(token);
      }
    });

    this.failedQueue = [];
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
        const originalRequest = error.config as ExtendedAxiosRequestConfig;

        if (
          error.response?.status === 401 &&
          originalRequest &&
          !originalRequest._retry
        ) {
          // Prevent infinite loops by marking the request as retried
          originalRequest._retry = true;

          // If we're already refreshing, queue this request
          if (this.isRefreshing) {
            return new Promise((resolve, reject) => {
              this.failedQueue.push({ resolve, reject });
            })
              .then((token) => {
                if (originalRequest.headers) {
                  originalRequest.headers.Authorization = `Bearer ${token}`;
                }
                return this.client.request(originalRequest);
              })
              .catch((err) => {
                return Promise.reject(err);
              });
          }

          const refreshToken = localStorage.getItem("refreshToken");
          const currentToken = localStorage.getItem("token");

          if (refreshToken && currentToken) {
            this.isRefreshing = true;

            try {
              // Create a new axios instance to avoid interceptor loops
              const refreshResponse = await axios.post(
                "/api/auth/refresh",
                {
                  refreshToken,
                },
                {
                  headers: {
                    "Content-Type": "application/json",
                    Authorization: `Bearer ${currentToken}`,
                  },
                }
              );

              const { accessToken, refreshToken: newRefreshToken } =
                refreshResponse.data;

              localStorage.setItem("token", accessToken);
              localStorage.setItem("refreshToken", newRefreshToken);

              // Process the failed queue
              this.processQueue(null, accessToken);

              // Retry the original request
              if (originalRequest.headers) {
                originalRequest.headers.Authorization = `Bearer ${accessToken}`;
              }
              return this.client.request(originalRequest);
            } catch (refreshError) {
              // Process the failed queue with error
              this.processQueue(refreshError, null);

              // Clear tokens and redirect to login
              localStorage.removeItem("token");
              localStorage.removeItem("refreshToken");
              window.location.href = "/login";

              return Promise.reject(refreshError);
            } finally {
              this.isRefreshing = false;
            }
          } else {
            // No refresh token, redirect to login
            localStorage.removeItem("token");
            localStorage.removeItem("refreshToken");
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

// Force bundle refresh - v2.0
