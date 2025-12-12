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
  activateAdminUser,
  createAdminUser,
  deactivateAdminUser,
  getAdminEmployees,
  getAdminRoles,
  getAdminUserRoles,
  getAdminUsers,
  replaceAdminUserRoles,
  resetAdminUserPassword,
  updateAdminUser,
} from "../api/adminSecurityApi";
import type {
  AdminEmployeeDto,
  AdminRoleDto,
  AdminUserListItemDto,
  CreateAdminUserPayload,
  UpdateAdminUserPayload,
} from "../api/types";

type Mode = "create" | "edit";
type ActiveFilter = "all" | "active" | "inactive";

export const UsersSettingsPage: React.FC = () => {
  const canEditUsers = useCan("Admin.Security.EditUsers");
  const canEditRoles = useCan("Admin.Security.EditRoles");

  const [items, setItems] = useState<AdminUserListItemDto[]>([]);
  const [roles, setRoles] = useState<AdminRoleDto[]>([]);
  const [employees, setEmployees] = useState<AdminEmployeeDto[]>([]);

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [search, setSearch] = useState<string>("");
  const [activeFilter, setActiveFilter] = useState<ActiveFilter>("all");

  const [modalOpen, setModalOpen] = useState(false);
  const [mode, setMode] = useState<Mode>("create");
  const [editing, setEditing] = useState<AdminUserListItemDto | null>(null);
  const [form] = Form.useForm();

  const [rolesModalOpen, setRolesModalOpen] = useState(false);
  const [rolesUser, setRolesUser] = useState<AdminUserListItemDto | null>(null);
  const [rolesSelectedIds, setRolesSelectedIds] = useState<string[]>([]);

  const [resetPwModalOpen, setResetPwModalOpen] = useState(false);
  const [resetPwUser, setResetPwUser] = useState<AdminUserListItemDto | null>(null);
  const [resetPw, setResetPw] = useState<string>("");

  const loadSupport = useCallback(async () => {
    try {
      const [rolesData, employeesData] = await Promise.all([
        getAdminRoles(),
        getAdminEmployees({ isActive: true }),
      ]);
      setRoles(rolesData);
      setEmployees(employeesData);
    } catch (e) {
      // do not block page (main load will show error)
    }
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const isActive =
        activeFilter === "all" ? undefined : activeFilter === "active";
      const data = await getAdminUsers({
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
    void loadSupport();
    void load();
  }, [load, loadSupport]);

  const openCreate = () => {
    setMode("create");
    setEditing(null);
    form.resetFields();
    form.setFieldsValue({ isActive: true });
    setModalOpen(true);
  };

  const openEdit = (item: AdminUserListItemDto) => {
    setMode("edit");
    setEditing(item);
    form.resetFields();
    form.setFieldsValue({
      login: item.login,
      isActive: item.isActive,
      employeeId: item.employeeId ?? undefined,
    });
    setModalOpen(true);
  };

  const onSubmit = async () => {
    const values = await form.validateFields();
    try {
      if (mode === "create") {
        const payload: CreateAdminUserPayload = {
          login: values.login,
          password: values.password,
          isActive: values.isActive,
          employeeId: values.employeeId ?? null,
        };
        await createAdminUser(payload);
        message.success(t("common.actions.save"));
      } else {
        if (!editing) return;
        const payload: UpdateAdminUserPayload = {
          login: values.login,
          isActive: values.isActive,
          employeeId: values.employeeId ?? null,
        };
        await updateAdminUser(editing.id, payload);
        message.success(t("common.actions.save"));
      }

      setModalOpen(false);
      await loadSupport();
      await load();
    } catch (e) {
      message.error((e as Error).message);
    }
  };

  const onDeactivate = (item: AdminUserListItemDto) => {
    Modal.confirm({
      title: t("settings.security.confirm.deactivate"),
      okButtonProps: {
        "data-testid": "settings-security-users-deactivate-confirm",
      } as any,
      onOk: async () => {
        await deactivateAdminUser(item.id);
        await load();
      },
    });
  };

  const onActivate = (item: AdminUserListItemDto) => {
    Modal.confirm({
      title: t("settings.security.confirm.activate"),
      okButtonProps: {
        "data-testid": "settings-security-users-activate-confirm",
      } as any,
      onOk: async () => {
        await activateAdminUser(item.id);
        await load();
      },
    });
  };

  const openResetPassword = (item: AdminUserListItemDto) => {
    setResetPwUser(item);
    setResetPw("");
    setResetPwModalOpen(true);
  };

  const submitResetPassword = async () => {
    if (!resetPwUser) return;
    try {
      await resetAdminUserPassword(resetPwUser.id, { newPassword: resetPw });
      message.success(t("common.actions.save"));
      setResetPwModalOpen(false);
    } catch (e) {
      message.error((e as Error).message);
    }
  };

  const openManageRoles = async (item: AdminUserListItemDto) => {
    setRolesUser(item);
    setRolesSelectedIds([]);
    setRolesModalOpen(true);
    try {
      const dto = await getAdminUserRoles(item.id);
      setRolesSelectedIds(dto.roleIds);
    } catch (e) {
      message.error((e as Error).message);
    }
  };

  const submitRoles = async () => {
    if (!rolesUser) return;
    try {
      await replaceAdminUserRoles(rolesUser.id, { roleIds: rolesSelectedIds });
      message.success(t("common.actions.save"));
      setRolesModalOpen(false);
      await load();
    } catch (e) {
      message.error((e as Error).message);
    }
  };

  const roleLabel = (codes: string[]) => codes.join(", ");

  const columns: ColumnsType<AdminUserListItemDto> = useMemo(
    () => [
      {
        title: t("settings.security.users.columns.login"),
        dataIndex: "login",
        key: "login",
      },
      {
        title: t("settings.security.users.columns.employee"),
        dataIndex: "employeeFullName",
        key: "employeeFullName",
        render: (v?: string | null) => v ?? "—",
      },
      {
        title: t("settings.security.users.columns.roles"),
        dataIndex: "roleCodes",
        key: "roleCodes",
        render: (v: string[]) => (v?.length ? roleLabel(v) : "—"),
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
          <Space wrap>
            <Button
              size="small"
              onClick={() => openEdit(record)}
              disabled={!canEditUsers}
              data-testid={`settings-security-users-edit-${record.id}`}
            >
              {t("common.actions.edit")}
            </Button>
            <Button
              size="small"
              onClick={() => openManageRoles(record)}
              disabled={!canEditRoles}
              data-testid={`settings-security-users-roles-${record.id}`}
            >
              Роли
            </Button>
            <Button
              size="small"
              onClick={() => openResetPassword(record)}
              disabled={!canEditUsers}
              data-testid={`settings-security-users-resetpw-${record.id}`}
            >
              Пароль
            </Button>
            {record.isActive ? (
              <Button
                size="small"
                danger
                onClick={() => onDeactivate(record)}
                disabled={!canEditUsers}
                data-testid={`settings-security-users-deactivate-${record.id}`}
              >
                Деактивировать
              </Button>
            ) : (
              <Button
                size="small"
                onClick={() => onActivate(record)}
                disabled={!canEditUsers}
                data-testid={`settings-security-users-activate-${record.id}`}
              >
                Активировать
              </Button>
            )}
          </Space>
        ),
      },
    ],
    [canEditRoles, canEditUsers]
  );

  const employeeOptions = useMemo(
    () =>
      employees.map((e) => ({
        value: e.id,
        label: e.fullName,
      })),
    [employees]
  );

  const roleOptions = useMemo(
    () =>
      roles.map((r) => ({
        value: r.id,
        label: `${r.code} — ${r.name}`,
      })),
    [roles]
  );

  return (
    <div>
      <Typography.Title level={3} style={{ marginTop: 0 }}>
        {t("settings.security.users.title")}
      </Typography.Title>

      {(!canEditUsers || !canEditRoles) && (
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
          data-testid="settings-security-users-search"
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
          data-testid="settings-security-users-active-filter"
        />
        <Button onClick={() => void load()} data-testid="settings-security-users-refresh">
          {t("common.actions.refresh")}
        </Button>
        <Button
          type="primary"
          onClick={openCreate}
          disabled={!canEditUsers}
          data-testid="settings-security-users-create"
        >
          {t("common.actions.create")}
        </Button>
      </Space>

      <Table
        data-testid="settings-security-users-table"
        rowKey={(r: AdminUserListItemDto) => r.id}
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
          disabled: !canEditUsers,
          "data-testid": "settings-security-users-save",
        }}
        cancelButtonProps={{
          "data-testid": "settings-security-users-cancel",
        }}
        title={mode === "create" ? t("common.actions.create") : t("common.actions.edit")}
      >
        <Form form={form} layout="vertical">
          <Form.Item
            label={t("settings.security.users.columns.login")}
            name="login"
            rules={[{ required: true, message: "Введите логин" }]}
          >
            <Input data-testid="settings-security-users-form-login" />
          </Form.Item>

          {mode === "create" && (
            <Form.Item
              label="Пароль"
              name="password"
              rules={[{ required: true, message: "Введите пароль" }]}
            >
              <Input.Password data-testid="settings-security-users-form-password" />
            </Form.Item>
          )}

          <Form.Item
            label={t("settings.security.users.columns.employee")}
            name="employeeId"
          >
            <Select
              allowClear
              showSearch
              options={employeeOptions}
              optionFilterProp="label"
              data-testid="settings-security-users-form-employee"
            />
          </Form.Item>

          <Form.Item
            label={t("settings.security.common.columns.active")}
            name="isActive"
            valuePropName="checked"
          >
            <Switch data-testid="settings-security-users-form-isActive" />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        open={rolesModalOpen}
        onCancel={() => setRolesModalOpen(false)}
        onOk={() => void submitRoles()}
        okButtonProps={{
          disabled: !canEditRoles,
          "data-testid": "settings-security-users-roles-save",
        }}
        cancelButtonProps={{
          "data-testid": "settings-security-users-roles-cancel",
        }}
        title="Роли пользователя"
      >
        <Select
          mode="multiple"
          style={{ width: "100%" }}
          value={rolesSelectedIds}
          onChange={(v: string[]) => setRolesSelectedIds(v)}
          options={roleOptions}
          data-testid="settings-security-users-roles-select"
        />
      </Modal>

      <Modal
        open={resetPwModalOpen}
        onCancel={() => setResetPwModalOpen(false)}
        onOk={() => void submitResetPassword()}
        okButtonProps={{
          disabled: !canEditUsers,
          "data-testid": "settings-security-users-resetpw-save",
        }}
        cancelButtonProps={{
          "data-testid": "settings-security-users-resetpw-cancel",
        }}
        title={t("settings.security.confirm.resetPassword")}
      >
        <Input.Password
          value={resetPw}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
            setResetPw(e.target.value)
          }
          placeholder="Новый пароль"
          data-testid="settings-security-users-resetpw-input"
        />
      </Modal>
    </div>
  );
};

