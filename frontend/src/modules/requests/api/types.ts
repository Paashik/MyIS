// TypeScript DTOs for Requests module, aligned with backend DTOs in
// MyIS.Core.Application.Requests.Dto and Common.Dto.PagedResultDto

export interface PagedResultDto<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export type RequestDirection = "Incoming" | "Outgoing";

export interface RequestListItemDto {
  id: string;
  title: string;
  requestTypeId: string;
  requestTypeName: string;
  requestStatusId: string;
  requestStatusCode: string;
  requestStatusName: string;
  initiatorId: string;
  initiatorFullName?: string | null;
  targetEntityName?: string | null;
  relatedEntityName?: string | null;
  createdAt: string;
  dueDate?: string | null;
}

export interface RequestDto extends RequestListItemDto {
  description?: string | null;
  bodyText?: string | null;
  relatedEntityType?: string | null;
  relatedEntityId?: string | null;
  relatedEntityName?: string | null;
  externalReferenceId?: string | null;
  targetEntityType?: string | null;
  targetEntityId?: string | null;
  targetEntityName?: string | null;
  updatedAt: string;

  lines: RequestLineDto[];
}

export interface RequestLineDto {
  id: string;
  lineNo: number;
  itemId?: string | null;
  externalItemCode?: string | null;
  description?: string | null;
  quantity: number;
  unitOfMeasureId?: string | null;
  needByDate?: string | null;
  supplierName?: string | null;
  supplierContact?: string | null;
  externalRowReferenceId?: string | null;
}

export interface RequestLineInputDto {
  lineNo: number;
  itemId?: string;
  externalItemCode?: string;
  description?: string;
  quantity: number;
  unitOfMeasureId?: string;
  needByDate?: string;
  supplierName?: string;
  supplierContact?: string;
  externalRowReferenceId?: string;
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
  name: string;
  direction: RequestDirection;
  description?: string | null;
}

export interface RequestCounterpartyLookupDto {
  id: string;
  name: string;
  fullName?: string | null;
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
  direction?: RequestDirection;
  onlyMine?: boolean;
  pageNumber?: number;
  pageSize?: number;
}

export interface CreateRequestPayload {
  requestTypeId: string;
  title: string;
  description?: string;
  lines?: RequestLineInputDto[];
  dueDate?: string;
  relatedEntityType?: string;
  relatedEntityId?: string;
  relatedEntityName?: string;
  externalReferenceId?: string;
  targetEntityType?: string;
  targetEntityId?: string;
  targetEntityName?: string;
}

export interface UpdateRequestPayload {
  title: string;
  description?: string;
  lines?: RequestLineInputDto[];
  dueDate?: string;
  relatedEntityType?: string;
  relatedEntityId?: string;
  relatedEntityName?: string;
  externalReferenceId?: string;
  targetEntityType?: string;
  targetEntityId?: string;
  targetEntityName?: string;
}

export interface AddRequestCommentPayload {
  text: string;
}
