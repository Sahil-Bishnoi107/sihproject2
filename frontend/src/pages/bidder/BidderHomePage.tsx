import { useQuery } from "@tanstack/react-query";
import { AlarmClock, FileSearch, Inbox } from "lucide-react";
import { Link } from "react-router-dom";
import { bidsApi, organizationApi } from "../../api/endpoints";
import { useAuth } from "../../auth/AuthContext";
import { BidStatusBadge, DecisionBadge } from "../../components/StatusBadges";
import { Badge, Button, Card, EmptyState, ErrorState, PageHeader, Spinner, Table } from "../../components/ui";
import { daysUntil, formatDate } from "../../lib/format";

export function BidderHomePage() {
  const { user } = useAuth();
  const bids = useQuery({ queryKey: ["bids", "mine"], queryFn: bidsApi.mine });
  const org = useQuery({ queryKey: ["organization"], queryFn: organizationApi.get });

  const expiring = (org.data?.credentials ?? []).filter((c) => c.expiresOn && daysUntil(c.expiresOn) <= 30);

  return (
    <>
      <PageHeader
        title={`Welcome, ${user?.fullName.split(" ")[0] ?? ""}`}
        subtitle={org.data?.legalName}
        actions={
          <Link to="/tenders">
            <Button><FileSearch className="size-4" /> Browse open tenders</Button>
          </Link>
        }
      />
      {expiring.length > 0 && (
        <div className="mb-6 rounded-xl border border-warn/30 bg-warn-bg p-4">
          <div className="flex items-center gap-2 font-medium text-warn"><AlarmClock className="size-5" /> Credentials needing renewal</div>
          <ul className="mt-2 space-y-1 text-sm text-ink-800">
            {expiring.map((c) => {
              const days = daysUntil(c.expiresOn!);
              return (
                <li key={c.id}>
                  {c.type} {c.number} — {days < 0 ? `expired ${-days} days ago` : `expires in ${days} days`} ({formatDate(c.expiresOn)})
                </li>
              );
            })}
          </ul>
          <Link to="/organization" className="mt-2 inline-block text-sm font-medium text-saffron-600 hover:underline">Manage credentials</Link>
        </div>
      )}
      <Card title="My bids and simulations" padded={false}>
        {bids.isLoading && <Spinner />}
        {bids.error && <div className="p-4"><ErrorState error={bids.error} /></div>}
        {bids.data?.length === 0 && (
          <EmptyState icon={<Inbox className="size-10" />} title="No bids yet">
            Open a tender and run a pre-bid simulation to see how your documents measure up.
          </EmptyState>
        )}
        {bids.data && bids.data.length > 0 && (
          <Table head={["Tender", "Type", "Status", "Score", "Outcome", ""]}>
            {bids.data.map((b) => (
              <tr key={b.id} className="hover:bg-ink-50/60">
                <td className="px-4 py-3">
                  <div className="font-medium">{b.tenderTitle}</div>
                  <div className="font-mono text-xs text-ink-700">{b.gemBidNumber}</div>
                </td>
                <td className="px-4 py-3">{b.isSimulation ? <Badge tone="accent">Simulation</Badge> : <Badge tone="ink">Official</Badge>}</td>
                <td className="px-4 py-3"><BidStatusBadge status={b.status} /></td>
                <td className="px-4 py-3">{b.overallScore ?? "—"}</td>
                <td className="px-4 py-3">
                  {b.decision ? <DecisionBadge decision={b.decision} /> : b.suggestedDecision ? <DecisionBadge decision={b.suggestedDecision} /> : "—"}
                </td>
                <td className="px-4 py-3 text-right">
                  <Link to={`/bids/${b.id}`} className="text-sm font-medium text-saffron-600 hover:underline">Open</Link>
                </td>
              </tr>
            ))}
          </Table>
        )}
      </Card>
    </>
  );
}
