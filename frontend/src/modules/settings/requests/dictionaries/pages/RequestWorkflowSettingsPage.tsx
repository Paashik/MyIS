import React, { useCallback, useEffect, useMemo, useState } from "react";
import { Alert, Button, Select, Table, Typography } from "antd";
import type { ColumnsType } from "antd/es/table";
import { useNavigate } from "react-router-dom";

import { CommandBar } from "../../../../../components/ui/CommandBar";
import { useCan } from "../../../../../core/auth/permissions";
import { t } from "../../../../../core/i18n/t";
import {
  getAdminRequestStatuses,
  getAdminRequestTypes,
  getAdminWorkflowTransitions,
} from "../api/adminRequestsDictionariesApi";
import type {
  AdminRequestStatusDto,
  AdminRequestTypeDto,
  AdminRequestWorkflowTransitionDto,
} from "../api/types";

export const RequestWorkflowSettingsPage: React.FC = () => {
  const canEdit = useCan("Admin.Requests.EditWorkflow");
  const navigate = useNavigate();

  const [types, setTypes] = useState<AdminRequestTypeDto[]>([]);
  const [statuses, setStatuses] = useState<AdminRequestStatusDto[]>([]);
  const [typeId, setTypeId] = useState<string>("");

  const [items, setItems] = useState<AdminRequestWorkflowTransitionDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadLookups = useCallback(async () => {
    setError(null);
    try {
      const [tps, sts] = await Promise.all([
        getAdminRequestTypes(),
        getAdminRequestStatuses(),
      ]);
      setTypes(tps);
      setStatuses(sts);

      const firstActive = tps.find((x) => x.isActive) ?? tps[0];
      if (firstActive && !typeId) {
        setTypeId(firstActive.id);
      }
    } catch (e) {
      setError((e as Error).message);
    }
  }, [typeId]);

  const loadTransitions = useCallback(async () => {
    if (!typeId) return;

    setLoading(true);
    setError(null);
    try {
      const data = await getAdminWorkflowTransitions(typeId);
      setItems(data);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setLoading(false);
    }
  }, [typeId]);

  useEffect(() => {
    void loadLookups();
  }, [loadLookups]);

  useEffect(() => {
    void loadTransitions();
  }, [loadTransitions]);

  const statusCodeById = useMemo(() => {
    const map = new Map<string, string>();
    for (const s of statuses) map.set(s.id, s.code);
    return map;
  }, [statuses]);

  const columns: ColumnsType<AdminRequestWorkflowTransitionDto> = useMemo(
    () => [
      {
        title: t("settings.requests.form.fromStatus"),
        dataIndex: "fromStatusId",
        key: "fromStatusId",
        render: (v: string) => statusCodeById.get(v) ?? v,
      },
      {
        title: t("settings.requests.form.toStatus"),
        dataIndex: "toStatusId",
        key: "toStatusId",
        render: (v: string) => statusCodeById.get(v) ?? v,
      },
      {
        title: t("settings.requests.form.actionCode"),
        dataIndex: "actionCode",
        key: "actionCode",
      },
      {
        title: t("settings.requests.form.requiredPermission"),
        dataIndex: "requiredPermission",
        key: "requiredPermission",
        render: (v?: string | null) => v || "-",
      },
      {
        title: t("settings.requests.form.isEnabled"),
        dataIndex: "isEnabled",
        key: "isEnabled",
        render: (v: boolean) => (v ? "Да" : "Нет"),
      },
      {
        title: t("settings.requests.workflow.columns.actions"),
        key: "actions",
        render: (_, record) => (
          <Button
            size="small"
            onClick={() =>
              navigate(
                `/administration/requests/workflow/${encodeURIComponent(typeId)}/${encodeURIComponent(record.id)}`
              )
            }
            disabled={!canEdit || !typeId}
          >
            {t("common.actions.edit")}
          </Button>
        ),
      },
    ],
    [canEdit, navigate, statusCodeById, typeId]
  );

  return (
    <div data-testid="administration-requests-workflow-journal">
      <CommandBar
        left={
          <Typography.Title level={2} style={{ margin: 0 }}>
            {t("settings.requests.workflow.title")}
          </Typography.Title>
        }
        right={
          <>
            <Select
              value={typeId || undefined}
              onChange={(v: string) => setTypeId(v)}
              style={{ width: 320 }}
              options={types.map((x) => ({
                value: x.id,
                label: `${x.name}${x.isActive ? "" : " (inactive)"}`,
              }))}
              data-testid="administration-requests-workflow-type"
            />

            <Button onClick={() => void loadTransitions()} disabled={!typeId}>
              {t("common.actions.refresh")}
            </Button>

            <Button
              type="primary"
              onClick={() =>
                navigate(`/administration/requests/workflow/${encodeURIComponent(typeId)}/new`)
              }
              disabled={!canEdit || !typeId}
            >
              {t("common.actions.add")}
            </Button>
          </>
        }
      />

      {!canEdit && (
        <Alert type="warning" showIcon message={t("settings.forbidden")} style={{ marginBottom: 12 }} />
      )}

      {error && <Alert type="error" showIcon message={error} style={{ marginBottom: 12 }} />}

      <Table
        data-testid="administration-requests-workflow-table"
        rowKey={(r: AdminRequestWorkflowTransitionDto) => r.id}
        loading={loading}
        columns={columns}
        dataSource={items}
        pagination={false}
      />
    </div>
  );
};
