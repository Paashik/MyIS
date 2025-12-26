import React, { useCallback, useEffect, useMemo, useState } from "react";
import { Alert, Button, Card, Form, Input, Select, Typography, message } from "antd";
import Switch from "antd/es/switch";
import { useLocation, useNavigate, useParams } from "react-router-dom";

import { CommandBar } from "../../../../../components/ui/CommandBar";
import { useCan } from "../../../../../core/auth/permissions";
import { t } from "../../../../../core/i18n/t";
import {
  archiveAdminRequestType,
  createAdminRequestType,
  getAdminRequestTypes,
  updateAdminRequestType,
} from "../api/adminRequestsDictionariesApi";
import type {
  AdminRequestTypeDto,
  CreateAdminRequestTypePayload,
  RequestDirection,
  UpdateAdminRequestTypePayload,
} from "../api/types";

type Mode = "create" | "edit";

export const RequestTypeCardPage: React.FC = () => {
  const canEdit = useCan("Admin.Requests.EditTypes");
  const navigate = useNavigate();
  const location = useLocation();
  const { id } = useParams<{ id?: string }>();

  const mode: Mode = useMemo(() => {
    if (location.pathname.endsWith("/new")) return "create";
    return id ? "edit" : "create";
  }, [id, location.pathname]);

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [entity, setEntity] = useState<AdminRequestTypeDto | null>(null);
  const [form] = Form.useForm();

  const load = useCallback(async () => {
    if (mode !== "edit" || !id) return;

    setLoading(true);
    setError(null);
    try {
      const all = await getAdminRequestTypes();
      const found = all.find((x) => x.id === id) ?? null;
      setEntity(found);
      if (!found) {
        setError(t("requests.edit.error.notFound.title"));
        return;
      }

      form.setFieldsValue({
        name: found.name,
        description: found.description ?? "",
        direction: found.direction,
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
      form.setFieldsValue({ isActive: true, direction: "Incoming" });
      return;
    }

    void load();
  }, [form, load, mode]);

  const onCancel = () => {
    navigate("/references/requests/types");
  };

  const onSave = async () => {
    const values = await form.validateFields();
    try {
      if (mode === "create") {
        const payload: CreateAdminRequestTypePayload = {
          name: values.name,
          description: values.description,
          direction: values.direction as RequestDirection,
          isActive: !!values.isActive,
        };
        await createAdminRequestType(payload);
      } else {
        if (!id) return;
        const payload: UpdateAdminRequestTypePayload = {
          name: values.name,
          description: values.description,
          direction: values.direction as RequestDirection,
          isActive: !!values.isActive,
        };
        await updateAdminRequestType(id, payload);
      }
      message.success(t("common.actions.save"));
      navigate("/references/requests/types", { replace: true });
    } catch (e) {
      message.error((e as Error).message);
    }
  };

  const onArchive = async () => {
    if (!id) return;
    try {
      await archiveAdminRequestType(id);
      message.success(t("common.actions.save"));
      navigate("/references/requests/types", { replace: true });
    } catch (e) {
      message.error((e as Error).message);
    }
  };

  return (
    <div data-testid="references-requests-types-card">
      <CommandBar
        left={
          <Typography.Title level={2} style={{ margin: 0 }}>
            {mode === "create" ? t("common.actions.create") : t("common.actions.edit")}
          </Typography.Title>
        }
        right={
          <>
            {mode === "edit" && (
              <Button danger onClick={() => void onArchive()} disabled={!canEdit || !entity?.isActive}>
                {t("common.actions.archive")}
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
        <Alert
          type="warning"
          showIcon
          message={t("settings.forbidden")}
          style={{ marginBottom: 12 }}
        />
      )}

      {error && <Alert type="error" showIcon message={error} style={{ marginBottom: 12 }} />}

      <Card>
        <Form form={form} layout="vertical" disabled={!canEdit}>
          <Form.Item
            label={t("settings.requests.form.name")}
            name="name"
            rules={[{ required: true, message: t("settings.requests.form.name.required") }]}
          >
            <Input />
          </Form.Item>

          <Form.Item
            label={t("settings.requests.form.direction")}
            name="direction"
            rules={[{ required: true, message: t("settings.requests.form.direction.required") }]}
          >
            <Select
              options={[
                { value: "Incoming", label: t("settings.requests.types.direction.incoming") },
                { value: "Outgoing", label: t("settings.requests.types.direction.outgoing") },
              ]}
            />
          </Form.Item>

          <Form.Item label={t("settings.requests.form.description")} name="description">
            <Input.TextArea rows={3} />
          </Form.Item>

          <Form.Item label={t("settings.requests.form.isActive")} name="isActive" valuePropName="checked">
            <Switch />
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
};
