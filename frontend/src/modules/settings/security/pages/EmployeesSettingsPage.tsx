import React, { useCallback, useEffect, useMemo, useState } from "react";
import { Alert, Button, Input, Select, Table, Typography } from "antd";
import type { ColumnsType } from "antd/es/table";
import { useNavigate } from "react-router-dom";

import { CommandBar } from "../../../../components/ui/CommandBar";
import { useCan } from "../../../../core/auth/permissions";
import { t } from "../../../../core/i18n/t";
import { getAdminEmployees } from "../api/adminSecurityApi";
import type { AdminEmployeeDto } from "../api/types";

type ActiveFilter = "all" | "active" | "inactive";

export const EmployeesSettingsPage: React.FC = () => {
  const canEdit = useCan("Admin.Security.EditEmployees");
  const navigate = useNavigate();

  const [items, setItems] = useState<AdminEmployeeDto[]>([]);
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
          <>
            <Input
              placeholder="Поиск..."
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
                { value: "all", label: "Все" },
                { value: "active", label: "Только активные" },
                { value: "inactive", label: "Только неактивные" },
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
          </>
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

