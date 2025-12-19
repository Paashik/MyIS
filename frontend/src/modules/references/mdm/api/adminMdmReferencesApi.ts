import { t } from "../../../../core/i18n/t";

export type MdmDictionaryKey =
  | "units"
  | "counterparties"
  | "suppliers" // deprecated alias (old route)
  | "items"
  | "manufacturers"
  | "body-types"
  | "currencies"
  | "technical-parameters"
  | "parameter-sets"
  | "symbols";

export interface PagedResponse<T> {
  total: number;
  items: T[];
}

export interface MdmSimpleReferenceDto {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
  updatedAt: string; // ISO
}

export interface MdmUnitReferenceDto {
  id: string;
  code?: string | null;
  name: string;
  symbol: string;
  isActive: boolean;
  externalSystem?: string | null;
  externalId?: string | null;
  syncedAt?: string | null;
  updatedAt: string; // ISO
}

export interface MdmSupplierReferenceDto extends MdmSimpleReferenceDto {
  fullName?: string | null;
  inn?: string | null;
  kpp?: string | null;
}

export interface MdmCounterpartyRoleDto {
  roleType: string; // numeric string (e.g. "1", "2")
  isActive: boolean;
  updatedAt: string; // ISO
}

export interface MdmCounterpartyExternalLinkDto {
  externalSystem: string;
  externalEntity: string;
  externalId: string;
  sourceType?: string | null;
  syncedAt?: string | null;
  updatedAt: string; // ISO
}

export interface MdmCounterpartyReferenceDto {
  id: string;
  code?: string | null;
  name: string;
  fullName?: string | null;
  inn?: string | null;
  kpp?: string | null;
  email?: string | null;
  phone?: string | null;
  city?: string | null;
  address?: string | null;
  site?: string | null;
  siteLogin?: string | null;
  sitePassword?: string | null;
  note?: string | null;
  isActive: boolean;
  updatedAt: string; // ISO
  roles: MdmCounterpartyRoleDto[];
  externalLinks: MdmCounterpartyExternalLinkDto[];
}

export interface MdmItemReferenceDto extends MdmSimpleReferenceDto {
  itemKind: string;
  isEskd: boolean;
  manufacturerPartNumber?: string | null;
  externalSystem?: string | null;
  externalId?: string | null;
  syncedAt?: string | null;
  unitOfMeasureId: string;
  unitOfMeasureCode?: string | null;
  unitOfMeasureName?: string | null;
  unitOfMeasureSymbol?: string | null;
}

export interface MdmCurrencyReferenceDto {
  id: string;
  code?: string | null;
  name: string;
  symbol?: string | null;
  rate?: number | null;
  isActive: boolean;
  externalSystem?: string | null;
  externalId?: string | null;
  syncedAt?: string | null;
  updatedAt: string; // ISO
}

export interface MdmManufacturerReferenceDto {
  id: string;
  code?: string | null;
  name: string;
  fullName?: string | null;
  site?: string | null;
  note?: string | null;
  isActive: boolean;
  updatedAt: string; // ISO
  externalSystem?: string | null;
  externalId?: string | null;
  syncedAt?: string | null;
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

export async function getMdmDictionaryList<T>(
  dict: MdmDictionaryKey,
  args?: { q?: string; isActive?: boolean; skip?: number; take?: number; roleType?: string | number }
): Promise<PagedResponse<T>> {
  const params = new URLSearchParams();
  if (args?.q) params.set("q", args.q);
  if (typeof args?.isActive === "boolean") params.set("isActive", String(args.isActive));
  if (typeof args?.skip === "number") params.set("skip", String(args.skip));
  if (typeof args?.take === "number") params.set("take", String(args.take));
  if (typeof args?.roleType === "string" || typeof args?.roleType === "number") params.set("roleType", String(args.roleType));

  const qs = params.toString();
  return httpRequest<PagedResponse<T>>(
    `/api/admin/references/mdm/${encodeURIComponent(dict)}${qs ? `?${qs}` : ""}`,
    { method: "GET" }
  );
}

export async function getMdmDictionaryById<T>(dict: MdmDictionaryKey, id: string): Promise<T> {
  return httpRequest<T>(
    `/api/admin/references/mdm/${encodeURIComponent(dict)}/${encodeURIComponent(id)}`,
    { method: "GET" }
  );
}
