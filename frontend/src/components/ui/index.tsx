import { Loader2, X } from "lucide-react";
import {
  useEffect, useId, type ButtonHTMLAttributes, type InputHTMLAttributes, type ReactNode, type SelectHTMLAttributes,
  type TextareaHTMLAttributes,
} from "react";
import { ApiError } from "../../api/client";

const cx = (...classes: (string | false | null | undefined)[]) => classes.filter(Boolean).join(" ");
export { cx };

type Variant = "primary" | "secondary" | "accent" | "danger" | "ghost";

const VARIANTS: Record<Variant, string> = {
  primary: "bg-ink-900 text-white hover:bg-ink-800 disabled:bg-ink-700/60",
  accent: "bg-saffron-500 text-white hover:bg-saffron-600 disabled:bg-saffron-500/60",
  secondary: "bg-white text-ink-900 border border-ink-100 hover:bg-ink-50 disabled:text-ink-700/50",
  danger: "bg-white text-fail border border-fail/30 hover:bg-fail-bg disabled:opacity-50",
  ghost: "text-ink-700 hover:bg-ink-100 disabled:opacity-50",
};

export function Button({
  variant = "primary", size = "md", loading, children, className, disabled, ...rest
}: ButtonHTMLAttributes<HTMLButtonElement> & { variant?: Variant; size?: "sm" | "md"; loading?: boolean }) {
  return (
    <button
      {...rest}
      disabled={disabled || loading}
      className={cx(
        "inline-flex items-center justify-center gap-2 rounded-lg font-medium transition-colors disabled:cursor-not-allowed",
        size === "sm" ? "h-8 px-3 text-sm" : "h-10 px-4 text-sm",
        VARIANTS[variant],
        className,
      )}
    >
      {loading && <Loader2 className="size-4 animate-spin" aria-hidden />}
      {children}
    </button>
  );
}

export function Card({ title, actions, children, className, padded = true }: {
  title?: ReactNode; actions?: ReactNode; children: ReactNode; className?: string; padded?: boolean;
}) {
  return (
    <section className={cx("rounded-xl border border-ink-100 bg-white shadow-sm", className)}>
      {(title || actions) && (
        <header className="flex flex-wrap items-center justify-between gap-3 border-b border-ink-100 px-5 py-3.5">
          {typeof title === "string" ? <h2 className="text-base font-semibold text-ink-900">{title}</h2> : title}
          {actions && <div className="flex flex-wrap items-center gap-2">{actions}</div>}
        </header>
      )}
      <div className={padded ? "p-5" : ""}>{children}</div>
    </section>
  );
}

export function PageHeader({ title, subtitle, actions, eyebrow }: {
  title: ReactNode; subtitle?: ReactNode; actions?: ReactNode; eyebrow?: ReactNode;
}) {
  return (
    <div className="mb-6 flex flex-wrap items-end justify-between gap-4">
      <div className="min-w-0">
        {eyebrow && <div className="mb-1 text-xs font-semibold uppercase tracking-wider text-saffron-600">{eyebrow}</div>}
        <h1 className="text-2xl font-bold tracking-tight text-ink-900">{title}</h1>
        {subtitle && <p className="mt-1 text-sm text-ink-700">{subtitle}</p>}
      </div>
      {actions && <div className="flex flex-wrap items-center gap-2">{actions}</div>}
    </div>
  );
}

type Tone = "neutral" | "pass" | "fail" | "warn" | "info" | "accent" | "ink";
const TONES: Record<Tone, string> = {
  neutral: "bg-ink-50 text-ink-700 ring-ink-100",
  pass: "bg-pass-bg text-pass ring-pass/20",
  fail: "bg-fail-bg text-fail ring-fail/20",
  warn: "bg-warn-bg text-warn ring-warn/20",
  info: "bg-info-bg text-info ring-info/20",
  accent: "bg-saffron-50 text-saffron-600 ring-saffron-500/30",
  ink: "bg-ink-900 text-white ring-ink-900",
};

export function Badge({ tone = "neutral", children, icon }: { tone?: Tone; children: ReactNode; icon?: ReactNode }) {
  return (
    <span className={cx("inline-flex items-center gap-1 whitespace-nowrap rounded-full px-2 py-0.5 text-xs font-medium ring-1 ring-inset", TONES[tone])}>
      {icon}
      {children}
    </span>
  );
}

export function Spinner({ label = "Loading…" }: { label?: string }) {
  return (
    <div className="flex items-center justify-center gap-2 py-10 text-sm text-ink-700" role="status">
      <Loader2 className="size-5 animate-spin text-saffron-500" aria-hidden />
      {label}
    </div>
  );
}

export function EmptyState({ icon, title, children }: { icon?: ReactNode; title: string; children?: ReactNode }) {
  return (
    <div className="flex flex-col items-center justify-center gap-2 px-6 py-10 text-center">
      {icon && <div className="mb-1 text-ink-700/40">{icon}</div>}
      <p className="font-medium text-ink-900">{title}</p>
      {children && <div className="max-w-md text-sm text-ink-700">{children}</div>}
    </div>
  );
}

export function errorMessage(error: unknown): string {
  if (error instanceof ApiError) return error.message;
  if (error instanceof Error) return error.message;
  return "Something went wrong.";
}

export function ErrorState({ error, onRetry }: { error: unknown; onRetry?: () => void }) {
  return (
    <div className="rounded-lg border border-fail/20 bg-fail-bg px-4 py-3 text-sm text-fail" role="alert">
      <span>{errorMessage(error)}</span>
      {onRetry && (
        <button className="ml-3 font-medium underline" onClick={onRetry}>
          Try again
        </button>
      )}
    </div>
  );
}

