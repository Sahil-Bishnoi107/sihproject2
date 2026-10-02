import { CheckCircle2, CircleAlert, CircleHelp, ShieldCheck, XCircle } from "lucide-react";
import type { BidDecision, BidStatus, RuleOutcome, RuleStatus, TenderStatus, VerificationStatus } from "../api/types";
import { Badge } from "./ui";

const TENDER: Record<TenderStatus, { label: string; tone: Parameters<typeof Badge>[0]["tone"] }> = {
  Draft: { label: "Draft", tone: "neutral" },
  Extracting: { label: "AI extracting", tone: "accent" },
  RulesExtracted: { label: "Rules in review", tone: "info" },
  Published: { label: "Published", tone: "pass" },
  Closed: { label: "Closed", tone: "ink" },
};

export const TenderStatusBadge = ({ status }: { status: TenderStatus }) => (
  <Badge tone={TENDER[status].tone}>{TENDER[status].label}</Badge>
);

const BID: Record<BidStatus, { label: string; tone: Parameters<typeof Badge>[0]["tone"] }> = {
  Uploaded: { label: "Draft", tone: "neutral" },
  Extracting: { label: "Reading documents", tone: "accent" },
  Verifying: { label: "Verifying registries", tone: "accent" },
  Evaluating: { label: "Evaluating rules", tone: "accent" },
  UnderReview: { label: "Under review", tone: "info" },
  Decided: { label: "Decided", tone: "ink" },
  Failed: { label: "Processing failed", tone: "fail" },
};

export const BidStatusBadge = ({ status }: { status: BidStatus }) => <Badge tone={BID[status].tone}>{BID[status].label}</Badge>;

export const isProcessing = (status: BidStatus) => status === "Extracting" || status === "Verifying" || status === "Evaluating";

const DECISION: Record<BidDecision, { label: string; tone: Parameters<typeof Badge>[0]["tone"] }> = {
  Compliant: { label: "Compliant", tone: "pass" },
  NonCompliant: { label: "Non-compliant", tone: "fail" },
  NeedsClarification: { label: "Needs clarification", tone: "warn" },
};

export const DecisionBadge = ({ decision }: { decision: BidDecision }) => (
  <Badge tone={DECISION[decision].tone}>{DECISION[decision].label}</Badge>
);

export const decisionLabel = (d: BidDecision) => DECISION[d].label;

const OUTCOME: Record<RuleOutcome, { tone: Parameters<typeof Badge>[0]["tone"]; icon: typeof CheckCircle2 }> = {
  Pass: { tone: "pass", icon: CheckCircle2 },
  Fail: { tone: "fail", icon: XCircle },
  Missing: { tone: "warn", icon: CircleHelp },
  Exempted: { tone: "info", icon: ShieldCheck },
};

export function OutcomeChip({ outcome }: { outcome: RuleOutcome }) {
  const { tone, icon: Icon } = OUTCOME[outcome];
  return (
    <Badge tone={tone} icon={<Icon className="size-3.5" aria-hidden />}>
      {outcome}
    </Badge>
  );
}

export const RuleStatusBadge = ({ status }: { status: RuleStatus }) =>
  status === "AiDraft" ? <Badge tone="accent">AI draft</Badge> : <Badge tone="pass">Approved</Badge>;

export function VerificationBadge({ status }: { status: VerificationStatus }) {
  const tone = status === "Verified" ? "pass" : status === "Mismatch" ? "fail" : "warn";
  const Icon = status === "Verified" ? CheckCircle2 : CircleAlert;
  return (
    <Badge tone={tone} icon={<Icon className="size-3.5" aria-hidden />}>
      {status === "NotFound" ? "Not found" : status}
    </Badge>
  );
}
