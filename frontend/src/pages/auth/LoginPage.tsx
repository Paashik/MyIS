import React, { useState } from "react";
import { Alert, Button, Form, Input, Typography } from "antd";
import { useLocation, useNavigate } from "react-router-dom";
import { useAuth, User } from "../../auth/AuthContext";
import { AuthPageLayout } from "../../components/layout/AuthPageLayout";


interface LoginFormValues {
  login: string;
  password: string;
}

interface LoginResponseDto {
  user: {
    id: string;
    login: string;
    fullName: string;
    roles: string[];
  };
}

const mapUserDtoToUser = (dto: LoginResponseDto["user"]): User => ({
  id: String(dto.id),
  login: dto.login,
  fullName: dto.fullName,
  roles: Array.isArray(dto.roles) ? dto.roles.map(String) : [],
});

const LoginPage: React.FC = () => {
  const [form] = Form.useForm();
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const navigate = useNavigate();
  const location = useLocation();
  const { setUser } = useAuth();

  const handleFinish = async (values: LoginFormValues) => {
    setSubmitting(true);
    setError(null);

    try {
      const response = await fetch("/api/auth/login", {
        method: "POST",
        credentials: "include",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          login: values.login,
          password: values.password,
        }),
      });

      if (response.status === 401) {
        setError("Неверный логин или пароль");
        return;
      }

      if (response.status === 403) {
        setError("Учетная запись заблокирована");
        return;
      }

      if (!response.ok) {
        setError("Ошибка сервера, повторите попытку позже");
        return;
      }

      const data = (await response.json()) as LoginResponseDto;
      const user = mapUserDtoToUser(data.user);
      setUser(user);

      const state = location.state as { from?: Location } | undefined;
      const fromPath =
        state?.from && "pathname" in state.from
          ? state.from.pathname
          : "/";

      navigate(fromPath, { replace: true });
    } catch (e) {
      setError("Ошибка сети, попробуйте позже");
    } finally {
      setSubmitting(false);
    }
  };

  const handleGoToDbSetup = () => {
    navigate("/db-setup");
  };

  return (
    <AuthPageLayout title="Вход в MyIS">
      {error && (
        <Alert
          type="error"
          message={error}
          showIcon
          style={{ marginBottom: 16 }}
        />
      )}

      <Form
        form={form}
        layout="vertical"
        initialValues={{ login: "", password: "" }}
        onFinish={handleFinish}
      >
        <Form.Item
          label="Логин"
          name="login"
          rules={[{ required: true, message: "Введите логин" }]}
        >
          <Input autoComplete="username" />
        </Form.Item>

        <Form.Item
          label="Пароль"
          name="password"
          rules={[{ required: true, message: "Введите пароль" }]}
        >
          <Input.Password autoComplete="current-password" />
        </Form.Item>

        <Form.Item>
          <Button
            type="primary"
            htmlType="submit"
            loading={submitting}
            block
          >
            Войти
          </Button>
        </Form.Item>
      </Form>

      <Button
        type="link"
        block
        style={{ padding: 0, marginTop: 8 }}
        onClick={handleGoToDbSetup}
      >
        Настроить подключение к БД
      </Button>
    </AuthPageLayout>
  );
};

export { LoginPage };