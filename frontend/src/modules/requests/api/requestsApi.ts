import {
  AddRequestCommentPayload,
  CreateRequestPayload,
  GetRequestsParams,
  PagedResultDto,
  RequestCommentDto,
  RequestDto,
  RequestHistoryItemDto,
  RequestListItemDto,
  RequestStatusDto,
  RequestTypeDto,
  UpdateRequestPayload,
} from "./types";

import { t } from "../../../core/i18n/t";

/**
 * Базовый helper для HTTP-запросов к backend API.
 * Гарантирует:
 * - credentials: "include" для cookie-auth;
 * - разбор JSON-ответа;
 * - генерацию осмысленной Error при неуспешном статусе.
 */
async function httpRequest<TResponse>(
  input: string,
  init?: RequestInit
): Promise<TResponse> {
  const response = await fetch(input, {
    credentials: "include",
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(init?.headers ?? {}),
    },
  });

  if (!response.ok) {
    const text = await response.text().catch(() => "");
    const message =
      text ||
      t("requests.api.error.http", {
        status: response.status,
        statusText: response.statusText,
      });
    throw new Error(message);
  }

  const contentType = response.headers.get("content-type") ?? "";
  const isJson = contentType.toLowerCase().includes("application/json");

  if (!isJson) {
    const text = await response.text().catch(() => "");
    const snippet = text ? text.slice(0, 200) : "";
    throw new Error(
      t("requests.api.error.expectedJson", {
        contentType,
        snippet,
      })
    );
  }

  return (await response.json()) as TResponse;
}

function buildQueryString(params: GetRequestsParams | undefined): string {
  if (!params) {
    return "";
  }

  const searchParams = new URLSearchParams();

  if (params.requestTypeId) {
    searchParams.set("requestTypeId", params.requestTypeId);
  }
  if (params.requestStatusId) {
    searchParams.set("requestStatusId", params.requestStatusId);
  }
  if (typeof params.onlyMine === "boolean") {
    searchParams.set("onlyMine", String(params.onlyMine));
  }
  if (typeof params.pageNumber === "number") {
    searchParams.set("pageNumber", String(params.pageNumber));
  }
  if (typeof params.pageSize === "number") {
    searchParams.set("pageSize", String(params.pageSize));
  }

  const query = searchParams.toString();
  return query ? `?${query}` : "";
}

/**
 * Получить постраничный список заявок.
 * GET /api/requests
 */
export async function getRequests(
  params?: GetRequestsParams
): Promise<PagedResultDto<RequestListItemDto>> {
  const query = buildQueryString(params);
  return httpRequest<PagedResultDto<RequestListItemDto>>(
    `/api/requests${query}`,
    {
      method: "GET",
    }
  );
}

/**
 * Получить полные данные по заявке.
 * GET /api/requests/{id}
 */
export async function getRequest(id: string): Promise<RequestDto> {
  return httpRequest<RequestDto>(`/api/requests/${encodeURIComponent(id)}`, {
    method: "GET",
  });
}

/**
 * Создать новую заявку.
 * POST /api/requests
 */
export async function createRequest(
  payload: CreateRequestPayload
): Promise<RequestDto> {
  return httpRequest<RequestDto>("/api/requests", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

/**
 * Обновить существующую заявку.
 * PUT /api/requests/{id}
 */
export async function updateRequest(
  id: string,
  payload: UpdateRequestPayload
): Promise<RequestDto> {
  return httpRequest<RequestDto>(`/api/requests/${encodeURIComponent(id)}`, {
    method: "PUT",
    body: JSON.stringify(payload),
  });
}

/**
 * Получить историю изменений заявки.
 * GET /api/requests/{id}/history
 */
export async function getRequestHistory(
  id: string
): Promise<RequestHistoryItemDto[]> {
  return httpRequest<RequestHistoryItemDto[]>(
    `/api/requests/${encodeURIComponent(id)}/history`,
    {
      method: "GET",
    }
  );
}

/**
 * Получить комментарии по заявке.
 * GET /api/requests/{id}/comments
 */
export async function getRequestComments(
  id: string
): Promise<RequestCommentDto[]> {
  return httpRequest<RequestCommentDto[]>(
    `/api/requests/${encodeURIComponent(id)}/comments`,
    {
      method: "GET",
    }
  );
}

/**
 * Добавить комментарий к заявке.
 * POST /api/requests/{id}/comments
 */
export async function addRequestComment(
  id: string,
  payload: AddRequestCommentPayload
): Promise<RequestCommentDto> {
  return httpRequest<RequestCommentDto>(
    `/api/requests/${encodeURIComponent(id)}/comments`,
    {
      method: "POST",
      body: JSON.stringify(payload),
    }
  );
}

/**
 * Получить список типов заявок.
 * GET /api/request-types
 */
export async function getRequestTypes(): Promise<RequestTypeDto[]> {
  return httpRequest<RequestTypeDto[]>("/api/request-types", {
    method: "GET",
  });
}

/**
 * Получить список статусов заявок.
 * GET /api/request-statuses
 */
export async function getRequestStatuses(): Promise<RequestStatusDto[]> {
  return httpRequest<RequestStatusDto[]>("/api/request-statuses", {
    method: "GET",
  });
}
