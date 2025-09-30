import React, {
  createContext,
  useContext,
  useReducer,
  useEffect,
  ReactNode,
} from "react";
import { User, AuthState, LoginRequest, RegisterRequest } from "@/types/auth";
import { authService } from "@/services/authService";

interface AuthContextType extends AuthState {
  login: (credentials: LoginRequest) => Promise<void>;
  register: (userData: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
  hasRole: (role: string) => boolean;
  hasAnyRole: (roles: string[]) => boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

type AuthAction =
  | { type: "SET_LOADING"; payload: boolean }
  | { type: "SET_USER"; payload: { user: User; token: string } }
  | { type: "CLEAR_USER" }
  | {
      type: "INITIALIZE";
      payload: { user: User | null; token: string | null };
    };

const authReducer = (state: AuthState, action: AuthAction): AuthState => {
  switch (action.type) {
    case "SET_LOADING":
      return { ...state, isLoading: action.payload };
    case "SET_USER":
      return {
        ...state,
        user: action.payload.user,
        token: action.payload.token,
        isAuthenticated: true,
        isLoading: false,
      };
    case "CLEAR_USER":
      return {
        user: null,
        token: null,
        isAuthenticated: false,
        isLoading: false,
      };
    case "INITIALIZE":
      return {
        user: action.payload.user,
        token: action.payload.token,
        isAuthenticated: !!(action.payload.user && action.payload.token),
        isLoading: false,
      };
    default:
      return state;
  }
};

const initialState: AuthState = {
  user: null,
  token: null,
  isAuthenticated: false,
  isLoading: true,
};

interface AuthProviderProps {
  children: ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const [state, dispatch] = useReducer(authReducer, initialState);

  useEffect(() => {
    // Initialize auth state from localStorage
    const user = authService.getCurrentUser();
    const token = authService.getToken();

    dispatch({
      type: "INITIALIZE",
      payload: { user, token },
    });
  }, []);

  const login = async (credentials: LoginRequest): Promise<void> => {
    dispatch({ type: "SET_LOADING", payload: true });
    try {
      const response = await authService.login(credentials);
      dispatch({
        type: "SET_USER",
        payload: { user: response.user, token: response.token },
      });
    } catch (error) {
      dispatch({ type: "SET_LOADING", payload: false });
      throw error;
    }
  };

  const register = async (userData: RegisterRequest): Promise<void> => {
    dispatch({ type: "SET_LOADING", payload: true });
    try {
      const response = await authService.register(userData);
      dispatch({
        type: "SET_USER",
        payload: { user: response.user, token: response.token },
      });
    } catch (error) {
      dispatch({ type: "SET_LOADING", payload: false });
      throw error;
    }
  };

  const logout = async (): Promise<void> => {
    dispatch({ type: "SET_LOADING", payload: true });
    try {
      await authService.logout();
    } finally {
      dispatch({ type: "CLEAR_USER" });
    }
  };

  const hasRole = (role: string): boolean => {
    return authService.hasRole(role);
  };

  const hasAnyRole = (roles: string[]): boolean => {
    return authService.hasAnyRole(roles);
  };

  const value: AuthContextType = {
    ...state,
    login,
    register,
    logout,
    hasRole,
    hasAnyRole,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
};
