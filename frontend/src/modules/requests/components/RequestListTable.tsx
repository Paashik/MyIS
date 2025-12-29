import React, { useEffect, useRef, useState } from "react";
import { Checkbox, Pagination, Table, Select, Segmented, Space } from "antd";
import {
  CheckCircleOutlined,
  CheckOutlined,
  ClockCircleOutlined,
  CloseCircleOutlined,
  EditOutlined,
  SendOutlined,
  StopOutlined,
  ToolOutlined,
} from "@ant-design/icons";

import {
  RequestListItemDto,
  RequestBasisType,
  RequestStatusDto,
  RequestTypeDto,
  RequestWorkflowTransitionDto,
} from "../api/types";
import { RequestStatusBadge } from "./RequestStatusBadge";
import { t } from "../../../core/i18n/t";
import "./RequestListTable.css";
import { getRequestStatusLabel } from "../utils/requestWorkflowLocalization";

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
  workflowTransitions: RequestWorkflowTransitionDto[];
  selectedRowKeys: string[];
  onSelectionChange: (nextKeys: string[]) => void;
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
  workflowTransitions,
  selectedRowKeys,
  onSelectionChange,
  filters,
  onFiltersChange,
  onPageChange,
  onRowClick,
}) => {
  const lastSelectedIndexRef = useRef<number | null>(null);
  const tableWrapRef = useRef<HTMLDivElement | null>(null);
  const [listView, setListView] = useState(false);

  useEffect(() => {
    const handleDocumentClick = (event: MouseEvent) => {
      const target = event.target as HTMLElement | null;
      if (!target) return;
      const wrap = tableWrapRef.current;
      if (!wrap) return;
      if (wrap.contains(target)) return;
      if (selectedRowKeys.length === 0) return;
      lastSelectedIndexRef.current = null;
      onSelectionChange([]);
    };

    document.addEventListener("click", handleDocumentClick);
    return () => {
      document.removeEventListener("click", handleDocumentClick);
    };
  }, [onSelectionChange, selectedRowKeys.length]);

  useEffect(() => {
    const stored = localStorage.getItem("requestsListView");
    if (stored === "list") {
      setListView(true);
    }
  }, []);

  useEffect(() => {
    localStorage.setItem("requestsListView", listView ? "list" : "cards");
  }, [listView]);

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

  const handleSelectRecord = (record: RequestListItemDto, event: React.MouseEvent) => {
    event.stopPropagation();
    const index = items.findIndex((item) => item.id === record.id);
    if (index === -1) return;

    const isToggle = event.ctrlKey || event.metaKey;
    const isRange = event.shiftKey && lastSelectedIndexRef.current !== null;
    let nextSelection: string[] = [];

    if (isRange) {
      const start = Math.min(lastSelectedIndexRef.current ?? index, index);
      const end = Math.max(lastSelectedIndexRef.current ?? index, index);
      const rangeKeys = items.slice(start, end + 1).map((item) => item.id);
      nextSelection = isToggle
        ? Array.from(new Set([...selectedRowKeys, ...rangeKeys]))
        : rangeKeys;
    } else if (isToggle) {
      nextSelection = selectedRowKeys.includes(record.id)
        ? selectedRowKeys.filter((key) => key !== record.id)
        : [...selectedRowKeys, record.id];
    } else {
      nextSelection = [record.id];
    }

    lastSelectedIndexRef.current = index;
    onSelectionChange(nextSelection);
  };

  const statusNameByCode = new Map(
    requestStatuses.map((status) => [status.code, status.name])
  );

  const workflowTransitionsByType = new Map<string, RequestWorkflowTransitionDto[]>();
  for (const transition of workflowTransitions) {
    if (!transition.isEnabled) continue;
    const list = workflowTransitionsByType.get(transition.requestTypeId);
    if (list) list.push(transition);
    else workflowTransitionsByType.set(transition.requestTypeId, [transition]);
  }

  const chainActionPriority = [
    "Submit",
    "StartReview",
    "Approve",
    "StartWork",
    "Complete",
    "Close",
  ];

  const buildChainOrder = (transitions: RequestWorkflowTransitionDto[]) => {
    if (transitions.length === 0) return [];

    const chainTransitions = transitions.filter((t) =>
      chainActionPriority.includes(t.actionCode)
    );
    const baseTransitions = chainTransitions.length > 0 ? chainTransitions : transitions;

    const allCodes = new Set<string>();
    const incoming = new Map<string, number>();
    const outgoing = new Map<string, RequestWorkflowTransitionDto[]>();

    for (const t of baseTransitions) {
      allCodes.add(t.fromStatusCode);
      allCodes.add(t.toStatusCode);
      incoming.set(t.toStatusCode, (incoming.get(t.toStatusCode) ?? 0) + 1);
      outgoing.set(t.fromStatusCode, [...(outgoing.get(t.fromStatusCode) ?? []), t]);
    }

    const starts = Array.from(allCodes).filter((code) => !incoming.has(code));
    starts.sort((a, b) =>
      (statusNameByCode.get(a) ?? a).localeCompare(statusNameByCode.get(b) ?? b)
    );

    const sortedOutgoing = new Map<string, RequestWorkflowTransitionDto[]>();
    for (const [fromCode, list] of outgoing) {
      const byTo = new Map<string, RequestWorkflowTransitionDto>();
      for (const t of list) {
        const existing = byTo.get(t.toStatusCode);
        if (!existing) {
          byTo.set(t.toStatusCode, t);
          continue;
        }
        const existingIdx = chainActionPriority.indexOf(existing.actionCode);
        const nextIdx = chainActionPriority.indexOf(t.actionCode);
        const existingScore = existingIdx === -1 ? 999 : existingIdx;
        const nextScore = nextIdx === -1 ? 999 : nextIdx;
        if (nextScore < existingScore) {
          byTo.set(t.toStatusCode, t);
        }
      }

      const sorted = Array.from(byTo.values()).sort((a, b) => {
        const aIdx = chainActionPriority.indexOf(a.actionCode);
        const bIdx = chainActionPriority.indexOf(b.actionCode);
        if (aIdx !== bIdx) return (aIdx === -1 ? 999 : aIdx) - (bIdx === -1 ? 999 : bIdx);
        const aName = statusNameByCode.get(a.toStatusCode) ?? a.toStatusCode;
        const bName = statusNameByCode.get(b.toStatusCode) ?? b.toStatusCode;
        return aName.localeCompare(bName);
      });
      sortedOutgoing.set(fromCode, sorted);
    }

    const memo = new Map<string, string[]>();
    const visiting = new Set<string>();

    const dfs = (node: string): string[] => {
      const cached = memo.get(node);
      if (cached) return cached;
      if (visiting.has(node)) return [node];

      visiting.add(node);
      const neighbors = sortedOutgoing.get(node) ?? [];
      let best: string[] = [node];

      for (const edge of neighbors) {
        const candidate = [node, ...dfs(edge.toStatusCode)];
        if (candidate.length > best.length) {
          best = candidate;
        }
      }

      visiting.delete(node);
      memo.set(node, best);
      return best;
    };

    const startCandidates =
      starts.length > 0
        ? starts
        : Array.from(allCodes).sort((a, b) =>
            (statusNameByCode.get(a) ?? a).localeCompare(statusNameByCode.get(b) ?? b)
          );

    let longest: string[] = [];
    for (const start of startCandidates) {
      const path = dfs(start);
      if (path.length > longest.length) {
        longest = path;
      }
    }

    return longest;
  };

  const buildWorkflowPath = (
    transitions: RequestWorkflowTransitionDto[],
    currentStatusCode: string
  ) => {
    const chainOrder = buildChainOrder(transitions);
    if (chainOrder.includes(currentStatusCode)) {
      return chainOrder;
    }

    if (transitions.length === 0) {
      return [];
    }

    const outgoing = new Map<string, RequestWorkflowTransitionDto[]>();
    const incoming = new Map<string, number>();
    const allCodes = new Set<string>();

    for (const t of transitions) {
      allCodes.add(t.fromStatusCode);
      allCodes.add(t.toStatusCode);
      const list = outgoing.get(t.fromStatusCode) ?? [];
      list.push(t);
      outgoing.set(t.fromStatusCode, list);
      incoming.set(t.toStatusCode, (incoming.get(t.toStatusCode) ?? 0) + 1);
    }

    const sortedOutgoing = new Map<string, string[]>();
    for (const [fromCode, list] of outgoing) {
      const unique = Array.from(
        new Map(list.map((t) => [t.toStatusCode, t])).values()
      );
      unique.sort((a, b) => {
        const aIdx = chainActionPriority.indexOf(a.actionCode);
        const bIdx = chainActionPriority.indexOf(b.actionCode);
        if (aIdx !== bIdx) return (aIdx === -1 ? 999 : aIdx) - (bIdx === -1 ? 999 : bIdx);
        const aName = statusNameByCode.get(a.toStatusCode) ?? a.toStatusCode;
        const bName = statusNameByCode.get(b.toStatusCode) ?? b.toStatusCode;
        return aName.localeCompare(bName);
      });
      sortedOutgoing.set(
        fromCode,
        unique.map((t) => t.toStatusCode)
      );
    }

    const chainStart = chainOrder[0];
    const startCandidates =
      chainStart && allCodes.has(chainStart)
        ? [chainStart]
        : Array.from(allCodes).filter((code) => !incoming.has(code));

    const bfs = (start: string) => {
      const queue: string[] = [start];
      const visited = new Set<string>([start]);
      const parent = new Map<string, string>();

      while (queue.length > 0) {
        const current = queue.shift();
        if (!current) break;
        if (current === currentStatusCode) {
          const path: string[] = [];
          let cursor: string | undefined = current;
          while (cursor) {
            path.push(cursor);
            cursor = parent.get(cursor);
          }
          return path.reverse();
        }
        const neighbors = sortedOutgoing.get(current) ?? [];
        for (const next of neighbors) {
          if (visited.has(next)) continue;
          visited.add(next);
          parent.set(next, current);
          queue.push(next);
        }
      }

      return null;
    };

    for (const start of startCandidates) {
      const path = bfs(start);
      if (path) return path;
    }

    return [];
  };

  const formatDateTime = (value?: string | null) => {
    if (!value) return null;
    const date = new Date(value);
    return `${date.toLocaleDateString()} ${date.toLocaleTimeString()}`;
  };

  const formatDate = (value?: string | null) => {
    if (!value) return null;
    const date = new Date(value);
    return date.toLocaleDateString();
  };

  const getBasisTypeLabel = (type?: RequestBasisType | null) => {
    switch (type) {
      case "IncomingRequest":
        return t("requests.basis.type.incoming");
      case "CustomerOrder":
        return t("requests.basis.type.customerOrder");
      case "ProductionOrder":
        return t("requests.basis.type.productionOrder");
      case "Other":
        return t("requests.basis.type.other");
      default:
        return null;
    }
  };

  const getStatusIcon = (code: string) => {
    switch (code) {
      case "Draft":
        return <EditOutlined />;
      case "Submitted":
        return <SendOutlined />;
      case "InReview":
        return <ClockCircleOutlined />;
      case "Approved":
        return <CheckCircleOutlined />;
      case "Rejected":
        return <CloseCircleOutlined />;
      case "InWork":
        return <ToolOutlined />;
      case "Done":
        return <CheckOutlined />;
      case "Closed":
        return <StopOutlined />;
      default:
        return null;
    }
  };

  const renderRequestRow = (record: RequestListItemDto) => {
    const transitions =
      workflowTransitionsByType.get(record.requestTypeId) ?? [];
    const workflowToRender = buildWorkflowPath(
      transitions,
      record.requestStatusCode
    );
    if (
      workflowToRender.length === 0 ||
      !workflowToRender.includes(record.requestStatusCode)
    ) {
      workflowToRender.splice(0, workflowToRender.length, record.requestStatusCode);
    }

    const currentIndex = workflowToRender.indexOf(record.requestStatusCode);
    const basisInfo = (() => {
      const typeLabel = getBasisTypeLabel(record.basisType);
      const description = record.basisDescription ?? "";
      const trimmedDescription = description.trim();

      if (record.basisType === "CustomerOrder" && trimmedDescription) {
        const splitToken = " · ";
        const orderNumber = trimmedDescription.includes(splitToken)
          ? trimmedDescription.split(splitToken, 2)[0]
          : trimmedDescription;

        return {
          basisValue: `${typeLabel ?? ""} ${orderNumber}`.trim(),
          clientValue: null,
        };
      }

      if (typeLabel && trimmedDescription) {
        return {
          basisValue: `${typeLabel} ${trimmedDescription}`.trim(),
          clientValue: null,
        };
      }

      return {
        basisValue: typeLabel ?? (trimmedDescription || null),
        clientValue: null,
      };
    })();

    const metaItems = [
      {
        label: t("requests.table.columns.initiator"),
        value: record.managerFullName || t("requests.table.value.unknownInitiator"),
      },
      {
        label: t("requests.table.columns.createdAt"),
        value: formatDateTime(record.createdAt),
      },
      {
        label: t("requests.table.columns.dueDate"),
        value: formatDate(record.dueDate),
      },
      {
        label: t("requests.table.columns.target"),
        value: record.targetEntityName,
      },
      {
        label: t("requests.table.columns.basisCombined"),
        value: basisInfo.basisValue,
      },
      {
        label: t("requests.table.columns.client"),
        value: basisInfo.clientValue,
      },
    ].filter((item) => item.value && !item.hidden);

    return (
      <div className="request-row">
        <div className="request-row__top">
          <div className="request-row__title">
            <span className="request-row__title-main">{record.title}</span>
            {record.requestTypeName && (
              <span className="request-row__title-type">· {record.requestTypeName}</span>
            )}
          </div>
          <RequestStatusBadge
            statusCode={record.requestStatusCode}
            statusName={getRequestStatusLabel(
              record.requestStatusCode,
              record.requestStatusName
            )}
          />
        </div>

        {workflowToRender.length > 0 && (
          <div className="request-row__line request-row__line--status">
            <span className="request-row__label">
              {t("requests.workflow.statuses.label")}
            </span>
            <div className="request-row__status-list">
              {workflowToRender.map((item, index) => {
                const code = item;
                const label = getRequestStatusLabel(
                  code,
                  code === record.requestStatusCode
                    ? record.requestStatusName
                    : statusNameByCode.get(code)
                );
                const isCurrent = code === record.requestStatusCode;
                const isPassed = currentIndex >= 0 && index < currentIndex;
                const state = isCurrent
                  ? "current"
                  : isPassed
                  ? "passed"
                  : "upcoming";
                const icon = getStatusIcon(code);

                return (
                  <span
                    key={`${record.id}-${code}-${index}`}
                    className={`request-row__status request-row__status--${state} request-row__status--code-${code.toLowerCase()}`}
                  >
                    {icon && <span className="request-row__status-icon">{icon}</span>}
                    {label}
                  </span>
                );
              })}
            </div>
          </div>
        )}

        {record.description && (
          <div className="request-row__line request-row__line--detail">
            <span className="request-row__label">
              {t("requests.table.columns.basis")}
            </span>
            <span className="request-row__value">{record.description}</span>
          </div>
        )}

        {metaItems.length > 0 && (
          <div className="request-row__meta">
            {metaItems.map((item) => (
              <span key={item.label} className="request-row__meta-item">
                <span className="request-row__meta-label">{item.label}:</span>{" "}
                <span className="request-row__meta-value">{item.value}</span>
              </span>
            ))}
          </div>
        )}
      </div>
    );
  };

  const columns: any[] = [
    {
      title: t("requests.table.columns.title"),
      dataIndex: "title",
      key: "title",
      render: (_: any, record: RequestListItemDto) => renderRequestRow(record),
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

        <Space wrap>
          <Segmented
            data-testid="requests-filter-only-mine"
            value={filters.onlyMine ? "mine" : "all"}
            onChange={handleOnlyMineChange}
            options={[
              { label: t("requests.table.filters.onlyMine.all"), value: "all" },
              { label: t("requests.table.filters.onlyMine.mine"), value: "mine" },
            ]}
          />
          <Checkbox
            checked={listView}
            onChange={(event) => setListView(event.target.checked)}
          >
            {t("requests.list.view.list")}
          </Checkbox>
        </Space>
      </Space>

      <div
        ref={tableWrapRef}
        onClick={(event) => {
          const target = event.target as HTMLElement | null;
          if (!target) return;
          if (listView) {
            const hasRow = target.closest(".ant-table-row");
            if (hasRow) return;
            const inBody = target.closest(".ant-table-tbody");
            if (!inBody) return;
          } else {
            const hasCard = target.closest(".requests-card");
            if (hasCard) return;
          }
          lastSelectedIndexRef.current = null;
          onSelectionChange([]);
        }}
      >
        {listView ? (
          <Table
            data-testid="requests-table"
            rowKey={(record: RequestListItemDto) => record.id}
            loading={loading}
            columns={columns}
            dataSource={items}
            pagination={pagination}
            showHeader={false}
            className="requests-table"
            rowClassName={(record) =>
              selectedRowKeys.includes(record.id) ? "requests-table__row--selected" : ""
            }
            onRow={(record: RequestListItemDto) => ({
              onClick: (event) => handleSelectRecord(record, event),
              onDoubleClick: () => onRowClick(record),
              style: { cursor: "pointer" },
            })}
            size="middle"
          />
        ) : (
          <>
            <div className="requests-cards">
              {items.map((record) => {
                const isSelected = selectedRowKeys.includes(record.id);
                return (
                  <button
                    key={record.id}
                    type="button"
                    className={`requests-card${isSelected ? " requests-card--selected" : ""}`}
                    onClick={(event) => handleSelectRecord(record, event)}
                    onDoubleClick={() => onRowClick(record)}
                  >
                    {renderRequestRow(record)}
                  </button>
                );
              })}
            </div>
            <div className="requests-cards__pagination">
              <Pagination
                current={pageNumber}
                pageSize={pageSize}
                total={totalCount}
                showSizeChanger
                onChange={(page, size) => onPageChange(page, size)}
              />
            </div>
          </>
        )}
      </div>
    </div>
  );
};
