import { CalendarClock, FileText, IndianRupee, Landmark } from "lucide-react";
import type { ReactNode } from "react";
import type { TenderDto } from "../api/types";
import { documentsApi } from "../api/endpoints";
import { formatDate, formatINRShort } from "../lib/format";
import { TenderStatusBadge } from "./StatusBadges";

function Fact({ icon, label, children }: { icon: ReactNode; label: string; children: ReactNode }) {
  return (
    <div className="flex items-start gap-3">
      <span className="mt-0.5 text-saffron-500">{icon}</span>
      <div>
        <div className="text-xs text-ink-700">{label}</div>
        <div className="text-sm font-medium text-ink-900">{children}</div>
      </div>
    </div>
  );
}

export function TenderFacts({ tender }: { tender: TenderDto }) {
  const daysLeft = Math.ceil((new Date(tender.bidEndDate).getTime() - Date.now()) / 86_400_000);
  return (
    <div className="grid gap-4 rounded-xl border border-ink-100 bg-white p-5 shadow-sm sm:grid-cols-2 lg:grid-cols-4">
      <Fact icon={<IndianRupee className="size-4" />} label="Estimated value">{formatINRShort(tender.estimatedValue)}</Fact>
      <Fact icon={<Landmark className="size-4" />} label="EMD">{tender.emdAmount > 0 ? formatINRShort(tender.emdAmount) : "None"}</Fact>
      <Fact icon={<CalendarClock className="size-4" />} label="Bid end date">
        {formatDate(tender.bidEndDate)}
        <span className="ml-1 text-xs font-normal text-ink-700">{daysLeft > 0 ? `(${daysLeft} days left)` : "(closed)"}</span>
      </Fact>
      <Fact icon={<FileText className="size-4" />} label="Status">
        <div className="flex items-center gap-2">
          <TenderStatusBadge status={tender.status} />
          {tender.tenderDocumentId && (
            <button className="text-xs font-medium text-saffron-600 hover:underline" onClick={() => documentsApi.open(tender.tenderDocumentId!)}>
              View PDF
            </button>
          )}
        </div>
      </Fact>
    </div>
  );
}
