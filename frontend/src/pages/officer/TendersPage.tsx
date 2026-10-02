import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ClipboardList, Plus, Search } from "lucide-react";
import { useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { tendersApi } from "../../api/endpoints";
import type { TenderStatus } from "../../api/types";
import { TenderStatusBadge } from "../../components/StatusBadges";
import {
  Button, Card, EmptyState, ErrorState, fieldError, FormError, Modal, PageHeader, Spinner, Table, TextField,
} from "../../components/ui";
import { formatDate, formatINRShort } from "../../lib/format";

const STATUSES: (TenderStatus | "")[] = ["", "Draft", "Extracting", "RulesExtracted", "Published", "Closed"];

export function TendersPage() {
  const [status, setStatus] = useState<TenderStatus | "">("");
  const [search, setSearch] = useState("");
  const [creating, setCreating] = useState(false);

  const tenders = useQuery({
    queryKey: ["tenders", { status, search }],
    queryFn: () => tendersApi.list({ status, search, pageSize: 50 }),
  });

  return (
    <>
      <PageHeader
        title="Tenders"
        subtitle="Upload a tender PDF, review the AI-drafted rules, then publish it to bidders."
        actions={
          <Button onClick={() => setCreating(true)}>
            <Plus className="size-4" /> New tender
          </Button>
        }
      />
      <Card padded={false}>
        <div className="flex flex-wrap gap-3 border-b border-ink-100 p-4">
          <div className="relative min-w-60 flex-1">
            <Search className="pointer-events-none absolute left-3 top-2.5 size-4 text-ink-700/50" aria-hidden />
            <input
              className="w-full rounded-lg border border-ink-100 py-2 pl-9 pr-3 text-sm focus:border-saffron-500 focus:outline-none"
              placeholder="Search by title or GeM bid number"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              aria-label="Search tenders"
            />
          </div>
          <select
            className="rounded-lg border border-ink-100 px-3 py-2 text-sm"
            value={status}
            onChange={(e) => setStatus(e.target.value as TenderStatus | "")}
            aria-label="Filter by status"
          >
            {STATUSES.map((s) => (
              <option key={s} value={s}>
                {s === "" ? "All statuses" : s === "RulesExtracted" ? "Rules in review" : s}
              </option>
            ))}
          </select>
        </div>
        {tenders.isLoading && <Spinner />}
        {tenders.error && <div className="p-4"><ErrorState error={tenders.error} onRetry={() => tenders.refetch()} /></div>}
        {tenders.data && tenders.data.items.length === 0 && (
          <EmptyState icon={<ClipboardList className="size-10" />} title="No tenders yet">
            Create a tender and upload its PDF to let the AI draft the compliance rules.
          </EmptyState>
        )}
        {tenders.data && tenders.data.items.length > 0 && (
          <Table head={["GeM bid number", "Title", "Value", "Bid end", "Status"]}>
            {tenders.data.items.map((t) => (
              <tr key={t.id} className="hover:bg-ink-50/60">
                <td className="px-4 py-3 font-mono text-xs">
                  <Link to={`/tenders/${t.id}`} className="font-medium text-ink-900 hover:text-saffron-600">
                    {t.gemBidNumber}
                  </Link>
                </td>
                <td className="px-4 py-3">
                  <Link to={`/tenders/${t.id}`} className="hover:text-saffron-600">
                    {t.title}
                  </Link>
                  <div className="text-xs text-ink-700">{t.category}</div>
                </td>
                <td className="px-4 py-3">{formatINRShort(t.estimatedValue)}</td>
                <td className="px-4 py-3">{formatDate(t.bidEndDate)}</td>
                <td className="px-4 py-3">
                  <TenderStatusBadge status={t.status} />
                </td>
              </tr>
            ))}
          </Table>
        )}
      </Card>
      <CreateTenderModal open={creating} onClose={() => setCreating(false)} />
    </>
  );
}

function CreateTenderModal({ open, onClose }: { open: boolean; onClose: () => void }) {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const defaultEnd = new Date(Date.now() + 21 * 86_400_000).toISOString().slice(0, 10);
  const [form, setForm] = useState({
    gemBidNumber: `GEM/${new Date().getFullYear()}/B/`, title: "", category: "", estimatedValue: "", emdAmount: "", bidEndDate: defaultEnd,
  });
  const set = (k: keyof typeof form) => (e: { target: { value: string } }) => setForm({ ...form, [k]: e.target.value });

  const create = useMutation({
    mutationFn: () =>
      tendersApi.create({
        gemBidNumber: form.gemBidNumber.trim().toUpperCase(),
        title: form.title,
        category: form.category,
        estimatedValue: Number(form.estimatedValue),
        emdAmount: Number(form.emdAmount || 0),
        bidEndDate: new Date(`${form.bidEndDate}T23:59:00`).toISOString(),
      }),
    onSuccess: (tender) => {
      queryClient.invalidateQueries({ queryKey: ["tenders"] });
      onClose();
      navigate(`/tenders/${tender.id}`);
    },
  });

  const submit = (e: FormEvent) => {
    e.preventDefault();
    create.mutate();
  };
  const err = (f: string) => fieldError(create.error, f);

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="New tender"
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Cancel</Button>
          <Button type="submit" form="create-tender" loading={create.isPending}>Create tender</Button>
        </>
      }
    >
      <form id="create-tender" className="space-y-4" onSubmit={submit}>
        <TextField label="GeM bid number" required value={form.gemBidNumber} onChange={set("gemBidNumber")} hint="Format GEM/2026/B/1234567" error={err("GemBidNumber")} />
        <TextField label="Title" required value={form.title} onChange={set("title")} placeholder="Supply of 200 laptops" error={err("Title")} />
        <TextField label="Category" required value={form.category} onChange={set("category")} placeholder="IT Hardware" error={err("Category")} />
        <div className="grid gap-4 sm:grid-cols-2">
          <TextField label="Estimated value (₹)" type="number" min={1} required value={form.estimatedValue} onChange={set("estimatedValue")} error={err("EstimatedValue")} />
          <TextField label="EMD amount (₹)" type="number" min={0} value={form.emdAmount} onChange={set("emdAmount")} error={err("EmdAmount")} />
        </div>
        <TextField label="Bid end date" type="date" required value={form.bidEndDate} onChange={set("bidEndDate")} error={err("BidEndDate")} />
        <FormError error={create.error} />
      </form>
    </Modal>
  );
}
