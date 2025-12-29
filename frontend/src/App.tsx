import React from "react";
import { Navigate, Route, Routes } from "react-router-dom";

import { DbStatusGuard } from "./routes/DbStatusGuard";
import { RequireAuth } from "./routes/RequireAuth";
import { AppShell } from "./components/layout/AppShell";

import { LoginPage } from "./pages/auth/LoginPage";
import { DbSetupPage } from "./pages/admin/DbSetupPage";

import { HomePage } from "./pages/HomePage";
import { CustomersPage } from "./pages/customers/CustomersPage";
import { ProcurementPage } from "./pages/procurement/ProcurementPage";
import { ProductionPage } from "./pages/production/ProductionPage";
import { WarehousePage } from "./pages/warehouse/WarehousePage";
import { EngineeringPage } from "./pages/engineering/EngineeringPage";
import { TechnologyPage } from "./pages/technology/TechnologyPage";
import { QualityPage } from "./pages/quality/QualityPage";
import { ReferencesPage } from "./pages/references/ReferencesPage";
import { AdministrationPage } from "./pages/administration/AdministrationPage";
import { MdmOwnershipPage } from "./pages/administration/MdmOwnershipPage";
import { OrgStructurePage } from "./pages/administration/OrgStructurePage";

// Requests module pages
import { RequestsListPage } from "./modules/requests/pages/RequestsListPage";
import { RequestDetailsPage } from "./modules/requests/pages/RequestDetailsPage";
import { RequestEditPage } from "./modules/requests/pages/RequestEditPage";

// Settings module (Requests dictionaries)
import { RequestTypesSettingsPage } from "./modules/settings/requests/dictionaries/pages/RequestTypesSettingsPage";
import { RequestWorkflowSettingsPage } from "./modules/settings/requests/dictionaries/pages/RequestWorkflowSettingsPage";
import { RequestTypeCardPage } from "./modules/settings/requests/dictionaries/pages/RequestTypeCardPage";
import { RequestWorkflowTransitionCardPage } from "./modules/settings/requests/dictionaries/pages/RequestWorkflowTransitionCardPage";

// Settings module (Security)
import { EmployeesSettingsPage } from "./modules/settings/security/pages/EmployeesSettingsPage";
import { UsersSettingsPage } from "./modules/settings/security/pages/UsersSettingsPage";
import { RolesSettingsPage } from "./modules/settings/security/pages/RolesSettingsPage";
import { EmployeeCardPage } from "./modules/settings/security/pages/EmployeeCardPage";
import { UserCardPage } from "./modules/settings/security/pages/UserCardPage";
import { UserRolesCardPage } from "./modules/settings/security/pages/UserRolesCardPage";
import { UserResetPasswordCardPage } from "./modules/settings/security/pages/UserResetPasswordCardPage";
import { RoleCardPage } from "./modules/settings/security/pages/RoleCardPage";

// Settings module (Integrations)
import { Component2020SettingsPage } from "./modules/settings/integrations/component2020/pages/Component2020SettingsPage";
import { Component2020RunCardPage } from "./modules/settings/integrations/component2020/pages/Component2020RunCardPage";

// References (MDM dictionaries - read-only)
import { MdmDictionaryJournalPage } from "./modules/references/mdm/pages/MdmDictionaryJournalPage";
import { MdmDictionaryCardPage } from "./modules/references/mdm/pages/MdmDictionaryCardPage";
import { MdmItemCardPage } from "./modules/references/mdm/pages/MdmItemCardPage";
import { ItemsPage } from "./pages/references/mdm/ItemsPage";
import { StatusDictionaryPage } from "./modules/references/statuses/pages/StatusDictionaryPage";

// Settings module (System)
import { GlobalPathsSettingsPage } from "./modules/settings/system/pages/GlobalPathsSettingsPage";

