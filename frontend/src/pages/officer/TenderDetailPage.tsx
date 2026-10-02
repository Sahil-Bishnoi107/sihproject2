import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Bot, CheckCheck, FileUp, Loader2, Pencil, Plus, Rocket, Trash2, Users } from "lucide-react";
import { useState } from "react";
import { Link, useParams } from "react-router-dom";
import { tendersApi } from "../../api/endpoints";
import type { RuleDto } from "../../api/types";
import { BidStatusBadge, DecisionBadge, RuleStatusBadge } from "../../components/StatusBadges";
import { TenderFacts } from "../../components/TenderSummary";
import { Badge, Button, Card, EmptyState, ErrorState, FormError, PageHeader, Spinner, Table } from "../../components/ui";
import { expressionToText } from "../../lib/format";
import { RuleEditorModal } from "./RuleEditorModal";

export function TenderDetailPage() {
  const { id = "" } = useParams();
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState<RuleDto | null>(null);
  const [editorOpen, setEditorOpen] = useState(false);

  const tender = useQuery({
    queryKey: ["tender", id],
    queryFn: () => tendersApi.get(id),
    // Poll while the AI sidecar drafts the rules.
    refetchInterval: (q) => (q.state.data?.status === "Extracting" ? 2000 : false),
  });
  const status = tender.data?.status;
  const rules = useQuery({ queryKey: ["tender", id, "rules", status], queryFn: () => tendersApi.rules(id), enabled: !!status });
  const bids = useQuery({
    queryKey: ["tender", id, "bids"],
    queryFn: () => tendersApi.bids(id),
    enabled: status === "Published" || status === "Closed",
    refetchInterval: 10_000,
  });

  const refresh = () => queryClient.invalidateQueries({ queryKey: ["tender", id] });
  const upload = useMutation({ mutationFn: (file: File) => tendersApi.uploadDocument(id, file), onSuccess: refresh });
  const approve = useMutation({ mutationFn: (ruleId: string) => tendersApi.approveRule(id, ruleId), onSuccess: refresh });
  const approveAll = useMutation({ mutationFn: () => tendersApi.approveAll(id), onSuccess: refresh });
  const remove = useMutation({ mutationFn: (ruleId: string) => tendersApi.deleteRule(id, ruleId), onSuccess: refresh });
  const publish = useMutation({
    mutationFn: () => tendersApi.publish(id),
    onSuccess: () => {
      refresh();
      queryClient.invalidateQueries({ queryKey: ["tenders"] });
    },
  });

  if (tender.isLoading) return <Spinner />;
  if (tender.error || !tender.data) return <ErrorState error={tender.error} onRetry={() => tender.refetch()} />;

  const t = tender.data;
  const editable = t.status === "Draft" || t.status === "RulesExtracted";
  const ruleList = rules.data ?? [];
  const drafts = ruleList.filter((r) => r.status === "AiDraft").length;
  const canPublish = editable && ruleList.length > 0 && drafts === 0;

  return (
    <>
      <PageHeader
        eyebrow={t.gemBidNumber}
        title={t.title}
        subtitle={t.category}
        actions={
          editable && (
            <Button variant="accent" onClick={() => publish.mutate()} disabled={!canPublish} loading={publish.isPending}
              title={canPublish ? "Publish to bidders" : "Approve every rule first"}>
              <Rocket className="size-4" /> Publish tender
            </Button>
          )
        }
      />
      <div className="space-y-6">
        <TenderFacts tender={t} />
        <FormError error={publish.error} />

        {editable && (
          <Card title="Tender document">
            {t.status === "Extracting" ? (
              <div className="flex items-center gap-3 text-sm text-ink-800">
                <Loader2 className="size-5 animate-spin text-saffron-500" aria-hidden />
                The AI is reading the tender and drafting compliance rules…
              </div>
            ) : (
              <div className="space-y-3">
                {t.failureReason && <ErrorState error={new Error(t.failureReason)} />}
                <label className="flex cursor-pointer flex-col items-center justify-center gap-2 rounded-xl border-2 border-dashed border-ink-100 px-6 py-8 text-center hover:border-saffron-500 hover:bg-saffron-50/40">
                  <FileUp className="size-8 text-saffron-500" aria-hidden />
                  <span className="text-sm font-medium text-ink-900">
                    {t.tenderDocumentId ? "Upload a new version to re-extract rules" : "Upload the tender PDF"}
                  </span>
                  <span className="text-xs text-ink-700">PDF up to 25 MB. Scanned pages are OCR'd automatically.</span>
                  <input
                    type="file"
                    accept="application/pdf,.pdf"
                    className="sr-only"
                    onChange={(e) => {
                      const file = e.target.files?.[0];
                      if (file) upload.mutate(file);
                      e.target.value = "";
                    }}
                  />
                </label>
                {upload.isPending && <Spinner label="Uploading…" />}
                <FormError error={upload.error} />
              </div>
            )}
          </Card>
        )}

        <Card
          title={
            <div className="flex items-center gap-2">
              <h2 className="text-base font-semibold">Compliance rules</h2>
              <Badge>{ruleList.length}</Badge>
              {drafts > 0 && <Badge tone="accent" icon={<Bot className="size-3.5" />}>{drafts} AI drafts to review</Badge>}
            </div>
          }
          actions={
            editable && (
              <>
                {drafts > 0 && (
                  <Button size="sm" variant="secondary" onClick={() => approveAll.mutate()} loading={approveAll.isPending}>
                    <CheckCheck className="size-4" /> Approve all
                  </Button>
                )}
                <Button size="sm" onClick={() => { setEditing(null); setEditorOpen(true); }}>
                  <Plus className="size-4" /> Add rule
                </Button>
              </>
            )
          }
          padded={false}
        >
          {rules.isLoading && <Spinner />}
          {rules.error && <div className="p-4"><ErrorState error={rules.error} /></div>}
          {rules.data && ruleList.length === 0 && (
            <EmptyState icon={<Bot className="size-10" />} title={t.status === "Extracting" ? "Rules are being drafted" : "No rules yet"}>
              {t.tenderDocumentId ? "Add rules manually, or upload the PDF again." : "Upload the tender PDF and the AI will draft them, or add rules manually."}
            </EmptyState>
          )}
          {ruleList.length > 0 && (
            <Table head={["Rule", "Checks", "Weight", "Status", ""]}>
              {ruleList.map((r) => (
                <tr key={r.id} className="align-top">
                  <td className="max-w-xs px-4 py-3">
                    <div className="font-mono text-xs text-ink-700">{r.code}</div>
                    <div className="font-medium">{r.title}</div>
                    <div className="mt-1 flex flex-wrap gap-1">
                      <Badge>{r.category}</Badge>
                      {r.isMandatory ? <Badge tone="ink">Mandatory</Badge> : <Badge>Optional</Badge>}
                      {r.exemptionKind && <Badge tone="info">{r.exemptionKind === "Emd" ? "EMD exemption" : "Startup relaxation"}</Badge>}
                    </div>
                    {r.sourceClause && (
                      <p className="mt-2 text-xs italic text-ink-700">
                        “{r.sourceClause}”{r.sourcePage ? ` — p.${r.sourcePage}` : ""}
                      </p>
                    )}
                  </td>
                  <td className="px-4 py-3 text-sm text-ink-800">{expressionToText(r.expression)}</td>
                  <td className="px-4 py-3">{r.weight}</td>
                  <td className="px-4 py-3"><RuleStatusBadge status={r.status} /></td>
                  <td className="px-4 py-3">
                    {editable && (
                      <div className="flex justify-end gap-1">
                        {r.status === "AiDraft" && (
                          <Button size="sm" variant="secondary" onClick={() => approve.mutate(r.id)} disabled={approve.isPending}>
                            Approve
                          </Button>
                        )}
                        <Button size="sm" variant="ghost" aria-label={`Edit ${r.code}`} onClick={() => { setEditing(r); setEditorOpen(true); }}>
                          <Pencil className="size-4" />
                        </Button>
                        <Button size="sm" variant="ghost" aria-label={`Delete ${r.code}`}
                          onClick={() => window.confirm(`Delete rule ${r.code}?`) && remove.mutate(r.id)}>
                          <Trash2 className="size-4 text-fail" />
                        </Button>
                      </div>
                    )}
                  </td>
                </tr>
              ))}
            </Table>
          )}
          <div className="px-4 pb-2">
            <FormError error={approve.error ?? approveAll.error ?? remove.error} />
          </div>
        </Card>

        {(t.status === "Published" || t.status === "Closed") && (
          <Card title="Bids received" padded={false}>
            {bids.isLoading && <Spinner />}
            {bids.data && bids.data.length === 0 && (
              <EmptyState icon={<Users className="size-10" />} title="No bids yet">Official bids appear here as bidders submit them.</EmptyState>
            )}
            {bids.data && bids.data.length > 0 && (
              <Table head={["Bidder", "Status", "Score", "Suggested", "Decision", ""]}>
                {bids.data.map((b) => (
                  <tr key={b.id} className="hover:bg-ink-50/60">
                    <td className="px-4 py-3 font-medium">{b.organizationName}</td>
                    <td className="px-4 py-3"><BidStatusBadge status={b.status} /></td>
                    <td className="px-4 py-3">{b.overallScore ?? "—"}</td>
                    <td className="px-4 py-3">{b.suggestedDecision ? <DecisionBadge decision={b.suggestedDecision} /> : "—"}</td>
                    <td className="px-4 py-3">{b.decision ? <DecisionBadge decision={b.decision} /> : "—"}</td>
                    <td className="px-4 py-3 text-right">
                      <Link to={`/bids/${b.id}`} className="text-sm font-medium text-saffron-600 hover:underline">Review</Link>
                    </td>
                  </tr>
                ))}
              </Table>
            )}
          </Card>
        )}
      </div>
      <RuleEditorModal tenderId={id} rule={editing} open={editorOpen} onClose={() => setEditorOpen(false)} />
    </>
  );
}
