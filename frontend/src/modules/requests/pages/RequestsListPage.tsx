import React, { useEffect, useState } from "react";
import { Button, Alert, Space, Typography } from "antd";
import { useNavigate } from "react-router-dom";
import { RequestListTable, RequestListTableFilters } from "../components/RequestListTable";
import {
  RequestListItemDto,
  RequestStatusDto,
  RequestTypeDto,
} from "../api/types";
import {
  getRequestStatuses,
  getRequestTypes,
  getRequests,
} from "../api/requestsApi";
import { useCan } from "../../../core/auth/permissions";
import { t } from "../../../core/i18n/t";

const { Title } = Typography;

const DEFAULT_PAGE_SIZE = 20;

type LoadState =
  | { kind: "idle" }
  | { kind: "loading" }
  | { kind: "loaded" }
  | { kind: "error"; message: string };

export const RequestsListPage: React.FC = () => {
  const navigate = useNavigate();
  const canCreate = useCan("Requests.Create");

  const [items, setItems] = useState<RequestListItemDto[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(DEFAULT_PAGE_SIZE);

  const [requestTypes, setRequestTypes] = useState<RequestTypeDto[]>([]);
  const [requestStatuses, setRequestStatuses] = useState<RequestStatusDto[]>([]);

  const [filters, setFilters] = useState<RequestListTableFilters>({
    requestTypeId: undefined,
    requestStatusId: undefined,
    onlyMine: false,
  });

  const [state, setState] = useState<LoadState>({ kind: "idle" });

  // Загрузка справочников типов и статусов один раз
  useEffect(() => {
    let cancelled = false;

    const loadLookups = async () => {
      try {
        const [types, statuses] = await Promise.all([
          getRequestTypes(),
          getRequestStatuses(),
        ]);
        if (cancelled) return;
        setRequestTypes(types);
        setRequestStatuses(statuses);
      } catch {
        // Справочники не критичны для работы страницы: ошибки не блокируют UI,
        // пользователю всё равно покажется таблица, просто без фильтров.
      }
    };

    void loadLookups();

    return () => {
      cancelled = true;
    };
  }, []);

  // Загрузка списка заявок при изменении фильтров/страницы
  useEffect(() => {
    let cancelled = false;

    const load = async () => {
      setState({ kind: "loading" });
      try {
        const result = await getRequests({
          requestTypeId: filters.requestTypeId,
          requestStatusId: filters.requestStatusId,
          onlyMine: filters.onlyMine,
          pageNumber,
          pageSize,
        });

        if (cancelled) return;

        setItems(result.items);
        setTotalCount(result.totalCount);
        setState({ kind: "loaded" });
      } catch (error) {
        if (cancelled) return;

        const message =
          error instanceof Error
            ? error.message
            : t("requests.list.error.unknown");
        setState({ kind: "error", message });
      }
    };

    void load();

    return () => {
      cancelled = true;
    };
  }, [filters.requestTypeId, filters.requestStatusId, filters.onlyMine, pageNumber, pageSize]);

  const handleFiltersChange = (next: RequestListTableFilters) => {
    setFilters(next);
  };

  const handlePageChange = (page: number, size: number) => {
    setPageNumber(page);
    setPageSize(size);
  };

  const handleRowClick = (item: RequestListItemDto) => {
    navigate(`/requests/${encodeURIComponent(item.id)}`);
  };

  const handleCreateClick = () => {
    navigate("/requests/new");
  };

  const showError = state.kind === "error";

  return (
    <div data-testid="requests-list-page">
      <Space
        style={{ marginBottom: 16, display: "flex", justifyContent: "space-between" }}
        align="center"
      >
        <Title level={2} style={{ margin: 0 }}>
          {t("requests.list.title")}
        </Title>

        {canCreate && (
          <Button
            data-testid="requests-create-button"
            type="primary"
            onClick={handleCreateClick}
          >
            {t("requests.list.create")}
          </Button>
        )}
      </Space>

      {showError && state.kind === "error" && (
        <Alert
          data-testid="requests-list-error-alert"
          type="error"
          message={t("requests.list.error.title")}
          description={state.message}
          showIcon
          style={{ marginBottom: 16 }}
        />
      )}

      <RequestListTable
        loading={state.kind === "loading" && items.length === 0}
        items={items}
        totalCount={totalCount}
        pageNumber={pageNumber}
        pageSize={pageSize}
        requestTypes={requestTypes}
        requestStatuses={requestStatuses}
        filters={filters}
        onFiltersChange={handleFiltersChange}
        onPageChange={handlePageChange}
        onRowClick={handleRowClick}
      />
    </div>
  );
};
