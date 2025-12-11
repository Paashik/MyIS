// TypeScript DTOs for Requests module, aligned with backend DTOs in
// MyIS.Core.Application.Requests.Dto and Common.Dto.PagedResultDto

export interface PagedResultDto<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export interface RequestListItemDto {
  id: string;
  title: string;
  requestTypeId: string;
  requestTypeCode: string;
  requestTypeName: string;
  requestStatusId: string;
  requestStatusCode: string;
  requestStatusName: string;
  initiatorId: string;
  initiatorFullName?: string | null;
  createdAt: string;
  dueDate?: string | null;
}

export interface RequestDto extends RequestListItemDto {
  description?: string | null;
  relatedEntityType?: string | null;
  relatedEntityId?: string | null;
  externalReferenceId?: string | null;
  updatedAt: string;
}

export interface RequestHistoryItemDto {
  id: string;
  requestId: string;
  action: string;
  performedBy: string;
  performedByFullName?: string | null;
  timestamp: string;
  oldValue: string;
  newValue: string;
  comment?: string | null;
}

export interface RequestCommentDto {
  id: string;
  requestId: string;
  authorId: string;
  authorFullName?: string | null;
  text: string;
  createdAt: string;
}

export interface RequestTypeDto {
  id: string;
  code: string;
  name: string;
  description?: string | null;
}

export interface RequestStatusDto {
  id: string;
  code: string;
  name: string;
  isFinal: boolean;
  description?: string | null;
}

// Query/filter and command payloads

export interface GetRequestsParams {
  requestTypeId?: string;
  requestStatusId?: string;
  onlyMine?: boolean;
  pageNumber?: number;
  pageSize?: number;
}

export interface CreateRequestPayload {
  requestTypeId: string;
  title: string;
  description?: string;
  dueDate?: string;
  relatedEntityType?: string;
  relatedEntityId?: string;
  externalReferenceId?: string;
}

export interface UpdateRequestPayload {
  title: string;
  description?: string;
  dueDate?: string;
  relatedEntityType?: string;
  relatedEntityId?: string;
  externalReferenceId?: string;
}

export interface AddRequestCommentPayload {
  text: string;
}