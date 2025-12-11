import React from "react";
import { Table, Select, Segmented, Space, Typography } from "antd";
import {
  RequestListItemDto,
  RequestStatusDto,
  RequestTypeDto,
} from "../api/types";
import { RequestStatusBadge } from "./RequestStatusBadge";

const { Text } = Typography;

export interface RequestListTableFilters {
  requestTypeId?: string;
  requestStatusId?: string;
  onlyMine?: boolean;
}

export interface RequestListTableProps {
  loading: boolean;
  items: RequestListItemDto[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  requestTypes: RequestTypeDto[];
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
  requestTypes,
  requestStatuses,
  filters,
  onFiltersChange,
  onPageChange,
  onRowClick,
}) => {
  const handleTypeChange = (value: string | undefined) => {
    onFiltersChange({
      ...filters,
      requestTypeId: value || undefined,
      // при смене фильтров сбрасываем на первую страницу
    });
    onPageChange(1, pageSize);
  };

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
      title: "ID",
      dataIndex: "id",
      key: "id",
      width: 260,
      render: (value: string) => (
        <Text code ellipsis>{value}</Text>
      ),
    },
    {
      title: "Заголовок",
      dataIndex: "title",
      key: "title",
      ellipsis: true,
    },
    {
      title: "Тип",
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
      title: "Статус",
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
      title: "Инициатор",
      dataIndex: "initiatorFullName",
      key: "initiatorFullName",
      width: 220,
      render: (_: any, record: RequestListItemDto) =>
        record.initiatorFullName || <Text type="secondary">Неизвестно</Text>,
    },
    {
      title: "Создана",
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
      title: "Срок",
      dataIndex: "dueDate",
      key: "dueDate",
      width: 160,
      render: (value?: string | null) => {
        if (!value) {
          return <Text type="secondary">Не задан</Text>;
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
            allowClear
            placeholder="Тип заявки"
            style={{ minWidth: 200 }}
            value={filters.requestTypeId}
            onChange={handleTypeChange}
            options={requestTypes.map((t) => ({
              label: `${t.code} — ${t.name}`,
              value: t.id,
            }))}
          />
          <Select
            allowClear
            placeholder="Статус"
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
          value={filters.onlyMine ? "mine" : "all"}
          onChange={handleOnlyMineChange}
          options={[
            { label: "Все", value: "all" },
            { label: "Мои", value: "mine" },
          ]}
        />
      </Space>

      <Table
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