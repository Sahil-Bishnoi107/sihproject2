import { useMutation } from "@tanstack/react-query";
import { useState, type FormEvent } from "react";
import { Link, Navigate } from "react-router-dom";
import { authApi } from "../../api/endpoints";
import type { OrganizationCategory } from "../../api/types";
import { useAuth } from "../../auth/AuthContext";
import { Button, fieldError, FormError, SelectField, TextField } from "../../components/ui";
import { AuthLayout } from "./AuthLayout";

export function RegisterPage() {
  const { user, signIn } = useAuth();
  const [form, setForm] = useState({
    fullName: "", email: "", password: "", organizationName: "", pan: "", category: "MSE" as OrganizationCategory,
  });
  const set = (k: keyof typeof form) => (e: { target: { value: string } }) => setForm({ ...form, [k]: e.target.value });

  const register = useMutation({
    mutationFn: () => authApi.register({ ...form, pan: form.pan.toUpperCase() }),
    onSuccess: signIn,
  });

  if (user) return <Navigate to="/" replace />;

  const submit = (e: FormEvent) => {
    e.preventDefault();
    register.mutate();
  };
  const err = (f: string) => fieldError(register.error, f);

  return (
    <AuthLayout>
      <h2 className="text-2xl font-bold text-ink-900">Register your organization</h2>
      <p className="mt-1 text-sm text-ink-700">Create a bidder account to check your bids against GeM tenders before you submit.</p>
      <form className="mt-8 space-y-4" onSubmit={submit}>
        <TextField label="Your name" required value={form.fullName} onChange={set("fullName")} error={err("FullName")} />
        <TextField label="Work email" type="email" required value={form.email} onChange={set("email")} error={err("Email")} />
        <TextField
          label="Password" type="password" required value={form.password} onChange={set("password")}
          hint="At least 8 characters with upper- and lowercase letters and a digit." error={err("Password")}
        />
        <TextField label="Organization legal name" required value={form.organizationName} onChange={set("organizationName")} error={err("OrganizationName")} />
        <div className="grid gap-4 sm:grid-cols-2">
          <TextField label="PAN" required maxLength={10} value={form.pan} onChange={set("pan")} placeholder="AAACA1234F" error={err("Pan")} />
          <SelectField label="Enterprise category" value={form.category} onChange={set("category")}>
            <option value="MSE">Micro / Small (MSE)</option>
            <option value="Medium">Medium</option>
            <option value="Large">Large</option>
          </SelectField>
        </div>
        <FormError error={register.error} />
        <Button type="submit" className="w-full" loading={register.isPending}>
          Create account
        </Button>
      </form>
      <p className="mt-6 text-center text-sm text-ink-700">
        Already registered?{" "}
        <Link to="/login" className="font-medium text-saffron-600 hover:underline">
          Sign in
        </Link>
      </p>
    </AuthLayout>
  );
}
