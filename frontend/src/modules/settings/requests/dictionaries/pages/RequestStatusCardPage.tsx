import React, { useCallback, useEffect, useMemo, useState } from "react";
import { Alert, Button, Card, Form, Input, Typography, message } from "antd";
import Switch from "antd/es/switch";
import { useLocation, useNavigate, useParams } from "react-router-dom";

import { CommandBar } from "../../../../../components/ui/CommandBar";
import { useCan } from "../../../../../core/auth/permissions";
import { t } from "../../../../../core/i18n/t";
import {
  archiveAdminRequestStatus,
  createAdminRequestStatus,
  getAdminRequestStatuses,
  updateAdminRequestStatus,
} from "../api/adminRequestsDictionariesApi";
import type {
  AdminRequestStatusDto,
  CreateAdminRequestStatusPayload,
  UpdateAdminRequestStatusPayload,
} from "../api/types";

type Mode = "create" | "edit";

export const RequestStatusCardPage: React.FC = () => {
  const canEdit = useCan("Admin.Requests.EditStatuses");
  const navigate = useNavigate();
  const location = useLocation();
  const { id } = useParams<{ id?: string }>();

  const mode: Mode = useMemo(() => {
    if (location.pathname.endsWith("/new")) return "create";
    return id ? "edit" : "create";
  }, [id, location.pathname]);

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [entity, setEntity] = useState<AdminRequestStatusDto | null>(null);
  const [form] = Form.useForm();

  const load = useCallback(async () => {
    if (mode !== "edit" || !id) return;

    setLoading(true);
    setError(null);
    try {
      const all = await getAdminRequestStatuses();
      const found = all.find((x) => x.id === id) ?? null;
      setEntity(found);
      if (!found) {
        setError(t("requests.edit.error.notFound.title"));
        return;
      }

      form.setFieldsValue({
        code: found.code,
        name: found.name,
        description: found.description ?? "",
        isFinal: found.isFinal,
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
      form.setFieldsValue({ isActive: true, isFinal: false });
      return;
    }

    void load();
  }, [form, load, mode]);

  const onCancel = () => {
    navigate("/references/requests/statuses");
  };

  const onSave = async () => {
    const values = await form.validateFields();
    try {
      if (mode === "create") {
        const payload: CreateAdminRequestStatusPayload = {
          code: values.code,
          name: values.name,
          description: values.description,
          isFinal: !!values.isFinal,
          isActive: !!values.isActive,
        };
        await createAdminRequestStatus(payload);
      } else {
        if (!id) return;
        const payload: UpdateAdminRequestStatusPayload = {
          name: values.name,
          description: values.description,
          isFinal: !!values.isFinal,
          isActive: !!values.isActive,
        };
        await updateAdminRequestStatus(id, payload);
      }
      message.success(t("common.actions.save"));
      navigate("/references/requests/statuses", { replace: true });
    } catch (e) {
      message.error((e as Error).message);
    }
  };

  const onArchive = async () => {
    if (!id) return;
    try {
      await archiveAdminRequestStatus(id);
      message.success(t("common.actions.save"));
      navigate("/references/requests/statuses", { replace: true });
    } catch (e) {
      message.error((e as Error).message);
    }
  };

  return (
    <div data-testid="references-requests-statuses-card">
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
        <Alert type="warning" showIcon message={t("settings.forbidden")} style={{ marginBottom: 12 }} />
      )}

      {error && <Alert type="error" showIcon message={error} style={{ marginBottom: 12 }} />}

      <Card>
        <Form form={form} layout="vertical" disabled={!canEdit}>
          <Form.Item
            label={t("settings.requests.form.code")}
            name="code"
            rules={[{ required: true, message: t("settings.requests.form.code.required") }]}
          >
            <Input disabled={mode === "edit"} />
          </Form.Item>

          <Form.Item
            label={t("settings.requests.form.name")}
            name="name"
            rules={[{ required: true, message: t("settings.requests.form.name.required") }]}
          >
            <Input />
          </Form.Item>

          <Form.Item label={t("settings.requests.form.description")} name="description">
            <Input.TextArea rows={3} />
          </Form.Item>

          <Form.Item label={t("settings.requests.form.isFinal")} name="isFinal" valuePropName="checked">
            <Switch />
          </Form.Item>

          <Form.Item label={t("settings.requests.form.isActive")} name="isActive" valuePropName="checked">
            <Switch />
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
};
