import React, { useEffect, useRef, useState } from "react";
import {
  Alert,
  Button,
  Form,
  Input,
  InputNumber,
  Typography,
  message,
} from "antd";
import { useNavigate } from "react-router-dom";
import { AuthPageLayout } from "../../components/layout/AuthPageLayout";
import { t } from "../../core/i18n/t";

const { Text } = Typography;

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

export interface DbTestResponse {
  configured: boolean;
  canConnect: boolean;
  lastError?: string | null;
  environment: string;
  connectionStringSource: DbConnectionSource;
  sourceDescription?: string | null;
  safeConnectionInfo?: string | null;
}

export interface DbApplyResponse {
  applied: boolean;
  canConnect: boolean;
  migrationsApplied: boolean;
  lastError?: string | null;
  environment: string;
  connectionStringSource: DbConnectionSource;
  sourceDescription?: string | null;
  safeConnectionInfo?: string | null;
}

export interface DbMigrationsResponse {
  canConnect: boolean;
  lastError?: string | null;
  allMigrations?: string[] | null;
  appliedMigrations?: string[] | null;
  pendingMigrations?: string[] | null;
}

export interface DbMigrationsApplyResponse {
  applied: boolean;
  lastError?: string | null;
  appliedMigrations?: string[] | null;
  pendingMigrations?: string[] | null;
}

export interface DbSetupFormValues {
  host: string;
  port: number;
  database: string;
  username: string;
  password: string;
}

const LOCAL_STORAGE_KEY = "myis:lastSuccessfulDbConfig";

type PersistedDbConfig = DbSetupFormValues;

const loadLastSuccessfulDbConfig = (): Partial<DbSetupFormValues> | null => {
  try {
    if (typeof window === "undefined" || !window.localStorage) {
      return null;
    }

    const raw = window.localStorage.getItem(LOCAL_STORAGE_KEY);
    if (!raw) return null;

    const parsed = JSON.parse(raw) as PersistedDbConfig;

    // Базовая валидация сохранённой конфигурации
    if (!parsed.host || !parsed.database || !parsed.username) {
      return null;
    }
    if (typeof parsed.port !== "number" || parsed.port <= 0) {
      return null;
    }

    return parsed;
  } catch {
    // При любой ошибке парсинга/доступа к localStorage просто игнорируем кеш
    return null;
  }
};

const saveLastSuccessfulDbConfig = (values: DbSetupFormValues): void => {
  try {
    if (typeof window === "undefined" || !window.localStorage) {
      return;
    }

    // ВНИМАНИЕ: пароль сохраняется в localStorage в открытом виде.
    // Это приемлемо для dev-сценария настройки MyIS, но в production
    // может потребоваться более безопасный механизм хранения.
    window.localStorage.setItem(LOCAL_STORAGE_KEY, JSON.stringify(values));
  } catch {
    // Игнорируем ошибки записи (например, disabled localStorage)
  }
};

const parseConfigFromRawDescription = (
  raw?: string | null
): Partial<DbSetupFormValues> | null => {
  if (!raw) return null;

  // Ищем подстроку внутри скобок: (...).
  const start = raw.indexOf("(");
  const end = raw.lastIndexOf(")");
  if (start === -1 || end === -1 || end <= start + 1) {
    return null;
  }

  const inside = raw.slice(start + 1, end);
  const parts = inside.split(";");

  let host: string | undefined;
  let port: number | undefined;
  let database: string | undefined;
  let username: string | undefined;

  for (const part of parts) {
    const [rawKey, rawValue] = part.split("=").map((s) => s.trim());
    if (!rawKey || rawValue === undefined) continue;

    const key = rawKey.toLowerCase();
    if (key === "host") {
      host = rawValue;
    } else if (key === "port") {
      const parsedPort = Number(rawValue);
      if (!Number.isNaN(parsedPort) && parsedPort > 0) {
        port = parsedPort;
      }
    } else if (key === "database") {
      database = rawValue;
    } else if (key === "username") {
      username = rawValue;
    }
  }

  if (!host && !port && !database && !username) {
    return null;
  }

  const result: Partial<DbSetupFormValues> = {};
  if (host) result.host = host;
  if (port) result.port = port;
  if (database) result.database = database;
  if (username) result.username = username;
  // password не трогаем, оставляем как есть
  return result;
};

