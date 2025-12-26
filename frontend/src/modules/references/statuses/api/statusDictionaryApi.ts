import { t } from "../../../../core/i18n/t";
import type {
  PagedResponse,
  StatusDto,
  StatusGroupDto,
  StatusGroupUpsertRequest,
  StatusUpsertRequest,
} from "./types";

class HttpError extends Error {
  public readonly status: number;
  public readonly statusText: string;

  constructor(status: number, statusText: string, message: string) {
    super(message);
    this.status = status;
    this.statusText = statusText;
  }
}

async function httpRequest<TResponse>(input: string, init?: RequestInit): Promise<TResponse> {
  const response = await fetch(input, {
    credentials: "include",
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(init?.headers ?? {}),
    },
  });

  if (!response.ok) {
    if (response.status === 403) {
      throw new HttpError(403, response.statusText, t("settings.forbidden"));
    }
    const text = await response.text().catch(() => "");
    throw new HttpError(response.status, response.statusText, text || `${response.status} ${response.statusText}`);
  }

  const contentType = response.headers.get("content-type") ?? "";
  const isJson = contentType.toLowerCase().includes("application/json");
  if (!isJson) {
    const text = await response.text().catch(() => "");
    throw new Error(text || "Expected JSON response");
  }

  return (await response.json()) as TResponse;
}

export async function getStatusGroups(params?: {
  q?: string;
  isActive?: boolean;
  skip?: number;
  take?: number;
}): Promise<PagedResponse<StatusGroupDto>> {
  const qs = new URLSearchParams();
  if (params?.q) qs.set("q", params.q);
  if (typeof params?.isActive === "boolean") qs.set("isActive", String(params.isActive));
  if (typeof params?.skip === "number") qs.set("skip", String(params.skip));
  if (typeof params?.take === "number") qs.set("take", String(params.take));

  return httpRequest<PagedResponse<StatusGroupDto>>(
    `/api/admin/references/mdm/status-groups${qs.toString() ? `?${qs}` : ""}`,
    { method: "GET" }
  );
}

export async function createStatusGroup(request: StatusGroupUpsertRequest): Promise<StatusGroupDto> {
  return httpRequest<StatusGroupDto>("/api/admin/references/mdm/status-groups", {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export async function updateStatusGroup(id: string, request: StatusGroupUpsertRequest): Promise<StatusGroupDto> {
  return httpRequest<StatusGroupDto>(`/api/admin/references/mdm/status-groups/${encodeURIComponent(id)}`, {
    method: "PUT",
    body: JSON.stringify(request),
  });
}

export async function archiveStatusGroup(id: string): Promise<void> {
  await httpRequest<void>(`/api/admin/references/mdm/status-groups/${encodeURIComponent(id)}/archive`, {
    method: "POST",
  });
}

export async function getStatuses(params?: {
  q?: string;
  groupId?: string;
  isActive?: boolean;
  skip?: number;
  take?: number;
}): Promise<PagedResponse<StatusDto>> {
  const qs = new URLSearchParams();
  if (params?.q) qs.set("q", params.q);
  if (params?.groupId) qs.set("groupId", params.groupId);
  if (typeof params?.isActive === "boolean") qs.set("isActive", String(params.isActive));
  if (typeof params?.skip === "number") qs.set("skip", String(params.skip));
  if (typeof params?.take === "number") qs.set("take", String(params.take));

  return httpRequest<PagedResponse<StatusDto>>(
    `/api/admin/references/mdm/statuses${qs.toString() ? `?${qs}` : ""}`,
    { method: "GET" }
  );
}

export async function createStatus(request: StatusUpsertRequest): Promise<StatusDto> {
  return httpRequest<StatusDto>("/api/admin/references/mdm/statuses", {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export async function updateStatus(id: string, request: StatusUpsertRequest): Promise<StatusDto> {
  return httpRequest<StatusDto>(`/api/admin/references/mdm/statuses/${encodeURIComponent(id)}`, {
    method: "PUT",
    body: JSON.stringify(request),
  });
}

export async function archiveStatus(id: string): Promise<void> {
  await httpRequest<void>(`/api/admin/references/mdm/statuses/${encodeURIComponent(id)}/archive`, {
    method: "POST",
  });
}
