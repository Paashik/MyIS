import React, { useCallback, useEffect, useMemo, useState } from "react";
import { Alert, Button, Dropdown, Input, Modal, Select, Space, Table, Typography, message } from "antd";
import type { ColumnsType } from "antd/es/table";
import { useNavigate } from "react-router-dom";
import type { MenuProps } from "antd";

import { CommandBar } from "../../../../components/ui/CommandBar";
import { useCan } from "../../../../core/auth/permissions";
import { t } from "../../../../core/i18n/t";
import { getAdminUsers } from "../api/adminSecurityApi";
import {
  getComponent2020Connection,
  runComponent2020Sync,
} from "../../integrations/component2020/api/adminComponent2020Api";
import { Component2020SyncMode, Component2020SyncScope } from "../../integrations/component2020/api/types";
import type { AdminUserListItemDto } from "../api/types";

type ActiveFilter = "all" | "active" | "inactive";

export const UsersSettingsPage: React.FC = () => {
  const canEditUsers = useCan("Admin.Security.EditUsers");
  const canEditRoles = useCan("Admin.Security.EditRoles");
  const canExecuteImport = useCan("Admin.Integration.Execute");
  const navigate = useNavigate();

  const [items, setItems] = useState<AdminUserListItemDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [importLoading, setImportLoading] = useState(false);
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

  const importMenuItems: MenuProps["items"] = useMemo(
    () => [
      { key: "delta", label: t("references.mdm.import.delta") },
      { key: "snapshotUpsert", label: t("references.mdm.import.snapshotUpsert") },
      { key: "overwrite", label: t("references.mdm.import.overwrite"), danger: true },
    ],
    []
  );

  const runImport = async (syncMode: Component2020SyncMode) => {
    setImportLoading(true);
    try {
      const toastKey = "users-import";
      message.loading({ key: toastKey, content: t("references.mdm.import.running"), duration: 0 });

      const connection = await getComponent2020Connection();
      const connectionId = connection.id;

      if (!connectionId || connection.isActive === false) {
        message.error({ key: toastKey, content: t("references.mdm.import.noActiveConnection"), duration: 6 });
        return;
      }

      const resp = await runComponent2020Sync({
        connectionId,
        scope: Component2020SyncScope.Users,
        dryRun: false,
        syncMode,
      });

      message.success({
        key: toastKey,
        duration: 6,
        content: (
          <span>
            {t("references.mdm.import.started", { status: resp.status })} ({resp.processedCount}) [{String(syncMode)}/{String(Component2020SyncScope.Users)}]
          </span>
        ),
      });

      await load();
    } catch (e) {
      message.error({ key: "users-import", content: (e as Error).message, duration: 6 });
    } finally {
      setImportLoading(false);
    }
  };

  const confirmOverwrite = () => {
    Modal.confirm({
      title: t("references.mdm.import.overwrite.confirmTitle"),
      content: t("references.mdm.import.overwrite.confirmBody"),
      okText: t("references.mdm.import.overwrite.confirmOk"),
      okType: "danger",
      closable: true,
      cancelText: t("common.actions.cancel"),
      onOk: async () => runImport(Component2020SyncMode.Overwrite),
    });
  };

  const onImportMenuClick: MenuProps["onClick"] = ({ key }: { key: string }) => {
    if (key === "delta") {
      void runImport(Component2020SyncMode.Delta);
      return;
    }

    if (key === "snapshotUpsert") {
      void runImport(Component2020SyncMode.SnapshotUpsert);
      return;
    }

    if (key === "overwrite") {
      confirmOverwrite();
    }
  };


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
        render: (v: boolean) => (v ? t("common.yes") : t("common.no")),
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
          <Space align="center">
            <Dropdown.Button
              trigger={["click"]}
              loading={importLoading}
              disabled={!canExecuteImport}
              menu={{ items: importMenuItems, onClick: onImportMenuClick }}
              onClick={() => void runImport(Component2020SyncMode.SnapshotUpsert)}
              data-testid="users-import"
            >
              {t("references.mdm.import.button")}
            </Dropdown.Button>
            <Input
              placeholder={t("common.search")}
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
                { value: "all", label: t("references.filters.all") },
                { value: "active", label: t("references.filters.active") },
                { value: "inactive", label: t("references.filters.inactive") },
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
          </Space>
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
