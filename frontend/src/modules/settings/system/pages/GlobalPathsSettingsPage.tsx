import React, { useCallback, useEffect, useMemo, useState } from "react";
import { Alert, Button, Card, Form, Input, Space, Typography, message } from "antd";

import { t } from "../../../../core/i18n/t";
import { useCan } from "../../../../core/auth/permissions";
import { CommandBar } from "../../../../components/ui/CommandBar";
import {
  getGlobalPathsSettings,
  updateGlobalPathsSettings,
  type GlobalPathsSettingsResponse,
} from "../api/adminGlobalPathsApi";

export const GlobalPathsSettingsPage: React.FC = () => {
  const canEdit = useCan("Admin.Settings.Access");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [data, setData] = useState<GlobalPathsSettingsResponse | null>(null);

  const [form] = Form.useForm();

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await getGlobalPathsSettings();
      setData(response);
      form.setFieldsValue({
        projectsRoot: response.settings.projectsRoot,
        documentsRoot: response.settings.documentsRoot,
        databasesRoot: response.settings.databasesRoot,
      });
    } catch (e) {
      setError((e as Error).message || t("common.error.unknownNetwork"));
    } finally {
      setLoading(false);
    }
  }, [form]);

  useEffect(() => {
    void load();
  }, [load]);

  const checkSummary = useMemo(() => {
    if (!data) return null;
    const rows = [
      { label: t("settings.system.paths.projectsRoot"), check: data.projectsRoot },
      { label: t("settings.system.paths.documentsRoot"), check: data.documentsRoot },
      { label: t("settings.system.paths.databasesRoot"), check: data.databasesRoot },
    ];

    return rows
      .map((r) => {
        if (!r.check.isSet) return `${r.label}: ${t("settings.system.paths.status.notSet")}`;
        if (r.check.exists && r.check.canWrite) return `${r.label}: ${t("settings.system.paths.status.ok")}`;
        if (r.check.exists && !r.check.canWrite) return `${r.label}: ${t("settings.system.paths.status.readOnly")}`;
        return `${r.label}: ${t("settings.system.paths.status.missing")}${r.check.error ? ` (${r.check.error})` : ""}`;
      })
      .join("\n");
  }, [data]);

  const onSave = async (createDirectories: boolean) => {
    const values = await form.validateFields();
    try {
      const response = await updateGlobalPathsSettings({
        projectsRoot: values.projectsRoot ?? "",
        documentsRoot: values.documentsRoot ?? "",
        databasesRoot: values.databasesRoot ?? "",
        createDirectories,
      });
      setData(response);
      message.success(t("settings.system.paths.save.ok"));
    } catch (e) {
      message.error((e as Error).message);
    }
  };

  return (
    <div>
      <CommandBar
        left={
          <Typography.Title level={2} style={{ margin: 0 }}>
            {t("settings.system.paths.title")}
          </Typography.Title>
        }
        right={
          <Button onClick={() => void load()} loading={loading} data-testid="global-paths-refresh">
            {t("common.actions.refresh")}
          </Button>
        }
      />

      {!canEdit && (
        <Alert
          type="warning"
          showIcon
          message={t("settings.forbidden")}
          style={{ marginBottom: 12 }}
        />
      )}

      {error && (
        <Alert type="error" showIcon message={error} style={{ marginBottom: 12 }} />
      )}

      <Card title={t("settings.system.paths.form.title")} style={{ marginBottom: 16 }}>
        <Form form={form} layout="vertical" disabled={!canEdit}>
          <Form.Item
            label={t("settings.system.paths.projectsRoot")}
            name="projectsRoot"
            extra={t("settings.system.paths.hint.serverPath")}
          >
            <Input data-testid="global-paths-projects-root" />
          </Form.Item>

          <Form.Item
            label={t("settings.system.paths.documentsRoot")}
            name="documentsRoot"
            extra={t("settings.system.paths.hint.serverPath")}
          >
            <Input data-testid="global-paths-documents-root" />
          </Form.Item>

          <Form.Item
            label={t("settings.system.paths.databasesRoot")}
            name="databasesRoot"
            extra={t("settings.system.paths.hint.serverPath")}
          >
            <Input data-testid="global-paths-databases-root" />
          </Form.Item>

          <Space>
            <Button
              type="primary"
              onClick={() => void onSave(false)}
              disabled={!canEdit}
              data-testid="global-paths-save"
            >
              {t("common.actions.save")}
            </Button>
            <Button
              onClick={() => void onSave(true)}
              disabled={!canEdit}
              data-testid="global-paths-save-create"
            >
              {t("settings.system.paths.save.createDirs")}
            </Button>
          </Space>
        </Form>
      </Card>

      {checkSummary && (
        <Card title={t("settings.system.paths.status.title")}>
          <Typography.Paragraph style={{ whiteSpace: "pre-wrap", marginBottom: 0 }}>
            {checkSummary}
          </Typography.Paragraph>
        </Card>
      )}
    </div>
  );
};
