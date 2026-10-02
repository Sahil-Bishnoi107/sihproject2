import { api } from "./client";
import type {
  AuditChainStatus, AuditLogDto, AuthResult, BidDecision, BidDto, BidReportDto, ComplianceStats, CredentialDto,
  CredentialType, DocumentDto, NotificationDto, OrganizationCategory, OrganizationDto, PagedResult, RuleDto, RuleInput,
  RuleOutcome, TenderDto, TenderStatus, UserDto, UserRole,
} from "./types";

const qs = (params: Record<string, string | number | boolean | null | undefined>) => {
  const search = new URLSearchParams();
  for (const [k, v] of Object.entries(params)) if (v !== null && v !== undefined && v !== "") search.set(k, String(v));
  const s = search.toString();
  return s ? `?${s}` : "";
};

const upload = (file: File, extra: Record<string, string> = {}) => {
  const form = new FormData();
  form.append("file", file);
  for (const [k, v] of Object.entries(extra)) form.append(k, v);
  return form;
};

export const authApi = {
  login: (email: string, password: string) => api<AuthResult>("/api/auth/login", { method: "POST", body: { email, password } }),
  register: (body: { email: string; password: string; fullName: string; organizationName: string; pan: string; category: OrganizationCategory }) =>
    api<AuthResult>("/api/auth/register", { method: "POST", body }),
  logout: (refreshToken: string) => api<void>("/api/auth/logout", { method: "POST", body: { refreshToken } }),
  me: () => api<UserDto>("/api/auth/me"),
};

export const usersApi = {
  create: (body: { email: string; password: string; fullName: string; role: UserRole }) =>
    api<UserDto>("/api/users", { method: "POST", body }),
};

export const tendersApi = {
  list: (p: { status?: TenderStatus | ""; search?: string; page?: number; pageSize?: number }) =>
    api<PagedResult<TenderDto>>(`/api/tenders${qs(p)}`),
  get: (id: string) => api<TenderDto>(`/api/tenders/${id}`),
  create: (body: { gemBidNumber: string; title: string; category: string; estimatedValue: number; emdAmount: number; bidEndDate: string }) =>
    api<TenderDto>("/api/tenders", { method: "POST", body }),
  uploadDocument: (id: string, file: File) => api<TenderDto>(`/api/tenders/${id}/document`, { method: "POST", form: upload(file) }),
  publish: (id: string) => api<TenderDto>(`/api/tenders/${id}/publish`, { method: "POST" }),
  rules: (id: string) => api<RuleDto[]>(`/api/tenders/${id}/rules`),
  createRule: (id: string, rule: RuleInput) => api<RuleDto>(`/api/tenders/${id}/rules`, { method: "POST", body: { tenderId: id, ...rule } }),
  updateRule: (id: string, ruleId: string, rule: RuleInput) =>
    api<RuleDto>(`/api/tenders/${id}/rules/${ruleId}`, { method: "PUT", body: { tenderId: id, ruleId, ...rule } }),
  deleteRule: (id: string, ruleId: string) => api<void>(`/api/tenders/${id}/rules/${ruleId}`, { method: "DELETE" }),
  approveRule: (id: string, ruleId: string) => api<RuleDto[]>(`/api/tenders/${id}/rules/${ruleId}/approve`, { method: "POST" }),
  approveAll: (id: string) => api<RuleDto[]>(`/api/tenders/${id}/rules/approve-all`, { method: "POST" }),
  bids: (id: string) => api<BidDto[]>(`/api/tenders/${id}/bids`),
};

export const bidsApi = {
  create: (tenderId: string) => api<BidDto>(`/api/tenders/${tenderId}/bids`, { method: "POST" }),
  simulate: (tenderId: string) => api<BidDto>(`/api/tenders/${tenderId}/simulate`, { method: "POST" }),
  uploadDocument: (bidId: string, file: File, docType: string) =>
    api<DocumentDto>(`/api/bids/${bidId}/documents`, { method: "POST", form: upload(file, docType ? { docType } : {}) }),
  submit: (bidId: string) => api<BidDto>(`/api/bids/${bidId}/submit`, { method: "POST" }),
  mine: () => api<BidDto[]>("/api/bids/mine"),
  get: (bidId: string) => api<BidDto>(`/api/bids/${bidId}`),
  report: (bidId: string) => api<BidReportDto>(`/api/bids/${bidId}/report`),
  override: (bidId: string, evaluationId: string, outcome: RuleOutcome, reason: string) =>
    api<BidDto>(`/api/bids/${bidId}/evaluations/${evaluationId}/override`, { method: "PUT", body: { outcome, reason } }),
  decide: (bidId: string, decision: BidDecision, remarks: string) =>
    api<BidDto>(`/api/bids/${bidId}/decision`, { method: "POST", body: { decision, remarks } }),
};

export const organizationApi = {
  get: () => api<OrganizationDto>("/api/organizations/me"),
  update: (body: Omit<OrganizationDto, "id" | "pan" | "credentials">) =>
    api<OrganizationDto>("/api/organizations/me", { method: "PUT", body }),
  addCredential: (body: { type: CredentialType; number: string; issuedOn: string | null; expiresOn: string | null }) =>
    api<CredentialDto>("/api/organizations/me/credentials", { method: "POST", body }),
};

export const notificationsApi = {
  list: (unreadOnly = false) => api<NotificationDto[]>(`/api/notifications${qs({ unreadOnly })}`),
  markRead: (id: string) => api<void>(`/api/notifications/${id}/read`, { method: "POST" }),
};

export const dashboardApi = { stats: () => api<ComplianceStats>("/api/dashboard/stats") };

export const auditApi = {
  list: (p: { entityType?: string; entityId?: string; page?: number; pageSize?: number }) =>
    api<PagedResult<AuditLogDto>>(`/api/audit${qs(p)}`),
  verify: () => api<AuditChainStatus>("/api/audit/verify"),
};

export const documentsApi = {
  /** Authenticated download as bytes (the browser cannot attach the bearer token to a plain link). */
  async blob(id: string): Promise<Blob> {
    const response = await api<Response>(`/api/documents/${id}/download`, { raw: true });
    return response.blob();
  },
  async open(id: string, page?: number | null) {
    const url = URL.createObjectURL(await documentsApi.blob(id));
    window.open(page ? `${url}#page=${page}` : url, "_blank", "noopener");
    setTimeout(() => URL.revokeObjectURL(url), 60_000);
  },
};
