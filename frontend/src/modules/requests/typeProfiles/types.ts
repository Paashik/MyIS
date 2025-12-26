import type React from "react";
import type { FormInstance } from "rc-field-form";

import type { RequestDirection, RequestDto, RequestLineInputDto } from "../api/types";

export type RequestTypeId = string;

export interface ValidationError {
  /** AntD Form path, например: ["lines", 0, "quantity"] */
  path: (string | number)[];
  message: string;
}

export interface RequestDraft {
  requestTypeId: RequestTypeId;
  description?: string;
  lines?: RequestLineInputDto[];
}

export interface DetailsContext {
  request: RequestDto;
}

export interface EditContext {
  form: FormInstance;
  requestTypeId: RequestTypeId;
  mode: "create" | "edit";
}

export interface RequestTypeProfile {
  id: RequestTypeId;
  title: string;
  direction?: RequestDirection;
  renderDetails: (ctx: DetailsContext) => React.ReactNode;
  renderEdit: (ctx: EditContext) => React.ReactNode;
  validate?: (model: RequestDraft) => ValidationError[];
}

