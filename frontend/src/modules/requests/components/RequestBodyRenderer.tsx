import React from "react";

import type { FormInstance } from "rc-field-form";

import type { RequestDto } from "../api/types";
import { getRequestTypeProfile } from "../typeProfiles/registry";

export type RequestBodyRendererMode = "details" | "edit";

export interface RequestBodyRendererProps {
  mode: RequestBodyRendererMode;
  requestTypeCode?: string | null;
  request?: RequestDto;
  form?: FormInstance;
  editMode?: "create" | "edit";
}

export const RequestBodyRenderer: React.FC<RequestBodyRendererProps> = ({
  mode,
  requestTypeCode,
  request,
  form,
  editMode,
}) => {
  const profile = getRequestTypeProfile(requestTypeCode);

  if (mode === "details") {
    if (!request) return null;
    return <>{profile.renderDetails({ request })}</>;
  }

  if (!form) return null;

  return (
    <>
      {profile.renderEdit({
        form,
        requestTypeCode: requestTypeCode ?? profile.code,
        mode: editMode ?? "create",
      })}
    </>
  );
};

