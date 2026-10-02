import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useEffect, useMemo, useState, type FormEvent } from "react";
import { tendersApi } from "../../api/endpoints";
import type { ExemptionKind, RuleCategory, RuleDto, RuleExpression, RuleInput } from "../../api/types";
import { Button, Checkbox, fieldError, FormError, Modal, SelectField, TextAreaField, TextField } from "../../components/ui";
import { expressionToText } from "../../lib/format";

const EXAMPLE = `{
  "all": [
    { "field": "gstin", "op": "exists" },
    { "field": "gstin", "op": "verified", "source": "GST" }
  ]
}`;

const KEYS = "gstin, pan, cin, udyam, epfo, turnover.avg3y, turnover.fy<YYYY>, experience.years, iso9001.number, iso9001.expiry, oem.authorization, emd.paid, org.category, org.isStartup";

function parseExpression(text: string): { value?: RuleExpression; error?: string } {
  try {
    const value = JSON.parse(text);
    if (typeof value !== "object" || value === null || Array.isArray(value)) return { error: "Expression must be a JSON object." };
    return { value };
  } catch (e) {
    return { error: (e as Error).message };
  }
}

export function RuleEditorModal({ tenderId, rule, open, onClose }: {
  tenderId: string; rule: RuleDto | null; open: boolean; onClose: () => void;
}) {
  const queryClient = useQueryClient();
  const [form, setForm] = useState({
    code: "", title: "", category: "Eligibility" as RuleCategory, isMandatory: true, weight: "1",
    sourceClause: "", sourcePage: "", exemptionKind: "" as ExemptionKind | "", expression: EXAMPLE,
  });

  useEffect(() => {
    if (!open) return;
    setForm(
      rule
        ? {
            code: rule.code, title: rule.title, category: rule.category, isMandatory: rule.isMandatory, weight: String(rule.weight),
            sourceClause: rule.sourceClause ?? "", sourcePage: rule.sourcePage ? String(rule.sourcePage) : "",
            exemptionKind: rule.exemptionKind ?? "", expression: JSON.stringify(rule.expression, null, 2),
          }
        : { code: "", title: "", category: "Eligibility", isMandatory: true, weight: "1", sourceClause: "", sourcePage: "", exemptionKind: "", expression: EXAMPLE },
    );
  }, [open, rule]);

  const parsed = useMemo(() => parseExpression(form.expression), [form.expression]);
  const set = (k: keyof typeof form) => (e: { target: { value: string } }) => setForm({ ...form, [k]: e.target.value });

  const save = useMutation({
    mutationFn: () => {
      const input: RuleInput = {
        code: form.code.trim().toUpperCase(),
        title: form.title.trim(),
        category: form.category,
        isMandatory: form.isMandatory,
        weight: Number(form.weight),
        expression: parsed.value!,
        sourceClause: form.sourceClause.trim() || null,
        sourcePage: form.sourcePage ? Number(form.sourcePage) : null,
        exemptionKind: form.exemptionKind || null,
      };
      return rule ? tendersApi.updateRule(tenderId, rule.id, input) : tendersApi.createRule(tenderId, input);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["tender", tenderId] });
      onClose();
    },
  });

  const submit = (e: FormEvent) => {
    e.preventDefault();
    if (parsed.value) save.mutate();
  };
  const err = (f: string) => fieldError(save.error, f);

  return (
    <Modal
      open={open}
      onClose={onClose}
      wide
      title={rule ? `Edit rule ${rule.code}` : "Add rule"}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Cancel</Button>
          <Button type="submit" form="rule-form" loading={save.isPending} disabled={!parsed.value}>
            {rule ? "Save changes" : "Add rule"}
          </Button>
        </>
      }
    >
      <form id="rule-form" className="grid gap-4 md:grid-cols-2" onSubmit={submit}>
        <div className="space-y-4">
          <div className="grid grid-cols-3 gap-3">
            <TextField label="Code" required value={form.code} onChange={set("code")} placeholder="FIN-TURNOVER" error={err("Code")} />
            <div className="col-span-2">
              <TextField label="Title" required value={form.title} onChange={set("title")} error={err("Title")} />
            </div>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <SelectField label="Category" value={form.category} onChange={set("category")}>
              {["Eligibility", "Technical", "Financial", "Document"].map((c) => <option key={c}>{c}</option>)}
            </SelectField>
            <TextField label="Weight" type="number" min={0.5} step={0.5} required value={form.weight} onChange={set("weight")} error={err("Weight")} />
          </div>
          <SelectField label="Statutory exemption" value={form.exemptionKind} onChange={set("exemptionKind")} hint="Relaxation applied automatically for eligible MSEs / DPIIT startups.">
            <option value="">None</option>
            <option value="Emd">EMD (MSE with Udyam, DPIIT startup)</option>
            <option value="PriorExperienceTurnover">Prior experience / turnover (DPIIT startup)</option>
          </SelectField>
          <Checkbox label="Mandatory — failing it makes the bid non-compliant" checked={form.isMandatory} onChange={(e) => setForm({ ...form, isMandatory: e.target.checked })} />
          <TextAreaField label="Source clause" rows={3} value={form.sourceClause} onChange={set("sourceClause")} error={err("SourceClause")} />
          <TextField label="Source page" type="number" min={1} value={form.sourcePage} onChange={set("sourcePage")} />
        </div>
        <div className="space-y-3">
          <TextAreaField
            label="Rule expression (JSON)"
            rows={14}
            spellCheck={false}
            value={form.expression}
            onChange={set("expression")}
            error={parsed.error ?? err("Expression")}
            hint={<>Ops: exists, ==, !=, &gt;=, &lt;=, &gt;, &lt;, in, before, after, verified · combine with all / any / not</>}
          />
          {parsed.value && (
            <div className="rounded-lg bg-ink-50 p-3 text-sm">
              <div className="text-xs font-semibold uppercase tracking-wide text-ink-700">Reads as</div>
              <div className="mt-1 text-ink-900">{expressionToText(parsed.value)}</div>
            </div>
          )}
          <p className="text-xs text-ink-700">Field keys: {KEYS}. Tender variables: $bidEndDate, $estimatedValue, $emdAmount.</p>
        </div>
        <div className="md:col-span-2">
          <FormError error={save.error} />
        </div>
      </form>
    </Modal>
  );
}