type StatusState =
  | { kind: "idle" }
  | { kind: "loading" }
  | { kind: "loaded"; status: DbStatusResponse }
  | { kind: "error"; message: string };

type MigrationsState =
  | { kind: "idle" }
  | { kind: "loading" }
  | { kind: "loaded"; status: DbMigrationsResponse }
  | { kind: "error"; message: string };

const DbSetupPage: React.FC = () => {
  const [form] = Form.useForm();
  const [statusState, setStatusState] = useState<StatusState>({ kind: "idle" });
  const [migrationsState, setMigrationsState] = useState<MigrationsState>({
    kind: "idle",
  });
  const [testing, setTesting] = useState(false);
  const [applying, setApplying] = useState(false);
  const [applyingMigrations, setApplyingMigrations] = useState(false);
  const [hasLocalConfig, setHasLocalConfig] = useState(false);
  const prefillFromStatusRef = useRef(false);
  const navigate = useNavigate();

  useEffect(() => {
    const lastConfig = loadLastSuccessfulDbConfig();
    if (lastConfig) {
      form.setFieldsValue(lastConfig);
      setHasLocalConfig(true);
    }
  }, [form]);

  useEffect(() => {
    if (prefillFromStatusRef.current) return;
    if (hasLocalConfig) return;
    if (statusState.kind !== "loaded") return;

    const status = statusState.status;

    if (!status.configured || !status.canConnect) {
      prefillFromStatusRef.current = true;
      return;
    }

    // Не перетирать пользовательский ввод
    if (form.isFieldsTouched()) {
      prefillFromStatusRef.current = true;
      return;
    }

    const fromStatus = parseConfigFromRawDescription(
      status.rawSourceDescription
    );

    if (fromStatus) {
      const currentValues = form.getFieldsValue() as DbSetupFormValues;

      form.setFieldsValue({
        ...currentValues,
        host: fromStatus.host ?? currentValues.host ?? "localhost",
        port: fromStatus.port ?? currentValues.port ?? 5432,
        database: fromStatus.database ?? currentValues.database ?? "",
        username: fromStatus.username ?? currentValues.username ?? "",
        // password оставляем как есть (из текущих значений формы)
        // runMigrations не используется
      });
    }

    prefillFromStatusRef.current = true;
  }, [statusState, hasLocalConfig, form]);

  useEffect(() => {
    let cancelled = false;

    const loadStatus = async () => {
      setStatusState({ kind: "loading" });

      try {
        const response = await fetch("/api/admin/db-status", {
          method: "GET",
          credentials: "include",
        });

        if (!response.ok) {
          const text = await response.text();
          if (!cancelled) {
            setStatusState({
              kind: "error",
              message:
                text ||
                t("db.setup.errors.loadStatus", { status: response.status }),
            });
          }
          return;
        }

        const data = (await response.json()) as DbStatusResponse;
        if (!cancelled) {
          setStatusState({ kind: "loaded", status: data });
        }
      } catch (error) {
        if (cancelled) return;

        const msg =
          error instanceof Error ? error.message : t("common.error.unknownNetwork");

        setStatusState({
          kind: "error",
          message: t("db.setup.errors.cannotGetStatus", { message: msg }),
        });
      }
    };

    void loadStatus();

    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    let cancelled = false;

    const loadMigrations = async () => {
      setMigrationsState({ kind: "loading" });

      try {
        const response = await fetch("/api/admin/db-migrations", {
          method: "GET",
          credentials: "include",
        });

        if (!response.ok) {
          const text = await response.text();
          if (!cancelled) {
            setMigrationsState({
              kind: "error",
              message:
                text ||
                t("db.setup.migrations.errors.load", { status: response.status }),
            });
          }
          return;
        }

        const data = (await response.json()) as DbMigrationsResponse;
        if (!cancelled) {
          setMigrationsState({ kind: "loaded", status: data });
        }
      } catch (error) {
        if (cancelled) return;

        const msg =
          error instanceof Error ? error.message : t("common.error.unknownNetwork");

        setMigrationsState({
          kind: "error",
          message: t("db.setup.migrations.errors.loadUnexpected", { message: msg }),
        });
      }
    };

    void loadMigrations();

    return () => {
      cancelled = true;
    };
  }, []);

  const buildRequestBody = (values: DbSetupFormValues) => {
    return {
      host: values.host,
      port: values.port,
      database: values.database,
      username: values.username,
      password: values.password,
      // Дополнительные поля backend-контракта, не отображаемые в форме
      useSsl: false,
      trustServerCertificate: false,
      timeoutSeconds: 30,
    };
  };

  const handleTestConnection = async () => {
    try {
      const values = await form.validateFields();
      setTesting(true);

      const response = await fetch("/api/admin/db-config/test", {
        method: "POST",
        credentials: "include",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(buildRequestBody(values)),
      });

      if (!response.ok) {
        const text = await response.text();
        message.error(
          text ||
            t("db.setup.errors.testHttp", { status: response.status })
        );
        return;
      }

      const data = (await response.json()) as DbTestResponse;

      if (data.canConnect) {
        message.success(t("db.setup.success.testOk"));

        const currentValues = form.getFieldsValue() as DbSetupFormValues;
        saveLastSuccessfulDbConfig(currentValues);
      } else {
        message.warning(
          data.lastError
            ? t("db.setup.errors.testFailedWithDetails", { lastError: data.lastError })
            : t("db.setup.errors.testFailed")
        );
      }
    } catch (error) {
      if (error instanceof Error) {
        message.error(
          t("db.setup.errors.testUnexpected", { message: error.message })
        );
      }
    } finally {
      setTesting(false);
    }
  };

  const handleApply = async (values: DbSetupFormValues) => {
    setApplying(true);
    try {
      const response = await fetch("/api/admin/db-config/apply", {
        method: "POST",
        credentials: "include",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(buildRequestBody(values)),
      });

      if (response.status === 403) {
        message.error(
          t("db.setup.errors.applyForbidden")
        );
        return;
      }

      if (!response.ok) {
        const text = await response.text();
        message.error(
          text || t("db.setup.errors.applyHttp", { status: response.status })
        );
        return;
      }

      const data = (await response.json()) as DbApplyResponse;

      if (!data.applied) {
        message.error(
          data.lastError
            ? t("db.setup.errors.applyFailedWithDetails", {
                lastError: data.lastError,
              })
            : t("db.setup.errors.applyFailed")
        );
        return;
      }

      if (!data.canConnect) {
        message.warning(
          data.lastError
            ? t("db.setup.warnings.appliedButCannotConnectWithDetails", {
                lastError: data.lastError,
              })
            : t("db.setup.warnings.appliedButCannotConnect")
        );
      } else {
        message.success(t("db.setup.success.applyOk"));

        const currentValues = form.getFieldsValue() as DbSetupFormValues;
        saveLastSuccessfulDbConfig(currentValues);
      }

      navigate("/login", { replace: true });
    } catch (error) {
      const msg =
        error instanceof Error ? error.message : t("common.error.unknownNetwork");
      message.error(t("db.setup.errors.applyUnexpected", { message: msg }));
    } finally {
      setApplying(false);
    }
  };

  const handleCancel = () => {
    navigate("/login", { replace: true });
  };

  const handleApplyMigrations = async () => {
    setApplyingMigrations(true);
    try {
      const response = await fetch("/api/admin/db-migrations/apply", {
        method: "POST",
        credentials: "include",
      });

      if (response.status === 403) {
        message.error(t("db.setup.migrations.applyForbidden"));
        return;
      }

      if (!response.ok) {
        const text = await response.text();
        message.error(
          text || t("db.setup.migrations.errors.applyHttp", { status: response.status })
        );
        return;
      }

      const data = (await response.json()) as DbMigrationsApplyResponse;
      if (!data.applied) {
        message.error(
          data.lastError
            ? t("db.setup.migrations.errors.applyFailedWithDetails", { lastError: data.lastError })
            : t("db.setup.migrations.errors.applyFailed")
        );
        return;
      }

      message.success(t("db.setup.migrations.applyOk"));
      setMigrationsState({
        kind: "loaded",
        status: {
          canConnect: true,
          appliedMigrations: data.appliedMigrations ?? [],
          pendingMigrations: data.pendingMigrations ?? [],
        },
      });
    } catch (error) {
      const msg =
        error instanceof Error ? error.message : t("common.error.unknownNetwork");
      message.error(t("db.setup.migrations.errors.applyUnexpected", { message: msg }));
    } finally {
      setApplyingMigrations(false);
    }
  };

  const currentStatusAlert = () => {
    if (statusState.kind === "loading" || statusState.kind === "idle") {
      return (
        <Alert
          data-testid="db-setup-status-alert"
          type="info"
          message={t("db.setup.currentStatus.loading")}
          showIcon
        />
      );
    }

    if (statusState.kind === "error") {
      return (
        <Alert
          data-testid="db-setup-status-alert"
          type="error"
          message={t("db.setup.currentStatus.error.title")}
          description={statusState.message}
          showIcon
        />
      );
    }

    const status = statusState.status;
    if (!status.configured) {
      return (
        <Alert
          data-testid="db-setup-status-alert"
          type="warning"
          message={t("db.setup.currentStatus.notConfigured.title")}
          description={t("db.setup.currentStatus.notConfigured.description")}
          showIcon
        />
      );
    }

    if (!status.canConnect) {
      return (
        <Alert
          data-testid="db-setup-status-alert"
          type="error"
          message={t("db.setup.currentStatus.cannotConnect.title")}
          description={
            status.lastError ||
            t("db.setup.currentStatus.cannotConnect.descriptionFallback")
          }
          showIcon
        />
      );
    }

    return (
      <Alert
        data-testid="db-setup-status-alert"
        type="success"
        message={t("db.setup.currentStatus.ok.title")}
        description={
          status.rawSourceDescription ||
          t("db.setup.currentStatus.ok.description", {
            environment: status.environment,
            connectionStringSource: status.connectionStringSource,
          })
        }
        showIcon
      />
    );
  };

  const migrationsAlert = () => {
    if (migrationsState.kind === "loading" || migrationsState.kind === "idle") {
      return (
        <Alert
          type="info"
          message={t("db.setup.migrations.loading")}
          showIcon
        />
      );
    }

    if (migrationsState.kind === "error") {
      return (
        <Alert
          type="error"
          message={t("db.setup.migrations.errors.title")}
          description={migrationsState.message}
          showIcon
        />
      );
    }

    const status = migrationsState.status;

    if (!status.canConnect) {
      return (
        <Alert
          type="warning"
          message={t("db.setup.migrations.cannotConnect")}
          description={status.lastError || t("db.setup.migrations.cannotConnectFallback")}
          showIcon
        />
      );
    }

    const all = status.allMigrations ?? [];
    const applied = status.appliedMigrations ?? [];
    const pending = status.pendingMigrations ?? [];
    const lastApplied = applied.length > 0 ? applied[applied.length - 1] : null;
    const lastAvailable = all.length > 0 ? all[all.length - 1] : null;

    return (
      <Alert
        type={pending.length > 0 ? "warning" : "success"}
        message={t("db.setup.migrations.title")}
        description={
          <div>
            <div>
              {t("db.setup.migrations.totalCount", { count: all.length })}
            </div>
            <div>
              {t("db.setup.migrations.appliedCount", { count: applied.length })}
            </div>
            <div>
              {t("db.setup.migrations.pendingCount", { count: pending.length })}
            </div>
            {lastAvailable && (
              <div>
                {t("db.setup.migrations.latestAvailable", { id: lastAvailable })}
              </div>
            )}
            {lastApplied && (
              <div>
                {t("db.setup.migrations.latestApplied", { id: lastApplied })}
              </div>
            )}
            {pending.length > 0 && (
              <div style={{ marginTop: 8 }}>
                <div>{t("db.setup.migrations.pendingList")}</div>
                <div style={{ display: "flex", flexWrap: "wrap", gap: 6 }}>
                  {pending.map((item) => (
                    <Text key={item} code>
                      {item}
                    </Text>
                  ))}
                </div>
              </div>
            )}
            <div style={{ marginTop: 12 }}>
              <Button
                type="primary"
                onClick={handleApplyMigrations}
                loading={applyingMigrations}
              >
                {t("db.setup.migrations.apply")}
              </Button>
            </div>
          </div>
        }
        showIcon
      />
    );
  };

  return (
    <AuthPageLayout
      title={t("db.setup.title")}
      description={
        <>
          {t("db.setup.description.part1")} <Text code>appsettings.Local.json</Text>{" "}
          {t("db.setup.description.part2")}
        </>
      }
      cardWidth={720}
    >
      <div style={{ marginBottom: 16 }}>{currentStatusAlert()}</div>
      <div style={{ marginBottom: 16 }}>{migrationsAlert()}</div>

      <Form
        data-testid="db-setup-form"
        layout="vertical"
        form={form}
        initialValues={{
          host: "localhost",
          port: 5432,
          database: "",
          username: "",
          password: "",
        }}
        onFinish={handleApply}
      >
        <Form.Item
          label={t("db.setup.form.host.label")}
          name="host"
          rules={[{ required: true, message: t("db.setup.form.host.required") }]}
        >
          <Input
            data-testid="db-setup-host-input"
            placeholder={t("db.setup.form.host.placeholder")}
          />
        </Form.Item>

        <Form.Item
          label={t("db.setup.form.port.label")}
          name="port"
          rules={[{ required: true, message: t("db.setup.form.port.required") }]}
        >
          <InputNumber
            data-testid="db-setup-port-input"
            style={{ width: "100%" }}
            min={1}
            max={65535}
          />
        </Form.Item>

        <Form.Item
          label={t("db.setup.form.database.label")}
          name="database"
          rules={[{ required: true, message: t("db.setup.form.database.required") }]}
        >
          <Input
            data-testid="db-setup-database-input"
            placeholder={t("db.setup.form.database.placeholder")}
          />
        </Form.Item>

        <Form.Item
          label={t("db.setup.form.username.label")}
          name="username"
          rules={[{ required: true, message: t("db.setup.form.username.required") }]}
        >
          <Input data-testid="db-setup-username-input" />
        </Form.Item>

        <Form.Item
          label={t("db.setup.form.password.label")}
          name="password"
          rules={[{ required: true, message: t("db.setup.form.password.required") }]}
        >
          <Input.Password data-testid="db-setup-password-input" />
        </Form.Item>

        <Form.Item>
          <div
            style={{
              display: "flex",
              gap: 8,
              justifyContent: "flex-end",
              flexWrap: "wrap",
            }}
          >
            <Button htmlType="button" onClick={handleCancel}>
              {t("common.actions.cancel")}
            </Button>
            <Button
              data-testid="db-setup-test-connection-button"
              type="default"
              htmlType="button"
              loading={testing}
              onClick={handleTestConnection}
            >
              {t("db.setup.actions.test")}
            </Button>
            <Button
              data-testid="db-setup-apply-button"
              type="primary"
              htmlType="submit"
              loading={applying}
            >
              {t("db.setup.actions.apply")}
            </Button>
          </div>
        </Form.Item>
      </Form>
    </AuthPageLayout>
  );
};

export { DbSetupPage };
