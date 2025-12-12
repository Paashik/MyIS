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

import Modal from "antd/es/modal";
import Switch from "antd/es/switch";

import type { ColumnsType } from "antd/es/table";

import { t } from "../../../../../core/i18n/t";
import { useCan } from "../../../../../core/auth/permissions";
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

export const RequestTypesSettingsPage: React.FC = () => {
  const canEdit = useCan("Admin.Requests.EditTypes");

  const [items, setItems] = useState<AdminRequestTypeDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [modalOpen, setModalOpen] = useState(false);
  const [mode, setMode] = useState<Mode>("create");
  const [editing, setEditing] = useState<AdminRequestTypeDto | null>(null);
  const [form] = Form.useForm();

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await getAdminRequestTypes();
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
    form.setFieldsValue({ isActive: true, direction: "Incoming" });
    setModalOpen(true);
  };

  const openEdit = (item: AdminRequestTypeDto) => {
    setMode("edit");
    setEditing(item);
    form.resetFields();
    form.setFieldsValue({
      code: item.code,
      name: item.name,
      description: item.description ?? undefined,
      direction: item.direction,
      isActive: item.isActive,
    });
    setModalOpen(true);
  };

  const onSubmit = async () => {
    const values = await form.validateFields();

    try {
      if (mode === "create") {
        await createAdminRequestType(values as CreateAdminRequestTypePayload);
        message.success(t("common.actions.save"));
      } else {
        if (!editing) {
          return;
        }
        const payload: UpdateAdminRequestTypePayload = {
          name: values.name,
          direction: values.direction as RequestDirection,
          description: values.description,
          isActive: values.isActive,
        };
        await updateAdminRequestType(editing.id, payload);
        message.success(t("common.actions.save"));
      }

      setModalOpen(false);
      await load();
    } catch (e) {
      message.error((e as Error).message);
    }
  };

  const onArchive = (item: AdminRequestTypeDto) => {
    Modal.confirm({
      title: t("settings.requests.confirm.archive"),
      okButtonProps: { "data-testid": "settings-requests-types-archive-confirm" } as any,
      onOk: async () => {
        try {
          await archiveAdminRequestType(item.id);
          await load();
        } catch (e) {
          message.error((e as Error).message);
        }
      },
    });
  };

  const columns: ColumnsType<AdminRequestTypeDto> = useMemo(
    () => [
      {
        title: t("settings.requests.types.columns.code"),
        dataIndex: "code",
        key: "code",
      },
      {
        title: t("settings.requests.types.columns.name"),
        dataIndex: "name",
        key: "name",
      },
      {
        title: t("settings.requests.types.columns.direction"),
        dataIndex: "direction",
        key: "direction",
      },
      {
        title: t("settings.requests.types.columns.isActive"),
        dataIndex: "isActive",
        key: "isActive",
        render: (v: boolean) => (v ? "Да" : "Нет"),
      },
      {
        title: t("settings.requests.types.columns.actions"),
        key: "actions",
        render: (_, record) => (
          <Space>
            <Button
              size="small"
              onClick={() => openEdit(record)}
              disabled={!canEdit}
              data-testid="settings-requests-types-edit"
            >
              {t("common.actions.edit")}
            </Button>
            <Button
              size="small"
              danger
              onClick={() => onArchive(record)}
              disabled={!canEdit || !record.isActive}
              data-testid="settings-requests-types-archive"
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
        {t("settings.requests.types.title")}
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
        <Button onClick={() => void load()} data-testid="settings-requests-types-refresh">
          {t("common.actions.refresh")}
        </Button>
        <Button
          type="primary"
          onClick={openCreate}
          disabled={!canEdit}
          data-testid="settings-requests-types-create"
        >
          {t("common.actions.create")}
        </Button>
      </Space>

      <Table
        data-testid="settings-requests-types-table"
        rowKey={(r: AdminRequestTypeDto) => r.id}
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
          "data-testid": "settings-requests-types-save",
        }}
        cancelButtonProps={{
          "data-testid": "settings-requests-types-cancel",
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
              data-testid="settings-requests-types-form-code"
            />
          </Form.Item>

          <Form.Item
            label={t("settings.requests.form.name")}
            name="name"
            rules={[{ required: true, message: "Введите название" }]}
          >
            <Input data-testid="settings-requests-types-form-name" />
          </Form.Item>

          <Form.Item
            label={t("settings.requests.form.direction")}
            name="direction"
            rules={[{ required: true, message: "Выберите направление" }]}
          >
            <Select
              options={[
                { value: "Incoming", label: "Incoming" },
                { value: "Outgoing", label: "Outgoing" },
              ]}
              data-testid="settings-requests-types-form-direction"
            />
          </Form.Item>

          <Form.Item label={t("settings.requests.form.description")} name="description">
            <Input.TextArea rows={3} data-testid="settings-requests-types-form-description" />
          </Form.Item>

          <Form.Item
            label={t("settings.requests.form.isActive")}
            name="isActive"
            valuePropName="checked"
          >
            <Switch data-testid="settings-requests-types-form-isActive" />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

