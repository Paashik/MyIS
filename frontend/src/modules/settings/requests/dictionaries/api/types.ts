export type RequestDirection = "Incoming" | "Outgoing";

export interface AdminRequestTypeDto {
  id: string;
  name: string;
  direction: RequestDirection;
  description?: string | null;
  isActive: boolean;
}

export interface AdminRequestStatusDto {
  id: string;
  code: string;
  name: string;
  isFinal: boolean;
  description?: string | null;
  isActive: boolean;
}

export interface AdminRequestWorkflowTransitionDto {
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

export interface CreateAdminRequestTypePayload {
  name: string;
  direction: RequestDirection;
  description?: string;
  isActive: boolean;
}

export interface UpdateAdminRequestTypePayload {
  name: string;
  direction: RequestDirection;
  description?: string;
  isActive: boolean;
}

export interface CreateAdminRequestStatusPayload {
  code: string;
  name: string;
  isFinal: boolean;
  description?: string;
  isActive: boolean;
}

export interface UpdateAdminRequestStatusPayload {
  name: string;
  isFinal: boolean;
  description?: string;
  isActive: boolean;
}

export interface ReplaceWorkflowTransitionsPayload {
  typeId: string;
  transitions: WorkflowTransitionInput[];
}

export interface WorkflowTransitionInput {
  fromStatusId: string;
  toStatusId: string;
  actionCode: string;
  requiredPermission?: string;
  isEnabled: boolean;
}

