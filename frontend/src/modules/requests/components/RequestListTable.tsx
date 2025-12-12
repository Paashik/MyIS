import React from "react";
import { Table, Select, Segmented, Space, Typography } from "antd";
import {
  RequestListItemDto,
  RequestStatusDto,
} from "../api/types";
import { RequestStatusBadge } from "./RequestStatusBadge";
import { t } from "../../../core/i18n/t";

const { Text } = Typography;

export interface RequestListTableFilters {
  requestStatusId?: string;
  onlyMine?: boolean;
}

export interface RequestListTableProps {
  loading: boolean;
  items: RequestListItemDto[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  requestStatuses: RequestStatusDto[];
  filters: RequestListTableFilters;
  onFiltersChange: (next: RequestListTableFilters) => void;
  onPageChange: (page: number, pageSize: number) => void;
  onRowClick: (item: RequestListItemDto) => void;
}

/**
 * Презентационный компонент таблицы заявок с фильтрами и пагинацией.
 * Все загрузки данных выполняются на уровне страницы.
 */
export const RequestListTable: React.FC<RequestListTableProps> = ({
  loading,
  items,
  totalCount,
  pageNumber,
  pageSize,
  requestStatuses,
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
      title: t("requests.details.fields.id"),
      dataIndex: "id",
      key: "id",
      width: 260,
      render: (value: string) => (
        <Text code ellipsis>{value}</Text>
      ),
    },
    {
      title: t("requests.table.columns.title"),
      dataIndex: "title",
      key: "title",
      ellipsis: true,
    },
    {
      title: t("requests.table.columns.type"),
      dataIndex: "requestTypeName",
      key: "requestTypeName",
      width: 200,
      render: (_: any, record: RequestListItemDto) => (
        <span>
          <Text strong>{record.requestTypeCode}</Text> {record.requestTypeName}
        </span>
      ),
    },
    {
      title: t("requests.table.columns.status"),
      dataIndex: "requestStatusName",
      key: "requestStatusName",
      width: 200,
      render: (_: any, record: RequestListItemDto) => (
        <RequestStatusBadge
          statusCode={record.requestStatusCode}
          statusName={record.requestStatusName}
        />
      ),
    },
    {
      title: t("requests.table.columns.initiator"),
      dataIndex: "initiatorFullName",
      key: "initiatorFullName",
      width: 220,
      render: (_: any, record: RequestListItemDto) =>
        record.initiatorFullName || (
          <Text type="secondary">{t("requests.table.value.unknownInitiator")}</Text>
        ),
    },
    {
      title: t("requests.table.columns.createdAt"),
      dataIndex: "createdAt",
      key: "createdAt",
      width: 180,
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
      title: t("requests.table.columns.dueDate"),
      dataIndex: "dueDate",
      key: "dueDate",
      width: 160,
      render: (value?: string | null) => {
        if (!value) {
          return <Text type="secondary">{t("requests.details.value.notSet")}</Text>;
        }
        const date = new Date(value);
        return (
          <span>
            {date.toLocaleDateString()} {date.toLocaleTimeString()}
          </span>
        );
      },
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
