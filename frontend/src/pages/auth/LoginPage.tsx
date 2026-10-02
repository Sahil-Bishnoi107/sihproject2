import { useMutation } from "@tanstack/react-query";
import { Building2, Gavel, ShieldCheck } from "lucide-react";
import { useState, type FormEvent } from "react";
import { Navigate } from "react-router-dom";
import { authApi } from "../../api/endpoints";
import { useAuth } from "../../auth/AuthContext";
import { Button, cx, FormError, TextField } from "../../components/ui";
import { AuthLayout } from "./AuthLayout";

// Prototype: the three seeded demo accounts, one click to fill in.
const DEMO_ACCOUNTS = [
  { label: "Officer", hint: "Reviews bids", icon: Gavel, email: "officer@bidguard.local", password: "Officer#BidGuard2026" },
  { label: "Bidder", hint: "Submits bids", icon: Building2, email: "bidder@shakti.local", password: "Bidder#BidGuard2026" },
  { label: "Admin", hint: "Manages users", icon: ShieldCheck, email: "admin@bidguard.local", password: "Admin#BidGuard2026" },
];

export function LoginPage() {
  const { user, signIn } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  const login = useMutation({
    mutationFn: () => authApi.login(email, password),
    onSuccess: signIn,
  });

  if (user) return <Navigate to="/" replace />;

  const submit = (e: FormEvent) => {
    e.preventDefault();
    login.mutate();
  };

  return (
    <AuthLayout>
      <div className="login-sharp">
        <h2 className="text-2xl font-bold text-ink-900">Sign in</h2>
        <p className="mt-1 text-sm text-ink-700">Choose a role to fill in its demo account.</p>

        <div className="mt-6 grid grid-cols-3 gap-2" role="group" aria-label="Demo accounts">
          {DEMO_ACCOUNTS.map(({ label, hint, icon: Icon, ...account }) => {
            const selected = email === account.email;
            return (
              <button
                key={label}
                type="button"
                aria-pressed={selected}
                onClick={() => {
                  setEmail(account.email);
                  setPassword(account.password);
                  login.reset();
                }}
                className={cx(
                  "flex flex-col items-center gap-1 border px-2 py-3 text-center transition-colors",
                  selected ? "border-saffron-500 bg-saffron-50 text-ink-900" : "border-ink-100 bg-white text-ink-800 hover:border-ink-700/40",
                )}
              >
                <Icon className={cx("size-5", selected ? "text-saffron-600" : "text-ink-700")} aria-hidden />
                <span className="text-sm font-semibold">{label}</span>
                <span className="text-[11px] text-ink-700">{hint}</span>
              </button>
            );
          })}
        </div>

        <form className="mt-6 space-y-4" onSubmit={submit}>
          <TextField label="Email" type="email" autoComplete="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
          <TextField label="Password" type="password" autoComplete="current-password" required value={password} onChange={(e) => setPassword(e.target.value)} />
          <FormError error={login.error} />
          <Button type="submit" className="w-full" loading={login.isPending}>
            Sign in
          </Button>
        </form>
      </div>
    </AuthLayout>
  );
}