const App: React.FC = () => {
  return (
    <Routes>
      {/* Публичные маршруты */ }
      <Route path="/login" element={<LoginPage />} />
      <Route path="/db-setup" element={<DbSetupPage />} />

      {/* Приватная зона: проверка статуса БД -> авторизация -> shell */ }
      <Route element={<DbStatusGuard />}>
        <Route element={<RequireAuth />}>
          <Route element={<AppShell />}>
            <Route index element={<HomePage />} />
            <Route
              path="requests"
              element={
                <Navigate to="/requests/journal?direction=incoming&type=all" replace />
              }
            />

            {/* Requests (Journal -> Card) */}
            <Route path="requests/journal" element={<RequestsListPage />} />
            <Route path="requests/new" element={<RequestEditPage />} />

            {/* Backward-compatible Requests routes */}
            <Route
              path="requests/incoming"
              element={
                <Navigate to="/requests/journal?direction=incoming&type=all" replace />
              }
            />
            <Route
              path="requests/outgoing"
              element={
                <Navigate to="/requests/journal?direction=outgoing&type=all" replace />
              }
            />
            <Route
              path="requests/incoming/new"
              element={<Navigate to="/requests/new?direction=incoming" replace />}
            />
            <Route
              path="requests/outgoing/new"
              element={<Navigate to="/requests/new?direction=outgoing" replace />}
            />
            <Route path="requests/:id" element={<RequestDetailsPage />} />
            <Route path="requests/:id/edit" element={<RequestEditPage />} />
            <Route path="customers" element={<CustomersPage />} />
            <Route path="procurement" element={<ProcurementPage />} />
            <Route path="production" element={<ProductionPage />} />
            <Route path="warehouse" element={<WarehousePage />} />
            <Route path="engineering" element={<EngineeringPage />} />
            <Route path="technology" element={<TechnologyPage />} />
            <Route path="quality" element={<QualityPage />} />

            {/* References */}
            <Route path="references" element={<ReferencesPage />} />
            <Route
              path="references/requests/types"
              element={<RequestTypesSettingsPage />}
            />
            <Route
              path="references/requests/types/new"
              element={<RequestTypeCardPage />}
            />
            <Route
              path="references/requests/types/:id"
              element={<RequestTypeCardPage />}
            />
            <Route
              path="references/requests/statuses"
              element={<Navigate to="/references/mdm/statuses?group=Requests" replace />}
            />
            <Route
              path="references/requests/statuses/new"
              element={<Navigate to="/references/mdm/statuses?group=Requests" replace />}
            />
            <Route
              path="references/requests/statuses/:id"
              element={<Navigate to="/references/mdm/statuses?group=Requests" replace />}
            />
            <Route path="references/mdm/statuses" element={<StatusDictionaryPage />} />
            <Route path="references/mdm/items" element={<ItemsPage />} />
            <Route path="references/mdm/items/:id" element={<MdmItemCardPage />} />
            <Route path="references/mdm/:dict" element={<MdmDictionaryJournalPage />} />
            <Route path="references/mdm/:dict/:id" element={<MdmDictionaryCardPage />} />

            {/* Administration */}
            <Route path="administration" element={<AdministrationPage />} />
            <Route path="administration/mdm" element={<MdmOwnershipPage />} />
            <Route path="administration/org-structure" element={<OrgStructurePage />} />
            <Route
              path="administration/security/employees"
              element={<EmployeesSettingsPage />}
            />
            <Route
              path="administration/security/employees/new"
              element={<EmployeeCardPage />}
            />
            <Route
              path="administration/security/employees/:id"
              element={<EmployeeCardPage />}
            />
            <Route
              path="administration/security/users"
              element={<UsersSettingsPage />}
            />
            <Route
              path="administration/security/users/new"
              element={<UserCardPage />}
            />
            <Route
              path="administration/security/users/:id"
              element={<UserCardPage />}
            />
            <Route
              path="administration/security/users/:id/roles"
              element={<UserRolesCardPage />}
            />
            <Route
              path="administration/security/users/:id/reset-password"
              element={<UserResetPasswordCardPage />}
            />
            <Route
              path="administration/security/roles"
              element={<RolesSettingsPage />}
            />
            <Route
              path="administration/security/roles/new"
              element={<RoleCardPage />}
            />
            <Route
              path="administration/security/roles/:id"
              element={<RoleCardPage />}
            />
            <Route
              path="administration/requests/workflow"
              element={<RequestWorkflowSettingsPage />}
            />
            <Route
              path="administration/requests/workflow/:typeId/new"
              element={<RequestWorkflowTransitionCardPage />}
            />
            <Route
              path="administration/requests/workflow/:typeId/:id"
              element={<RequestWorkflowTransitionCardPage />}
            />
            <Route
              path="administration/integrations/component2020"
              element={<Component2020SettingsPage />}
            />
            <Route
              path="administration/integrations/component2020/runs/:runId"
              element={<Component2020RunCardPage />}
            />
            <Route
              path="administration/system/paths"
              element={<GlobalPathsSettingsPage />}
            />

            {/* Backward-compatible Settings routes */}
            <Route path="settings" element={<Navigate to="/administration" replace />} />
            <Route
              path="settings/requests/types"
              element={<Navigate to="/references/requests/types" replace />}
            />
            <Route
              path="settings/requests/statuses"
              element={<Navigate to="/references/requests/statuses" replace />}
            />
            <Route
              path="settings/requests/workflow"
              element={<Navigate to="/administration/requests/workflow" replace />}
            />
            <Route
              path="settings/security/employees"
              element={<Navigate to="/administration/security/employees" replace />}
            />
            <Route
              path="settings/security/users"
              element={<Navigate to="/administration/security/users" replace />}
            />
            <Route
              path="settings/security/roles"
              element={<Navigate to="/administration/security/roles" replace />}
            />
            <Route
              path="settings/integrations/component2020"
              element={<Navigate to="/administration/integrations/component2020" replace />}
            />
            <Route
              path="settings/system/paths"
              element={<Navigate to="/administration/system/paths" replace />}
            />
          </Route>
        </Route>
      </Route>

      {/* Fallback */ }
      <Route path="*" element={<Navigate to="/login" replace />} />
    </Routes>
  );
};

export default App;
