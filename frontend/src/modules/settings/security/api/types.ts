export interface AdminEmployeeDto {
  id: string;
  fullName: string;
  email?: string | null;
  phone?: string | null;
  notes?: string | null;
  isActive: boolean;
}

export interface AdminUserListItemDto {
  id: string;
  login: string;
  isActive: boolean;
  employeeId?: string | null;
  employeeFullName?: string | null;
  roleCodes: string[];
}

export interface AdminUserDetailsDto {
  id: string;
  login: string;
  isActive: boolean;
  employeeId?: string | null;
  employeeFullName?: string | null;
  roleCodes: string[];
}

export interface AdminRoleDto {
  id: string;
  code: string;
  name: string;
}

export interface AdminUserRolesDto {
  userId: string;
  roleIds: string[];
}

export interface CreateAdminEmployeePayload {
  fullName: string;
  email?: string;
  phone?: string;
  notes?: string;
}

export interface UpdateAdminEmployeePayload {
  fullName: string;
  email?: string;
  phone?: string;
  notes?: string;
}

export interface CreateAdminUserPayload {
  login: string;
  password: string;
  isActive: boolean;
  employeeId?: string | null;
}

export interface UpdateAdminUserPayload {
  login: string;
  isActive: boolean;
  employeeId?: string | null;
}

export interface ResetAdminUserPasswordPayload {
  newPassword: string;
}

export interface ReplaceAdminUserRolesPayload {
  roleIds: string[];
}

export interface CreateAdminRolePayload {
  code: string;
  name: string;
}

export interface UpdateAdminRolePayload {
  name: string;
}

