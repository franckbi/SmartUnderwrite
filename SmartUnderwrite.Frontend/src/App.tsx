import React from "react";
import {
  BrowserRouter as Router,
  Routes,
  Route,
  Navigate,
} from "react-router-dom";
import { ThemeProvider } from "@mui/material/styles";
import { CssBaseline } from "@mui/material";
import { theme } from "@/theme/theme";
import { AuthProvider } from "@/contexts/AuthContext";
import { ProtectedRoute } from "@/components/auth/ProtectedRoute";
import { AppLayout } from "@/components/layout/AppLayout";
import { LoginPage } from "@/pages/auth/LoginPage";
import { DashboardPage } from "@/pages/DashboardPage";
import { UnauthorizedPage } from "@/pages/UnauthorizedPage";
import { ApplicationsPage } from "@/pages/applications/ApplicationsPage";
import { ApplicationDetailPage } from "@/pages/applications/ApplicationDetailPage";
import { CreateApplicationPage } from "@/pages/applications/CreateApplicationPage";
import { DecisionsPage } from "@/pages/decisions/DecisionsPage";
import { PendingDecisionsPage } from "@/pages/decisions/PendingDecisionsPage";
import { AdminPage } from "@/pages/admin/AdminPage";
import { AffiliatesPage } from "@/pages/admin/AffiliatesPage";
import { ReportsPage } from "@/pages/admin/ReportsPage";
import { AuditLogsPage } from "@/pages/admin/AuditLogsPage";
import { UserRole } from "@/types/auth";

function App() {
  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <AuthProvider>
        <Router>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/unauthorized" element={<UnauthorizedPage />} />
            <Route
              path="/dashboard"
              element={
                <ProtectedRoute>
                  <AppLayout>
                    <DashboardPage />
                  </AppLayout>
                </ProtectedRoute>
              }
            />
            <Route
              path="/applications"
              element={
                <ProtectedRoute>
                  <AppLayout>
                    <ApplicationsPage />
                  </AppLayout>
                </ProtectedRoute>
              }
            />
            <Route
              path="/applications/create"
              element={
                <ProtectedRoute requiredRoles={[UserRole.Affiliate]}>
                  <AppLayout>
                    <CreateApplicationPage />
                  </AppLayout>
                </ProtectedRoute>
              }
            />
            <Route
              path="/applications/:id"
              element={
                <ProtectedRoute>
                  <AppLayout>
                    <ApplicationDetailPage />
                  </AppLayout>
                </ProtectedRoute>
              }
            />
            <Route
              path="/decisions"
              element={
                <ProtectedRoute
                  requiredRoles={[UserRole.Underwriter, UserRole.Admin]}
                >
                  <AppLayout>
                    <DecisionsPage />
                  </AppLayout>
                </ProtectedRoute>
              }
            />
            <Route
              path="/decisions/pending"
              element={
                <ProtectedRoute
                  requiredRoles={[UserRole.Underwriter, UserRole.Admin]}
                >
                  <AppLayout>
                    <PendingDecisionsPage />
                  </AppLayout>
                </ProtectedRoute>
              }
            />
            <Route
              path="/admin/rules"
              element={
                <ProtectedRoute requiredRoles={[UserRole.Admin]}>
                  <AppLayout>
                    <AdminPage />
                  </AppLayout>
                </ProtectedRoute>
              }
            />
            <Route
              path="/admin/affiliates"
              element={
                <ProtectedRoute requiredRoles={[UserRole.Admin]}>
                  <AppLayout>
                    <AffiliatesPage />
                  </AppLayout>
                </ProtectedRoute>
              }
            />
            <Route
              path="/admin/reports"
              element={
                <ProtectedRoute requiredRoles={[UserRole.Admin]}>
                  <AppLayout>
                    <ReportsPage />
                  </AppLayout>
                </ProtectedRoute>
              }
            />
            <Route
              path="/admin/audit"
              element={
                <ProtectedRoute requiredRoles={[UserRole.Admin]}>
                  <AppLayout>
                    <AuditLogsPage />
                  </AppLayout>
                </ProtectedRoute>
              }
            />
            <Route
              path="/admin"
              element={<Navigate to="/admin/rules" replace />}
            />
            <Route path="/" element={<Navigate to="/dashboard" replace />} />
          </Routes>
        </Router>
      </AuthProvider>
    </ThemeProvider>
  );
}

export default App;
