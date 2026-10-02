import { useMutation } from "@tanstack/react-query";
import { UserPlus } from "lucide-react";
import { useState, type FormEvent } from "react";
import { usersApi } from "../../api/endpoints";
import type { UserDto, UserRole } from "../../api/types";
import { Badge, Button, Card, fieldError, FormError, PageHeader, SelectField, TextField } from "../../components/ui";

export function UsersPage() {
  const [form, setForm] = useState({ fullName: "", email: "", password: "", role: "Officer" as UserRole });
  const [created, setCreated] = useState<UserDto[]>([]);
  const create = useMutation({
    mutationFn: () => usersApi.create(form),
    onSuccess: (user) => {
      setCreated([user, ...created]);
      setForm({ fullName: "", email: "", password: "", role: form.role });
    },
  });
  const set = (k: keyof typeof form) => (e: { target: { value: string } }) => setForm({ ...form, [k]: e.target.value });
  const err = (f: string) => fieldError(create.error, f);

  const submit = (e: FormEvent) => {
    e.preventDefault();
    create.mutate();
  };

  return (
    <>
      <PageHeader title="Users" subtitle="Provision procurement officers and administrators. Bidders register themselves." />
      <div className="grid gap-6 lg:grid-cols-2">
        <Card title="New staff account">
          <form className="space-y-4" onSubmit={submit}>
            <TextField label="Full name" required value={form.fullName} onChange={set("fullName")} error={err("FullName")} />
            <TextField label="Email" type="email" required value={form.email} onChange={set("email")} error={err("Email")} />
            <TextField label="Temporary password" type="password" required value={form.password} onChange={set("password")}
              hint="At least 8 characters with upper- and lowercase letters and a digit." error={err("Password")} />
            <SelectField label="Role" value={form.role} onChange={set("role")}>
              <option value="Officer">Procurement officer</option>
              <option value="Admin">Administrator</option>
            </SelectField>
            <FormError error={create.error} />
            <Button type="submit" loading={create.isPending}><UserPlus className="size-4" /> Create account</Button>
          </form>
        </Card>
        <Card title="Created this session">
          {created.length === 0 ? (
            <p className="text-sm text-ink-700">New accounts appear here. Every creation is also written to the audit log.</p>
          ) : (
            <ul className="divide-y divide-ink-100">
              {created.map((u) => (
                <li key={u.id} className="flex items-center justify-between py-2.5 text-sm">
                  <div>
                    <div className="font-medium">{u.fullName}</div>
                    <div className="text-ink-700">{u.email}</div>
                  </div>
                  <Badge tone={u.role === "Admin" ? "ink" : "info"}>{u.role}</Badge>
                </li>
              ))}
            </ul>
          )}
        </Card>
      </div>
    </>
  );
}
