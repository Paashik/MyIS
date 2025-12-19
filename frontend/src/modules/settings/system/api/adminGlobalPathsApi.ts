import { t } from "../../../../core/i18n/t";

export interface GlobalPathsSettingsDto {
  projectsRoot: string;
  documentsRoot: string;
  databasesRoot: string;
}

export interface GlobalPathCheckDto {
  isSet: boolean;
  exists: boolean;
  canWrite: boolean;
  error?: string | null;
}

export interface GlobalPathsSettingsResponse {
  settings: GlobalPathsSettingsDto;
  projectsRoot: GlobalPathCheckDto;
  documentsRoot: GlobalPathCheckDto;
  databasesRoot: GlobalPathCheckDto;
}

export interface UpdateGlobalPathsSettingsRequest extends GlobalPathsSettingsDto {
  createDirectories: boolean;
}

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

export async function getGlobalPathsSettings(): Promise<GlobalPathsSettingsResponse> {
  return httpRequest<GlobalPathsSettingsResponse>("/api/admin/settings/global-paths", {
    method: "GET",
  });
}

export async function updateGlobalPathsSettings(
  request: UpdateGlobalPathsSettingsRequest
): Promise<GlobalPathsSettingsResponse> {
  return httpRequest<GlobalPathsSettingsResponse>("/api/admin/settings/global-paths", {
    method: "POST",
    body: JSON.stringify(request),
  });
}

