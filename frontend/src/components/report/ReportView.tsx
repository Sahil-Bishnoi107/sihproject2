import { FileText, Gavel, Lightbulb, Landmark, Scale } from "lucide-react";
import { useState, type ReactNode } from "react";
import type { BidReportDto, DocumentEvidence, EvaluationDto, Evidence, RuleOutcome } from "../../api/types";
import { documentsApi } from "../../api/endpoints";
import { formatBytes, formatDateTime } from "../../lib/format";
import { DecisionBadge, OutcomeChip, VerificationBadge } from "../StatusBadges";
import { Badge, Button, Card, EmptyState, cx } from "../ui";
import { EvidenceViewer } from "./EvidenceViewer";

export function ScoreRing({ score, size = 112 }: { score: number | null; size?: number }) {
  const r = 44;
  const circumference = 2 * Math.PI * r;
  const value = score ?? 0;
  const color = value >= 80 ? "var(--color-pass)" : value >= 50 ? "var(--color-warn)" : "var(--color-fail)";
  return (
    <svg width={size} height={size} viewBox="0 0 100 100" role="img" aria-label={`Compliance score ${score ?? "pending"}`}>
      <circle cx="50" cy="50" r={r} fill="none" stroke="var(--color-ink-100)" strokeWidth="9" />
      {score !== null && (
        <circle cx="50" cy="50" r={r} fill="none" stroke={color} strokeWidth="9" strokeLinecap="round"
          strokeDasharray={`${(value / 100) * circumference} ${circumference}`} transform="rotate(-90 50 50)" />
      )}
      <text x="50" y="48" textAnchor="middle" fontSize="22" fontWeight="700" fill="var(--color-ink-900)">{score === null ? "—" : Math.round(value)}</text>
      <text x="50" y="64" textAnchor="middle" fontSize="9" fill="var(--color-ink-700)">/ 100</text>
    </svg>
  );
}

const OUTCOMES: RuleOutcome[] = ["Pass", "Fail", "Missing", "Exempted"];

