import type { OrgUnitDetailsDto } from "./types";
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

  return (await response.json()) as TResponse;
}

export async function getOrgUnit(id: string): Promise<OrgUnitDetailsDto> {
  return httpRequest<OrgUnitDetailsDto>(`/api/org-units/${encodeURIComponent(id)}`, {
    method: "GET",
  });
}
