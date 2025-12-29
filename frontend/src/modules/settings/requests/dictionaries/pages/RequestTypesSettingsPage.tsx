import React, { useCallback, useEffect, useMemo, useState } from "react";
import { Alert, Button, Table, Typography, message } from "antd";
import type { ColumnsType } from "antd/es/table";
import { useNavigate } from "react-router-dom";

import { t } from "../../../../../core/i18n/t";
import { useCan } from "../../../../../core/auth/permissions";
import {
  archiveAdminRequestType,
  getAdminRequestTypes,
} from "../api/adminRequestsDictionariesApi";
import type { AdminRequestTypeDto } from "../api/types";
import "./RequestDictionaries.css";

export const RequestTypesSettingsPage: React.FC = () => {
  const canEdit = useCan("Admin.Requests.EditTypes");
  const navigate = useNavigate();

  const [items, setItems] = useState<AdminRequestTypeDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await getAdminRequestTypes();
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

  useEffect(() => {
    document.body.classList.add("request-dict-scroll-lock");
    return () => {
      document.body.classList.remove("request-dict-scroll-lock");
    };
  }, []);

  const onArchive = async (item: AdminRequestTypeDto) => {
    try {
      await archiveAdminRequestType(item.id);
      await load();
      message.success(t("common.actions.save"));
    } catch (e) {
      message.error((e as Error).message);
    }
  };

  const columns: ColumnsType<AdminRequestTypeDto> = useMemo(
    () => [
      {
        title: t("settings.requests.types.columns.name"),
        dataIndex: "name",
        key: "name",
      },
      {
        title: t("settings.requests.types.columns.direction"),
        dataIndex: "direction",
        key: "direction",
        render: (v: string) => {
          if (v === "Incoming") return t("settings.requests.types.direction.incoming");
          if (v === "Outgoing") return t("settings.requests.types.direction.outgoing");
          return v;
        },
      },
      {
        title: t("settings.requests.types.columns.isActive"),
        dataIndex: "isActive",
        key: "isActive",
        render: (v: boolean) => (v ? t("common.yes") : t("common.no")),
      },
      {
        title: t("settings.requests.types.columns.actions"),
        key: "actions",
        render: (_, record) => (
          <>
            <Button
              size="small"
              onClick={() => navigate(`/references/requests/types/${encodeURIComponent(record.id)}`)}
              disabled={!canEdit}
              data-testid="references-requests-types-open"
              style={{ marginRight: 8 }}
            >
              {t("common.actions.edit")}
            </Button>
            <Button
              size="small"
              danger
              onClick={() => void onArchive(record)}
              disabled={!canEdit || !record.isActive}
              data-testid="references-requests-types-archive"
            >
              {t("common.actions.archive")}
            </Button>
          </>
        ),
      },
    ],
    [canEdit, navigate, load]
  );

  return (
    <div className="request-dict-page" data-testid="references-requests-types-journal">
      <div className="request-dict-header">
        <Typography.Title level={2} style={{ margin: 0 }}>
          {t("settings.requests.types.title")}
        </Typography.Title>
        <div className="request-dict-controls">
          <Button onClick={() => void load()} data-testid="references-requests-types-refresh">
            {t("common.actions.refresh")}
          </Button>
          <Button
            type="primary"
            onClick={() => navigate("/references/requests/types/new")}
            disabled={!canEdit}
            data-testid="references-requests-types-create"
          >
            {t("common.actions.create")}
          </Button>
        </div>
      </div>

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

      <div className="request-dict-divider" />

      <div className="request-dict-scroll">
        <div className="request-dict-list">
          <Table
            data-testid="references-requests-types-table"
            rowKey={(r: AdminRequestTypeDto) => r.id}
            loading={loading}
            columns={columns}
            dataSource={items}
            pagination={false}
            scroll={{ y: "100%" }}
          />
        </div>
      </div>
    </div>
  );
};
