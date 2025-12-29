import { t } from "../../../../../core/i18n/t";
import {
  Component2020ConnectionDto,
  Component2020StatusResponse,
  Component2020SyncRunDto,
  GetComponent2020FsEntriesResponse,
  GetComponent2020MdbFilesResponse,
  GetComponent2020SyncRunsResponse,
  GetComponent2020SyncRunErrorsResponse,
  RunComponent2020SyncRequest,
  RunComponent2020SyncResponse,
  ScheduleComponent2020SyncRequest,
  ScheduleComponent2020SyncResponse,
  Component2020ImportPreviewRequest,
  Component2020ImportPreviewResponse,
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

export async function getComponent2020Status(): Promise<Component2020StatusResponse> {
  return httpRequest<Component2020StatusResponse>("/api/admin/integrations/component2020/status", {
    method: "GET",
  });
}

export async function getComponent2020Connection(): Promise<Component2020ConnectionDto> {
  return httpRequest<Component2020ConnectionDto>(
    "/api/admin/integrations/component2020/connection",
    {
      method: "GET",
    }
  );
}

export async function getComponent2020MdbFiles(): Promise<GetComponent2020MdbFilesResponse> {
  return httpRequest<GetComponent2020MdbFilesResponse>(
    "/api/admin/integrations/component2020/mdb-files",
    {
      method: "GET",
    }
  );
}

export async function getComponent2020FsEntries(
  path?: string
): Promise<GetComponent2020FsEntriesResponse> {
  const query = path ? `?path=${encodeURIComponent(path)}` : "";
  return httpRequest<GetComponent2020FsEntriesResponse>(
    `/api/admin/integrations/component2020/fs${query}`,
    {
      method: "GET",
    }
  );
}

export async function getComponent2020SyncRuns(
  page: number = 1,
  pageSize: number = 20,
  fromDate?: string,
  status?: string
): Promise<GetComponent2020SyncRunsResponse> {
  const params = new URLSearchParams({
    page: page.toString(),
    pageSize: pageSize.toString(),
  });
  if (fromDate) params.append("fromDate", fromDate);
  if (status) params.append("status", status);

  return httpRequest<GetComponent2020SyncRunsResponse>(
    `/api/admin/integrations/component2020/runs?${params.toString()}`,
    {
      method: "GET",
    }
  );
}

export async function testComponent2020Connection(
  connection: Component2020ConnectionDto
): Promise<{ isConnected: boolean }> {
  return httpRequest<{ isConnected: boolean }>("/api/admin/integrations/component2020/test-connection", {
    method: "POST",
    body: JSON.stringify(connection),
  });
}

export async function saveComponent2020Connection(
  connection: Component2020ConnectionDto
): Promise<void> {
  return httpRequest<void>("/api/admin/integrations/component2020/save-connection", {
    method: "POST",
    body: JSON.stringify(connection),
  });
}

export async function runComponent2020Sync(
  request: RunComponent2020SyncRequest
): Promise<RunComponent2020SyncResponse> {
  return httpRequest<RunComponent2020SyncResponse>("/api/admin/integrations/component2020/run", {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export async function getComponent2020SyncRunErrors(
  runId: string
): Promise<GetComponent2020SyncRunErrorsResponse> {
  return httpRequest<GetComponent2020SyncRunErrorsResponse>(
    `/api/admin/integrations/component2020/runs/${encodeURIComponent(runId)}/errors`,
    {
      method: "GET",
    }
  );
}

export async function getComponent2020SyncRunById(runId: string): Promise<any> {
  return httpRequest<Component2020SyncRunDto>(
    `/api/admin/integrations/component2020/runs/${encodeURIComponent(runId)}`,
    {
      method: "GET",
    }
  );
}

export async function scheduleComponent2020Sync(
  request: ScheduleComponent2020SyncRequest
): Promise<ScheduleComponent2020SyncResponse> {
  return httpRequest<ScheduleComponent2020SyncResponse>("/api/admin/integrations/component2020/schedule", {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export async function getComponent2020ImportPreview(
  request: Component2020ImportPreviewRequest
): Promise<Component2020ImportPreviewResponse> {
  return httpRequest<Component2020ImportPreviewResponse>("/api/admin/integrations/component2020/preview", {
    method: "POST",
    body: JSON.stringify(request),
  });
}
