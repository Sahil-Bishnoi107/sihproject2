import { useQuery } from "@tanstack/react-query";
import { ClipboardList, FileCheck2, Inbox, Plus, TriangleAlert } from "lucide-react";
import { Link } from "react-router-dom";
import { dashboardApi, tendersApi } from "../../api/endpoints";
import type { BidDecision } from "../../api/types";
import { decisionLabel, TenderStatusBadge } from "../../components/StatusBadges";
import { Button, Card, EmptyState, ErrorState, PageHeader, Spinner, Stat } from "../../components/ui";
import { formatDate } from "../../lib/format";

const DECISION_COLORS: Record<BidDecision, string> = {
  Compliant: "bg-pass",
  NonCompliant: "bg-fail",
  NeedsClarification: "bg-warn",
};

export function DashboardPage() {
  const stats = useQuery({ queryKey: ["dashboard"], queryFn: dashboardApi.stats });
  const recent = useQuery({ queryKey: ["tenders", "recent"], queryFn: () => tendersApi.list({ pageSize: 5 }) });

  if (stats.isLoading) return <Spinner />;
  if (stats.error || !stats.data) return <ErrorState error={stats.error} onRetry={() => stats.refetch()} />;

  const s = stats.data;
  const decisions = (["Compliant", "NonCompliant", "NeedsClarification"] as BidDecision[]).map((d) => ({ d, n: s.bidsByDecision[d] ?? 0 }));
  const decided = decisions.reduce((sum, x) => sum + x.n, 0);
  const maxFailures = Math.max(1, ...s.topFailingRules.map((r) => r.failures));

  return (
    <>
      <PageHeader
        title="Compliance dashboard"
        subtitle="Official bids only — bidder simulations stay private."
        actions={<Link to="/tenders"><Button><Plus className="size-4" /> New tender</Button></Link>}
      />
      <div className="grid gap-4 sm:grid-cols-3">
        <Stat label="Tenders" value={s.totalTenders} hint={`${s.publishedTenders} published`} icon={<ClipboardList className="size-5" />} />
        <Stat label="Bids received" value={s.totalBids} hint={`${decided} evaluated`} icon={<Inbox className="size-5" />} />
        <Stat
          label="Compliance rate"
          value={decided ? `${Math.round(((s.bidsByDecision.Compliant ?? 0) / decided) * 100)}%` : "—"}
          hint="Compliant ÷ evaluated bids"
          icon={<FileCheck2 className="size-5" />}
        />
      </div>

      <div className="mt-6 grid gap-6 lg:grid-cols-2">
        <Card title="Bids by outcome">
          {decided === 0 ? (
            <EmptyState title="No evaluated bids yet" />
          ) : (
            <>
              <div className="flex h-4 overflow-hidden rounded-full bg-ink-100" role="img" aria-label="Bids by outcome">
                {decisions.filter((x) => x.n > 0).map((x) => (
                  <div key={x.d} className={DECISION_COLORS[x.d]} style={{ width: `${(x.n / decided) * 100}%` }} />
                ))}
              </div>
              <ul className="mt-4 space-y-2">
                {decisions.map((x) => (
                  <li key={x.d} className="flex items-center justify-between text-sm">
                    <span className="flex items-center gap-2">
                      <span className={`size-3 rounded-sm ${DECISION_COLORS[x.d]}`} /> {decisionLabel(x.d)}
                    </span>
                    <span className="font-semibold">{x.n}</span>
                  </li>
                ))}
              </ul>
            </>
          )}
        </Card>

        <Card title={<h2 className="flex items-center gap-2 text-base font-semibold"><TriangleAlert className="size-5 text-saffron-500" /> Most-failed requirements</h2>}>
          {s.topFailingRules.length === 0 ? (
            <EmptyState title="No failures recorded" />
          ) : (
            <ul className="space-y-3">
              {s.topFailingRules.map((r) => (
                <li key={r.ruleCode}>
                  <div className="flex justify-between text-sm">
                    <span className="truncate pr-3">{r.ruleTitle}</span>
                    <span className="font-semibold">{r.failures}</span>
                  </div>
                  <div className="mt-1 h-2 rounded-full bg-ink-100">
                    <div className="h-2 rounded-full bg-fail" style={{ width: `${(r.failures / maxFailures) * 100}%` }} />
                  </div>
                </li>
              ))}
            </ul>
          )}
          <p className="mt-4 text-xs text-ink-700">Frequent failures often point to unclear or restrictive tender clauses.</p>
        </Card>
      </div>

      <Card title="Recent tenders" className="mt-6" padded={false}>
        {recent.data?.items.length === 0 && <EmptyState title="No tenders yet" />}
        <ul className="divide-y divide-ink-100">
          {recent.data?.items.map((t) => (
            <li key={t.id}>
              <Link to={`/tenders/${t.id}`} className="flex items-center justify-between gap-4 px-5 py-3 hover:bg-ink-50/60">
                <div className="min-w-0">
                  <div className="truncate font-medium">{t.title}</div>
                  <div className="font-mono text-xs text-ink-700">{t.gemBidNumber} · ends {formatDate(t.bidEndDate)}</div>
                </div>
                <TenderStatusBadge status={t.status} />
              </Link>
            </li>
          ))}
        </ul>
      </Card>
    </>
  );
}
