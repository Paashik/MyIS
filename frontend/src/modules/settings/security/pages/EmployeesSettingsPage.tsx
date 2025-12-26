import React, { useCallback, useEffect, useMemo, useState } from "react";
import { Alert, Button, Dropdown, Input, Modal, Select, Space, Table, Typography, message } from "antd";
import type { ColumnsType } from "antd/es/table";
import { useNavigate } from "react-router-dom";
import type { MenuProps } from "antd";

import { CommandBar } from "../../../../components/ui/CommandBar";
import { useCan } from "../../../../core/auth/permissions";
import { t } from "../../../../core/i18n/t";
import { getAdminEmployees } from "../api/adminSecurityApi";
import type { AdminEmployeeDto } from "../api/types";
import {
  getComponent2020Connection,
  runComponent2020Sync,
} from "../../integrations/component2020/api/adminComponent2020Api";
import { Component2020SyncMode, Component2020SyncScope } from "../../integrations/component2020/api/types";

type ActiveFilter = "all" | "active" | "inactive";

export const EmployeesSettingsPage: React.FC = () => {
  const canEdit = useCan("Admin.Security.EditEmployees");
  const canExecuteImport = useCan("Admin.Integration.Execute");
  const navigate = useNavigate();

  const [items, setItems] = useState<AdminEmployeeDto[]>([]);
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
      const toastKey = "employees-import";
      message.loading({ key: toastKey, content: t("references.mdm.import.running"), duration: 0 });

      const connection = await getComponent2020Connection();
      const connectionId = connection.id;

      if (!connectionId || connection.isActive === false) {
        message.error({ key: toastKey, content: t("references.mdm.import.noActiveConnection"), duration: 6 });
        return;
      }

      const resp = await runComponent2020Sync({
        connectionId,
        scope: Component2020SyncScope.Employees,
        dryRun: false,
        syncMode,
      });

      message.success({
        key: toastKey,
        duration: 6,
        content: (
          <Space size={8}>
            <span>
              {t("references.mdm.import.started", { status: resp.status })}{" "}
              ({resp.processedCount}) [{String(syncMode)}/{String(Component2020SyncScope.Employees)}]
            </span>
            <Button
              type="link"
              size="small"
              onClick={() =>
                navigate(
                  `/administration/integrations/component2020/runs/${encodeURIComponent(resp.runId)}`
                )
              }
            >
              {t("common.actions.open")}
            </Button>
          </Space>
        ),
      });

      await load();
    } catch (e) {
      message.error({ key: "employees-import", content: (e as Error).message, duration: 6 });
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

  const columns: ColumnsType<AdminEmployeeDto> = useMemo(
    () => [
      {
        title: t("settings.security.employees.columns.fullName"),
        dataIndex: "fullName",
        key: "fullName",
      },
      {
        title: t("settings.security.employees.columns.shortName"),
        dataIndex: "shortName",
        key: "shortName",
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
        render: (v: boolean) => (v ? t("common.yes") : t("common.no")),
      },
      {
        title: t("settings.security.common.columns.actions"),
        key: "actions",
        render: (_, record) => (
          <Button
            size="small"
            onClick={() =>
              navigate(`/administration/security/employees/${encodeURIComponent(record.id)}`)
            }
            disabled={!canEdit}
            data-testid={`administration-security-employees-open-${record.id}`}
          >
            {t("common.actions.edit")}
          </Button>
        ),
      },
    ],
    [canEdit, navigate]
  );

  return (
    <div data-testid="administration-security-employees-journal">
      <CommandBar
        left={
          <Typography.Title level={2} style={{ margin: 0 }}>
            {t("settings.security.employees.title")}
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
              data-testid="employees-import"
            >
              {t("references.mdm.import.button")}
            </Dropdown.Button>
            <Input
              placeholder={t("common.search")}
              value={search}
              onChange={(e: React.ChangeEvent<HTMLInputElement>) => setSearch(e.target.value)}
              style={{ width: 260 }}
              data-testid="administration-security-employees-search"
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
              data-testid="administration-security-employees-active-filter"
            />
            <Button onClick={() => void load()} data-testid="administration-security-employees-refresh">
              {t("common.actions.refresh")}
            </Button>
            <Button
              type="primary"
              onClick={() => navigate("/administration/security/employees/new")}
              disabled={!canEdit}
              data-testid="administration-security-employees-create"
            >
              {t("common.actions.create")}
            </Button>
          </Space>
        }
      />

      {!canEdit && (
        <Alert type="warning" showIcon message={t("settings.forbidden")} style={{ marginBottom: 12 }} />
      )}

      {error && <Alert type="error" showIcon message={error} style={{ marginBottom: 12 }} />}

      <Table
        data-testid="administration-security-employees-table"
        rowKey={(r: AdminEmployeeDto) => r.id}
        loading={loading}
        columns={columns}
        dataSource={items}
        pagination={false}
      />
    </div>
  );
};
