import React, { useCallback, useEffect, useMemo, useState } from "react";
import { Alert, Button, Form, Input, Space, Table, Typography, message } from "antd";
import type { ColumnsType } from "antd/es/table";
import Modal from "antd/es/modal";

import { t } from "../../../../core/i18n/t";
import { useCan } from "../../../../core/auth/permissions";
import { createAdminRole, getAdminRoles, updateAdminRole } from "../api/adminSecurityApi";
import type { AdminRoleDto, CreateAdminRolePayload, UpdateAdminRolePayload } from "../api/types";

type Mode = "create" | "edit";

export const RolesSettingsPage: React.FC = () => {
  const canEdit = useCan("Admin.Security.EditRoles");

  const [items, setItems] = useState<AdminRoleDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [modalOpen, setModalOpen] = useState(false);
  const [mode, setMode] = useState<Mode>("create");
  const [editing, setEditing] = useState<AdminRoleDto | null>(null);
  const [form] = Form.useForm();

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await getAdminRoles();
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
    setModalOpen(true);
  };

  const openEdit = (item: AdminRoleDto) => {
    setMode("edit");
    setEditing(item);
    form.resetFields();
    form.setFieldsValue({ code: item.code, name: item.name });
    setModalOpen(true);
  };

  const onSubmit = async () => {
    const values = await form.validateFields();
    try {
      if (mode === "create") {
        const payload: CreateAdminRolePayload = {
          code: values.code,
          name: values.name,
        };
        await createAdminRole(payload);
        message.success(t("common.actions.save"));
      } else {
        if (!editing) return;
        const payload: UpdateAdminRolePayload = { name: values.name };
        await updateAdminRole(editing.id, payload);
        message.success(t("common.actions.save"));
      }

      setModalOpen(false);
      await load();
    } catch (e) {
      message.error((e as Error).message);
    }
  };

  const columns: ColumnsType<AdminRoleDto> = useMemo(
    () => [
      {
        title: t("settings.security.roles.columns.code"),
        dataIndex: "code",
        key: "code",
      },
      {
        title: t("settings.security.roles.columns.name"),
        dataIndex: "name",
        key: "name",
      },
      {
        title: t("settings.security.common.columns.actions"),
        key: "actions",
        render: (_, record) => (
          <Space>
            <Button
              size="small"
              onClick={() => openEdit(record)}
              disabled={!canEdit}
              data-testid={`settings-security-roles-edit-${record.id}`}
            >
              {t("common.actions.edit")}
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
        {t("settings.security.roles.title")}
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
        <Button onClick={() => void load()} data-testid="settings-security-roles-refresh">
          {t("common.actions.refresh")}
        </Button>
        <Button
          type="primary"
          onClick={openCreate}
          disabled={!canEdit}
          data-testid="settings-security-roles-create"
        >
          {t("common.actions.create")}
        </Button>
      </Space>

      <Table
        data-testid="settings-security-roles-table"
        rowKey={(r: AdminRoleDto) => r.id}
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
          "data-testid": "settings-security-roles-save",
        }}
        cancelButtonProps={{
          "data-testid": "settings-security-roles-cancel",
        }}
        title={mode === "create" ? t("common.actions.create") : t("common.actions.edit")}
      >
        <Form form={form} layout="vertical">
          <Form.Item
            label={t("settings.security.roles.columns.code")}
            name="code"
            rules={[{ required: true, message: "Введите код" }]}
          >
            <Input
              disabled={mode === "edit"}
              data-testid="settings-security-roles-form-code"
            />
          </Form.Item>

          <Form.Item
            label={t("settings.security.roles.columns.name")}
            name="name"
            rules={[{ required: true, message: "Введите название" }]}
          >
            <Input data-testid="settings-security-roles-form-name" />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