export function FormError({ error }: { error: unknown }) {
  if (!error) return null;
  return (
    <p className="rounded-lg bg-fail-bg px-3 py-2 text-sm text-fail" role="alert">
      {errorMessage(error)}
    </p>
  );
}

/** Field-level message for a ProblemDetails `errors` entry (keys are PascalCase property names). */
export function fieldError(error: unknown, field: string): string | undefined {
  if (!(error instanceof ApiError)) return undefined;
  const match = Object.entries(error.fieldErrors).find(([k]) => k.toLowerCase() === field.toLowerCase());
  return match?.[1][0];
}

function FieldShell({ label, hint, error, id, children }: { label: string; hint?: ReactNode; error?: string; id: string; children: ReactNode }) {
  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor={id} className="text-sm font-medium text-ink-900">
        {label}
      </label>
      {children}
      {error ? <p className="text-xs text-fail">{error}</p> : hint ? <p className="text-xs text-ink-700">{hint}</p> : null}
    </div>
  );
}

const inputClass =
  "w-full rounded-lg border border-ink-100 bg-white px-3 py-2 text-sm text-ink-900 placeholder:text-ink-700/40 focus:border-saffron-500 focus:outline-none focus:ring-2 focus:ring-saffron-500/20 disabled:bg-ink-50";

export function TextField({ label, hint, error, ...rest }: InputHTMLAttributes<HTMLInputElement> & { label: string; hint?: ReactNode; error?: string }) {
  const id = useId();
  return (
    <FieldShell label={label} hint={hint} error={error} id={id}>
      <input id={id} {...rest} className={cx(inputClass, error && "border-fail")} aria-invalid={!!error} />
    </FieldShell>
  );
}

export function SelectField({ label, hint, error, children, ...rest }: SelectHTMLAttributes<HTMLSelectElement> & { label: string; hint?: ReactNode; error?: string }) {
  const id = useId();
  return (
    <FieldShell label={label} hint={hint} error={error} id={id}>
      <select id={id} {...rest} className={cx(inputClass, error && "border-fail")} aria-invalid={!!error}>
        {children}
      </select>
    </FieldShell>
  );
}

export function TextAreaField({ label, hint, error, ...rest }: TextareaHTMLAttributes<HTMLTextAreaElement> & { label: string; hint?: ReactNode; error?: string }) {
  const id = useId();
  return (
    <FieldShell label={label} hint={hint} error={error} id={id}>
      <textarea id={id} {...rest} className={cx(inputClass, "font-mono text-xs", error && "border-fail")} aria-invalid={!!error} />
    </FieldShell>
  );
}

export function Checkbox({ label, ...rest }: InputHTMLAttributes<HTMLInputElement> & { label: string }) {
  const id = useId();
  return (
    <label htmlFor={id} className="inline-flex items-center gap-2 text-sm text-ink-900">
      <input id={id} type="checkbox" {...rest} className="size-4 rounded border-ink-100 accent-saffron-500" />
      {label}
    </label>
  );
}

export function Modal({ open, onClose, title, children, footer, wide }: {
  open: boolean; onClose: () => void; title: string; children: ReactNode; footer?: ReactNode; wide?: boolean;
}) {
  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => e.key === "Escape" && onClose();
    document.addEventListener("keydown", onKey);
    return () => document.removeEventListener("keydown", onKey);
  }, [open, onClose]);

  if (!open) return null;
  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-ink-950/50 p-4 sm:p-8" onMouseDown={onClose}>
      <div
        role="dialog"
        aria-modal="true"
        aria-label={title}
        className={cx("w-full rounded-xl bg-white shadow-xl", wide ? "max-w-4xl" : "max-w-lg")}
        onMouseDown={(e) => e.stopPropagation()}
      >
        <header className="flex items-center justify-between border-b border-ink-100 px-5 py-3.5">
          <h2 className="text-base font-semibold">{title}</h2>
          <button className="rounded p-1 text-ink-700 hover:bg-ink-50" onClick={onClose} aria-label="Close">
            <X className="size-5" />
          </button>
        </header>
        <div className="max-h-[75vh] overflow-y-auto p-5">{children}</div>
        {footer && <footer className="flex justify-end gap-2 border-t border-ink-100 px-5 py-3">{footer}</footer>}
      </div>
    </div>
  );
}

export function Stat({ label, value, hint, icon }: { label: string; value: ReactNode; hint?: ReactNode; icon?: ReactNode }) {
  return (
    <div className="rounded-xl border border-ink-100 bg-white p-5 shadow-sm">
      <div className="flex items-center justify-between text-sm text-ink-700">
        {label}
        {icon && <span className="text-saffron-500">{icon}</span>}
      </div>
      <div className="mt-2 text-3xl font-bold tracking-tight text-ink-900">{value}</div>
      {hint && <div className="mt-1 text-xs text-ink-700">{hint}</div>}
    </div>
  );
}

export function Table({ head, children }: { head: ReactNode[]; children: ReactNode }) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full text-left text-sm">
        <thead className="border-b border-ink-100 bg-ink-50/60 text-xs uppercase tracking-wide text-ink-700">
          <tr>
            {head.map((h, i) => (
              <th key={i} className="px-4 py-2.5 font-medium">
                {h}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-ink-100">{children}</tbody>
      </table>
    </div>
  );
}
