import React from "react";
import ReactDOM from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import { ConfigProvider, theme } from "antd";
import { QueryClientProvider } from "@tanstack/react-query";
import ruRU from "antd/locale/ru_RU";
import "antd/dist/reset.css";
import "./core/styles/antd-overrides.css";

import dayjs from "dayjs";
import "dayjs/locale/ru";

import { AuthProvider } from "./auth/AuthContext";
import { queryClient } from "./shared/api/queryClient";
import App from "./App";

dayjs.locale("ru");

const rootElement = document.getElementById("root");

if (!rootElement) {
  throw new Error("Root element with id 'root' not found");
}

ReactDOM.createRoot(rootElement).render(
  <React.StrictMode>
    <QueryClientProvider client={queryClient}>
      <ConfigProvider
        locale={ruRU}
        theme={{
          algorithm: theme.defaultAlgorithm,
        }}
      >
        <BrowserRouter>
          <AuthProvider>
            <App />
          </AuthProvider>
        </BrowserRouter>
      </ConfigProvider>
    </QueryClientProvider>
  </React.StrictMode>
);
