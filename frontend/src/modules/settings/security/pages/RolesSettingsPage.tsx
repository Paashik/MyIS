import React, { useCallback, useEffect, useMemo, useState } from "react";
import { Alert, Button, Table, Typography } from "antd";
import type { ColumnsType } from "antd/es/table";
import { useNavigate } from "react-router-dom";

import { CommandBar } from "../../../../components/ui/CommandBar";
import { useCan } from "../../../../core/auth/permissions";
import { t } from "../../../../core/i18n/t";
import { getAdminRoles } from "../api/adminSecurityApi";
import type { AdminRoleDto } from "../api/types";

export const RolesSettingsPage: React.FC = () => {
  const canEdit = useCan("Admin.Security.EditRoles");
  const navigate = useNavigate();

  const [items, setItems] = useState<AdminRoleDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

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
          <Button
            size="small"
            onClick={() => navigate(`/administration/security/roles/${encodeURIComponent(record.id)}`)}
            disabled={!canEdit}
            data-testid={`administration-security-roles-open-${record.id}`}
          >
            {t("common.actions.edit")}
          </Button>
        ),
      },
    ],
    [canEdit, navigate]
  );

  return (
    <div data-testid="administration-security-roles-journal">
      <CommandBar
        left={
          <Typography.Title level={2} style={{ margin: 0 }}>
            {t("settings.security.roles.title")}
          </Typography.Title>
        }
        right={
          <>
            <Button onClick={() => void load()} data-testid="administration-security-roles-refresh">
              {t("common.actions.refresh")}
            </Button>
            <Button
              type="primary"
              onClick={() => navigate("/administration/security/roles/new")}
              disabled={!canEdit}
              data-testid="administration-security-roles-create"
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
        data-testid="administration-security-roles-table"
        rowKey={(r: AdminRoleDto) => r.id}
        loading={loading}
        columns={columns}
        dataSource={items}
        pagination={false}
      />
    </div>
  );
};

