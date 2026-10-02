import { useMutation, useQuery } from "@tanstack/react-query";
import { FlaskConical, Send } from "lucide-react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { ApiError } from "../../api/client";
import { bidsApi, tendersApi } from "../../api/endpoints";
import { TenderFacts } from "../../components/TenderSummary";
import { Badge, Button, Card, ErrorState, FormError, PageHeader, Spinner } from "../../components/ui";
import { expressionToText } from "../../lib/format";

export function TenderRequirementsPage() {
  const { id = "" } = useParams();
  const navigate = useNavigate();
  const tender = useQuery({ queryKey: ["tender", id], queryFn: () => tendersApi.get(id) });
  const rules = useQuery({ queryKey: ["tender", id, "rules"], queryFn: () => tendersApi.rules(id), enabled: !!tender.data });
  const mine = useQuery({ queryKey: ["bids", "mine"], queryFn: bidsApi.mine });

  const simulate = useMutation({ mutationFn: () => bidsApi.simulate(id), onSuccess: (b) => navigate(`/bids/${b.id}`) });
  const startBid = useMutation({ mutationFn: () => bidsApi.create(id), onSuccess: (b) => navigate(`/bids/${b.id}`) });

  if (tender.isLoading) return <Spinner />;
  if (tender.error || !tender.data) return <ErrorState error={tender.error} />;

  const t = tender.data;
  const official = mine.data?.find((b) => b.tenderId === id && !b.isSimulation);
  const simulations = mine.data?.filter((b) => b.tenderId === id && b.isSimulation) ?? [];
  const closed = new Date(t.bidEndDate).getTime() < Date.now();
  const conflict = startBid.error instanceof ApiError && startBid.error.status === 409;

  return (
    <>
      <PageHeader eyebrow={t.gemBidNumber} title={t.title} subtitle={t.category} />
      <div className="grid gap-6 lg:grid-cols-3">
        <div className="space-y-6 lg:col-span-2">
          <TenderFacts tender={t} />
          <Card title="What this tender requires">
            {rules.isLoading && <Spinner />}
            <ul className="space-y-4">
              {rules.data?.map((r) => (
                <li key={r.id} className="border-l-2 border-saffron-500 pl-4">
                  <div className="flex flex-wrap items-center gap-2">
                    <span className="font-medium">{r.title}</span>
                    {r.isMandatory ? <Badge tone="ink">Mandatory</Badge> : <Badge>Optional</Badge>}
                    {r.exemptionKind && <Badge tone="info">{r.exemptionKind === "Emd" ? "MSE/startup exempt" : "Startup relaxation"}</Badge>}
                  </div>
                  <div className="mt-1 text-sm text-ink-700">{expressionToText(r.expression)}</div>
                  {r.sourceClause && <div className="mt-1 text-xs italic text-ink-700">“{r.sourceClause}”</div>}
                </li>
              ))}
            </ul>
          </Card>
        </div>
        <div className="space-y-6">
          <Card title="Check before you bid">
            <p className="text-sm text-ink-700">
              Run a <strong>private simulation</strong>: upload your documents and see exactly which requirements pass, with a ranked fix list.
              Officers never see simulations.
            </p>
            <Button className="mt-4 w-full" variant="accent" onClick={() => simulate.mutate()} loading={simulate.isPending}>
              <FlaskConical className="size-4" /> Run pre-bid simulation
            </Button>
            <FormError error={simulate.error} />
            {simulations.length > 0 && (
              <div className="mt-4 text-sm">
                <div className="text-xs font-semibold uppercase tracking-wide text-ink-700">Your simulations</div>
                <ul className="mt-2 space-y-1">
                  {simulations.map((s) => (
                    <li key={s.id}>
                      <Link to={`/bids/${s.id}`} className="text-saffron-600 hover:underline">
                        Simulation · score {s.overallScore ?? "pending"}
                      </Link>
                    </li>
                  ))}
                </ul>
              </div>
            )}
          </Card>
          <Card title="Official bid">
            {official ? (
              <Link to={`/bids/${official.id}`}>
                <Button className="w-full" variant="secondary">Open your bid</Button>
              </Link>
            ) : (
              <>
                <p className="text-sm text-ink-700">Submitted bids are evaluated and reviewed by the procurement officer.</p>
                <Button className="mt-4 w-full" onClick={() => startBid.mutate()} loading={startBid.isPending} disabled={closed}>
                  <Send className="size-4" /> {closed ? "Bidding closed" : "Start official bid"}
                </Button>
                {!conflict && <FormError error={startBid.error} />}
                {conflict && <p className="mt-2 text-sm text-warn">You already have a bid on this tender — find it under My bids.</p>}
              </>
            )}
          </Card>
        </div>
      </div>
    </>
  );
}
