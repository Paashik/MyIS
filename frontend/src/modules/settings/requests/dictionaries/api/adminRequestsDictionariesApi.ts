import { t } from "../../../../../core/i18n/t";
import {
  AdminRequestStatusDto,
  AdminRequestTypeDto,
  AdminRequestWorkflowTransitionDto,
  CreateAdminRequestStatusPayload,
  CreateAdminRequestTypePayload,
  ReplaceWorkflowTransitionsPayload,
  UpdateAdminRequestStatusPayload,
  UpdateAdminRequestTypePayload,
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
    if (response.status === 403) {
      throw new HttpError(403, response.statusText, t("settings.forbidden"));
    }

    const text = await response.text().catch(() => "");
    const message = text || `${response.status} ${response.statusText}`;
    throw new HttpError(response.status, response.statusText, message);
  }

  if (response.status === 204) {
    return undefined as unknown as TResponse;
  }

  const contentType = response.headers.get("content-type") ?? "";
  const isJson = contentType.toLowerCase().includes("application/json");
  if (!isJson) {
    const text = await response.text().catch(() => "");
    throw new Error(text || "Expected JSON response");
  }

  return (await response.json()) as TResponse;
}

export async function getAdminRequestTypes(): Promise<AdminRequestTypeDto[]> {
  return httpRequest<AdminRequestTypeDto[]>("/api/admin/requests/types", {
    method: "GET",
  });
}

export async function createAdminRequestType(
  payload: CreateAdminRequestTypePayload
): Promise<AdminRequestTypeDto> {
  return httpRequest<AdminRequestTypeDto>("/api/admin/requests/types", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export async function updateAdminRequestType(
  id: string,
  payload: UpdateAdminRequestTypePayload
): Promise<AdminRequestTypeDto> {
  return httpRequest<AdminRequestTypeDto>(
    `/api/admin/requests/types/${encodeURIComponent(id)}`,
    {
      method: "PUT",
      body: JSON.stringify(payload),
    }
  );
}

export async function archiveAdminRequestType(id: string): Promise<void> {
  return httpRequest<void>(
    `/api/admin/requests/types/${encodeURIComponent(id)}/archive`,
    {
      method: "POST",
    }
  );
}

export async function getAdminRequestStatuses(): Promise<AdminRequestStatusDto[]> {
  return httpRequest<AdminRequestStatusDto[]>("/api/admin/requests/statuses", {
    method: "GET",
  });
}

export async function createAdminRequestStatus(
  payload: CreateAdminRequestStatusPayload
): Promise<AdminRequestStatusDto> {
  return httpRequest<AdminRequestStatusDto>("/api/admin/requests/statuses", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export async function updateAdminRequestStatus(
  id: string,
  payload: UpdateAdminRequestStatusPayload
): Promise<AdminRequestStatusDto> {
  return httpRequest<AdminRequestStatusDto>(
    `/api/admin/requests/statuses/${encodeURIComponent(id)}`,
    {
      method: "PUT",
      body: JSON.stringify(payload),
    }
  );
}

export async function archiveAdminRequestStatus(id: string): Promise<void> {
  return httpRequest<void>(
    `/api/admin/requests/statuses/${encodeURIComponent(id)}/archive`,
    {
      method: "POST",
    }
  );
}

export async function getAdminWorkflowTransitions(
  typeCode: string
): Promise<AdminRequestWorkflowTransitionDto[]> {
  const query = new URLSearchParams({ typeCode }).toString();
  return httpRequest<AdminRequestWorkflowTransitionDto[]>(
    `/api/admin/requests/workflow/transitions?${query}`,
    {
      method: "GET",
    }
  );
}

export async function replaceAdminWorkflowTransitions(
  payload: ReplaceWorkflowTransitionsPayload
): Promise<void> {
  return httpRequest<void>("/api/admin/requests/workflow/transitions", {
    method: "PUT",
    body: JSON.stringify(payload),
  });
}

