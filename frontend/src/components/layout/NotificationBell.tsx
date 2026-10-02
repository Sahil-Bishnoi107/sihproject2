import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Bell } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { notificationsApi } from "../../api/endpoints";
import { useAuth } from "../../auth/AuthContext";
import { timeAgo } from "../../lib/format";
import { notificationLink } from "../../lib/notifications";
import { cx } from "../ui";

export function NotificationBell() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  const { data = [] } = useQuery({
    queryKey: ["notifications"],
    queryFn: () => notificationsApi.list(false),
    refetchInterval: 20_000,
  });
  const unread = data.filter((n) => !n.isRead).length;

  const markRead = useMutation({
    mutationFn: notificationsApi.markRead,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["notifications"] }),
  });

  useEffect(() => {
    const close = (e: MouseEvent) => ref.current && !ref.current.contains(e.target as Node) && setOpen(false);
    document.addEventListener("mousedown", close);
    return () => document.removeEventListener("mousedown", close);
  }, []);

  return (
    <div className="relative" ref={ref}>
      <button
        className="relative rounded-lg p-2 text-ink-700 hover:bg-ink-100"
        onClick={() => setOpen((o) => !o)}
        aria-label={`Notifications${unread ? `, ${unread} unread` : ""}`}
      >
        <Bell className="size-5" />
        {unread > 0 && (
          <span className="absolute right-1 top-1 flex size-4 items-center justify-center rounded-full bg-saffron-500 text-[10px] font-bold text-white">
            {unread > 9 ? "9+" : unread}
          </span>
        )}
      </button>
      {open && (
        <div className="absolute right-0 z-40 mt-2 w-80 overflow-hidden rounded-xl border border-ink-100 bg-white shadow-lg">
          <div className="border-b border-ink-100 px-4 py-2.5 text-sm font-semibold">Notifications</div>
          <ul className="max-h-96 divide-y divide-ink-100 overflow-y-auto">
            {data.length === 0 && <li className="px-4 py-6 text-center text-sm text-ink-700">You're all caught up.</li>}
            {data.slice(0, 8).map((n) => (
              <li key={n.id}>
                <button
                  className={cx("block w-full px-4 py-3 text-left text-sm hover:bg-ink-50", !n.isRead && "bg-saffron-50/60")}
                  onClick={() => {
                    if (!n.isRead) markRead.mutate(n.id);
                    const to = user && notificationLink(n, user.role);
                    setOpen(false);
                    if (to) navigate(to);
                  }}
                >
                  <div className="text-ink-900">{n.message}</div>
                  <div className="mt-1 text-xs text-ink-700">{timeAgo(n.createdAt)}</div>
                </button>
              </li>
            ))}
          </ul>
          <Link to="/notifications" onClick={() => setOpen(false)} className="block border-t border-ink-100 px-4 py-2.5 text-center text-sm font-medium text-ink-800 hover:bg-ink-50">
            View all
          </Link>
        </div>
      )}
    </div>
  );
}
