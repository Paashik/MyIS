import React, { useEffect, useState } from "react";
import {
  Alert,
  Button,
  Card,
  Checkbox,
  Form,
  Input,
  InputNumber,
  Space,
  Typography,
  message,
} from "antd";
import { useNavigate } from "react-router-dom";

const { Title, Paragraph, Text } = Typography;

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
  const navigate = useNavigate();

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
    <Space
      direction="vertical"
      size="large"
      style={{ width: "100%", maxWidth: 800 }}
    >
      <div>
        <Title level={2}>Настройка подключения к базе данных</Title>
        <Paragraph>
          Укажите параметры подключения к PostgreSQL. Эти настройки будут
          сохранены в <Text code>appsettings.Local.json</Text> (в режиме
          Development) и будут использоваться backend-сервисом MyIS.
        </Paragraph>
      </div>

      <Card title="Текущее состояние подключения">{currentStatusAlert()}</Card>

      <Card title="Конфигурация подключения">
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
            <Space>
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
            </Space>
          </Form.Item>
        </Form>
      </Card>
    </Space>
  );
};

export { DbSetupPage };