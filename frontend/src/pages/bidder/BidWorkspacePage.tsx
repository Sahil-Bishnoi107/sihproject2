import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, FileUp, Send } from "lucide-react";
import { useState } from "react";
import { Link, useParams } from "react-router-dom";
import { bidsApi } from "../../api/endpoints";
import { PipelineStepper } from "../../components/PipelineStepper";
import { ReportView } from "../../components/report/ReportView";
import { BidStatusBadge, isProcessing } from "../../components/StatusBadges";
import { Badge, Button, Card, ErrorState, FormError, PageHeader, SelectField, Spinner } from "../../components/ui";
import { formatBytes } from "../../lib/format";

const DOC_TYPES = [
  { value: "gst_certificate", label: "GST registration certificate" },
  { value: "udyam_certificate", label: "Udyam / MSE certificate" },
  { value: "financials", label: "Turnover / CA certificate" },
  { value: "iso_certificate", label: "ISO certificate" },
  { value: "oem_authorization", label: "OEM authorization" },
  { value: "experience_certificate", label: "Experience certificate" },
  { value: "emd_receipt", label: "EMD receipt" },
  { value: "other", label: "Other" },
];

export function BidWorkspacePage() {
  const { id = "" } = useParams();
  const queryClient = useQueryClient();
  const [docType, setDocType] = useState("financials");

  const report = useQuery({
    queryKey: ["report", id],
    queryFn: () => bidsApi.report(id),
    // Poll while the pipeline runs; stop once the report is ready or processing failed.
    refetchInterval: (q) => (q.state.data && isProcessing(q.state.data.bid.status) ? 2000 : false),
  });

  const refresh = () => {
    queryClient.invalidateQueries({ queryKey: ["report", id] });
    queryClient.invalidateQueries({ queryKey: ["bids", "mine"] });
  };
  const upload = useMutation({ mutationFn: (file: File) => bidsApi.uploadDocument(id, file, docType), onSuccess: refresh });
  const submit = useMutation({ mutationFn: () => bidsApi.submit(id), onSuccess: refresh });

  if (report.isLoading) return <Spinner />;
  if (report.error || !report.data) return <ErrorState error={report.error} onRetry={() => report.refetch()} />;

  const { bid, tender, documents } = report.data;
  const editable = bid.status === "Uploaded" || bid.status === "Failed";
  const done = bid.status === "UnderReview" || bid.status === "Decided";

  return (
    <>
      <Link to={`/tenders/${tender.id}`} className="mb-3 inline-flex items-center gap-1 text-sm text-ink-700 hover:text-saffron-600">
        <ArrowLeft className="size-4" /> {tender.gemBidNumber}
      </Link>
      <PageHeader
        eyebrow={bid.isSimulation ? "Pre-bid simulation (private)" : "Official bid"}
        title={tender.title}
        actions={<BidStatusBadge status={bid.status} />}
      />
      <div className="space-y-6">
        <Card>
          <PipelineStepper status={bid.status} failed={bid.status === "Failed"} />
          {bid.status === "Failed" && bid.failureReason && (
            <div className="mt-4"><ErrorState error={new Error(`${bid.failureReason} — you can resubmit.`)} /></div>
          )}
        </Card>

        {editable && (
          <Card title="Upload your documents">
            <div className="grid gap-5 lg:grid-cols-2">
              <div className="space-y-4">
                <SelectField label="Document type" value={docType} onChange={(e) => setDocType(e.target.value)}
                  hint="Helps the AI know what to look for.">
                  {DOC_TYPES.map((d) => <option key={d.value} value={d.value}>{d.label}</option>)}
                </SelectField>
                <label className="flex cursor-pointer flex-col items-center justify-center gap-2 rounded-xl border-2 border-dashed border-ink-100 px-6 py-8 text-center hover:border-saffron-500 hover:bg-saffron-50/40">
                  <FileUp className="size-8 text-saffron-500" aria-hidden />
                  <span className="text-sm font-medium">Choose a PDF or image</span>
                  <span className="text-xs text-ink-700">PDF, PNG or JPEG up to 25 MB</span>
                  <input type="file" accept=".pdf,.png,.jpg,.jpeg" className="sr-only"
                    onChange={(e) => {
                      const file = e.target.files?.[0];
                      if (file) upload.mutate(file);
                      e.target.value = "";
                    }} />
                </label>
                {upload.isPending && <Spinner label="Uploading…" />}
                <FormError error={upload.error} />
              </div>
              <div>
                <div className="text-sm font-medium">Uploaded ({documents.length})</div>
                {documents.length === 0 ? (
                  <p className="mt-2 text-sm text-ink-700">Nothing uploaded yet.</p>
                ) : (
                  <ul className="mt-2 divide-y divide-ink-100 rounded-lg border border-ink-100">
                    {documents.map((d) => (
                      <li key={d.id} className="flex items-center justify-between gap-3 px-3 py-2 text-sm">
                        <span className="truncate">{d.fileName}</span>
                        <span className="flex shrink-0 items-center gap-2 text-xs text-ink-700">
                          <Badge>{DOC_TYPES.find((t) => t.value === d.docType)?.label ?? d.docType ?? "document"}</Badge>
                          {formatBytes(d.sizeBytes)}
                        </span>
                      </li>
                    ))}
                  </ul>
                )}
                <Button className="mt-4 w-full" variant="accent" disabled={documents.length === 0} loading={submit.isPending} onClick={() => submit.mutate()}>
                  <Send className="size-4" /> {bid.isSimulation ? "Run simulation" : bid.status === "Failed" ? "Resubmit" : "Submit for evaluation"}
                </Button>
                <FormError error={submit.error} />
              </div>
            </div>
          </Card>
        )}

        {isProcessing(bid.status) && (
          <Card>
            <Spinner label="Reading your documents, checking registries and evaluating every rule…" />
          </Card>
        )}

        {done && <ReportView report={report.data} mode="bidder" />}
      </div>
    </>
  );
}
