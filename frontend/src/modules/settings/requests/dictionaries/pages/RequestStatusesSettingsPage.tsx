import React, { useCallback, useEffect, useMemo, useState } from "react";
import {
  Alert,
  Button,
  Form,
  Input,
  Space,
  Table,
  Typography,
  message,
} from "antd";

import Modal from "antd/es/modal";
import Switch from "antd/es/switch";

import type { ColumnsType } from "antd/es/table";

import { t } from "../../../../../core/i18n/t";
import { useCan } from "../../../../../core/auth/permissions";
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

export const RequestStatusesSettingsPage: React.FC = () => {
  const canEdit = useCan("Admin.Requests.EditStatuses");

  const [items, setItems] = useState<AdminRequestStatusDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [modalOpen, setModalOpen] = useState(false);
  const [mode, setMode] = useState<Mode>("create");
  const [editing, setEditing] = useState<AdminRequestStatusDto | null>(null);
  const [form] = Form.useForm();

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await getAdminRequestStatuses();
      setItems(data);
    } catch (e) {
      setError((e as Error).message || t("common.error.unknownNetwork"));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const openCreate = () => {
    setMode("create");
    setEditing(null);
    form.resetFields();
    form.setFieldsValue({ isActive: true, isFinal: false });
    setModalOpen(true);
  };

  const openEdit = (item: AdminRequestStatusDto) => {
    setMode("edit");
    setEditing(item);
    form.resetFields();
    form.setFieldsValue({
      code: item.code,
      name: item.name,
      description: item.description ?? undefined,
      isFinal: item.isFinal,
      isActive: item.isActive,
    });
    setModalOpen(true);
  };

  const onSubmit = async () => {
    const values = await form.validateFields();

    try {
      if (mode === "create") {
        await createAdminRequestStatus(values as CreateAdminRequestStatusPayload);
        message.success(t("common.actions.save"));
      } else {
        if (!editing) {
          return;
        }
        const payload: UpdateAdminRequestStatusPayload = {
          name: values.name,
          isFinal: values.isFinal,
          description: values.description,
          isActive: values.isActive,
        };
        await updateAdminRequestStatus(editing.id, payload);
        message.success(t("common.actions.save"));
      }

      setModalOpen(false);
      await load();
    } catch (e) {
      message.error((e as Error).message);
    }
  };

  const onArchive = (item: AdminRequestStatusDto) => {
    Modal.confirm({
      title: t("settings.requests.confirm.archive"),
      okButtonProps: { "data-testid": "settings-requests-statuses-archive-confirm" } as any,
      onOk: async () => {
        try {
          await archiveAdminRequestStatus(item.id);
          await load();
        } catch (e) {
          message.error((e as Error).message);
        }
      },
    });
  };

  const columns: ColumnsType<AdminRequestStatusDto> = useMemo(
    () => [
      {
        title: t("settings.requests.statuses.columns.code"),
        dataIndex: "code",
        key: "code",
      },
      {
        title: t("settings.requests.statuses.columns.name"),
        dataIndex: "name",
        key: "name",
      },
      {
        title: t("settings.requests.statuses.columns.isFinal"),
        dataIndex: "isFinal",
        key: "isFinal",
        render: (v: boolean) => (v ? "Да" : "Нет"),
      },
      {
        title: t("settings.requests.statuses.columns.isActive"),
        dataIndex: "isActive",
        key: "isActive",
        render: (v: boolean) => (v ? "Да" : "Нет"),
      },
      {
        title: t("settings.requests.statuses.columns.actions"),
        key: "actions",
        render: (_, record) => (
          <Space>
            <Button
              size="small"
              onClick={() => openEdit(record)}
              disabled={!canEdit}
              data-testid="settings-requests-statuses-edit"
            >
              {t("common.actions.edit")}
            </Button>
            <Button
              size="small"
              danger
              onClick={() => onArchive(record)}
              disabled={!canEdit || !record.isActive}
              data-testid="settings-requests-statuses-archive"
            >
              Архивировать
            </Button>
          </Space>
        ),
      },
    ],
    [canEdit]
  );

  return (
    <div>
      <Typography.Title level={3} style={{ marginTop: 0 }}>
        {t("settings.requests.statuses.title")}
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
        <Button onClick={() => void load()} data-testid="settings-requests-statuses-refresh">
          {t("common.actions.refresh")}
        </Button>
        <Button
          type="primary"
          onClick={openCreate}
          disabled={!canEdit}
          data-testid="settings-requests-statuses-create"
        >
          {t("common.actions.create")}
        </Button>
      </Space>

      <Table
        data-testid="settings-requests-statuses-table"
        rowKey={(r: AdminRequestStatusDto) => r.id}
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
          "data-testid": "settings-requests-statuses-save",
        }}
        cancelButtonProps={{
          "data-testid": "settings-requests-statuses-cancel",
        }}
        title={mode === "create" ? t("common.actions.create") : t("common.actions.edit")}
      >
        <Form form={form} layout="vertical">
          <Form.Item
            label={t("settings.requests.form.code")}
            name="code"
            rules={[{ required: true, message: "Введите код" }]}
          >
            <Input
              disabled={mode === "edit"}
              data-testid="settings-requests-statuses-form-code"
            />
          </Form.Item>

          <Form.Item
            label={t("settings.requests.form.name")}
            name="name"
            rules={[{ required: true, message: "Введите название" }]}
          >
            <Input data-testid="settings-requests-statuses-form-name" />
          </Form.Item>

          <Form.Item label={t("settings.requests.form.description")} name="description">
            <Input.TextArea rows={3} data-testid="settings-requests-statuses-form-description" />
          </Form.Item>

          <Form.Item
            label={t("settings.requests.form.isFinal")}
            name="isFinal"
            valuePropName="checked"
          >
            <Switch data-testid="settings-requests-statuses-form-isFinal" />
          </Form.Item>

          <Form.Item
            label={t("settings.requests.form.isActive")}
            name="isActive"
            valuePropName="checked"
          >
            <Switch data-testid="settings-requests-statuses-form-isActive" />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

