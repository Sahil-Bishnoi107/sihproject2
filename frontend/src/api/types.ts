// Mirrors the BidGuard API DTOs (JSON camelCase, enums as strings).

export type UserRole = "Admin" | "Officer" | "Bidder";
export type OrganizationCategory = "MSE" | "Medium" | "Large";
export type TenderStatus = "Draft" | "Extracting" | "RulesExtracted" | "Published" | "Closed";
export type RuleCategory = "Eligibility" | "Technical" | "Financial" | "Document";
export type RuleStatus = "AiDraft" | "Approved";
export type ExemptionKind = "Emd" | "PriorExperienceTurnover";
export type RuleOutcome = "Pass" | "Fail" | "Exempted" | "Missing";
export type BidStatus = "Uploaded" | "Extracting" | "Verifying" | "Evaluating" | "UnderReview" | "Decided" | "Failed";
export type BidDecision = "Compliant" | "NonCompliant" | "NeedsClarification";
export type ExtractionStatus = "Pending" | "Completed" | "Failed";
export type VerificationSource = "GST" | "MCA" | "Udyam" | "EPFO";
export type VerificationStatus = "Verified" | "Mismatch" | "NotFound" | "Error";
export type CredentialType =
  | "GST" | "Udyam" | "PAN" | "EPFO" | "ISO9001" | "OemAuthorization" | "ExperienceCertificate" | "DPIIT" | "Other";
export type NotificationType = "CredentialExpiring" | "BidEvaluated" | "RulesReady" | "BidDecided" | "ProcessingFailed";

export interface UserDto {
  id: string;
  email: string;
  fullName: string;
  role: UserRole;
  organizationId: string | null;
  isActive: boolean;
}

export interface AuthResult {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  user: UserDto;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export interface TenderDto {
  id: string;
  gemBidNumber: string;
  title: string;
  category: string;
  estimatedValue: number;
  emdAmount: number;
  bidEndDate: string;
  status: TenderStatus;
  tenderDocumentId: string | null;
  failureReason: string | null;
  publishedAt: string | null;
  createdAt: string;
}

/** Compliance-as-Code expression tree. */
export type RuleExpression =
  | { all: RuleExpression[] }
  | { any: RuleExpression[] }
  | { not: RuleExpression }
  | { field: string; op: string; value?: unknown; source?: string };

export interface RuleDto {
  id: string;
  tenderId: string;
  code: string;
  title: string;
  category: RuleCategory;
  isMandatory: boolean;
  weight: number;
  expression: RuleExpression;
  sourceClause: string | null;
  sourcePage: number | null;
  exemptionKind: ExemptionKind | null;
  status: RuleStatus;
}

export interface RuleInput {
  code: string;
  title: string;
  category: RuleCategory;
  isMandatory: boolean;
  weight: number;
  expression: RuleExpression;
  sourceClause: string | null;
  sourcePage: number | null;
  exemptionKind: ExemptionKind | null;
}

export interface BidDto {
  id: string;
  tenderId: string;
  tenderTitle: string | null;
  gemBidNumber: string | null;
  organizationId: string;
  organizationName: string | null;
  isSimulation: boolean;
  status: BidStatus;
  submittedAt: string | null;
  overallScore: number | null;
  suggestedDecision: BidDecision | null;
  decision: BidDecision | null;
  decisionRemarks: string | null;
  failureReason: string | null;
  createdAt: string;
}

export interface DocumentDto {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  sha256: string;
  docType: string | null;
  extractionStatus: ExtractionStatus;
  ocrUsed: boolean;
  createdAt: string;
}

export interface DocumentEvidence {
  kind: "document";
  key: string;
  value: string;
  confidence: number;
  documentId: string;
  fileName: string | null;
  page: number | null;
  boundingBox: number[] | null;
}

export interface RegistryEvidence {
  kind: "registry";
  source: VerificationSource;
  identifier: string;
  status: VerificationStatus;
  registeredName: string | null;
  checkedAt: string;
}

export type Evidence = DocumentEvidence | RegistryEvidence;

export interface EvaluationDto {
  id: string;
  ruleId: string;
  ruleCode: string;
  ruleTitle: string;
  category: RuleCategory;
  isMandatory: boolean;
  weight: number;
  outcome: RuleOutcome;
  effectiveOutcome: RuleOutcome;
  explanation: string;
  evidence: Evidence[];
  officerOverride: RuleOutcome | null;
  overrideReason: string | null;
  sourceClause: string | null;
  sourcePage: number | null;
}

export interface VerificationDto {
  source: VerificationSource;
  identifier: string;
  status: VerificationStatus;
  registeredName: string | null;
  checkedAt: string;
}

export interface FixDto {
  ruleCode: string;
  ruleTitle: string;
  isMandatory: boolean;
  currentOutcome: RuleOutcome;
  scoreGain: number;
  action: string;
}

export interface BidReportDto {
  bid: BidDto;
  tender: TenderDto;
  documents: DocumentDto[];
  verifications: VerificationDto[];
  evaluations: EvaluationDto[];
  fixes: FixDto[];
}

export interface CredentialDto {
  id: string;
  type: CredentialType;
  number: string;
  issuedOn: string | null;
  expiresOn: string | null;
}

export interface OrganizationDto {
  id: string;
  legalName: string;
  pan: string;
  gstin: string | null;
  udyamNumber: string | null;
  cin: string | null;
  epfoCode: string | null;
  category: OrganizationCategory;
  isStartup: boolean;
  dpiitNumber: string | null;
  credentials: CredentialDto[];
}

export interface NotificationDto {
  id: string;
  type: NotificationType;
  message: string;
  referenceId: string | null;
  isRead: boolean;
  createdAt: string;
}

export interface ComplianceStats {
  totalTenders: number;
  publishedTenders: number;
  totalBids: number;
  bidsByDecision: Record<string, number>;
  topFailingRules: { ruleCode: string; ruleTitle: string; failures: number }[];
}

export interface AuditLogDto {
  sequence: number;
  timestamp: string;
  actorId: string | null;
  action: string;
  entityType: string;
  entityId: string;
  payloadJson: string;
  hash: string;
}

export interface AuditChainStatus {
  isIntact: boolean;
  entriesChecked: number;
  firstBrokenEntryId: string | null;
  firstBrokenSequence: number | null;
}
