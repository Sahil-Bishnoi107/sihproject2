import { BadgeCheck, FileSearch, ScrollText } from "lucide-react";
import type { ReactNode } from "react";
import { Logo } from "../../components/layout/AppShell";

const POINTS = [
  { icon: FileSearch, title: "Compliance-as-Code", text: "Tender clauses become machine-checkable rules that officers approve." },
  { icon: BadgeCheck, title: "Explainable verdicts", text: "Every decision shows the rule, the value found and the page it came from." },
  { icon: ScrollText, title: "Tamper-evident audit", text: "A SHA-256 hash chain records every step from upload to decision." },
];

export function AuthLayout({ children }: { children: ReactNode }) {
  return (
    <div className="grid min-h-full lg:grid-cols-2">
      <div className="hidden flex-col justify-between bg-ink-900 p-12 text-white lg:flex">
        <Logo />
        <div>
          <h1 className="max-w-md text-4xl font-bold leading-tight">
            Smarter compliance. <span className="text-saffron-500">Stronger bids.</span>
          </h1>
          <p className="mt-4 max-w-md text-ink-100/80">
            AI reads tenders and bidder documents; a deterministic rule engine decides; the procurement officer has the final word.
          </p>
          <ul className="mt-10 space-y-5">
            {POINTS.map(({ icon: Icon, title, text }) => (
              <li key={title} className="flex gap-4">
                <span className="flex size-10 shrink-0 items-center justify-center rounded bg-white/10 text-saffron-500">
                  <Icon className="size-5" aria-hidden />
                </span>
                <div>
                  <div className="font-semibold">{title}</div>
                  <div className="text-sm text-ink-100/70">{text}</div>
                </div>
              </li>
            ))}
          </ul>
        </div>
        <div className="text-xs text-ink-100/50">Smart India Hackathon 2026 · PS SIH26100 · Team Innoverse</div>
      </div>
      <div className="flex items-center justify-center bg-white px-6 py-12">
        <div className="w-full max-w-md">
          <div className="mb-8 lg:hidden">
            <Logo dark />
          </div>
          {children}
        </div>
      </div>
    </div>
  );
}
