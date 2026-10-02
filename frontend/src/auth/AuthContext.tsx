import { useQueryClient } from "@tanstack/react-query";
import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { onSessionExpired, refreshSession, tokenStore } from "../api/client";
import { authApi } from "../api/endpoints";
import type { AuthResult, UserDto } from "../api/types";

interface AuthState {
  user: UserDto | null;
  ready: boolean;
  signIn: (result: AuthResult) => void;
  signOut: () => Promise<void>;
}

const AuthContext = createContext<AuthState | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const [user, setUser] = useState<UserDto | null>(null);
  const [ready, setReady] = useState(false);

  const clearSession = useCallback(() => {
    tokenStore.clear();
    setUser(null);
    queryClient.clear();
  }, [queryClient]);

  // Restore the session after a reload: the access token lived in memory, the refresh token did not.
  useEffect(() => {
    onSessionExpired(clearSession);
    (async () => {
      try {
        if (!tokenStore.hasAccessToken() && tokenStore.refreshToken() && (await refreshSession())) {
          setUser(await authApi.me());
        }
      } catch {
        clearSession();
      } finally {
        setReady(true);
      }
    })();
  }, [clearSession]);

  const value = useMemo<AuthState>(
    () => ({
      user,
      ready,
      signIn(result) {
        tokenStore.set(result);
        setUser(result.user);
      },
      async signOut() {
        const refresh = tokenStore.refreshToken();
        if (refresh) await authApi.logout(refresh).catch(() => undefined);
        clearSession();
      },
    }),
    [user, ready, clearSession],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthState {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used inside AuthProvider");
  return ctx;
}

export const isStaff = (user: UserDto | null) => user?.role === "Officer" || user?.role === "Admin";
