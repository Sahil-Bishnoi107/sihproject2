import type { ReactNode } from "react";
import { Navigate, Route, Routes } from "react-router-dom";
import type { UserRole } from "./api/types";
import { isStaff, useAuth } from "./auth/AuthContext";
import { AppShell } from "./components/layout/AppShell";
import { Spinner } from "./components/ui";
import { UsersPage } from "./pages/admin/UsersPage";
import { LoginPage } from "./pages/auth/LoginPage";
import { RegisterPage } from "./pages/auth/RegisterPage";
import { BidderHomePage } from "./pages/bidder/BidderHomePage";
import { BidWorkspacePage } from "./pages/bidder/BidWorkspacePage";
import { OpenTendersPage } from "./pages/bidder/OpenTendersPage";
import { OrganizationPage } from "./pages/bidder/OrganizationPage";
import { TenderRequirementsPage } from "./pages/bidder/TenderRequirementsPage";
import { NotFoundPage } from "./pages/NotFoundPage";
import { NotificationsPage } from "./pages/NotificationsPage";
import { AuditPage } from "./pages/officer/AuditPage";
import { BidReviewPage } from "./pages/officer/BidReviewPage";
import { DashboardPage } from "./pages/officer/DashboardPage";
import { TenderDetailPage } from "./pages/officer/TenderDetailPage";
import { TendersPage } from "./pages/officer/TendersPage";

function RequireAuth({ roles, children }: { roles?: UserRole[]; children: ReactNode }) {
  const { user, ready } = useAuth();
  if (!ready) return <Spinner label="Restoring your session…" />;
  if (!user) return <Navigate to="/login" replace />;
  if (roles && !roles.includes(user.role)) return <Navigate to="/" replace />;
  return <>{children}</>;
}

/** Same URL, different screen per role (officers manage, bidders participate). */
function ByRole({ staff, bidder }: { staff: ReactNode; bidder: ReactNode }) {
  const { user } = useAuth();
  return <>{isStaff(user) ? staff : bidder}</>;
}

function Home() {
  const { user } = useAuth();
  return <Navigate to={isStaff(user) ? "/dashboard" : "/home"} replace />;
}

const STAFF: UserRole[] = ["Officer", "Admin"];

export function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route element={<RequireAuth><AppShell /></RequireAuth>}>
        <Route index element={<Home />} />
        <Route path="dashboard" element={<RequireAuth roles={STAFF}><DashboardPage /></RequireAuth>} />
        <Route path="audit" element={<RequireAuth roles={STAFF}><AuditPage /></RequireAuth>} />
        <Route path="admin/users" element={<RequireAuth roles={["Admin"]}><UsersPage /></RequireAuth>} />
        <Route path="home" element={<RequireAuth roles={["Bidder"]}><BidderHomePage /></RequireAuth>} />
        <Route path="organization" element={<RequireAuth roles={["Bidder"]}><OrganizationPage /></RequireAuth>} />
        <Route path="tenders" element={<ByRole staff={<TendersPage />} bidder={<OpenTendersPage />} />} />
        <Route path="tenders/:id" element={<ByRole staff={<TenderDetailPage />} bidder={<TenderRequirementsPage />} />} />
        <Route path="bids/:id" element={<ByRole staff={<BidReviewPage />} bidder={<BidWorkspacePage />} />} />
        <Route path="notifications" element={<NotificationsPage />} />
        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  );
}
