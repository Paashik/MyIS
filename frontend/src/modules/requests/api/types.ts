// TypeScript DTOs for Requests module, aligned with backend DTOs in
// MyIS.Core.Application.Requests.Dto and Common.Dto.PagedResultDto

export interface PagedResultDto<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export type RequestDirection = "Incoming" | "Outgoing";
export type RequestBasisType =
  | "IncomingRequest"
  | "CustomerOrder"
  | "ProductionOrder"
  | "Other";

export interface RequestListItemDto {
  id: string;
  title: string;
  requestTypeId: string;
  requestTypeName: string;
  requestStatusId: string;
  requestStatusCode: string;
  requestStatusName: string;
  managerId: string;
  managerFullName?: string | null;
  targetEntityName?: string | null;
  relatedEntityName?: string | null;
  description?: string | null;
  basisType?: RequestBasisType | null;
  basisRequestId?: string | null;
  basisCustomerOrderId?: string | null;
  basisDescription?: string | null;
  createdAt: string;
  dueDate?: string | null;
}

export interface RequestDto extends RequestListItemDto {
  description?: string | null;
  bodyText?: string | null;
  relatedEntityType?: string | null;
  relatedEntityId?: string | null;
  relatedEntityName?: string | null;
  targetEntityType?: string | null;
  targetEntityId?: string | null;
  targetEntityName?: string | null;
  basisType?: RequestBasisType | null;
  basisRequestId?: string | null;
  basisCustomerOrderId?: string | null;
  basisDescription?: string | null;
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

export interface RequestOrgUnitLookupDto {
  id: string;
  name: string;
  code?: string | null;
  parentId?: string | null;
  phone?: string | null;
  email?: string | null;
}

export interface RequestBasisIncomingRequestLookupDto {
  id: string;
  title: string;
  requestTypeName?: string | null;
}

export interface RequestBasisCustomerOrderLookupDto {
  id: string;
  number?: string | null;
  customerName?: string | null;
}

export interface RequestStatusDto {
  id: string;
  code: string;
  name: string;
  isFinal: boolean;
  description?: string | null;
}

export interface RequestWorkflowTransitionDto {
  id: string;
  requestTypeId: string;
  fromStatusId: string;
  fromStatusCode: string;
  toStatusId: string;
  toStatusCode: string;
  actionCode: string;
  requiredPermission?: string | null;
  isEnabled: boolean;
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
  targetEntityType?: string;
  targetEntityId?: string;
  targetEntityName?: string;
  basisType?: RequestBasisType;
  basisRequestId?: string;
  basisCustomerOrderId?: string;
  basisDescription?: string;
}

export interface UpdateRequestPayload {
  requestTypeId?: string;
  title: string;
  description?: string;
  lines?: RequestLineInputDto[];
  dueDate?: string;
  relatedEntityType?: string;
  relatedEntityId?: string;
  relatedEntityName?: string;
  targetEntityType?: string;
  targetEntityId?: string;
  targetEntityName?: string;
  basisType?: RequestBasisType;
  basisRequestId?: string;
  basisCustomerOrderId?: string;
  basisDescription?: string;
}

export interface AddRequestCommentPayload {
  text: string;
}



