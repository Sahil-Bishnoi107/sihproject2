import { useMutation, useQuery } from "@tanstack/react-query";
import { Link2, ShieldAlert, ShieldCheck } from "lucide-react";
import { useState } from "react";
import { auditApi } from "../../api/endpoints";
import { Button, Card, ErrorState, PageHeader, Spinner, Table } from "../../components/ui";
import { formatDateTime, shortId } from "../../lib/format";

const ENTITY_TYPES = ["", "Tender", "ComplianceRule", "Bid", "Organization", "User"];

export function AuditPage() {
  const [entityType, setEntityType] = useState("");
  const [page, setPage] = useState(1);
  const logs = useQuery({ queryKey: ["audit", entityType, page], queryFn: () => auditApi.list({ entityType, page, pageSize: 25 }) });
  const verify = useMutation({ mutationFn: auditApi.verify });

  const totalPages = logs.data ? Math.max(1, Math.ceil(logs.data.total / logs.data.pageSize)) : 1;

  return (
    <>
      <PageHeader
        title="Audit log"
        subtitle="Append-only, SHA-256 hash-chained: changing any past entry breaks every hash after it."
        actions={
          <Button variant="accent" onClick={() => verify.mutate()} loading={verify.isPending}>
            <Link2 className="size-4" /> Verify chain
          </Button>
        }
      />
      {verify.data && (
        <div className={`mb-5 flex items-center gap-3 rounded-xl border p-4 ${verify.data.isIntact ? "border-pass/30 bg-pass-bg text-pass" : "border-fail/30 bg-fail-bg text-fail"}`}>
          {verify.data.isIntact ? <ShieldCheck className="size-6" /> : <ShieldAlert className="size-6" />}
          <div className="text-sm">
            {verify.data.isIntact
              ? <><strong>Chain intact.</strong> All {verify.data.entriesChecked} entries recomputed and matched.</>
              : <><strong>Tampering detected</strong> at entry #{verify.data.firstBrokenSequence}. Entries from there on cannot be trusted.</>}
          </div>
        </div>
      )}
      {verify.error && <div className="mb-5"><ErrorState error={verify.error} /></div>}
      <Card padded={false}>
        <div className="flex items-center gap-3 border-b border-ink-100 p-4">
          <select className="rounded-lg border border-ink-100 px-3 py-2 text-sm" value={entityType} aria-label="Filter by entity type"
            onChange={(e) => { setEntityType(e.target.value); setPage(1); }}>
            {ENTITY_TYPES.map((t) => <option key={t} value={t}>{t || "All entities"}</option>)}
          </select>
          {logs.data && <span className="text-sm text-ink-700">{logs.data.total} entries</span>}
        </div>
        {logs.isLoading && <Spinner />}
        {logs.error && <div className="p-4"><ErrorState error={logs.error} /></div>}
        {logs.data && (
          <Table head={["#", "Time", "Action", "Entity", "Actor", "Details", "Hash"]}>
            {logs.data.items.map((a) => (
              <tr key={a.sequence} className="align-top">
                <td className="px-4 py-2.5 font-mono text-xs">{a.sequence}</td>
                <td className="whitespace-nowrap px-4 py-2.5 text-xs">{formatDateTime(a.timestamp)}</td>
                <td className="px-4 py-2.5 font-medium">{a.action}</td>
                <td className="px-4 py-2.5 text-xs">{a.entityType} <span className="font-mono text-ink-700">{shortId(a.entityId)}</span></td>
                <td className="px-4 py-2.5 font-mono text-xs text-ink-700">{a.actorId ? shortId(a.actorId) : "system"}</td>
                <td className="max-w-xs px-4 py-2.5">
                  <code className="line-clamp-2 break-all text-[11px] text-ink-700" title={a.payloadJson}>{a.payloadJson}</code>
                </td>
                <td className="px-4 py-2.5 font-mono text-[11px] text-ink-700" title={a.hash}>{a.hash.slice(0, 12)}…</td>
              </tr>
            ))}
          </Table>
        )}
        <div className="flex items-center justify-end gap-2 border-t border-ink-100 p-3">
          <Button size="sm" variant="secondary" disabled={page <= 1} onClick={() => setPage(page - 1)}>Previous</Button>
          <span className="text-sm text-ink-700">Page {page} of {totalPages}</span>
          <Button size="sm" variant="secondary" disabled={page >= totalPages} onClick={() => setPage(page + 1)}>Next</Button>
        </div>
      </Card>
    </>
  );
}
