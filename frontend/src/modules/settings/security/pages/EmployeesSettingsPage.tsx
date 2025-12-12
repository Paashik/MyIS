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
  Select,
} from "antd";
import type { ColumnsType } from "antd/es/table";
import Modal from "antd/es/modal";
import Switch from "antd/es/switch";

import { t } from "../../../../core/i18n/t";
import { useCan } from "../../../../core/auth/permissions";
import {
  activateAdminEmployee,
  createAdminEmployee,
  deactivateAdminEmployee,
  getAdminEmployees,
  updateAdminEmployee,
} from "../api/adminSecurityApi";
import type {
  AdminEmployeeDto,
  CreateAdminEmployeePayload,
  UpdateAdminEmployeePayload,
} from "../api/types";

type Mode = "create" | "edit";
type ActiveFilter = "all" | "active" | "inactive";

export const EmployeesSettingsPage: React.FC = () => {
  const canEdit = useCan("Admin.Security.EditEmployees");

  const [items, setItems] = useState<AdminEmployeeDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [search, setSearch] = useState<string>("");
  const [activeFilter, setActiveFilter] = useState<ActiveFilter>("all");

  const [modalOpen, setModalOpen] = useState(false);
  const [mode, setMode] = useState<Mode>("create");
  const [editing, setEditing] = useState<AdminEmployeeDto | null>(null);
  const [form] = Form.useForm();

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const isActive =
        activeFilter === "all" ? undefined : activeFilter === "active";
      const data = await getAdminEmployees({
        search: search.trim() ? search.trim() : undefined,
        isActive,
      });
      setItems(data);
    } catch (e) {
      setError((e as Error).message || t("common.error.unknownNetwork"));
    } finally {
      setLoading(false);
    }
  }, [activeFilter, search]);

  useEffect(() => {
    void load();
  }, [load]);

  const openCreate = () => {
    setMode("create");
    setEditing(null);
    form.resetFields();
    setModalOpen(true);
  };

  const openEdit = (item: AdminEmployeeDto) => {
    setMode("edit");
    setEditing(item);
    form.resetFields();
    form.setFieldsValue({
      fullName: item.fullName,
      email: item.email ?? undefined,
      phone: item.phone ?? undefined,
      notes: item.notes ?? undefined,
      isActive: item.isActive,
    });
    setModalOpen(true);
  };

  const onSubmit = async () => {
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
        message.success(t("common.actions.save"));
      } else {
        if (!editing) return;
        const payload: UpdateAdminEmployeePayload = {
          fullName: values.fullName,
          email: values.email,
          phone: values.phone,
          notes: values.notes,
        };
        await updateAdminEmployee(editing.id, payload);
        message.success(t("common.actions.save"));
      }

      setModalOpen(false);
      await load();
    } catch (e) {
      message.error((e as Error).message);
    }
  };

  const onDeactivate = (item: AdminEmployeeDto) => {
    Modal.confirm({
      title: t("settings.security.confirm.deactivate"),
      okButtonProps: {
        "data-testid": "settings-security-employees-deactivate-confirm",
      } as any,
      onOk: async () => {
        await deactivateAdminEmployee(item.id);
        await load();
      },
    });
  };

  const onActivate = (item: AdminEmployeeDto) => {
    Modal.confirm({
      title: t("settings.security.confirm.activate"),
      okButtonProps: {
        "data-testid": "settings-security-employees-activate-confirm",
      } as any,
      onOk: async () => {
        await activateAdminEmployee(item.id);
        await load();
      },
    });
  };

  const columns: ColumnsType<AdminEmployeeDto> = useMemo(
    () => [
      {
        title: t("settings.security.employees.columns.fullName"),
        dataIndex: "fullName",
        key: "fullName",
      },
      {
        title: t("settings.security.employees.columns.email"),
        dataIndex: "email",
        key: "email",
      },
      {
        title: t("settings.security.employees.columns.phone"),
        dataIndex: "phone",
        key: "phone",
      },
      {
        title: t("settings.security.common.columns.active"),
        dataIndex: "isActive",
        key: "isActive",
        render: (v: boolean) => (v ? "Да" : "Нет"),
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
              data-testid={`settings-security-employees-edit-${record.id}`}
            >
              {t("common.actions.edit")}
            </Button>
            {record.isActive ? (
              <Button
                size="small"
                danger
                onClick={() => onDeactivate(record)}
                disabled={!canEdit}
                data-testid={`settings-security-employees-deactivate-${record.id}`}
              >
                Деактивировать
              </Button>
            ) : (
              <Button
                size="small"
                onClick={() => onActivate(record)}
                disabled={!canEdit}
                data-testid={`settings-security-employees-activate-${record.id}`}
              >
                Активировать
              </Button>
            )}
          </Space>
        ),
      },
    ],
    [canEdit]
  );

  return (
    <div>
      <Typography.Title level={3} style={{ marginTop: 0 }}>
        {t("settings.security.employees.title")}
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

      <Space style={{ marginBottom: 12, flexWrap: "wrap" }}>
        <Input
          placeholder="Поиск..."
          value={search}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
            setSearch(e.target.value)
          }
          style={{ width: 260 }}
          data-testid="settings-security-employees-search"
        />
        <Select
          value={activeFilter}
          onChange={(v: ActiveFilter) => setActiveFilter(v)}
          style={{ width: 180 }}
          options={[
            { value: "all", label: "Все" },
            { value: "active", label: "Только активные" },
            { value: "inactive", label: "Только неактивные" },
          ]}
          data-testid="settings-security-employees-active-filter"
        />
        <Button
          onClick={() => void load()}
          data-testid="settings-security-employees-refresh"
        >
          {t("common.actions.refresh")}
        </Button>
        <Button
          type="primary"
          onClick={openCreate}
          disabled={!canEdit}
          data-testid="settings-security-employees-create"
        >
          {t("common.actions.create")}
        </Button>
      </Space>

      <Table
        data-testid="settings-security-employees-table"
        rowKey={(r: AdminEmployeeDto) => r.id}
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
          "data-testid": "settings-security-employees-save",
        }}
        cancelButtonProps={{
          "data-testid": "settings-security-employees-cancel",
        }}
        title={mode === "create" ? t("common.actions.create") : t("common.actions.edit")}
      >
        <Form form={form} layout="vertical">
          <Form.Item
            label={t("settings.security.employees.columns.fullName")}
            name="fullName"
            rules={[{ required: true, message: "Введите ФИО" }]}
          >
            <Input data-testid="settings-security-employees-form-fullName" />
          </Form.Item>

          <Form.Item label="Email" name="email">
            <Input data-testid="settings-security-employees-form-email" />
          </Form.Item>

          <Form.Item label={t("settings.security.employees.columns.phone")} name="phone">
            <Input data-testid="settings-security-employees-form-phone" />
          </Form.Item>

          <Form.Item label="Примечания" name="notes">
            <Input.TextArea
              rows={3}
              data-testid="settings-security-employees-form-notes"
            />
          </Form.Item>

          <Form.Item label={t("settings.security.common.columns.active")} name="isActive" valuePropName="checked">
            <Switch disabled data-testid="settings-security-employees-form-isActive" />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

