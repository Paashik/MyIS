import React, { useCallback, useEffect, useMemo, useState } from "react";
import { Alert, Button, Card, Select, Typography, message } from "antd";
import { useNavigate, useParams } from "react-router-dom";

import { CommandBar } from "../../../../components/ui/CommandBar";
import { useCan } from "../../../../core/auth/permissions";
import { t } from "../../../../core/i18n/t";
import { getAdminRoles, getAdminUserRoles, replaceAdminUserRoles } from "../api/adminSecurityApi";
import type { AdminRoleDto } from "../api/types";

export const UserRolesCardPage: React.FC = () => {
  const canEditRoles = useCan("Admin.Security.EditRoles");
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const userId = id ?? "";

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [roles, setRoles] = useState<AdminRoleDto[]>([]);
  const [selectedRoleIds, setSelectedRoleIds] = useState<string[]>([]);

  const load = useCallback(async () => {
    if (!id) {
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const [rolesData, userRoles] = await Promise.all([
        getAdminRoles(),
        getAdminUserRoles(userId),
      ]);
      setRoles(rolesData);
      setSelectedRoleIds(userRoles.roleIds ?? []);
    } catch (e) {
      setError((e as Error).message || t("common.error.unknownNetwork"));
    } finally {
      setLoading(false);
    }
  }, [id, userId]);

  useEffect(() => {
    void load();
  }, [load]);

  const options = useMemo(
    () => roles.map((r) => ({ value: r.id, label: `${r.code} — ${r.name}` })),
    [roles]
  );

  const onCancel = () => navigate("/administration/security/users");

  const onSave = async () => {
    if (!id) {
      return;
    }

    try {
      await replaceAdminUserRoles(userId, { roleIds: selectedRoleIds });
      message.success(t("common.actions.save"));
      navigate("/administration/security/users", { replace: true });
    } catch (e) {
      message.error((e as Error).message);
    }
  };

  return (
    <div data-testid="administration-security-users-roles-card">
      <CommandBar
        left={
          <Typography.Title level={2} style={{ margin: 0 }}>
            {t("settings.security.users.roles.title")}
          </Typography.Title>
        }
        right={
          <>
            <Button onClick={() => void load()} disabled={loading}>
              {t("common.actions.refresh")}
            </Button>
            <Button onClick={onCancel}>{t("common.actions.cancel")}</Button>
            <Button type="primary" onClick={() => void onSave()} disabled={!canEditRoles} loading={loading}>
              {t("common.actions.save")}
            </Button>
          </>
        }
      />

      {!canEditRoles && (
        <Alert type="warning" showIcon message={t("settings.forbidden")} style={{ marginBottom: 12 }} />
      )}

      {error && <Alert type="error" showIcon message={error} style={{ marginBottom: 12 }} />}

      <Card>
        <Select
          mode="multiple"
          style={{ width: "100%" }}
          value={selectedRoleIds}
          onChange={(v: string[]) => setSelectedRoleIds(v)}
          options={options}
          disabled={!canEditRoles}
        />
      </Card>
    </div>
  );
};

