import React, { useEffect, useState } from "react";
import { Alert, Spin } from "antd";
import { Navigate, Outlet } from "react-router-dom";
import { t } from "../core/i18n/t";

type DbConnectionSource =
  | "Unknown"
  | "Configuration"
  | "Environment"
  | "AppSettingsLocal";

export interface DbStatusResponse {
  configured: boolean;
  canConnect: boolean;
  lastError?: string | null;
  environment: string;
  connectionStringSource: DbConnectionSource;
  rawSourceDescription?: string | null;
}

type DbStatusState =
  | { kind: "loading" }
  | { kind: "error"; message: string }
  | { kind: "redirectToSetup" }
  | { kind: "ok" };

const DbStatusGuard: React.FC = () => {
  const [state, setState] = useState<DbStatusState>({ kind: "loading" });

  useEffect(() => {
    let cancelled = false;

    const load = async () => {
      setState({ kind: "loading" });

      try {
        const response = await fetch("/api/admin/db-status", {
          method: "GET",
          credentials: "include",
        });

        if (!response.ok) {
          const message = t("db.status.error.http", {
            status: response.status,
            statusText: response.statusText,
          });
          if (!cancelled) {
            setState({
              kind: "error",
              message,
            });
          }
          return;
        }

        const contentType = response.headers.get("content-type") ?? "";
        if (!contentType.toLowerCase().includes("application/json")) {
          const text = await response.text().catch(() => "");
          const snippet = text ? text.slice(0, 200) : "";
          const details = snippet
            ? t("db.status.error.responseSnippet", { snippet })
            : "";
          throw new Error(
            t("db.status.error.expectedJson", {
              contentType,
              details,
            })
          );
        }

        const data = (await response.json()) as DbStatusResponse;

        if (!data.configured || !data.canConnect) {
          if (!cancelled) {
            setState({ kind: "redirectToSetup" });
          }
          return;
        }

        if (!cancelled) {
          setState({ kind: "ok" });
        }
      } catch (error) {
        if (cancelled) return;

        const message =
          error instanceof Error
            ? error.message
            : t("common.error.unknownNetwork");

        setState({
          kind: "error",
          message: t("db.status.error.failed", { message }),
        });
      }
    };

    void load();

    return () => {
      cancelled = true;
    };
  }, []);

  if (state.kind === "redirectToSetup") {
    return <Navigate to="/db-setup" replace />;
  }

  if (state.kind === "loading") {
    return (
      <div
        data-testid="db-status-loading"
        style={{
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          minHeight: "50vh",
        }}
      >
        <Spin tip={t("db.status.loading")} />
      </div>
    );
  }

  if (state.kind === "error") {
    return (
      <div style={{ padding: 24 }}>
        <Alert
          data-testid="db-status-error-alert"
          type="error"
          message={t("db.status.error.title")}
          description={state.message}
          showIcon
        />
      </div>
    );
  }

  // ok
  return <Outlet />;
};

export { DbStatusGuard };
