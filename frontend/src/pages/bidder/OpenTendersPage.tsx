import { useQuery } from "@tanstack/react-query";
import { CalendarClock, FileSearch, Search } from "lucide-react";
import { useState } from "react";
import { Link } from "react-router-dom";
import { tendersApi } from "../../api/endpoints";
import { Card, EmptyState, ErrorState, PageHeader, Spinner } from "../../components/ui";
import { formatDate, formatINRShort } from "../../lib/format";

export function OpenTendersPage() {
  const [search, setSearch] = useState("");
  const tenders = useQuery({ queryKey: ["tenders", "open", search], queryFn: () => tendersApi.list({ search, pageSize: 50 }) });

  return (
    <>
      <PageHeader title="Open tenders" subtitle="Check your documents against a tender with a private simulation before you bid." />
      <div className="relative mb-5 max-w-md">
        <Search className="pointer-events-none absolute left-3 top-2.5 size-4 text-ink-700/50" aria-hidden />
        <input
          className="w-full rounded-lg border border-ink-100 bg-white py-2 pl-9 pr-3 text-sm focus:border-saffron-500 focus:outline-none"
          placeholder="Search tenders"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          aria-label="Search tenders"
        />
      </div>
      {tenders.isLoading && <Spinner />}
      {tenders.error && <ErrorState error={tenders.error} onRetry={() => tenders.refetch()} />}
      {tenders.data?.items.length === 0 && (
        <Card><EmptyState icon={<FileSearch className="size-10" />} title="No open tenders">Published tenders will appear here.</EmptyState></Card>
      )}
      <div className="grid gap-4 md:grid-cols-2">
        {tenders.data?.items.map((t) => {
          const daysLeft = Math.ceil((new Date(t.bidEndDate).getTime() - Date.now()) / 86_400_000);
          return (
            <Link key={t.id} to={`/tenders/${t.id}`} className="group rounded-xl border border-ink-100 bg-white p-5 shadow-sm transition hover:border-saffron-500 hover:shadow">
              <div className="font-mono text-xs text-ink-700">{t.gemBidNumber}</div>
              <div className="mt-1 text-lg font-semibold text-ink-900 group-hover:text-saffron-600">{t.title}</div>
              <div className="text-sm text-ink-700">{t.category}</div>
              <div className="mt-4 flex flex-wrap items-center gap-x-5 gap-y-1 text-sm">
                <span>Value <strong>{formatINRShort(t.estimatedValue)}</strong></span>
                <span>EMD <strong>{t.emdAmount ? formatINRShort(t.emdAmount) : "None"}</strong></span>
                <span className="inline-flex items-center gap-1 text-ink-700">
                  <CalendarClock className="size-4" /> {formatDate(t.bidEndDate)} {daysLeft > 0 && `· ${daysLeft} days left`}
                </span>
              </div>
            </Link>
          );
        })}
      </div>
    </>
  );
}
