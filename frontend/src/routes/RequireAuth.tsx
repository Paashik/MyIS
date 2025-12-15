import React, { useEffect, useState } from "react";
import { Alert, Button, Result, Spin } from "antd";
import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useAuth, User } from "../auth/AuthContext";
import { t } from "../core/i18n/t";

type AuthCheckState =
  | { kind: "idle" }
  | { kind: "loading" }
  | { kind: "authorized" }
  | { kind: "unauthorized" }
  | { kind: "forbidden"; message?: string }
  | { kind: "error"; message: string };

const mapUserDto = (dto: any): User => {
  return {
    id: String(dto.id),
    login: dto.login,
    fullName: dto.fullName,
    roles: Array.isArray(dto.roles) ? dto.roles.map(String) : [],
  };
};

const RequireAuth: React.FC = () => {
  const location = useLocation();
  const { user, setUser, logout } = useAuth();
  const [state, setState] = useState<AuthCheckState>({ kind: "idle" });

  const fromLocation = (location.state as any)?.from;

  useEffect(() => {
    if (user) {
      setState({ kind: "authorized" });
      return;
    }

    let cancelled = false;

    const load = async () => {
      setState({ kind: "loading" });

      try {
        const response = await fetch("/api/auth/me", {
          method: "GET",
          credentials: "include",
        });

        if (cancelled) return;

        if (response.status === 401) {
          setUser(null);
          setState({ kind: "unauthorized" });
          return;
        }

        if (response.status === 403) {
          setUser(null);
          let message: string | undefined;
          try {
            const text = (await response.text()).trim();
            message = text.length > 0 ? text : undefined;
          } catch {
            // игнорируем ошибки чтения тела ответа
          }

          setState({
            kind: "forbidden",
            message:
              message ??
              t("auth.check.forbidden.subtitle"),
          });
          return;
        }

        if (!response.ok) {
          const text = await response.text();
          setState({
            kind: "error",
            message:
              text ||
              t("auth.check.error.http", { status: response.status }),
          });
          return;
        }

        const dto = await response.json();
        const mapped = mapUserDto(dto);
        setUser(mapped);
        setState({ kind: "authorized" });
      } catch (error) {
        if (cancelled) return;

        const message =
          error instanceof Error
            ? error.message
            : t("common.error.unknownNetwork");

        setState({
          kind: "error",
          message: t("auth.check.error.failed", { message }),
        });
      }
    };

    void load();

    return () => {
      cancelled = true;
    };
  }, [user, setUser]);

  if (state.kind === "forbidden") {
    return (
      <div style={{ padding: 24 }}>
        <Result
          status="403"
          title={t("auth.check.forbidden.title")}
          subTitle={state.message ?? t("auth.check.forbidden.subtitle")}
          extra={
            <Button
              type="primary"
              data-testid="auth-check-forbidden-login-button"
              onClick={() => {
                void logout();
              }}
            >
              {t("auth.check.forbidden.goToLogin")}
            </Button>
          }
        />
      </div>
    );
  }

  if (user && (state.kind === "idle" || state.kind === "authorized")) {
    return <Outlet />;
  }

  if (state.kind === "unauthorized") {
    return (
      <Navigate
        to="/login"
        replace
        state={{ from: fromLocation ?? location }}
      />
    );
  }

  if (state.kind === "loading" || state.kind === "idle") {
    return (
      <div
        data-testid="auth-check-loading"
        style={{
          minHeight: "50vh",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
        }}
      >
        <Spin tip={t("auth.check.loading")} />
      </div>
    );
  }

  if (state.kind === "error") {
    return (
      <div style={{ padding: 24 }}>
        <Alert
          data-testid="auth-check-error-alert"
          type="error"
          message={t("auth.check.error.title")}
          description={
            <>
              <div style={{ marginBottom: 8 }}>{state.message}</div>
              <Button
                data-testid="auth-check-refresh-button"
                type="primary"
                onClick={() => setState({ kind: "idle" })}
              >
                {t("common.actions.refresh")}
              </Button>
            </>
          }
          showIcon
        />
      </div>
    );
  }

  return <Outlet />;
};

export { RequireAuth };
