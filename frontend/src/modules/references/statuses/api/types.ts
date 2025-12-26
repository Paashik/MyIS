export interface StatusGroupDto {
  id: string;
  name: string;
  description?: string | null;
  sortOrder?: number | null;
  isActive: boolean;
  isRequestsGroup: boolean;
  createdAt: string; // ISO
  updatedAt: string; // ISO
}

export interface StatusDto {
  id: string;
  groupId: string;
  groupName?: string | null;
  name: string;
  color?: number | null;
  flags?: number | null;
  sortOrder?: number | null;
  isActive: boolean;
  createdAt: string; // ISO
  updatedAt: string; // ISO
}

export interface PagedResponse<T> {
  total: number;
  items: T[];
}

export interface StatusGroupUpsertRequest {
  name: string;
  description?: string | null;
  sortOrder?: number | null;
  isActive?: boolean;
}

export interface StatusUpsertRequest {
  groupId: string;
  name: string;
  color?: number | null;
  flags?: number | null;
  sortOrder?: number | null;
  isActive?: boolean;
}
