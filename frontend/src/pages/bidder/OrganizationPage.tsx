import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Plus, Wallet } from "lucide-react";
import { useEffect, useState, type FormEvent } from "react";
import { organizationApi } from "../../api/endpoints";
import type { CredentialType, OrganizationCategory } from "../../api/types";
import {
  Badge, Button, Card, Checkbox, EmptyState, ErrorState, fieldError, FormError, PageHeader, SelectField, Spinner, Table, TextField,
} from "../../components/ui";
import { daysUntil, formatDate } from "../../lib/format";

const CREDENTIAL_TYPES: CredentialType[] = ["GST", "Udyam", "PAN", "EPFO", "ISO9001", "OemAuthorization", "ExperienceCertificate", "DPIIT", "Other"];

export function OrganizationPage() {
  const queryClient = useQueryClient();
  const org = useQuery({ queryKey: ["organization"], queryFn: organizationApi.get });
  const [form, setForm] = useState({
    legalName: "", category: "MSE" as OrganizationCategory, gstin: "", udyamNumber: "", cin: "", epfoCode: "", isStartup: false, dpiitNumber: "",
  });

  useEffect(() => {
    if (!org.data) return;
    const o = org.data;
    setForm({
      legalName: o.legalName, category: o.category, gstin: o.gstin ?? "", udyamNumber: o.udyamNumber ?? "", cin: o.cin ?? "",
      epfoCode: o.epfoCode ?? "", isStartup: o.isStartup, dpiitNumber: o.dpiitNumber ?? "",
    });
  }, [org.data]);

  const save = useMutation({
    mutationFn: () =>
      organizationApi.update({
        legalName: form.legalName, category: form.category, isStartup: form.isStartup,
        gstin: form.gstin || null, udyamNumber: form.udyamNumber || null, cin: form.cin || null,
        epfoCode: form.epfoCode || null, dpiitNumber: form.dpiitNumber || null,
      }),
    onSuccess: (data) => queryClient.setQueryData(["organization"], data),
  });

  if (org.isLoading) return <Spinner />;
  if (org.error || !org.data) return <ErrorState error={org.error} />;

  const set = (k: keyof typeof form) => (e: { target: { value: string } }) => setForm({ ...form, [k]: e.target.value });
  const err = (f: string) => fieldError(save.error, f);
  const submit = (e: FormEvent) => {
    e.preventDefault();
    save.mutate();
  };

  return (
    <>
      <PageHeader title="Organization" subtitle={`PAN ${org.data.pan}`} />
      <div className="grid gap-6 lg:grid-cols-2">
        <Card title="Registered profile">
          <form className="space-y-4" onSubmit={submit}>
            <TextField label="Legal name" required value={form.legalName} onChange={set("legalName")} error={err("LegalName")} />
            <SelectField label="Enterprise category" value={form.category} onChange={set("category")}>
              <option value="MSE">Micro / Small (MSE)</option>
              <option value="Medium">Medium</option>
              <option value="Large">Large</option>
            </SelectField>
            <div className="grid gap-4 sm:grid-cols-2">
              <TextField label="GSTIN" value={form.gstin} onChange={set("gstin")} error={err("Gstin")} />
              <TextField label="Udyam number" value={form.udyamNumber} onChange={set("udyamNumber")} placeholder="UDYAM-MH-26-0012345" error={err("UdyamNumber")} />
              <TextField label="CIN" value={form.cin} onChange={set("cin")} error={err("Cin")} />
              <TextField label="EPFO code" value={form.epfoCode} onChange={set("epfoCode")} error={err("EpfoCode")} />
            </div>
            <Checkbox label="DPIIT-recognised startup" checked={form.isStartup} onChange={(e) => setForm({ ...form, isStartup: e.target.checked })} />
            {form.isStartup && <TextField label="DPIIT recognition number" value={form.dpiitNumber} onChange={set("dpiitNumber")} error={err("DpiitNumber")} />}
            <p className="text-xs text-ink-700">
              Registry checks use these identifiers when your documents don't show them, and flag a mismatch when they differ.
              MSE and startup status unlock statutory exemptions.
            </p>
            <FormError error={save.error} />
            <div className="flex items-center gap-3">
              <Button type="submit" loading={save.isPending}>Save profile</Button>
              {save.isSuccess && <span className="text-sm text-pass">Saved</span>}
            </div>
          </form>
        </Card>
        <CredentialWallet credentials={org.data.credentials} />
      </div>
    </>
  );
}

function CredentialWallet({ credentials }: { credentials: { id: string; type: CredentialType; number: string; issuedOn: string | null; expiresOn: string | null }[] }) {
  const queryClient = useQueryClient();
  const [form, setForm] = useState({ type: "ISO9001" as CredentialType, number: "", issuedOn: "", expiresOn: "" });
  const add = useMutation({
    mutationFn: () => organizationApi.addCredential({ type: form.type, number: form.number, issuedOn: form.issuedOn || null, expiresOn: form.expiresOn || null }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["organization"] });
      setForm({ ...form, number: "", issuedOn: "", expiresOn: "" });
    },
  });
  const set = (k: keyof typeof form) => (e: { target: { value: string } }) => setForm({ ...form, [k]: e.target.value });

  return (
    <Card title={<h2 className="flex items-center gap-2 text-base font-semibold"><Wallet className="size-5 text-saffron-500" /> Credential wallet</h2>} padded={false}>
      {credentials.length === 0 ? (
        <EmptyState title="No credentials yet">Add certificates to get reminders 30 days before they expire.</EmptyState>
      ) : (
        <Table head={["Credential", "Number", "Expires"]}>
          {credentials.map((c) => {
            const days = c.expiresOn ? daysUntil(c.expiresOn) : null;
            return (
              <tr key={c.id}>
                <td className="px-4 py-3 font-medium">{c.type}</td>
                <td className="px-4 py-3 font-mono text-xs">{c.number}</td>
                <td className="px-4 py-3">
                  {c.expiresOn ? formatDate(c.expiresOn) : "—"}{" "}
                  {days !== null && days < 0 && <Badge tone="fail">Expired</Badge>}
                  {days !== null && days >= 0 && days <= 30 && <Badge tone="warn">{days} days</Badge>}
                </td>
              </tr>
            );
          })}
        </Table>
      )}
      <form className="grid gap-3 border-t border-ink-100 p-5 sm:grid-cols-2" onSubmit={(e) => { e.preventDefault(); add.mutate(); }}>
        <SelectField label="Type" value={form.type} onChange={set("type")}>
          {CREDENTIAL_TYPES.map((t) => <option key={t}>{t}</option>)}
        </SelectField>
        <TextField label="Number" required value={form.number} onChange={set("number")} error={fieldError(add.error, "Number")} />
        <TextField label="Issued on" type="date" value={form.issuedOn} onChange={set("issuedOn")} />
        <TextField label="Expires on" type="date" value={form.expiresOn} onChange={set("expiresOn")} error={fieldError(add.error, "ExpiresOn")} />
        <div className="sm:col-span-2">
          <FormError error={add.error} />
          <Button type="submit" size="sm" loading={add.isPending}><Plus className="size-4" /> Add credential</Button>
        </div>
      </form>
    </Card>
  );
}
