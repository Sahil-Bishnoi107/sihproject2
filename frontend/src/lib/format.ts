import type { RuleExpression } from "../api/types";

const inr = new Intl.NumberFormat("en-IN", { maximumFractionDigits: 0 });
const inrDecimal = new Intl.NumberFormat("en-IN", { maximumFractionDigits: 2 });

export const formatINR = (value: number) => `₹${inr.format(value)}`;

/** ₹50 lakh, ₹2.5 crore - how Indian procurement documents state amounts. */
export function formatINRShort(value: number): string {
  if (value >= 10_000_000) return `₹${inrDecimal.format(value / 10_000_000)} crore`;
  if (value >= 100_000) return `₹${inrDecimal.format(value / 100_000)} lakh`;
  return formatINR(value);
}

const dateFmt = new Intl.DateTimeFormat("en-IN", { day: "numeric", month: "short", year: "numeric" });
const dateTimeFmt = new Intl.DateTimeFormat("en-IN", {
  day: "numeric", month: "short", year: "numeric", hour: "2-digit", minute: "2-digit",
});

export const formatDate = (iso: string | null | undefined) => (iso ? dateFmt.format(new Date(iso)) : "—");
export const formatDateTime = (iso: string | null | undefined) => (iso ? dateTimeFmt.format(new Date(iso)) : "—");

export function timeAgo(iso: string): string {
  const seconds = Math.round((Date.now() - new Date(iso).getTime()) / 1000);
  if (seconds < 60) return "just now";
  const minutes = Math.round(seconds / 60);
  if (minutes < 60) return `${minutes} min ago`;
  const hours = Math.round(minutes / 60);
  if (hours < 24) return `${hours} h ago`;
  return formatDate(iso);
}

export function daysUntil(isoDate: string): number {
  const target = new Date(`${isoDate.slice(0, 10)}T00:00:00`);
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  return Math.round((target.getTime() - today.getTime()) / 86_400_000);
}

export const formatBytes = (bytes: number) =>
  bytes < 1024 ? `${bytes} B` : bytes < 1_048_576 ? `${(bytes / 1024).toFixed(0)} KB` : `${(bytes / 1_048_576).toFixed(1)} MB`;

const OP_TEXT: Record<string, string> = {
  "==": "=", "!=": "≠", ">=": "≥", "<=": "≤", ">": ">", "<": "<", before: "before", after: "after", in: "is one of",
};

const VARIABLES: Record<string, string> = {
  $bidEndDate: "bid end date", $estimatedValue: "estimated value", $emdAmount: "EMD amount",
};

function valueText(value: unknown): string {
  if (typeof value === "number") return inr.format(value);
  if (typeof value === "string") return VARIABLES[value] ?? value;
  if (Array.isArray(value)) return `[${value.map(valueText).join(", ")}]`;
  return JSON.stringify(value);
}

/** Human-readable form of a Compliance-as-Code expression. */
export function expressionToText(expr: RuleExpression): string {
  if ("all" in expr) return expr.all.map(expressionToText).join(" AND ");
  if ("any" in expr) return expr.any.map(expressionToText).join(" OR ");
  if ("not" in expr) return `NOT (${expressionToText(expr.not)})`;
  if (expr.op === "exists") return `${expr.field} is present`;
  if (expr.op === "verified") return `${expr.field} verified with ${expr.source}`;
  return `${expr.field} ${OP_TEXT[expr.op] ?? expr.op} ${valueText(expr.value)}`;
}

export const shortId = (id: string) => id.slice(0, 8);
