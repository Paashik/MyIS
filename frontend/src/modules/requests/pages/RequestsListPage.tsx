import React, { useEffect, useMemo, useState } from "react";
import { Alert, Button, Space, Tabs, Typography } from "antd";
import { useLocation, useNavigate } from "react-router-dom";
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
  const location = useLocation();
  const canCreate = useCan("Requests.Create");

  type RequestsDirectionSegment = "incoming" | "outgoing";
  type RequestsTypeTabKey = "all" | string;

  const direction: RequestsDirectionSegment = useMemo(() => {
    const seg = (location.pathname.split("/")[2] || "").toLowerCase();
    return seg === "outgoing" ? "outgoing" : "incoming";
  }, [location.pathname]);

  const getTypeFromLocation = (): RequestsTypeTabKey => {
    const sp = new URLSearchParams(location.search);
    const raw = (sp.get("type") || "").trim();
    return raw ? raw : "all";
  };

  const [activeTypeKey, setActiveTypeKey] = useState<RequestsTypeTabKey>(getTypeFromLocation());

  const [items, setItems] = useState<RequestListItemDto[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(DEFAULT_PAGE_SIZE);

  const [requestTypes, setRequestTypes] = useState<RequestTypeDto[]>([]);
  const [requestStatuses, setRequestStatuses] = useState<RequestStatusDto[]>([]);

  const [filters, setFilters] = useState<RequestListTableFilters>({
    requestStatusId: undefined,
    onlyMine: false,
  });

  const [state, setState] = useState<LoadState>({ kind: "idle" });

  // синхронизация вкладки типа с URL (?type=all|<typeCode>)
  useEffect(() => {
    setActiveTypeKey(getTypeFromLocation());
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [location.search]);

  const requestTypesForDirection = useMemo(() => {
    const expected = direction === "incoming" ? "Incoming" : "Outgoing";
    return requestTypes.filter((t) => t.direction === expected);
  }, [direction, requestTypes]);

  // Нормализация query-параметра type:
  // - отсутствует/пустой/неизвестный/не соответствует направлению => replace на ?type=all
  useEffect(() => {
    const sp = new URLSearchParams(location.search);
    const raw = sp.get("type");
    const trimmed = (raw || "").trim();

    // отсутствует/пустой
    if (!raw || !trimmed) {
      sp.set("type", "all");
      navigate(`${location.pathname}?${sp.toString()}`, { replace: true });
      return;
    }

    // неизвестный / не соответствует направлению — можем проверить только после загрузки справочника
    if (!requestTypes.length) return;
    if (trimmed === "all") return;

    const existsInDirection = requestTypesForDirection.some((t) => t.code === trimmed);
    if (!existsInDirection) {
      sp.set("type", "all");
      navigate(`${location.pathname}?${sp.toString()}`, { replace: true });
    }
  }, [location.pathname, location.search, navigate, requestTypes.length, requestTypesForDirection]);

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

  // activeTypeKey синхронизируем только из URL (source of truth = query param)

  const selectedRequestType = useMemo((): RequestTypeDto | undefined => {
    if (activeTypeKey === "all") return undefined;
    return requestTypesForDirection.find((t) => t.code === activeTypeKey);
  }, [activeTypeKey, requestTypesForDirection]);

  // Загрузка списка заявок при изменении фильтров/страницы
  useEffect(() => {
    let cancelled = false;

    const load = async () => {
      setState({ kind: "loading" });
      try {
        const result = await getRequests({
          requestTypeId: selectedRequestType?.id,
          requestStatusId: filters.requestStatusId,
          direction: direction === "incoming" ? "Incoming" : "Outgoing",
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
  }, [direction, filters.requestStatusId, filters.onlyMine, pageNumber, pageSize, selectedRequestType?.id]);

  const handleTypeTabChange = (key: string) => {
    const nextKey = key || "all";
    setActiveTypeKey(nextKey);
    setPageNumber(1);

    const sp = new URLSearchParams(location.search);
    sp.set("type", nextKey);
    navigate(`${location.pathname}?${sp.toString()}`, { replace: true });
  };

  const handleFiltersChange = (next: RequestListTableFilters) => {
    setFilters(next);
  };

  const handlePageChange = (page: number, size: number) => {
    setPageNumber(page);
    setPageSize(size);
  };

  const handleRowClick = (item: RequestListItemDto) => {
    navigate(
      `/requests/${encodeURIComponent(item.id)}?direction=${encodeURIComponent(direction)}&type=${encodeURIComponent(activeTypeKey)}`
    );
  };

  const handleCreateClick = () => {
    if (activeTypeKey === "all") return;
    navigate(`/requests/${encodeURIComponent(direction)}/new?type=${encodeURIComponent(activeTypeKey)}`);
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
          <Space align="center">
            <Button
              data-testid="requests-create-button"
              type="primary"
              onClick={handleCreateClick}
              disabled={activeTypeKey === "all"}
              title={activeTypeKey === "all" ? t("requests.list.create.selectTypeHint") : undefined}
            >
              {t("requests.list.create")}
            </Button>
            {activeTypeKey === "all" && (
              <Typography.Text type="secondary">
                {t("requests.list.create.selectTypeHint")}
              </Typography.Text>
            )}
          </Space>
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

      <Tabs
        data-testid="requests-list-type-tabs"
        activeKey={activeTypeKey}
        onChange={handleTypeTabChange}
        items={[
          { key: "all", label: t("requests.list.typeTabs.all") },
          ...requestTypesForDirection.map((rt) => ({ key: rt.code, label: rt.name })),
        ]}
        style={{ marginBottom: 12 }}
      />

      <RequestListTable
        loading={state.kind === "loading" && items.length === 0}
        items={items}
        totalCount={totalCount}
        pageNumber={pageNumber}
        pageSize={pageSize}
        requestStatuses={requestStatuses}
        filters={filters}
        onFiltersChange={handleFiltersChange}
        onPageChange={handlePageChange}
        onRowClick={handleRowClick}
      />
    </div>
  );
};
