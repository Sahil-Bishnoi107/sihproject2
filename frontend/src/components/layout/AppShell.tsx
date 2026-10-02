import {
  Building2, ClipboardList, FileSearch, Gauge, Home, LogOut, Menu, ScrollText, ShieldCheck, UserCog, X, type LucideIcon,
} from "lucide-react";
import { useState } from "react";
import { NavLink, Outlet } from "react-router-dom";
import { useAuth } from "../../auth/AuthContext";
import { cx } from "../ui";
import { NotificationBell } from "./NotificationBell";

interface NavItem {
  to: string;
  label: string;
  icon: LucideIcon;
}

const STAFF_NAV: NavItem[] = [
  { to: "/dashboard", label: "Dashboard", icon: Gauge },
  { to: "/tenders", label: "Tenders", icon: ClipboardList },
  { to: "/audit", label: "Audit log", icon: ScrollText },
];

const BIDDER_NAV: NavItem[] = [
  { to: "/home", label: "My bids", icon: Home },
  { to: "/tenders", label: "Open tenders", icon: FileSearch },
  { to: "/organization", label: "Organization", icon: Building2 },
];

export function Logo({ dark }: { dark?: boolean }) {
  return (
    <div className="flex items-center gap-2.5">
      <span className="flex size-9 items-center justify-center rounded-lg bg-saffron-500 text-white shadow-sm">
        <ShieldCheck className="size-5" aria-hidden />
      </span>
      <div className="leading-tight">
        <div className={cx("text-base font-bold", dark ? "text-ink-900" : "text-white")}>BidGuard AI</div>
        <div className={cx("text-[11px]", dark ? "text-ink-700" : "text-ink-100/70")}>GeM bid compliance</div>
      </div>
    </div>
  );
}

export function AppShell() {
  const { user, signOut } = useAuth();
  const [open, setOpen] = useState(false);
  const nav = user?.role === "Bidder" ? BIDDER_NAV : [...STAFF_NAV, ...(user?.role === "Admin" ? [{ to: "/admin/users", label: "Users", icon: UserCog }] : [])];

  const sidebar = (
    <nav className="flex h-full flex-col bg-ink-900 px-3 py-5 text-ink-100">
      <div className="px-2">
        <Logo />
      </div>
      <ul className="mt-8 flex flex-1 flex-col gap-1">
        {nav.map(({ to, label, icon: Icon }) => (
          <li key={to}>
            <NavLink
              to={to}
              onClick={() => setOpen(false)}
              className={({ isActive }) =>
                cx(
                  "flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition-colors",
                  isActive ? "bg-white/10 text-white" : "text-ink-100/75 hover:bg-white/5 hover:text-white",
                )
              }
            >
              <Icon className="size-4.5" aria-hidden />
              {label}
            </NavLink>
          </li>
        ))}
      </ul>
      <div className="rounded-lg bg-white/5 px-3 py-3">
        <div className="truncate text-sm font-medium text-white">{user?.fullName}</div>
        <div className="truncate text-xs text-ink-100/60">{user?.email}</div>
        <div className="mt-1 inline-block rounded bg-saffron-500/20 px-1.5 py-0.5 text-[11px] font-semibold text-saffron-500">{user?.role}</div>
        <button onClick={() => signOut()} className="mt-3 flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-sm text-ink-100/80 hover:bg-white/10 hover:text-white">
          <LogOut className="size-4" aria-hidden /> Sign out
        </button>
      </div>
    </nav>
  );

  return (
    <div className="flex h-full">
      <aside className="hidden w-64 shrink-0 lg:block">{sidebar}</aside>
      {open && (
        <div className="fixed inset-0 z-40 lg:hidden">
          <div className="absolute inset-0 bg-ink-950/50" onClick={() => setOpen(false)} />
          <aside className="relative h-full w-64">{sidebar}</aside>
        </div>
      )}
      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex h-14 shrink-0 items-center justify-between border-b border-ink-100 bg-white px-4 lg:px-8">
          <button className="rounded-lg p-2 text-ink-700 hover:bg-ink-100 lg:hidden" onClick={() => setOpen(true)} aria-label="Open menu">
            {open ? <X className="size-5" /> : <Menu className="size-5" />}
          </button>
          <div className="hidden text-sm text-ink-700 lg:block">Smart India Hackathon 2026 · PS SIH26100</div>
          <NotificationBell />
        </header>
        <main className="flex-1 overflow-y-auto px-4 py-6 lg:px-8">
          <div className="mx-auto max-w-6xl">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
}
