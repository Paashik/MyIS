import React, { useCallback, useEffect, useMemo, useState } from "react";
import { Alert, Button, Card, Form, Input, Select, Typography, message } from "antd";
import Switch from "antd/es/switch";
import { useLocation, useNavigate, useParams } from "react-router-dom";

import { CommandBar } from "../../../../components/ui/CommandBar";
import { useCan } from "../../../../core/auth/permissions";
import { t } from "../../../../core/i18n/t";
import {
  activateAdminUser,
  createAdminUser,
  deactivateAdminUser,
  getAdminEmployees,
  getAdminUserById,
  updateAdminUser,
} from "../api/adminSecurityApi";
import type { AdminEmployeeDto, AdminUserDetailsDto, CreateAdminUserPayload, UpdateAdminUserPayload } from "../api/types";

type Mode = "create" | "edit";

export const UserCardPage: React.FC = () => {
  const canEditUsers = useCan("Admin.Security.EditUsers");
  const navigate = useNavigate();
  const location = useLocation();
  const { id } = useParams<{ id?: string }>();

  const mode: Mode = useMemo(() => {
    if (location.pathname.endsWith("/new")) return "create";
    return id ? "edit" : "create";
  }, [id, location.pathname]);

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [entity, setEntity] = useState<AdminUserDetailsDto | null>(null);
  const [employees, setEmployees] = useState<AdminEmployeeDto[]>([]);
  const [form] = Form.useForm();

  const loadSupport = useCallback(async () => {
    try {
      const employeesData = await getAdminEmployees({ isActive: true });
      setEmployees(employeesData);
    } catch {
      // ignore
    }
  }, []);

  const load = useCallback(async () => {
    if (mode !== "edit" || !id) return;

    setLoading(true);
    setError(null);
    try {
      const dto = await getAdminUserById(id);
      setEntity(dto);
      form.setFieldsValue({
        login: dto.login,
        employeeId: dto.employeeId ?? undefined,
        isActive: dto.isActive,
      });
    } catch (e) {
      setError((e as Error).message || t("common.error.unknownNetwork"));
    } finally {
      setLoading(false);
    }
  }, [form, id, mode]);

  useEffect(() => {
    void loadSupport();

    if (mode === "create") {
      setEntity(null);
      form.resetFields();
      form.setFieldsValue({ isActive: true });
      return;
    }

    void load();
  }, [form, load, loadSupport, mode]);

  const onCancel = () => {
    navigate("/administration/security/users");
  };

  const onSave = async () => {
    const values = await form.validateFields();
    try {
      if (mode === "create") {
        const payload: CreateAdminUserPayload = {
          login: values.login,
          password: values.password,
          isActive: !!values.isActive,
          employeeId: values.employeeId ?? null,
        };
        await createAdminUser(payload);
      } else {
        if (!id) return;
        const payload: UpdateAdminUserPayload = {
          login: values.login,
          isActive: !!values.isActive,
          employeeId: values.employeeId ?? null,
        };
        await updateAdminUser(id, payload);
      }

      message.success(t("common.actions.save"));
      navigate("/administration/security/users", { replace: true });
    } catch (e) {
      message.error((e as Error).message);
    }
  };

  const onToggleActive = async () => {
    if (!id || !entity) return;
    try {
      if (entity.isActive) await deactivateAdminUser(id);
      else await activateAdminUser(id);
      message.success(t("common.actions.save"));
      await load();
    } catch (e) {
      message.error((e as Error).message);
    }
  };

  const employeeOptions = useMemo(
    () =>
      employees.map((e) => ({
        value: e.id,
        label: e.shortName || e.fullName,
      })),
    [employees]
  );

  return (
    <div data-testid="administration-security-users-card">
      <CommandBar
        left={
          <Typography.Title level={2} style={{ margin: 0 }}>
            {mode === "create" ? t("common.actions.create") : t("common.actions.edit")}
          </Typography.Title>
        }
        right={
          <>
            {mode === "edit" && (
              <Button onClick={() => void onToggleActive()} disabled={!canEditUsers || loading}>
                {entity?.isActive ? "Деактивировать" : "Активировать"}
              </Button>
            )}
            <Button onClick={onCancel}>{t("common.actions.cancel")}</Button>
            <Button type="primary" loading={loading} onClick={() => void onSave()} disabled={!canEditUsers}>
              {t("common.actions.save")}
            </Button>
          </>
        }
      />

      {!canEditUsers && (
        <Alert type="warning" showIcon message={t("settings.forbidden")} style={{ marginBottom: 12 }} />
      )}

      {error && <Alert type="error" showIcon message={error} style={{ marginBottom: 12 }} />}

      <Card>
        <Form form={form} layout="vertical" disabled={!canEditUsers}>
          <Form.Item
            label={t("settings.security.users.columns.login")}
            name="login"
            rules={[{ required: true, message: "Введите логин" }]}
          >
            <Input />
          </Form.Item>

          {mode === "create" && (
            <Form.Item
              label="Пароль"
              name="password"
              rules={[{ required: true, message: "Введите пароль" }]}
            >
              <Input.Password />
            </Form.Item>
          )}

          <Form.Item label={t("settings.security.users.columns.employee")} name="employeeId">
            <Select allowClear showSearch optionFilterProp="label" options={employeeOptions} />
          </Form.Item>

          <Form.Item label={t("settings.security.common.columns.active")} name="isActive" valuePropName="checked">
            <Switch />
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
};
