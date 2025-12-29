export interface OrgUnitListItemDto {
  id: string;
  name: string;
  code?: string | null;
  parentId?: string | null;
  managerEmployeeId?: string | null;
  phone?: string | null;
  email?: string | null;
  isActive: boolean;
  sortOrder: number;
}

export interface OrgUnitContactDto {
  employeeId: string;
  employeeFullName?: string | null;
  employeeEmail?: string | null;
  employeePhone?: string | null;
  includeInRequest: boolean;
  sortOrder: number;
}

export interface OrgUnitDetailsDto extends OrgUnitListItemDto {
  contacts: OrgUnitContactDto[];
}

export interface OrgUnitContactRequest {
  employeeId: string;
  includeInRequest: boolean;
  sortOrder: number;
}

export interface OrgUnitUpsertRequest {
  name: string;
  code?: string;
  parentId?: string | null;
  managerEmployeeId?: string | null;
  phone?: string;
  email?: string;
  isActive: boolean;
  sortOrder: number;
  contacts: OrgUnitContactRequest[];
}
