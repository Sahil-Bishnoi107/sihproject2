import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Bell } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { notificationsApi } from "../api/endpoints";
import { useAuth } from "../auth/AuthContext";
import { Card, EmptyState, ErrorState, PageHeader, Spinner, cx } from "../components/ui";
import { formatDateTime } from "../lib/format";
import { notificationLink } from "../lib/notifications";

export function NotificationsPage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const list = useQuery({ queryKey: ["notifications"], queryFn: () => notificationsApi.list(false) });
  const markRead = useMutation({
    mutationFn: notificationsApi.markRead,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["notifications"] }),
  });

  return (
    <>
      <PageHeader title="Notifications" />
      <Card padded={false}>
        {list.isLoading && <Spinner />}
        {list.error && <div className="p-4"><ErrorState error={list.error} /></div>}
        {list.data?.length === 0 && <EmptyState icon={<Bell className="size-10" />} title="No notifications yet" />}
        <ul className="divide-y divide-ink-100">
          {list.data?.map((n) => {
            const to = user ? notificationLink(n, user.role) : null;
            return (
              <li key={n.id}>
                <button
                  className={cx("flex w-full items-start gap-3 px-5 py-4 text-left hover:bg-ink-50", !n.isRead && "bg-saffron-50/60")}
                  onClick={() => {
                    if (!n.isRead) markRead.mutate(n.id);
                    if (to) navigate(to);
                  }}
                >
                  <span className={cx("mt-1.5 size-2 shrink-0 rounded-full", n.isRead ? "bg-transparent" : "bg-saffron-500")} />
                  <div>
                    <div className="text-sm text-ink-900">{n.message}</div>
                    <div className="mt-1 text-xs text-ink-700">{formatDateTime(n.createdAt)}</div>
                  </div>
                </button>
              </li>
            );
          })}
        </ul>
      </Card>
    </>
  );
}
