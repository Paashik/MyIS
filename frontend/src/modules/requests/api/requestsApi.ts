import {
  AddRequestCommentPayload,
  CreateRequestPayload,
  GetRequestsParams,
  PagedResultDto,
  RequestCommentDto,
  RequestDto,
  RequestHistoryItemDto,
  RequestListItemDto,
  RequestCounterpartyLookupDto,
  RequestOrgUnitLookupDto,
  RequestBasisCustomerOrderLookupDto,
  RequestBasisIncomingRequestLookupDto,
  RequestStatusDto,
  RequestTypeDto,
  RequestWorkflowTransitionDto,
  UpdateRequestPayload,
} from "./types";

import { t } from "../../../core/i18n/t";

/**
 * Р‘Р°Р·РѕРІС‹Р№ helper РґР»СЏ HTTP-Р·Р°РїСЂРѕСЃРѕРІ Рє backend API.
 * Р“Р°СЂР°РЅС‚РёСЂСѓРµС‚:
 * - credentials: "include" РґР»СЏ cookie-auth;
 * - СЂР°Р·Р±РѕСЂ JSON-РѕС‚РІРµС‚Р°;
 * - РіРµРЅРµСЂР°С†РёСЋ РѕСЃРјС‹СЃР»РµРЅРЅРѕР№ Error РїСЂРё РЅРµСѓСЃРїРµС€РЅРѕРј СЃС‚Р°С‚СѓСЃРµ.
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
  if (params.direction) {
    searchParams.set("direction", params.direction);
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
 * РџРѕР»СѓС‡РёС‚СЊ РїРѕСЃС‚СЂР°РЅРёС‡РЅС‹Р№ СЃРїРёСЃРѕРє Р·Р°СЏРІРѕРє.
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
 * РџРѕР»СѓС‡РёС‚СЊ РїРѕР»РЅС‹Рµ РґР°РЅРЅС‹Рµ РїРѕ Р·Р°СЏРІРєРµ.
 * GET /api/requests/{id}
 */
export async function getRequest(id: string): Promise<RequestDto> {
  return httpRequest<RequestDto>(`/api/requests/${encodeURIComponent(id)}`, {
    method: "GET",
  });
}

/**
 * РЎРѕР·РґР°С‚СЊ РЅРѕРІСѓСЋ Р·Р°СЏРІРєСѓ.
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
 * РћР±РЅРѕРІРёС‚СЊ СЃСѓС‰РµСЃС‚РІСѓСЋС‰СѓСЋ Р·Р°СЏРІРєСѓ.
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
 * РџРѕР»СѓС‡РёС‚СЊ РёСЃС‚РѕСЂРёСЋ РёР·РјРµРЅРµРЅРёР№ Р·Р°СЏРІРєРё.
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
 * РџРѕР»СѓС‡РёС‚СЊ РєРѕРјРјРµРЅС‚Р°СЂРёРё РїРѕ Р·Р°СЏРІРєРµ.
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
 * Р”РѕР±Р°РІРёС‚СЊ РєРѕРјРјРµРЅС‚Р°СЂРёР№ Рє Р·Р°СЏРІРєРµ.
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
 * РџРѕР»СѓС‡РёС‚СЊ СЃРїРёСЃРѕРє С‚РёРїРѕРІ Р·Р°СЏРІРѕРє.
 * GET /api/request-types
 */
export async function getRequestTypes(): Promise<RequestTypeDto[]> {
  return httpRequest<RequestTypeDto[]>("/api/request-types", {
    method: "GET",
  });
}

export async function getRequestCounterparties(
  q?: string,
  prioritizeByOrders?: boolean
): Promise<RequestCounterpartyLookupDto[]> {
  const params = new URLSearchParams();
  if (q) params.set("q", q);
  if (prioritizeByOrders) params.set("prioritizeByOrders", "true");
  params.set("take", "50");
  const query = params.toString();

  return httpRequest<RequestCounterpartyLookupDto[]>(
    `/api/requests/references/counterparties${query ? `?${query}` : ""}`,
    { method: "GET" }
  );
}

export async function getRequestOrgUnits(q?: string): Promise<RequestOrgUnitLookupDto[]> {
  const params = new URLSearchParams();
  if (q) params.set("q", q);
  const query = params.toString();

  return httpRequest<RequestOrgUnitLookupDto[]>(
    `/api/requests/references/org-units${query ? `?${query}` : ""}`,
    { method: "GET" }
  );
}

export async function getIncomingRequests(
  q?: string
): Promise<RequestBasisIncomingRequestLookupDto[]> {
  const params = new URLSearchParams();
  if (q) params.set("q", q);
  params.set("take", "50");
  const query = params.toString();

  return httpRequest<RequestBasisIncomingRequestLookupDto[]>(
    `/api/requests/references/incoming-requests${query ? `?${query}` : ""}`,
    { method: "GET" }
  );
}

export async function getCustomerOrders(
  q?: string
): Promise<RequestBasisCustomerOrderLookupDto[]> {
  const params = new URLSearchParams();
  if (q) params.set("q", q);
  params.set("take", "50");
  const query = params.toString();

  return httpRequest<RequestBasisCustomerOrderLookupDto[]>(
    `/api/requests/references/customer-orders${query ? `?${query}` : ""}`,
    { method: "GET" }
  );
}

/**
 * РџРѕР»СѓС‡РёС‚СЊ СЃРїРёСЃРѕРє СЃС‚Р°С‚СѓСЃРѕРІ Р·Р°СЏРІРѕРє.
 * GET /api/request-statuses
 */
export async function getRequestStatuses(): Promise<RequestStatusDto[]> {
  return httpRequest<RequestStatusDto[]>("/api/request-statuses", {
    method: "GET",
  });
}

/**
 * РџРѕР»СѓС‡РёС‚СЊ РїРµСЂРµС…РѕРґС‹ workflow РґР»СЏ Р·Р°СЏРІРѕРє.
 * GET /api/request-workflow/transitions
 */
export async function getRequestWorkflowTransitions(
  requestTypeId?: string
): Promise<RequestWorkflowTransitionDto[]> {
  const params = new URLSearchParams();
  if (requestTypeId) params.set("typeId", requestTypeId);
  const query = params.toString();

  return httpRequest<RequestWorkflowTransitionDto[]>(
    `/api/request-workflow/transitions${query ? `?${query}` : ""}`,
    { method: "GET" }
  );
}

/**
 * РЈРґР°Р»РёС‚СЊ Р·Р°СЏРІРєСѓ.
 * DELETE /api/requests/{id}
 */
export async function deleteRequest(id: string): Promise<void> {
  return httpRequest<void>(`/api/requests/${encodeURIComponent(id)}`, {
    method: "DELETE",
  });
}


