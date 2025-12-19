import React, { useState } from "react";
import { Alert, Button, Card, Input, Typography, message } from "antd";
import { useNavigate, useParams } from "react-router-dom";

import { CommandBar } from "../../../../components/ui/CommandBar";
import { useCan } from "../../../../core/auth/permissions";
import { t } from "../../../../core/i18n/t";
import { resetAdminUserPassword } from "../api/adminSecurityApi";

export const UserResetPasswordCardPage: React.FC = () => {
  const canEditUsers = useCan("Admin.Security.EditUsers");
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const userId = id;

  const [loading, setLoading] = useState(false);
  const [value, setValue] = useState("");

  const onCancel = () => navigate("/administration/security/users");

  const onSave = async () => {
    try {
      setLoading(true);
      await resetAdminUserPassword(userId, { newPassword: value });
      message.success(t("common.actions.save"));
      navigate("/administration/security/users", { replace: true });
    } catch (e) {
      message.error((e as Error).message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div data-testid="administration-security-users-resetpw-card">
      <CommandBar
        left={
          <Typography.Title level={2} style={{ margin: 0 }}>
            {t("settings.security.users.resetPassword.title")}
          </Typography.Title>
        }
        right={
          <>
            <Button onClick={onCancel}>{t("common.actions.cancel")}</Button>
            <Button type="primary" onClick={() => void onSave()} disabled={!canEditUsers} loading={loading}>
              {t("common.actions.save")}
            </Button>
          </>
        }
      />

      {!canEditUsers && (
        <Alert type="warning" showIcon message={t("settings.forbidden")} style={{ marginBottom: 12 }} />
      )}

      <Card>
        <Input.Password
          value={value}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => setValue(e.target.value)}
          placeholder="Новый пароль"
          disabled={!canEditUsers}
        />
      </Card>
    </div>
  );
};
