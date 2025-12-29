import type {
  OrgUnitDetailsDto,
  OrgUnitListItemDto,
  OrgUnitUpsertRequest,
} from "./types";

import { t } from "../../../core/i18n/t";

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
    const text = await response.text().catch(() => "");
    const message = text || t("common.error.unknownNetwork");
    throw new Error(message);
  }

  if (response.status === 204) {
    return undefined as TResponse;
  }

  return (await response.json()) as TResponse;
}

export async function getAdminOrgUnits(q?: string): Promise<OrgUnitListItemDto[]> {
  const params = new URLSearchParams();
  if (q) params.set("q", q);
  const query = params.toString();
  return httpRequest<OrgUnitListItemDto[]>(
    `/api/admin/org-units${query ? `?${query}` : ""}`,
    { method: "GET" }
  );
}

export async function getAdminOrgUnit(id: string): Promise<OrgUnitDetailsDto> {
  return httpRequest<OrgUnitDetailsDto>(
    `/api/admin/org-units/${encodeURIComponent(id)}`,
    { method: "GET" }
  );
}

export async function createAdminOrgUnit(payload: OrgUnitUpsertRequest): Promise<OrgUnitDetailsDto> {
  return httpRequest<OrgUnitDetailsDto>("/api/admin/org-units", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export async function updateAdminOrgUnit(
  id: string,
  payload: OrgUnitUpsertRequest
): Promise<OrgUnitDetailsDto> {
  return httpRequest<OrgUnitDetailsDto>(`/api/admin/org-units/${encodeURIComponent(id)}`, {
    method: "PUT",
    body: JSON.stringify(payload),
  });
}

export async function deleteAdminOrgUnit(id: string): Promise<void> {
  return httpRequest<void>(`/api/admin/org-units/${encodeURIComponent(id)}`, {
    method: "DELETE",
  });
}
