import React, { useEffect, useRef, useState } from "react";
import {
  Alert,
  Button,
  Checkbox,
  Form,
  Input,
  InputNumber,
  Typography,
  message,
} from "antd";
import { useNavigate } from "react-router-dom";
import { AuthPageLayout } from "../../components/layout/AuthPageLayout";

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

export interface DbSetupFormValues {
  host: string;
  port: number;
  database: string;
  username: string;
  password: string;
  runMigrations: boolean;
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
  // password и runMigrations не трогаем, оставляем как есть
  return result;
};

type StatusState =
  | { kind: "idle" }
  | { kind: "loading" }
  | { kind: "loaded"; status: DbStatusResponse }
  | { kind: "error"; message: string };

const DbSetupPage: React.FC = () => {
  const [form] = Form.useForm();
  const [statusState, setStatusState] = useState<StatusState>({ kind: "idle" });
  const [testing, setTesting] = useState(false);
  const [applying, setApplying] = useState(false);
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
        // runMigrations также оставляем без изменений
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
                `Ошибка при загрузке статуса базы данных (HTTP ${response.status})`,
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
          error instanceof Error ? error.message : "Неизвестная ошибка сети";

        setStatusState({
          kind: "error",
          message: `Не удалось получить статус базы данных: ${msg}`,
        });
      }
    };

    void loadStatus();

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
            `Ошибка при проверке подключения (HTTP ${response.status})`
        );
        return;
      }

      const data = (await response.json()) as DbTestResponse;

      if (data.canConnect) {
        message.success("Подключение успешно установлено.");

        const currentValues = form.getFieldsValue() as DbSetupFormValues;
        saveLastSuccessfulDbConfig(currentValues);
      } else {
        message.warning(
          data.lastError
            ? `Не удалось подключиться: ${data.lastError}`
            : "Не удалось подключиться к базе данных."
        );
      }
    } catch (error) {
      if (error instanceof Error) {
        message.error(`Ошибка при проверке подключения: ${error.message}`);
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
          "Сохранение настроек базы данных запрещено в этом окружении (Production)."
        );
        return;
      }

      if (!response.ok) {
        const text = await response.text();
        message.error(
          text || `Ошибка при сохранении настроек (HTTP ${response.status})`
        );
        return;
      }

      const data = (await response.json()) as DbApplyResponse;

      if (!data.applied) {
        message.error(
          data.lastError
            ? `Не удалось сохранить конфигурацию: ${data.lastError}`
            : "Не удалось сохранить конфигурацию базы данных."
        );
        return;
      }

      if (!data.canConnect) {
        message.warning(
          data.lastError
            ? `Конфигурация сохранена, но подключиться не удалось: ${data.lastError}`
            : "Конфигурация сохранена, но подключиться к БД не удалось."
        );
      } else if (!data.migrationsApplied) {
        message.warning(
          data.lastError
            ? `Подключение успешно, но миграции не были применены: ${data.lastError}`
            : "Подключение успешно, но миграции не были применены."
        );
      } else {
        message.success("Конфигурация сохранена и миграции успешно применены.");

        const currentValues = form.getFieldsValue() as DbSetupFormValues;
        saveLastSuccessfulDbConfig(currentValues);
      }

      navigate("/login", { replace: true });
    } catch (error) {
      const msg =
        error instanceof Error ? error.message : "Неизвестная ошибка сети";
      message.error(`Ошибка при сохранении настроек: ${msg}`);
    } finally {
      setApplying(false);
    }
  };

  const handleCancel = () => {
    navigate("/login", { replace: true });
  };

  const currentStatusAlert = () => {
    if (statusState.kind === "loading" || statusState.kind === "idle") {
      return (
        <Alert
          type="info"
          message="Проверка текущего состояния базы данных..."
          showIcon
        />
      );
    }

    if (statusState.kind === "error") {
      return (
        <Alert
          type="error"
          message="Ошибка статуса базы данных"
          description={statusState.message}
          showIcon
        />
      );
    }

    const status = statusState.status;
    if (!status.configured) {
      return (
        <Alert
          type="warning"
          message="База данных не сконфигурирована"
          description="Строка подключения не настроена. Заполните форму ниже и сохраните конфигурацию."
          showIcon
        />
      );
    }

    if (!status.canConnect) {
      return (
        <Alert
          type="error"
          message="Не удается подключиться к базе данных"
          description={
            status.lastError ||
            "Система не может подключиться к базе данных по текущей конфигурации."
          }
          showIcon
        />
      );
    }

    return (
      <Alert
        type="success"
        message="Подключение к базе данных успешно"
        description={
          status.rawSourceDescription ||
          `Окружение: ${status.environment}. Источник строки подключения: ${status.connectionStringSource}.`
        }
        showIcon
      />
    );
  };

  return (
    <AuthPageLayout
      title="Настройка подключения к базе данных"
      description={
        <>
          Укажите параметры подключения к PostgreSQL. Эти настройки будут
          сохранены в{" "}
          <Text code>appsettings.Local.json</Text> (в режиме Development) и
          будут использоваться backend-сервисом MyIS.
        </>
      }
      cardWidth={720}
    >
      <div style={{ marginBottom: 16 }}>{currentStatusAlert()}</div>

      <Form
        layout="vertical"
        form={form}
        initialValues={{
          host: "localhost",
          port: 5432,
          database: "",
          username: "",
          password: "",
          runMigrations: true,
        }}
        onFinish={handleApply}
      >
        <Form.Item
          label="Host"
          name="host"
          rules={[{ required: true, message: "Укажите хост БД" }]}
        >
          <Input placeholder="localhost" />
        </Form.Item>

        <Form.Item
          label="Port"
          name="port"
          rules={[{ required: true, message: "Укажите порт БД" }]}
        >
          <InputNumber style={{ width: "100%" }} min={1} max={65535} />
        </Form.Item>

        <Form.Item
          label="Database"
          name="database"
          rules={[{ required: true, message: "Укажите имя базы данных" }]}
        >
          <Input placeholder="myis" />
        </Form.Item>

        <Form.Item
          label="Username"
          name="username"
          rules={[{ required: true, message: "Укажите пользователя БД" }]}
        >
          <Input />
        </Form.Item>

        <Form.Item
          label="Password"
          name="password"
          rules={[{ required: true, message: "Укажите пароль" }]}
        >
          <Input.Password />
        </Form.Item>

        <Form.Item name="runMigrations" valuePropName="checked">
          <Checkbox>Запустить миграции после сохранения</Checkbox>
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
              Отмена
            </Button>
            <Button
              type="default"
              htmlType="button"
              loading={testing}
              onClick={handleTestConnection}
            >
              Проверить подключение
            </Button>
            <Button type="primary" htmlType="submit" loading={applying}>
              Сохранить и применить
            </Button>
          </div>
        </Form.Item>
      </Form>
    </AuthPageLayout>
  );
};

export { DbSetupPage };