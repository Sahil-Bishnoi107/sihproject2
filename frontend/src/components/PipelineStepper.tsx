import { Check, Loader2, X } from "lucide-react";
import type { BidStatus } from "../api/types";
import { cx } from "./ui";

const STEPS: { status: BidStatus; label: string; detail: string }[] = [
  { status: "Uploaded", label: "Documents", detail: "Upload and submit" },
  { status: "Extracting", label: "AI extraction", detail: "OCR + entity extraction" },
  { status: "Verifying", label: "Registry checks", detail: "GST · MCA · Udyam · EPFO" },
  { status: "Evaluating", label: "Rule engine", detail: "Exemptions + scoring" },
  { status: "UnderReview", label: "Report ready", detail: "Verdicts with evidence" },
];

const ORDER: BidStatus[] = ["Uploaded", "Extracting", "Verifying", "Evaluating", "UnderReview", "Decided"];

export function PipelineStepper({ status, failed }: { status: BidStatus; failed?: boolean }) {
  const current = ORDER.indexOf(status === "Failed" ? "Uploaded" : status);
  return (
    <ol className="grid gap-3 sm:grid-cols-5" aria-label="Processing progress">
      {STEPS.map((step, i) => {
        const done = current > i || (status === "Decided" && i === 4) || (status === "UnderReview" && i === 4);
        const active = current === i && !done && status !== "Uploaded";
        return (
          <li key={step.status} className="flex items-start gap-3 sm:flex-col sm:gap-2">
            <span
              className={cx(
                "flex size-8 shrink-0 items-center justify-center rounded-full border-2 text-sm font-semibold",
                done && "border-pass bg-pass text-white",
                active && "border-saffron-500 text-saffron-500",
                !done && !active && "border-ink-100 text-ink-700",
                failed && i === 0 && "border-fail bg-fail text-white",
              )}
            >
              {failed && i === 0 ? <X className="size-4" /> : done ? <Check className="size-4" /> : active ? <Loader2 className="size-4 animate-spin" /> : i + 1}
            </span>
            <div>
              <div className={cx("text-sm font-medium", (done || active) ? "text-ink-900" : "text-ink-700")}>{step.label}</div>
              <div className="text-xs text-ink-700">{step.detail}</div>
            </div>
          </li>
        );
      })}
    </ol>
  );
}
