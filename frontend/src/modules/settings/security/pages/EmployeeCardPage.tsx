import React, { useCallback, useEffect, useMemo, useState } from "react";
import { Alert, Button, Card, Form, Input, Typography, message } from "antd";
import Switch from "antd/es/switch";
import { useLocation, useNavigate, useParams } from "react-router-dom";

import { CommandBar } from "../../../../components/ui/CommandBar";
import { useCan } from "../../../../core/auth/permissions";
import { t } from "../../../../core/i18n/t";
import {
  activateAdminEmployee,
  createAdminEmployee,
  deactivateAdminEmployee,
  getAdminEmployeeById,
  updateAdminEmployee,
} from "../api/adminSecurityApi";
import type {
  AdminEmployeeDto,
  CreateAdminEmployeePayload,
  UpdateAdminEmployeePayload,
} from "../api/types";

type Mode = "create" | "edit";

export const EmployeeCardPage: React.FC = () => {
  const canEdit = useCan("Admin.Security.EditEmployees");
  const navigate = useNavigate();
  const location = useLocation();
  const { id } = useParams<{ id?: string }>();

  const mode: Mode = useMemo(() => {
    if (location.pathname.endsWith("/new")) return "create";
    return id ? "edit" : "create";
  }, [id, location.pathname]);

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [entity, setEntity] = useState<AdminEmployeeDto | null>(null);
  const [form] = Form.useForm();

  const load = useCallback(async () => {
    if (mode !== "edit" || !id) return;

    setLoading(true);
    setError(null);
    try {
      const found = await getAdminEmployeeById(id);
      setEntity(found);

      form.setFieldsValue({
        fullName: found.fullName,
        email: found.email ?? "",
        phone: found.phone ?? "",
        notes: found.notes ?? "",
        isActive: found.isActive,
      });
    } catch (e) {
      setError((e as Error).message || t("common.error.unknownNetwork"));
    } finally {
      setLoading(false);
    }
  }, [form, id, mode]);

  useEffect(() => {
    if (mode === "create") {
      setEntity(null);
      form.resetFields();
      form.setFieldsValue({ isActive: true });
      return;
    }

    void load();
  }, [form, load, mode]);

  const onCancel = () => {
    navigate("/administration/security/employees");
  };

  const onSave = async () => {
    const values = await form.validateFields();
    try {
      if (mode === "create") {
        const payload: CreateAdminEmployeePayload = {
          fullName: values.fullName,
          email: values.email,
          phone: values.phone,
          notes: values.notes,
        };
        await createAdminEmployee(payload);
      } else {
        if (!id) return;
        const payload: UpdateAdminEmployeePayload = {
          fullName: values.fullName,
          email: values.email,
          phone: values.phone,
          notes: values.notes,
        };
        await updateAdminEmployee(id, payload);
      }

      message.success(t("common.actions.save"));
      navigate("/administration/security/employees", { replace: true });
    } catch (e) {
      message.error((e as Error).message);
    }
  };

  const onToggleActive = async () => {
    if (!id || !entity) return;
    try {
      if (entity.isActive) {
        await deactivateAdminEmployee(id);
      } else {
        await activateAdminEmployee(id);
      }
      message.success(t("common.actions.save"));
      await load();
    } catch (e) {
      message.error((e as Error).message);
    }
  };

  return (
    <div data-testid="administration-security-employees-card">
      <CommandBar
        left={
          <Typography.Title level={2} style={{ margin: 0 }}>
            {mode === "create" ? t("common.actions.create") : t("common.actions.edit")}
          </Typography.Title>
        }
        right={
          <>
            {mode === "edit" && (
              <Button onClick={() => void onToggleActive()} disabled={!canEdit || loading}>
                {entity?.isActive ? "Деактивировать" : "Активировать"}
              </Button>
            )}
            <Button onClick={onCancel}>{t("common.actions.cancel")}</Button>
            <Button type="primary" loading={loading} onClick={() => void onSave()} disabled={!canEdit}>
              {t("common.actions.save")}
            </Button>
          </>
        }
      />

      {!canEdit && (
        <Alert type="warning" showIcon message={t("settings.forbidden")} style={{ marginBottom: 12 }} />
      )}

      {error && <Alert type="error" showIcon message={error} style={{ marginBottom: 12 }} />}

      <Card>
        <Form form={form} layout="vertical" disabled={!canEdit}>
          <Form.Item
            label={t("settings.security.employees.columns.fullName")}
            name="fullName"
            rules={[{ required: true, message: "Введите ФИО" }]}
          >
            <Input />
          </Form.Item>

          <Form.Item label="Email" name="email">
            <Input />
          </Form.Item>

          <Form.Item label={t("settings.security.employees.columns.phone")} name="phone">
            <Input />
          </Form.Item>

          <Form.Item label="Примечания" name="notes">
            <Input.TextArea rows={3} />
          </Form.Item>

          <Form.Item label={t("settings.security.common.columns.active")} name="isActive" valuePropName="checked">
            <Switch disabled />
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
};
