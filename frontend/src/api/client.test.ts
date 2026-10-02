import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError, api, onSessionExpired, tokenStore } from "./client";
import type { AuthResult } from "./types";

const json = (status: number, body: unknown) =>
  new Response(JSON.stringify(body), { status, headers: { "Content-Type": "application/json" } });

const failure = (promise: Promise<unknown>) => promise.then(() => { throw new Error("expected failure"); }, (e: ApiError) => e);

const authResult = (access: string, refresh: string): AuthResult => ({
  accessToken: access,
  accessTokenExpiresAt: "2030-01-01T00:00:00Z",
  refreshToken: refresh,
  refreshTokenExpiresAt: "2030-01-08T00:00:00Z",
  user: { id: "u1", email: "o@x.in", fullName: "O", role: "Officer", organizationId: null, isActive: true },
});

describe("api client", () => {
  const fetchMock = vi.fn();

  beforeEach(() => {
    localStorage.clear();
    tokenStore.clear();
    fetchMock.mockReset();
    vi.stubGlobal("fetch", fetchMock);
  });
  afterEach(() => vi.unstubAllGlobals());

  it("sends the bearer token", async () => {
    tokenStore.set(authResult("A1", "R1"));
    fetchMock.mockResolvedValueOnce(json(200, { ok: true }));

    await api("/api/auth/me");

    const [, init] = fetchMock.mock.calls[0];
    expect(new Headers(init.headers).get("Authorization")).toBe("Bearer A1");
  });

  it("refreshes once on 401 and retries the request", async () => {
    tokenStore.set(authResult("OLD", "R1"));
    fetchMock
      .mockResolvedValueOnce(json(401, {}))
      .mockResolvedValueOnce(json(200, authResult("NEW", "R2")))
      .mockResolvedValueOnce(json(200, { value: 42 }));

    const result = await api<{ value: number }>("/api/tenders");

    expect(result.value).toBe(42);
    expect(fetchMock.mock.calls[1][0]).toContain("/api/auth/refresh");
    expect(new Headers(fetchMock.mock.calls[2][1].headers).get("Authorization")).toBe("Bearer NEW");
    expect(localStorage.getItem("bidguard.refresh")).toBe("R2");
  });

  it("shares a single refresh between concurrent 401s", async () => {
    tokenStore.set(authResult("OLD", "R1"));
    let refreshCalls = 0;
    fetchMock.mockImplementation(async (url: string, init: RequestInit) => {
      if (url.includes("/api/auth/refresh")) {
        refreshCalls++;
        await new Promise((r) => setTimeout(r, 10));
        return json(200, authResult("NEW", "R2"));
      }
      const auth = new Headers(init.headers).get("Authorization");
      return auth === "Bearer NEW" ? json(200, { ok: true }) : json(401, {});
    });

    await Promise.all([api("/api/a"), api("/api/b"), api("/api/c")]);

    expect(refreshCalls).toBe(1);
  });

  it("expires the session when the refresh fails", async () => {
    tokenStore.set(authResult("OLD", "R1"));
    const expired = vi.fn();
    onSessionExpired(expired);
    fetchMock.mockResolvedValueOnce(json(401, {})).mockResolvedValueOnce(json(401, { title: "Unauthorized" }));

    await expect(api("/api/tenders")).rejects.toBeInstanceOf(ApiError);

    expect(expired).toHaveBeenCalledOnce();
    expect(localStorage.getItem("bidguard.refresh")).toBeNull();
  });

  it("turns ProblemDetails into an ApiError with field errors", async () => {
    fetchMock.mockResolvedValueOnce(
      json(400, { title: "Validation failed", status: 400, errors: { Email: ["'Email' is not a valid email address."] } }),
    );

    const error = await failure(api("/api/auth/login", { method: "POST", body: {} }));

    expect(error).toBeInstanceOf(ApiError);
    expect(error.status).toBe(400);
    expect(error.fieldErrors.Email[0]).toContain("valid email");
    expect(error.message).toContain("valid email");
  });

  it("does not try to refresh on a failed login", async () => {
    tokenStore.set(authResult("A", "R"));
    fetchMock.mockResolvedValueOnce(json(401, { title: "Unauthorized", detail: "Invalid credentials." }));

    const error = await failure(api("/api/auth/login", { method: "POST", body: {} }));

    expect(error.message).toBe("Invalid credentials.");
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });
});
