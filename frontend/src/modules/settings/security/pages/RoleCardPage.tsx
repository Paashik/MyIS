import React, { useCallback, useEffect, useMemo, useState } from "react";
import { Alert, Button, Card, Form, Input, Typography, message } from "antd";
import { useLocation, useNavigate, useParams } from "react-router-dom";

import { CommandBar } from "../../../../components/ui/CommandBar";
import { useCan } from "../../../../core/auth/permissions";
import { t } from "../../../../core/i18n/t";
import { createAdminRole, getAdminRoles, updateAdminRole } from "../api/adminSecurityApi";
import type { AdminRoleDto, CreateAdminRolePayload, UpdateAdminRolePayload } from "../api/types";

type Mode = "create" | "edit";

export const RoleCardPage: React.FC = () => {
  const canEdit = useCan("Admin.Security.EditRoles");
  const navigate = useNavigate();
  const location = useLocation();
  const { id } = useParams<{ id?: string }>();

  const mode: Mode = useMemo(() => {
    if (location.pathname.endsWith("/new")) return "create";
    return id ? "edit" : "create";
  }, [id, location.pathname]);

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [entity, setEntity] = useState<AdminRoleDto | null>(null);
  const [form] = Form.useForm();

  const load = useCallback(async () => {
    if (mode !== "edit" || !id) return;

    setLoading(true);
    setError(null);
    try {
      const all = await getAdminRoles();
      const found = all.find((x) => x.id === id) ?? null;
      setEntity(found);
      if (!found) {
        setError(t("requests.edit.error.notFound.title"));
        return;
      }

      form.setFieldsValue({
        code: found.code,
        name: found.name,
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
      return;
    }

    void load();
  }, [form, load, mode]);

  const onCancel = () => {
    navigate("/administration/security/roles");
  };

  const onSave = async () => {
    const values = await form.validateFields();
    try {
      if (mode === "create") {
        const payload: CreateAdminRolePayload = {
          code: values.code,
          name: values.name,
        };
        await createAdminRole(payload);
      } else {
        if (!id) return;
        const payload: UpdateAdminRolePayload = { name: values.name };
        await updateAdminRole(id, payload);
      }

      message.success(t("common.actions.save"));
      navigate("/administration/security/roles", { replace: true });
    } catch (e) {
      message.error((e as Error).message);
    }
  };

  return (
    <div data-testid="administration-security-roles-card">
      <CommandBar
        left={
          <Typography.Title level={2} style={{ margin: 0 }}>
            {mode === "create" ? t("common.actions.create") : t("common.actions.edit")}
          </Typography.Title>
        }
        right={
          <>
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
            label={t("settings.security.roles.columns.code")}
            name="code"
            rules={[{ required: true, message: "Введите код" }]}
          >
            <Input disabled={mode === "edit"} />
          </Form.Item>

          <Form.Item
            label={t("settings.security.roles.columns.name")}
            name="name"
            rules={[{ required: true, message: "Введите название" }]}
          >
            <Input />
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
};

