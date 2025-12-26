import React, { useEffect, useMemo, useState } from "react";
import { Alert, Button, Typography } from "antd";
import { useLocation, useNavigate } from "react-router-dom";

import { RequestListTable, RequestListTableFilters } from "../components/RequestListTable";
import {
  RequestListItemDto,
  RequestStatusDto,
  RequestTypeDto,
} from "../api/types";
import {
  CHANGE_REQUEST_TYPE_ID,
  CUSTOMER_DEVELOPMENT_TYPE_ID,
  EXTERNAL_TECH_STAGE_TYPE_ID,
  INTERNAL_PRODUCTION_TYPE_ID,
  SUPPLY_REQUEST_TYPE_ID,
} from "../requestTypeIds";
import {
  getRequestStatuses,
  getRequestTypes,
  getRequests,
} from "../api/requestsApi";
import { useCan } from "../../../core/auth/permissions";
import { t } from "../../../core/i18n/t";
import { CommandBar } from "../../../components/ui/CommandBar";

const { Title } = Typography;

const DEFAULT_PAGE_SIZE = 20;

type LoadState =
  | { kind: "idle" }
  | { kind: "loading" }
  | { kind: "loaded" }
  | { kind: "error"; message: string };

type RequestsDirectionSegment = "incoming" | "outgoing";

export const RequestsListPage: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const canCreate = useCan("Requests.Create");

  const direction: RequestsDirectionSegment = useMemo(() => {
    const sp = new URLSearchParams(location.search);
    const raw = (sp.get("direction") || "").trim().toLowerCase();
    if (raw === "outgoing") return "outgoing";
    if (raw === "incoming") return "incoming";

    const seg = (location.pathname.split("/")[2] || "").toLowerCase();
    return seg === "outgoing" ? "outgoing" : "incoming";
  }, [location.pathname, location.search]);

  const getTypeFromLocation = (): string | undefined => {
    const sp = new URLSearchParams(location.search);
    const raw = (sp.get("type") || "").trim();
    if (!raw || raw === "all") return undefined;
    return raw;
  };

  const getOnlyMineFromLocation = (): boolean => {
    const sp = new URLSearchParams(location.search);
    const raw = (sp.get("onlyMine") || "").trim().toLowerCase();
    return raw === "1" || raw === "true";
  };

  const [items, setItems] = useState<RequestListItemDto[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(DEFAULT_PAGE_SIZE);

  const [requestTypes, setRequestTypes] = useState<RequestTypeDto[]>([]);
  const [requestStatuses, setRequestStatuses] = useState<RequestStatusDto[]>([]);

  const [filters, setFilters] = useState<RequestListTableFilters>({
    requestStatusId: undefined,
    requestTypeId: getTypeFromLocation(),
    onlyMine: getOnlyMineFromLocation(),
  });

  const [state, setState] = useState<LoadState>({ kind: "idle" });

  useEffect(() => {
    setFilters((prev) => ({
      ...prev,
      requestTypeId: getTypeFromLocation(),
      onlyMine: getOnlyMineFromLocation(),
    }));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [location.search]);

  useEffect(() => {
    const sp = new URLSearchParams(location.search);

    const rawDirection = (sp.get("direction") || "").trim().toLowerCase();
    const hasDirection = rawDirection === "incoming" || rawDirection === "outgoing";
    if (!hasDirection) {
      sp.set("direction", direction);
      navigate(`${location.pathname}?${sp.toString()}`, { replace: true });
    }
  }, [direction, location.pathname, location.search, navigate]);

  const requestTypesForDirection = useMemo(() => {
    const expected = direction === "incoming" ? "Incoming" : "Outgoing";
    const filtered = requestTypes.filter((t) => t.direction === expected);

    const order =
      direction === "incoming"
        ? [CUSTOMER_DEVELOPMENT_TYPE_ID, INTERNAL_PRODUCTION_TYPE_ID, CHANGE_REQUEST_TYPE_ID]
        : [SUPPLY_REQUEST_TYPE_ID, EXTERNAL_TECH_STAGE_TYPE_ID, CHANGE_REQUEST_TYPE_ID];

    return filtered.sort((a, b) => {
      const indexA = order.indexOf(a.id);
      const indexB = order.indexOf(b.id);
      if (indexA === -1) return 1;
      if (indexB === -1) return -1;
      return indexA - indexB;
    });
  }, [direction, requestTypes]);

  useEffect(() => {
    if (!filters.requestTypeId) return;

    const exists = requestTypesForDirection.some((t) => t.id === filters.requestTypeId);
    if (exists) return;

    const sp = new URLSearchParams(location.search);
    sp.set("type", "all");
    navigate(`${location.pathname}?${sp.toString()}`, { replace: true });
    setFilters((prev) => ({ ...prev, requestTypeId: undefined }));
  }, [filters.requestTypeId, location.pathname, location.search, navigate, requestTypesForDirection]);

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
        // Ignore lookup errors; page will still show general error from main load.
      }
    };

    void loadLookups();

    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    let cancelled = false;

    const load = async () => {
      setState({ kind: "loading" });
      try {
        const result = await getRequests({
          requestTypeId: filters.requestTypeId,
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
  }, [direction, filters.requestStatusId, filters.onlyMine, filters.requestTypeId, pageNumber, pageSize]);

  const handleFiltersChange = (next: RequestListTableFilters) => {
    setFilters(next);

    const sp = new URLSearchParams(location.search);
    if (next.onlyMine) sp.set("onlyMine", "1");
    else sp.delete("onlyMine");

    sp.set("type", next.requestTypeId ?? "all");
    navigate(`${location.pathname}?${sp.toString()}`, { replace: true });
  };

  const handlePageChange = (page: number, size: number) => {
    setPageNumber(page);
    setPageSize(size);
  };

  const handleRowClick = (item: RequestListItemDto) => {
    const sp = new URLSearchParams();
    sp.set("direction", direction);
    sp.set("type", filters.requestTypeId ?? "all");
    if (filters.onlyMine) {
      sp.set("onlyMine", "1");
    }

    navigate(
      `/requests/${encodeURIComponent(item.id)}?${sp.toString()}`
    );
  };

  const handleCreateClick = () => {
    const typeParam = filters.requestTypeId ?? "all";
    navigate(
      `/requests/new?direction=${encodeURIComponent(direction)}&type=${encodeURIComponent(typeParam)}`
    );
  };

  const showError = state.kind === "error";

  return (
    <div data-testid="requests-list-page">
      <CommandBar
        left={
          <Title level={2} style={{ margin: 0 }}>
            {t("requests.list.title")}
          </Title>
        }
        right={
          canCreate ? (
            <Button
              data-testid="requests-create-button"
              type="primary"
              onClick={handleCreateClick}
            >
              {t("requests.list.create")}
            </Button>
          ) : null
        }
      />

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
        requestStatuses={requestStatuses}
        requestTypes={requestTypesForDirection}
        filters={filters}
        onFiltersChange={handleFiltersChange}
        onPageChange={handlePageChange}
        onRowClick={handleRowClick}
      />
    </div>
  );
};
