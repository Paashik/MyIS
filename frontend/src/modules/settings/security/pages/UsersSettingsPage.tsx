import React, { useCallback, useEffect, useMemo, useState } from "react";
import { Alert, Button, Input, Select, Table, Typography } from "antd";
import type { ColumnsType } from "antd/es/table";
import { useNavigate } from "react-router-dom";

import { CommandBar } from "../../../../components/ui/CommandBar";
import { useCan } from "../../../../core/auth/permissions";
import { t } from "../../../../core/i18n/t";
import { getAdminUsers } from "../api/adminSecurityApi";
import type { AdminUserListItemDto } from "../api/types";

type ActiveFilter = "all" | "active" | "inactive";

export const UsersSettingsPage: React.FC = () => {
  const canEditUsers = useCan("Admin.Security.EditUsers");
  const canEditRoles = useCan("Admin.Security.EditRoles");
  const navigate = useNavigate();

  const [items, setItems] = useState<AdminUserListItemDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [search, setSearch] = useState<string>("");
  const [activeFilter, setActiveFilter] = useState<ActiveFilter>("all");

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
    void load();
  }, [load]);

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
        render: (v: string | null | undefined, r) => v || r.employeeId || "-",
      },
      {
        title: t("settings.security.common.columns.active"),
        dataIndex: "isActive",
        key: "isActive",
        render: (v: boolean) => (v ? "Да" : "Нет"),
      },
      {
        title: t("settings.security.users.columns.roles"),
        dataIndex: "roleCodes",
        key: "roleCodes",
        render: (v: string[]) => (v?.length ? v.join(", ") : "-"),
      },
      {
        title: t("settings.security.common.columns.actions"),
        key: "actions",
        render: (_, record) => (
          <>
            <Button
              size="small"
              onClick={() => navigate(`/administration/security/users/${encodeURIComponent(record.id)}`)}
              disabled={!canEditUsers}
              style={{ marginRight: 8 }}
              data-testid={`administration-security-users-open-${record.id}`}
            >
              {t("common.actions.edit")}
            </Button>
            <Button
              size="small"
              onClick={() => navigate(`/administration/security/users/${encodeURIComponent(record.id)}/roles`)}
              disabled={!canEditRoles}
              style={{ marginRight: 8 }}
              data-testid={`administration-security-users-roles-${record.id}`}
            >
              {t("settings.security.users.roles.action")}
            </Button>
            <Button
              size="small"
              onClick={() => navigate(`/administration/security/users/${encodeURIComponent(record.id)}/reset-password`)}
              disabled={!canEditUsers}
              data-testid={`administration-security-users-resetpw-${record.id}`}
            >
              {t("settings.security.users.resetPassword.action")}
            </Button>
          </>
        ),
      },
    ],
    [canEditRoles, canEditUsers, navigate]
  );

  return (
    <div data-testid="administration-security-users-journal">
      <CommandBar
        left={
          <Typography.Title level={2} style={{ margin: 0 }}>
            {t("settings.security.users.title")}
          </Typography.Title>
        }
        right={
          <>
            <Input
              placeholder="Поиск..."
              value={search}
              onChange={(e: React.ChangeEvent<HTMLInputElement>) => setSearch(e.target.value)}
              style={{ width: 260 }}
              data-testid="administration-security-users-search"
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
              data-testid="administration-security-users-active-filter"
            />
            <Button onClick={() => void load()} data-testid="administration-security-users-refresh">
              {t("common.actions.refresh")}
            </Button>
            <Button
              type="primary"
              onClick={() => navigate("/administration/security/users/new")}
              disabled={!canEditUsers}
              data-testid="administration-security-users-create"
            >
              {t("common.actions.create")}
            </Button>
          </>
        }
      />

      {!canEditUsers && (
        <Alert type="warning" showIcon message={t("settings.forbidden")} style={{ marginBottom: 12 }} />
      )}

      {error && <Alert type="error" showIcon message={error} style={{ marginBottom: 12 }} />}

      <Table
        data-testid="administration-security-users-table"
        rowKey={(r: AdminUserListItemDto) => r.id}
        loading={loading}
        columns={columns}
        dataSource={items}
        pagination={false}
      />
    </div>
  );
};
