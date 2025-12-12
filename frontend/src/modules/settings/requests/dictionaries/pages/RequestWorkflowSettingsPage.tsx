import React, { useCallback, useEffect, useMemo, useState } from "react";
import {
  Alert,
  Button,
  Form,
  Input,
  Select,
  Space,
  Table,
  Typography,
  message,
} from "antd";
import type { ColumnsType } from "antd/es/table";

import Modal from "antd/es/modal";
import Switch from "antd/es/switch";

import { t } from "../../../../../core/i18n/t";
import { useCan } from "../../../../../core/auth/permissions";
import {
  getAdminRequestStatuses,
  getAdminRequestTypes,
  getAdminWorkflowTransitions,
  replaceAdminWorkflowTransitions,
} from "../api/adminRequestsDictionariesApi";
import type {
  AdminRequestStatusDto,
  AdminRequestTypeDto,
  AdminRequestWorkflowTransitionDto,
  WorkflowTransitionInput,
} from "../api/types";

type Mode = "create" | "edit";

type TransitionFormModel = {
  fromStatusId: string;
  toStatusId: string;
  actionCode: string;
  requiredPermission?: string;
  isEnabled: boolean;
};

export const RequestWorkflowSettingsPage: React.FC = () => {
  const canEdit = useCan("Admin.Requests.EditWorkflow");

  const [types, setTypes] = useState<AdminRequestTypeDto[]>([]);
  const [statuses, setStatuses] = useState<AdminRequestStatusDto[]>([]);
  const [typeCode, setTypeCode] = useState<string>("");

  const [items, setItems] = useState<AdminRequestWorkflowTransitionDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [modalOpen, setModalOpen] = useState(false);
  const [mode, setMode] = useState<Mode>("create");
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form] = Form.useForm();

  const statusOptions = useMemo(
    () =>
      statuses
        .slice()
        .sort((a, b) => a.code.localeCompare(b.code))
        .map((s) => ({ value: s.id, label: `${s.code} — ${s.name}` })),
    [statuses]
  );

  const loadLookups = useCallback(async () => {
    setError(null);
    try {
      const [tps, sts] = await Promise.all([
        getAdminRequestTypes(),
        getAdminRequestStatuses(),
      ]);
      setTypes(tps);
      setStatuses(sts);

      const firstActive = tps.find((x) => x.isActive) ?? tps[0];
      if (firstActive && !typeCode) {
        setTypeCode(firstActive.code);
      }
    } catch (e) {
      setError((e as Error).message);
    }
  }, [typeCode]);

  const loadTransitions = useCallback(async () => {
    if (!typeCode) {
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const data = await getAdminWorkflowTransitions(typeCode);
      setItems(data);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setLoading(false);
    }
  }, [typeCode]);

  useEffect(() => {
    void loadLookups();
  }, [loadLookups]);

  useEffect(() => {
    void loadTransitions();
  }, [loadTransitions]);

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

  const openCreate = () => {
    setMode("create");
    setEditingId(null);
    form.resetFields();
    form.setFieldsValue({ isEnabled: true });
    setModalOpen(true);
  };

  const openEdit = (item: AdminRequestWorkflowTransitionDto) => {
    setMode("edit");
    setEditingId(item.id);
    form.resetFields();
    form.setFieldsValue({
      fromStatusId: item.fromStatusId,
      toStatusId: item.toStatusId,
      actionCode: item.actionCode,
      requiredPermission: item.requiredPermission ?? undefined,
      isEnabled: item.isEnabled,
    });
    setModalOpen(true);
  };

  const onToggle = async (item: AdminRequestWorkflowTransitionDto) => {
    const next = items.map((x) =>
      x.id === item.id ? { ...x, isEnabled: !x.isEnabled } : x
    );
    setItems(next);
    try {
      await persist(next);
      message.success(t("common.actions.save"));
    } catch (e) {
      message.error((e as Error).message);
      await loadTransitions();
    }
  };

  const onSubmit = async () => {
    const values = await form.validateFields();
    if (!typeCode) {
      return;
    }

    const draft: AdminRequestWorkflowTransitionDto = {
      id: editingId ?? `new-${Date.now()}`,
      requestTypeId: "",
      requestTypeCode: typeCode,
      fromStatusId: values.fromStatusId,
      fromStatusCode: statuses.find((s) => s.id === values.fromStatusId)?.code ?? "",
      toStatusId: values.toStatusId,
      toStatusCode: statuses.find((s) => s.id === values.toStatusId)?.code ?? "",
      actionCode: values.actionCode,
      requiredPermission: values.requiredPermission ?? null,
      isEnabled: values.isEnabled,
    };

    const next =
      mode === "create"
        ? [...items, draft]
        : items.map((x) => (x.id === editingId ? { ...x, ...draft } : x));

    try {
      await persist(next);
      message.success(t("common.actions.save"));
      setModalOpen(false);
      await loadTransitions();
    } catch (e) {
      message.error((e as Error).message);
    }
  };

  const columns: ColumnsType<AdminRequestWorkflowTransitionDto> = useMemo(
    () => [
      {
        title: t("settings.requests.workflow.columns.from"),
        dataIndex: "fromStatusCode",
        key: "from",
      },
      {
        title: t("settings.requests.workflow.columns.to"),
        dataIndex: "toStatusCode",
        key: "to",
      },
      {
        title: t("settings.requests.workflow.columns.action"),
        dataIndex: "actionCode",
        key: "actionCode",
      },
      {
        title: t("settings.requests.workflow.columns.permission"),
        dataIndex: "requiredPermission",
        key: "requiredPermission",
        render: (v: string | null | undefined) => v || "—",
      },
      {
        title: t("settings.requests.workflow.columns.enabled"),
        dataIndex: "isEnabled",
        key: "isEnabled",
        render: (v: boolean) => (v ? "Да" : "Нет"),
      },
      {
        title: t("settings.requests.workflow.columns.actions"),
        key: "actions",
        render: (_, record) => (
          <Space>
            <Button
              size="small"
              onClick={() => openEdit(record)}
              disabled={!canEdit}
              data-testid="settings-requests-workflow-edit"
            >
              {t("common.actions.edit")}
            </Button>
            <Button
              size="small"
              onClick={() => void onToggle(record)}
              disabled={!canEdit}
              data-testid="settings-requests-workflow-toggle"
            >
              {record.isEnabled ? "Выключить" : "Включить"}
            </Button>
          </Space>
        ),
      },
    ],
    [canEdit, items, statuses]
  );

  return (
    <div>
      <Typography.Title level={3} style={{ marginTop: 0 }}>
        {t("settings.requests.workflow.title")}
      </Typography.Title>

      {!canEdit && (
        <Alert
          type="warning"
          showIcon
          message={t("settings.forbidden")}
          style={{ marginBottom: 12 }}
        />
      )}

      {error && (
        <Alert
          type="error"
          showIcon
          message={error}
          style={{ marginBottom: 12 }}
        />
      )}

      <Space style={{ marginBottom: 12 }}>
        <div style={{ minWidth: 320 }}>
          <div style={{ marginBottom: 4 }}>{t("settings.requests.workflow.filters.type")}</div>
          <Select
            value={typeCode || undefined}
            onChange={(v: string) => setTypeCode(v)}
            style={{ width: "100%" }}
            options={types.map((x) => ({
              value: x.code,
              label: `${x.code} — ${x.name}${x.isActive ? "" : " (архив)"}`,
            }))}
            data-testid="settings-requests-workflow-type"
          />
        </div>

        <Button onClick={() => void loadTransitions()} data-testid="settings-requests-workflow-refresh">
          {t("common.actions.refresh")}
        </Button>

        <Button
          type="primary"
          onClick={openCreate}
          disabled={!canEdit || !typeCode}
          data-testid="settings-requests-workflow-create"
        >
          {t("common.actions.add")}
        </Button>
      </Space>

      <Table
        data-testid="settings-requests-workflow-table"
        rowKey={(r: AdminRequestWorkflowTransitionDto) => r.id}
        loading={loading}
        columns={columns}
        dataSource={items}
        pagination={false}
      />

      <Modal
        open={modalOpen}
        onCancel={() => setModalOpen(false)}
        onOk={() => void onSubmit()}
        okButtonProps={{
          disabled: !canEdit,
          "data-testid": "settings-requests-workflow-save",
        }}
        cancelButtonProps={{
          "data-testid": "settings-requests-workflow-cancel",
        }}
        title={mode === "create" ? t("common.actions.add") : t("common.actions.edit")}
      >
        <Form form={form} layout="vertical">
          <Form.Item
            label={t("settings.requests.form.fromStatus")}
            name="fromStatusId"
            rules={[{ required: true, message: "Выберите статус" }]}
          >
            <Select
              options={statusOptions}
              data-testid="settings-requests-workflow-form-from"
            />
          </Form.Item>

          <Form.Item
            label={t("settings.requests.form.toStatus")}
            name="toStatusId"
            rules={[{ required: true, message: "Выберите статус" }]}
          >
            <Select
              options={statusOptions}
              data-testid="settings-requests-workflow-form-to"
            />
          </Form.Item>

          <Form.Item
            label={t("settings.requests.form.actionCode")}
            name="actionCode"
            rules={[{ required: true, message: "Введите ActionCode" }]}
          >
            <Input data-testid="settings-requests-workflow-form-actionCode" />
          </Form.Item>

          <Form.Item
            label={t("settings.requests.form.requiredPermission")}
            name="requiredPermission"
          >
            <Input data-testid="settings-requests-workflow-form-requiredPermission" />
          </Form.Item>

          <Form.Item
            label={t("settings.requests.form.isEnabled")}
            name="isEnabled"
            valuePropName="checked"
          >
            <Switch data-testid="settings-requests-workflow-form-isEnabled" />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

