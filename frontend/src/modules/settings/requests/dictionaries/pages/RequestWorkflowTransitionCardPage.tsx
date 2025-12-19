import React, { useCallback, useEffect, useMemo, useState } from "react";
import { Alert, Button, Card, Form, Input, Select, Typography, message } from "antd";
import Switch from "antd/es/switch";
import { useNavigate, useParams } from "react-router-dom";

import { CommandBar } from "../../../../../components/ui/CommandBar";
import { useCan } from "../../../../../core/auth/permissions";
import { t } from "../../../../../core/i18n/t";
import {
  getAdminRequestStatuses,
  getAdminWorkflowTransitions,
  replaceAdminWorkflowTransitions,
} from "../api/adminRequestsDictionariesApi";
import type {
  AdminRequestStatusDto,
  AdminRequestWorkflowTransitionDto,
  WorkflowTransitionInput,
} from "../api/types";

type Mode = "create" | "edit";

export const RequestWorkflowTransitionCardPage: React.FC = () => {
  const canEdit = useCan("Admin.Requests.EditWorkflow");
  const navigate = useNavigate();
  const { typeCode, id } = useParams<{ typeCode: string; id: string }>();

  const mode: Mode = id === "new" ? "create" : "edit";

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [statuses, setStatuses] = useState<AdminRequestStatusDto[]>([]);
  const [items, setItems] = useState<AdminRequestWorkflowTransitionDto[]>([]);
  const [entity, setEntity] = useState<AdminRequestWorkflowTransitionDto | null>(null);
  const [form] = Form.useForm();

  const statusOptions = useMemo(
    () =>
      statuses
        .slice()
        .sort((a, b) => a.code.localeCompare(b.code))
        .map((s) => ({ value: s.id, label: `${s.code} — ${s.name}` })),
    [statuses]
  );

  const load = useCallback(async () => {
    if (!typeCode) return;

    setLoading(true);
    setError(null);
    try {
      const [sts, transitions] = await Promise.all([
        getAdminRequestStatuses(),
        getAdminWorkflowTransitions(typeCode),
      ]);
      setStatuses(sts);
      setItems(transitions);

      if (mode === "edit") {
        const found = transitions.find((x) => x.id === id) ?? null;
        setEntity(found);
        if (!found) {
          setError(t("requests.edit.error.notFound.title"));
          return;
        }

        form.setFieldsValue({
          fromStatusId: found.fromStatusId,
          toStatusId: found.toStatusId,
          actionCode: found.actionCode,
          requiredPermission: found.requiredPermission ?? "",
          isEnabled: found.isEnabled,
        });
      } else {
        setEntity(null);
        form.resetFields();
        form.setFieldsValue({ isEnabled: true });
      }
    } catch (e) {
      setError((e as Error).message || t("common.error.unknownNetwork"));
    } finally {
      setLoading(false);
    }
  }, [form, id, mode, typeCode]);

  useEffect(() => {
    void load();
  }, [load]);

  const onCancel = () => navigate("/administration/requests/workflow");

  const persist = async (next: AdminRequestWorkflowTransitionDto[]) => {
    if (!typeCode) return;

    const payload = {
      typeCode,
      transitions: next.map<WorkflowTransitionInput>((x) => ({
        fromStatusId: x.fromStatusId,
        toStatusId: x.toStatusId,
        actionCode: x.actionCode,
        requiredPermission: x.requiredPermission ?? undefined,
        isEnabled: x.isEnabled,
      })),
    };

    await replaceAdminWorkflowTransitions(payload);
  };

  const onSave = async () => {
    if (!typeCode) return;
    const values = await form.validateFields();

    const fromStatus = statuses.find((s) => s.id === values.fromStatusId);
    const toStatus = statuses.find((s) => s.id === values.toStatusId);

    const draft: AdminRequestWorkflowTransitionDto = {
      id: mode === "edit" ? id : `new-${Date.now()}`,
      requestTypeId: "",
      requestTypeCode: typeCode,
      fromStatusId: values.fromStatusId,
      fromStatusCode: fromStatus?.code ?? "",
      toStatusId: values.toStatusId,
      toStatusCode: toStatus?.code ?? "",
      actionCode: values.actionCode,
      requiredPermission: values.requiredPermission ? String(values.requiredPermission) : null,
      isEnabled: !!values.isEnabled,
    };

    const next =
      mode === "edit"
        ? items.map((x) => (x.id === id ? { ...x, ...draft } : x))
        : [...items, draft];

    try {
      setLoading(true);
      await persist(next);
      message.success(t("common.actions.save"));
      navigate("/administration/requests/workflow", { replace: true });
    } catch (e) {
      message.error((e as Error).message);
      await load();
    } finally {
      setLoading(false);
    }
  };

  const onDelete = async () => {
    if (mode !== "edit") return;

    const next = items.filter((x) => x.id !== id);
    try {
      setLoading(true);
      await persist(next);
      message.success(t("common.actions.save"));
      navigate("/administration/requests/workflow", { replace: true });
    } catch (e) {
      message.error((e as Error).message);
      await load();
    } finally {
      setLoading(false);
    }
  };

  return (
    <div data-testid="administration-requests-workflow-card">
      <CommandBar
        left={
          <Typography.Title level={2} style={{ margin: 0 }}>
            {mode === "create" ? t("common.actions.add") : t("common.actions.edit")}
          </Typography.Title>
        }
        right={
          <>
            {mode === "edit" && (
              <Button danger onClick={() => void onDelete()} disabled={!canEdit || loading || !entity}>
                Удалить
              </Button>
            )}
            <Button onClick={onCancel}>{t("common.actions.cancel")}</Button>
            <Button type="primary" onClick={() => void onSave()} disabled={!canEdit} loading={loading}>
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
            label={t("settings.requests.form.fromStatus")}
            name="fromStatusId"
            rules={[{ required: true, message: "Выберите статус" }]}
          >
            <Select options={statusOptions} />
          </Form.Item>

          <Form.Item
            label={t("settings.requests.form.toStatus")}
            name="toStatusId"
            rules={[{ required: true, message: "Выберите статус" }]}
          >
            <Select options={statusOptions} />
          </Form.Item>

          <Form.Item
            label={t("settings.requests.form.actionCode")}
            name="actionCode"
            rules={[{ required: true, message: "Введите ActionCode" }]}
          >
            <Input />
          </Form.Item>

          <Form.Item label={t("settings.requests.form.requiredPermission")} name="requiredPermission">
            <Input />
          </Form.Item>

          <Form.Item label={t("settings.requests.form.isEnabled")} name="isEnabled" valuePropName="checked">
            <Switch />
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
};
