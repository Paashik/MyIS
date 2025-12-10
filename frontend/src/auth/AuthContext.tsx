import React, {
  useCallback,
  useState,
} from "react";
import { useNavigate } from "react-router-dom";

export interface User {
  id: string;
  login: string;
  fullName: string;
  roles: string[];
}

export interface AuthContextValue {
  user: User | null;
  setUser: (user: User | null) => void;
  logout: () => Promise<void>;
}

const AuthContext = React.createContext<AuthContextValue | undefined>(undefined);

export const AuthProvider: React.FC<React.PropsWithChildren> = ({ children }) => {
  const [user, setUserState] = useState<User | null>(null);
  const navigate = useNavigate();

  const setUser = useCallback((nextUser: User | null) => {
    setUserState(nextUser);
  }, []);

  const logout = useCallback(async () => {
    try {
      await fetch("/api/auth/logout", {
        method: "POST",
        credentials: "include",
      });
    } catch {
      // игнорируем сетевые ошибки при logout
    } finally {
      setUserState(null);
      navigate("/login", { replace: true });
    }
  }, [navigate]);

  const value: AuthContextValue = {
    user,
    setUser,
    logout,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = (): AuthContextValue => {
  const ctx = React.useContext(AuthContext);
  if (!ctx) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return ctx as AuthContextValue;
};