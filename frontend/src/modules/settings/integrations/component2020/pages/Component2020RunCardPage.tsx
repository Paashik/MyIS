import React, { useCallback, useEffect, useMemo, useState } from "react";
import { Alert, Card, Descriptions, Space, Table, Tag, Typography } from "antd";
import type { ColumnsType } from "antd/es/table";
import { useNavigate, useParams } from "react-router-dom";

import { CommandBar } from "../../../../../components/ui/CommandBar";
import { t } from "../../../../../core/i18n/t";
import {
  getComponent2020SyncRunById,
  getComponent2020SyncRunErrors,
} from "../api/adminComponent2020Api";
import type { Component2020SyncErrorDto, Component2020SyncRunDto } from "../api/types";

const { Title, Text } = Typography;

type Params = { runId?: string };

export const Component2020RunCardPage: React.FC = () => {
  const navigate = useNavigate();
  const { runId } = useParams<Params>();

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [run, setRun] = useState<Component2020SyncRunDto | null>(null);

  const [errorsLoading, setErrorsLoading] = useState(false);
  const [errors, setErrors] = useState<Component2020SyncErrorDto[]>([]);

  const load = useCallback(async () => {
    if (!runId) return;
    setLoading(true);
    setError(null);
    try {
      const data = await getComponent2020SyncRunById(runId);
      setRun(data);
    } catch (e) {
      setError((e as Error).message);
      setRun(null);
    } finally {
      setLoading(false);
    }
  }, [runId]);

  const loadErrors = useCallback(async () => {
    if (!runId) return;
    setErrorsLoading(true);
    try {
      const resp = await getComponent2020SyncRunErrors(runId);
      setErrors(resp.errors);
    } catch (e) {
      setErrors([]);
      setError((e as Error).message);
    } finally {
      setErrorsLoading(false);
    }
  }, [runId]);

  useEffect(() => {
    void load();
    void loadErrors();
  }, [load, loadErrors]);

  const title = useMemo(() => {
    if (!run) return t("settings.integrations.component2020.runs.card.title");
    return `${t("settings.integrations.component2020.runs.card.title")} — ${run.id}`;
  }, [run]);

  const errorsColumns: ColumnsType<Component2020SyncErrorDto> = useMemo(
    () => [
      {
        title: t("settings.integrations.component2020.errors.columns.createdAt"),
        dataIndex: "createdAt",
        key: "createdAt",
        render: (v: string) => new Date(v).toLocaleString(),
      },
      {
        title: t("settings.integrations.component2020.errors.columns.entityType"),
        dataIndex: "entityType",
        key: "entityType",
      },
      {
        title: t("settings.integrations.component2020.errors.columns.external"),
        key: "external",
        render: (_: unknown, r: Component2020SyncErrorDto) =>
          `${r.externalEntity ?? "-"}:${r.externalKey ?? "-"}`,
      },
      {
        title: t("settings.integrations.component2020.errors.columns.message"),
        dataIndex: "message",
        key: "message",
      },
      {
        title: t("settings.integrations.component2020.errors.columns.details"),
        dataIndex: "details",
        key: "details",
      },
    ],
    []
  );

  if (!runId) {
    return <Alert type="error" showIcon message={t("common.error.notFound")} />;
  }

  return (
    <div>
      <CommandBar
        left={
          <Space direction="vertical" size={0}>
            <Title level={2} style={{ margin: 0 }}>
              {title}
            </Title>
            <Text type="secondary">{t("settings.integrations.component2020.runs.card.subtitle")}</Text>
          </Space>
        }
        right={
          <Space>
            <a onClick={() => navigate("/administration/integrations/component2020")}>
              {t("common.actions.back")}
            </a>
            <a onClick={() => void load()}>{t("common.actions.refresh")}</a>
          </Space>
        }
      />

      {error && <Alert type="error" showIcon message={error} style={{ marginBottom: 12 }} />}

      <Card loading={loading} style={{ marginBottom: 16 }}>
        <Descriptions bordered size="small" column={1}>
          <Descriptions.Item label={t("settings.integrations.component2020.runs.columns.status")}>
            {run?.status ? <Tag>{run.status}</Tag> : "-"}
          </Descriptions.Item>
          <Descriptions.Item label={t("settings.integrations.component2020.runs.columns.scope")}>
            {run?.scope ?? "-"}
          </Descriptions.Item>
          <Descriptions.Item label={t("settings.integrations.component2020.runs.columns.mode")}>
            {run?.mode ?? "-"}
          </Descriptions.Item>
          <Descriptions.Item label={t("settings.integrations.component2020.runs.columns.startedAt")}>
            {run?.startedAt ? new Date(run.startedAt).toLocaleString() : "-"}
          </Descriptions.Item>
          <Descriptions.Item label={t("settings.integrations.component2020.runs.columns.finishedAt")}>
            {run?.finishedAt ? new Date(run.finishedAt).toLocaleString() : "-"}
          </Descriptions.Item>
          <Descriptions.Item label={t("settings.integrations.component2020.runs.columns.processed")}>
            {typeof run?.processedCount === "number" ? run.processedCount : "-"}
          </Descriptions.Item>
          <Descriptions.Item label={t("settings.integrations.component2020.runs.columns.errors")}>
            {typeof run?.errorCount === "number" ? run.errorCount : "-"}
          </Descriptions.Item>
          <Descriptions.Item label={t("settings.integrations.component2020.runs.columns.id")}>
            {run?.id ?? "-"}
          </Descriptions.Item>
        </Descriptions>
      </Card>

      <Card title={t("settings.integrations.component2020.errors.title")}>
        <Table
          data-testid="component2020-errors-table"
          rowKey={(e: Component2020SyncErrorDto) => e.id}
          loading={errorsLoading}
          dataSource={errors}
          pagination={{ pageSize: 10 }}
          columns={errorsColumns}
        />
      </Card>
    </div>
  );
};
