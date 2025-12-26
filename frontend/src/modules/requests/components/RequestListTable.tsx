import React from "react";
import { Table, Select, Segmented, Space, Typography } from "antd";

import {
  RequestListItemDto,
  RequestStatusDto,
  RequestTypeDto,
} from "../api/types";
import { RequestStatusBadge } from "./RequestStatusBadge";
import { t } from "../../../core/i18n/t";

const { Text } = Typography;

export interface RequestListTableFilters {
  requestStatusId?: string;
  requestTypeId?: string;
  onlyMine?: boolean;
}

export interface RequestListTableProps {
  loading: boolean;
  items: RequestListItemDto[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  requestStatuses: RequestStatusDto[];
  requestTypes: RequestTypeDto[];
  filters: RequestListTableFilters;
  onFiltersChange: (next: RequestListTableFilters) => void;
  onPageChange: (page: number, pageSize: number) => void;
  onRowClick: (item: RequestListItemDto) => void;
}

export const RequestListTable: React.FC<RequestListTableProps> = ({
  loading,
  items,
  totalCount,
  pageNumber,
  pageSize,
  requestStatuses,
  requestTypes,
  filters,
  onFiltersChange,
  onPageChange,
  onRowClick,
}) => {
  const handleStatusChange = (value: string | undefined) => {
    onFiltersChange({
      ...filters,
      requestStatusId: value || undefined,
    });
    onPageChange(1, pageSize);
  };

  const handleTypeChange = (value: string | undefined) => {
    onFiltersChange({
      ...filters,
      requestTypeId: value || undefined,
    });
    onPageChange(1, pageSize);
  };

  const handleOnlyMineChange = (value: string | number | boolean) => {
    const onlyMine = value === "mine";
    onFiltersChange({
      ...filters,
      onlyMine,
    });
    onPageChange(1, pageSize);
  };

  const columns: any[] = [
    {
      title: t("requests.table.columns.title"),
      dataIndex: "title",
      key: "title",
    },
    {
      title: t("requests.table.columns.createdAt"),
      dataIndex: "createdAt",
      key: "createdAt",
      render: (value: string) => {
        const date = new Date(value);
        return (
          <span>
            {date.toLocaleDateString()} {date.toLocaleTimeString()}
          </span>
        );
      },
    },
    {
      title: t("requests.table.columns.initiator"),
      dataIndex: "initiatorFullName",
      key: "initiatorFullName",
      render: (_: any, record: RequestListItemDto) =>
        record.initiatorFullName || (
          <Text type="secondary">{t("requests.table.value.unknownInitiator")}</Text>
        ),
    },
    {
      title: t("requests.table.columns.target"),
      dataIndex: "targetEntityName",
      key: "targetEntityName",
      ellipsis: true,
      render: (value?: string | null) =>
        value || <Text type="secondary">{t("requests.details.value.notSet")}</Text>,
    },
    {
      title: t("requests.table.columns.basis"),
      dataIndex: "relatedEntityName",
      key: "relatedEntityName",
      ellipsis: true,
      render: (value?: string | null) =>
        value || <Text type="secondary">{t("requests.details.value.notSet")}</Text>,
    },
    {
      title: t("requests.table.columns.status"),
      dataIndex: "requestStatusName",
      key: "requestStatusName",
      render: (_: any, record: RequestListItemDto) => (
        <RequestStatusBadge
          statusCode={record.requestStatusCode}
          statusName={record.requestStatusName}
        />
      ),
    },
  ];

  const pagination = {
    current: pageNumber,
    pageSize,
    total: totalCount,
    showSizeChanger: true,
    onChange: (page: number, pageSizeValue: number) => {
      onPageChange(page, pageSizeValue);
    },
  };

  return (
    <div>
      <Space
        style={{ marginBottom: 16, display: "flex", justifyContent: "space-between" }}
        wrap
      >
        <Space wrap>
          <Select
            data-testid="requests-filter-type"
            allowClear
            placeholder={t("requests.table.filters.type")}
            style={{ minWidth: 220 }}
            value={filters.requestTypeId}
            onChange={handleTypeChange}
            options={requestTypes.map((t) => ({
              label: t.name,
              value: t.id,
            }))}
          />
          <Select
            data-testid="requests-filter-status"
            allowClear
            placeholder={t("requests.table.filters.status")}
            style={{ minWidth: 200 }}
            value={filters.requestStatusId}
            onChange={handleStatusChange}
            options={requestStatuses.map((s) => ({
              label: s.name,
              value: s.id,
            }))}
          />
        </Space>

        <Segmented
          data-testid="requests-filter-only-mine"
          value={filters.onlyMine ? "mine" : "all"}
          onChange={handleOnlyMineChange}
          options={[
            { label: t("requests.table.filters.onlyMine.all"), value: "all" },
            { label: t("requests.table.filters.onlyMine.mine"), value: "mine" },
          ]}
        />
      </Space>

      <Table
        data-testid="requests-table"
        rowKey={(record: RequestListItemDto) => record.id}
        loading={loading}
        columns={columns}
        dataSource={items}
        pagination={pagination}
        onRow={(record: RequestListItemDto) => ({
          onClick: () => onRowClick(record),
          style: { cursor: "pointer" },
        })}
        size="middle"
      />
    </div>
  );
};
