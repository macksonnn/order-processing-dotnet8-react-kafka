import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { login as loginRequest, logout as logoutRequest } from "../api/auth";
import { setOnUnauthorized } from "../api/client";
import { clearSession, readSession, writeSession } from "./session";

type AuthState = {
  username: string | null;
  ready: boolean;
  login: (username: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
};

const AuthContext = createContext<AuthState | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [username, setUsername] = useState<string | null>(null);
  const [ready, setReady] = useState(false);

  useEffect(() => {
    const session = readSession();
    setUsername(session?.username ?? null);
    setReady(true);

    setOnUnauthorized(() => {
      clearSession();
      setUsername(null);
    });

    return () => setOnUnauthorized(null);
  }, []);

  const value = useMemo<AuthState>(
    () => ({
      username,
      ready,
      login: async (user, password) => {
        const result = await loginRequest(user, password);
        writeSession({
          accessToken: result.accessToken,
          refreshToken: result.refreshToken,
          username: result.username
        });
        setUsername(result.username);
      },
      logout: async () => {
        const session = readSession();
        if (session) {
          try {
            await logoutRequest(session.refreshToken);
          } catch {
          }
        }

        clearSession();
        setUsername(null);
      }
    }),
    [username, ready]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const auth = useContext(AuthContext);
  if (!auth) {
    throw new Error("useAuth precisa do AuthProvider");
  }

  return auth;
}