export function ReportView({ report, mode, onOverride, side }: {
  report: BidReportDto;
  mode: "officer" | "bidder";
  onOverride?: (evaluation: EvaluationDto) => void;
  side?: ReactNode;
}) {
  const [evidence, setEvidence] = useState<DocumentEvidence | null>(null);
  const { bid, evaluations, verifications, documents, fixes } = report;
  const counts = Object.fromEntries(OUTCOMES.map((o) => [o, evaluations.filter((e) => e.effectiveOutcome === o).length]));

  return (
    <div className="grid gap-6 lg:grid-cols-3">
      <div className="space-y-6 lg:col-span-2">
        <Card>
          <div className="flex flex-wrap items-center gap-6">
            <ScoreRing score={bid.overallScore} />
            <div className="min-w-0 flex-1 space-y-2">
              <div className="flex flex-wrap items-center gap-2">
                {bid.isSimulation && <Badge tone="accent">Pre-bid simulation</Badge>}
                {bid.suggestedDecision && (
                  <span className="inline-flex items-center gap-1.5 text-sm text-ink-700">
                    Engine suggests <DecisionBadge decision={bid.suggestedDecision} />
                  </span>
                )}
                {bid.decision && (
                  <span className="inline-flex items-center gap-1.5 text-sm text-ink-700">
                    <Gavel className="size-4" /> Officer decided <DecisionBadge decision={bid.decision} />
                  </span>
                )}
              </div>
              <div className="flex flex-wrap gap-2">
                {OUTCOMES.map((o) => (
                  <span key={o} className="inline-flex items-center gap-1.5 text-sm">
                    <OutcomeChip outcome={o} /> <span className="font-semibold">{counts[o]}</span>
                  </span>
                ))}
              </div>
              {bid.decisionRemarks && <p className="text-sm text-ink-700">Remarks: {bid.decisionRemarks}</p>}
              <p className="text-xs text-ink-700">
                Verdicts come from the deterministic rule engine; AI only extracted the values. Score = weighted share of rules passed or exempted.
              </p>
            </div>
          </div>
        </Card>

        {mode === "bidder" && fixes.length > 0 && (
          <Card title={<h2 className="flex items-center gap-2 text-base font-semibold"><Lightbulb className="size-5 text-saffron-500" /> Your path to compliance</h2>}>
            <ol className="space-y-3">
              {fixes.map((f, i) => (
                <li key={f.ruleCode} className="flex gap-3">
                  <span className="flex size-7 shrink-0 items-center justify-center rounded-full bg-saffron-500 text-sm font-bold text-white">{i + 1}</span>
                  <div className="min-w-0 flex-1">
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="font-medium">{f.ruleTitle}</span>
                      {f.isMandatory && <Badge tone="ink">Mandatory</Badge>}
                      <Badge tone="pass">+{f.scoreGain} pts</Badge>
                    </div>
                    <p className="text-sm text-ink-700">{f.action}</p>
                  </div>
                </li>
              ))}
            </ol>
          </Card>
        )}

        <Card title="Rule-by-rule verdicts" padded={false}>
          {evaluations.length === 0 ? (
            <EmptyState icon={<Scale className="size-10" />} title="Not evaluated yet">Verdicts appear once processing finishes.</EmptyState>
          ) : (
            <ul className="divide-y divide-ink-100">
              {evaluations.map((e) => (
                <li key={e.id} className={cx("px-5 py-4", e.effectiveOutcome === "Fail" && e.isMandatory && "bg-fail-bg/40")}>
                  <div className="flex flex-wrap items-start justify-between gap-3">
                    <div className="min-w-0 flex-1">
                      <div className="flex flex-wrap items-center gap-2">
                        <OutcomeChip outcome={e.effectiveOutcome} />
                        <span className="font-medium">{e.ruleTitle}</span>
                        <span className="font-mono text-xs text-ink-700">{e.ruleCode}</span>
                        {e.isMandatory && <Badge tone="ink">Mandatory</Badge>}
                      </div>
                      <p className="mt-1.5 text-sm text-ink-800">{e.explanation}</p>
                      {e.officerOverride && (
                        <p className="mt-1.5 text-sm text-info">
                          Officer override: {e.outcome} → {e.officerOverride}. “{e.overrideReason}”
                        </p>
                      )}
                      {e.sourceClause && (
                        <p className="mt-1.5 text-xs italic text-ink-700">
                          Tender: “{e.sourceClause}”{e.sourcePage ? ` (p.${e.sourcePage})` : ""}
                        </p>
                      )}
                      <EvidenceList items={e.evidence} onOpen={setEvidence} />
                    </div>
                    {mode === "officer" && onOverride && bid.status === "UnderReview" && (
                      <Button size="sm" variant="secondary" onClick={() => onOverride(e)}>Override</Button>
                    )}
                  </div>
                </li>
              ))}
            </ul>
          )}
        </Card>
      </div>

      <div className="space-y-6">
        {side}
        <Card title={<h2 className="flex items-center gap-2 text-base font-semibold"><Landmark className="size-5 text-saffron-500" /> Registry checks</h2>}>
          {verifications.length === 0 ? (
            <p className="text-sm text-ink-700">No registry checks yet.</p>
          ) : (
            <ul className="space-y-3">
              {verifications.map((v) => (
                <li key={v.source} className="flex items-start justify-between gap-3">
                  <div className="min-w-0">
                    <div className="text-sm font-medium">{v.source}</div>
                    <div className="truncate font-mono text-xs text-ink-700">{v.identifier}</div>
                    {v.registeredName && <div className="text-xs text-ink-700">{v.registeredName}</div>}
                  </div>
                  <VerificationBadge status={v.status} />
                </li>
              ))}
            </ul>
          )}
          <p className="mt-4 text-xs text-ink-700">Mock GSTN / MCA21 / Udyam / EPFO adapters for the demo; results cached 24 h.</p>
        </Card>
        <Card title={<h2 className="flex items-center gap-2 text-base font-semibold"><FileText className="size-5 text-saffron-500" /> Documents</h2>}>
          {documents.length === 0 ? (
            <p className="text-sm text-ink-700">No documents uploaded.</p>
          ) : (
            <ul className="space-y-3">
              {documents.map((d) => (
                <li key={d.id} className="flex items-start justify-between gap-3">
                  <div className="min-w-0">
                    <button className="truncate text-left text-sm font-medium text-ink-900 hover:text-saffron-600" onClick={() => documentsApi.open(d.id)}>
                      {d.fileName}
                    </button>
                    <div className="text-xs text-ink-700">
                      {d.docType ?? "document"} · {formatBytes(d.sizeBytes)} · {formatDateTime(d.createdAt)}
                    </div>
                  </div>
                  {d.ocrUsed && <Badge tone="info">OCR</Badge>}
                </li>
              ))}
            </ul>
          )}
        </Card>
      </div>
      <EvidenceViewer evidence={evidence} onClose={() => setEvidence(null)} />
    </div>
  );
}

function EvidenceList({ items, onOpen }: { items: Evidence[]; onOpen: (e: DocumentEvidence) => void }) {
  if (items.length === 0) return null;
  return (
    <div className="mt-2 flex flex-wrap gap-2">
      {items.map((item, i) =>
        item.kind === "document" ? (
          <button
            key={i}
            onClick={() => onOpen(item)}
            className="inline-flex max-w-full items-center gap-1.5 rounded-md border border-ink-100 bg-white px-2 py-1 text-xs text-ink-800 hover:border-saffron-500 hover:text-saffron-600"
            title="Show where this value was found"
          >
            <FileText className="size-3.5 shrink-0" aria-hidden />
            <span className="truncate">
              {item.fileName ?? "document"}
              {item.page ? ` p.${item.page}` : ""} · <strong>{item.value}</strong>
            </span>
          </button>
        ) : (
          <span key={i} className="inline-flex items-center gap-1.5 rounded-md bg-ink-50 px-2 py-1 text-xs text-ink-800">
            <Landmark className="size-3.5" aria-hidden /> {item.source}: {item.status}
            {item.registeredName ? ` · ${item.registeredName}` : ""}
          </span>
        ),
      )}
    </div>
  );
}
