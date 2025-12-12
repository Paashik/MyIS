import React, { useState } from "react";
import { Alert, Button, Form, Input, Typography } from "antd";
import { useLocation, useNavigate } from "react-router-dom";
import { useAuth, User } from "../../auth/AuthContext";
import { AuthPageLayout } from "../../components/layout/AuthPageLayout";
import { t } from "../../core/i18n/t";


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
        setError(t("auth.login.error.invalidCredentials"));
        return;
      }

      if (response.status === 403) {
        setError(t("auth.login.error.userBlocked"));
        return;
      }

      if (!response.ok) {
        setError(t("auth.login.error.server"));
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
      setError(t("auth.login.error.network"));
    } finally {
      setSubmitting(false);
    }
  };

  const handleGoToDbSetup = () => {
    navigate("/db-setup");
  };

  return (
    <AuthPageLayout title={t("auth.login.title")}>
      {error && (
        <Alert
          data-testid="login-error-alert"
          type="error"
          message={error}
          showIcon
          style={{ marginBottom: 16 }}
        />
      )}

      <Form
        data-testid="login-form"
        form={form}
        layout="vertical"
        initialValues={{ login: "", password: "" }}
        onFinish={handleFinish}
      >
        <Form.Item
          label={t("auth.login.form.login.label")}
          name="login"
          rules={[{ required: true, message: t("auth.login.form.login.required") }]}
        >
          <Input autoComplete="username" data-testid="login-login-input" />
        </Form.Item>

        <Form.Item
          label={t("auth.login.form.password.label")}
          name="password"
          rules={[{ required: true, message: t("auth.login.form.password.required") }]}
        >
          <Input.Password
            autoComplete="current-password"
            data-testid="login-password-input"
          />
        </Form.Item>

        <Form.Item>
          <Button
            data-testid="login-submit-button"
            type="primary"
            htmlType="submit"
            loading={submitting}
            block
          >
            {t("auth.login.form.submit")}
          </Button>
        </Form.Item>
      </Form>

      <Button
        data-testid="login-go-to-db-setup-button"
        type="link"
        block
        style={{ padding: 0, marginTop: 8 }}
        onClick={handleGoToDbSetup}
      >
        {t("auth.login.goToDbSetup")}
      </Button>
    </AuthPageLayout>
  );
};

export { LoginPage };
