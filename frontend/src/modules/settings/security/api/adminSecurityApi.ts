import { t } from "../../../../core/i18n/t";
import type {
  AdminEmployeeDto,
  AdminRoleDto,
  AdminUserDetailsDto,
  AdminUserListItemDto,
  AdminUserRolesDto,
  CreateAdminEmployeePayload,
  CreateAdminRolePayload,
  CreateAdminUserPayload,
  ReplaceAdminUserRolesPayload,
  ResetAdminUserPasswordPayload,
  UpdateAdminEmployeePayload,
  UpdateAdminRolePayload,
  UpdateAdminUserPayload,
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

export async function getAdminEmployees(params?: {
  search?: string;
  isActive?: boolean;
}): Promise<AdminEmployeeDto[]> {
  const query = new URLSearchParams();
  if (params?.search) query.set("search", params.search);
  if (params?.isActive !== undefined) query.set("isActive", String(params.isActive));
  const q = query.toString();

  return httpRequest<AdminEmployeeDto[]>(
    `/api/admin/security/employees${q ? `?${q}` : ""}`,
    { method: "GET" }
  );
}

export async function getAdminEmployeeById(id: string): Promise<AdminEmployeeDto> {
  return httpRequest<AdminEmployeeDto>(
    `/api/admin/security/employees/${encodeURIComponent(id)}`,
    { method: "GET" }
  );
}

export async function createAdminEmployee(
  payload: CreateAdminEmployeePayload
): Promise<AdminEmployeeDto> {
  return httpRequest<AdminEmployeeDto>("/api/admin/security/employees", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export async function updateAdminEmployee(
  id: string,
  payload: UpdateAdminEmployeePayload
): Promise<AdminEmployeeDto> {
  return httpRequest<AdminEmployeeDto>(
    `/api/admin/security/employees/${encodeURIComponent(id)}`,
    {
      method: "PUT",
      body: JSON.stringify(payload),
    }
  );
}

export async function activateAdminEmployee(id: string): Promise<void> {
  return httpRequest<void>(
    `/api/admin/security/employees/${encodeURIComponent(id)}/activate`,
    { method: "POST" }
  );
}

export async function deactivateAdminEmployee(id: string): Promise<void> {
  return httpRequest<void>(
    `/api/admin/security/employees/${encodeURIComponent(id)}/deactivate`,
    { method: "POST" }
  );
}

export async function getAdminUsers(params?: {
  search?: string;
  isActive?: boolean;
}): Promise<AdminUserListItemDto[]> {
  const query = new URLSearchParams();
  if (params?.search) query.set("search", params.search);
  if (params?.isActive !== undefined) query.set("isActive", String(params.isActive));
  const q = query.toString();

  return httpRequest<AdminUserListItemDto[]>(
    `/api/admin/security/users${q ? `?${q}` : ""}`,
    { method: "GET" }
  );
}

export async function getAdminUserById(id: string): Promise<AdminUserDetailsDto> {
  return httpRequest<AdminUserDetailsDto>(
    `/api/admin/security/users/${encodeURIComponent(id)}`,
    { method: "GET" }
  );
}

export async function createAdminUser(
  payload: CreateAdminUserPayload
): Promise<AdminUserDetailsDto> {
  return httpRequest<AdminUserDetailsDto>("/api/admin/security/users", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export async function updateAdminUser(
  id: string,
  payload: UpdateAdminUserPayload
): Promise<AdminUserDetailsDto> {
  return httpRequest<AdminUserDetailsDto>(
    `/api/admin/security/users/${encodeURIComponent(id)}`,
    {
      method: "PUT",
      body: JSON.stringify(payload),
    }
  );
}

export async function activateAdminUser(id: string): Promise<void> {
  return httpRequest<void>(
    `/api/admin/security/users/${encodeURIComponent(id)}/activate`,
    { method: "POST" }
  );
}

export async function deactivateAdminUser(id: string): Promise<void> {
  return httpRequest<void>(
    `/api/admin/security/users/${encodeURIComponent(id)}/deactivate`,
    { method: "POST" }
  );
}

export async function resetAdminUserPassword(
  id: string,
  payload: ResetAdminUserPasswordPayload
): Promise<void> {
  return httpRequest<void>(
    `/api/admin/security/users/${encodeURIComponent(id)}/reset-password`,
    {
      method: "POST",
      body: JSON.stringify(payload),
    }
  );
}

export async function getAdminRoles(): Promise<AdminRoleDto[]> {
  return httpRequest<AdminRoleDto[]>("/api/admin/security/roles", {
    method: "GET",
  });
}

export async function createAdminRole(
  payload: CreateAdminRolePayload
): Promise<AdminRoleDto> {
  return httpRequest<AdminRoleDto>("/api/admin/security/roles", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export async function updateAdminRole(
  id: string,
  payload: UpdateAdminRolePayload
): Promise<AdminRoleDto> {
  return httpRequest<AdminRoleDto>(
    `/api/admin/security/roles/${encodeURIComponent(id)}`,
    {
      method: "PUT",
      body: JSON.stringify(payload),
    }
  );
}

export async function getAdminUserRoles(userId: string): Promise<AdminUserRolesDto> {
  return httpRequest<AdminUserRolesDto>(
    `/api/admin/security/users/${encodeURIComponent(userId)}/roles`,
    { method: "GET" }
  );
}

export async function replaceAdminUserRoles(
  userId: string,
  payload: ReplaceAdminUserRolesPayload
): Promise<void> {
  return httpRequest<void>(
    `/api/admin/security/users/${encodeURIComponent(userId)}/roles`,
    { method: "PUT", body: JSON.stringify(payload) }
  );
}

