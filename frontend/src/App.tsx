import React from "react";
import { Navigate, Route, Routes } from "react-router-dom";

import { DbStatusGuard } from "./routes/DbStatusGuard";
import { RequireAuth } from "./routes/RequireAuth";
import { AppShell } from "./components/layout/AppShell";

import { LoginPage } from "./pages/auth/LoginPage";
import { DbSetupPage } from "./pages/admin/DbSetupPage";

import { HomePage } from "./pages/HomePage";
import { RequestsPage } from "./pages/requests/RequestsPage";
import { CustomersPage } from "./pages/customers/CustomersPage";
import { ProcurementPage } from "./pages/procurement/ProcurementPage";
import { ProductionPage } from "./pages/production/ProductionPage";
import { WarehousePage } from "./pages/warehouse/WarehousePage";
import { EngineeringPage } from "./pages/engineering/EngineeringPage";
import { TechnologyPage } from "./pages/technology/TechnologyPage";

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
            <Route path="requests" element={<RequestsPage />} />
            <Route path="customers" element={<CustomersPage />} />
            <Route path="procurement" element={<ProcurementPage />} />
            <Route path="production" element={<ProductionPage />} />
            <Route path="warehouse" element={<WarehousePage />} />
            <Route path="engineering" element={<EngineeringPage />} />
            <Route path="technology" element={<TechnologyPage />} />
          </Route>
        </Route>
      </Route>

      {/* Fallback */ }
      <Route path="*" element={<Navigate to="/login" replace />} />
    </Routes>
  );
};

export default App;