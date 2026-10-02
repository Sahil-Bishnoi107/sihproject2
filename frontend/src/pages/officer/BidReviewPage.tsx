import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, Gavel } from "lucide-react";
import { useState, type FormEvent } from "react";
import { Link, useParams } from "react-router-dom";
import { bidsApi } from "../../api/endpoints";
import type { BidDecision, EvaluationDto, RuleOutcome } from "../../api/types";
import { ReportView } from "../../components/report/ReportView";
import { BidStatusBadge, decisionLabel, isProcessing } from "../../components/StatusBadges";
import {
  Button, Card, ErrorState, fieldError, FormError, Modal, PageHeader, SelectField, Spinner, TextAreaField,
} from "../../components/ui";

export function BidReviewPage() {
  const { id = "" } = useParams();
  const queryClient = useQueryClient();
  const [overriding, setOverriding] = useState<EvaluationDto | null>(null);

  const report = useQuery({
    queryKey: ["report", id],
    queryFn: () => bidsApi.report(id),
    refetchInterval: (q) => (q.state.data && isProcessing(q.state.data.bid.status) ? 2500 : false),
  });

  if (report.isLoading) return <Spinner />;
  if (report.error || !report.data) return <ErrorState error={report.error} onRetry={() => report.refetch()} />;

  const { bid, tender } = report.data;
  const refresh = () => {
    queryClient.invalidateQueries({ queryKey: ["report", id] });
    queryClient.invalidateQueries({ queryKey: ["tender", tender.id, "bids"] });
  };

  return (
    <>
      <Link to={`/tenders/${tender.id}`} className="mb-3 inline-flex items-center gap-1 text-sm text-ink-700 hover:text-saffron-600">
        <ArrowLeft className="size-4" /> {tender.gemBidNumber}
      </Link>
      <PageHeader
        eyebrow="Bid review"
        title={bid.organizationName ?? "Bidder"}
        subtitle={tender.title}
        actions={<BidStatusBadge status={bid.status} />}
      />
      {bid.failureReason && <div className="mb-4"><ErrorState error={new Error(bid.failureReason)} /></div>}
      <ReportView
        report={report.data}
        mode="officer"
        onOverride={setOverriding}
        side={bid.status === "UnderReview" && <DecisionPanel bidId={bid.id} suggested={bid.suggestedDecision} onDone={refresh} />}
      />
      <OverrideModal bidId={bid.id} evaluation={overriding} onClose={() => setOverriding(null)} onDone={refresh} />
    </>
  );
}

function DecisionPanel({ bidId, suggested, onDone }: { bidId: string; suggested: BidDecision | null; onDone: () => void }) {
  const [decision, setDecision] = useState<BidDecision>(suggested ?? "Compliant");
  const [remarks, setRemarks] = useState("");
  const decide = useMutation({ mutationFn: () => bidsApi.decide(bidId, decision, remarks), onSuccess: onDone });

  const submit = (e: FormEvent) => {
    e.preventDefault();
    decide.mutate();
  };

  return (
    <Card title={<h2 className="flex items-center gap-2 text-base font-semibold"><Gavel className="size-5 text-saffron-500" /> Final decision</h2>}>
      <form className="space-y-4" onSubmit={submit}>
        <SelectField label="Decision" value={decision} onChange={(e) => setDecision(e.target.value as BidDecision)}
          hint={suggested ? `Engine suggests: ${decisionLabel(suggested)}` : undefined}>
          {(["Compliant", "NonCompliant", "NeedsClarification"] as BidDecision[]).map((d) => (
            <option key={d} value={d}>{decisionLabel(d)}</option>
          ))}
        </SelectField>
        <TextAreaField label="Remarks" rows={3} value={remarks} onChange={(e) => setRemarks(e.target.value)}
          hint="Required unless the bid is accepted as compliant. Shared with the bidder."
          error={fieldError(decide.error, "Remarks")} />
        <FormError error={decide.error} />
        <Button type="submit" variant="accent" className="w-full" loading={decide.isPending}>Record decision</Button>
      </form>
    </Card>
  );
}

function OverrideModal({ bidId, evaluation, onClose, onDone }: {
  bidId: string; evaluation: EvaluationDto | null; onClose: () => void; onDone: () => void;
}) {
  const [outcome, setOutcome] = useState<RuleOutcome>("Pass");
  const [reason, setReason] = useState("");
  const override = useMutation({
    mutationFn: () => bidsApi.override(bidId, evaluation!.id, outcome, reason),
    onSuccess: () => {
      onDone();
      setReason("");
      onClose();
    },
  });

  return (
    <Modal
      open={!!evaluation}
      onClose={onClose}
      title={`Override ${evaluation?.ruleCode ?? ""}`}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Cancel</Button>
          <Button onClick={() => override.mutate()} loading={override.isPending}>Save override</Button>
        </>
      }
    >
      {evaluation && (
        <div className="space-y-4">
          <p className="text-sm text-ink-700">
            Engine verdict: <strong>{evaluation.outcome}</strong> — {evaluation.explanation}
          </p>
          <SelectField label="New outcome" value={outcome} onChange={(e) => setOutcome(e.target.value as RuleOutcome)}>
            {(["Pass", "Fail", "Missing", "Exempted"] as RuleOutcome[]).map((o) => <option key={o}>{o}</option>)}
          </SelectField>
          <TextAreaField label="Reason (recorded in the audit log)" rows={3} value={reason} onChange={(e) => setReason(e.target.value)}
            error={fieldError(override.error, "Reason")} />
          <FormError error={override.error} />
        </div>
      )}
    </Modal>
  );
}
