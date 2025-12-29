import React from "react";

import type { FormInstance } from "rc-field-form";

import type { RequestDto } from "../api/types";
import { getRequestTypeProfile } from "../typeProfiles/registry";

export type RequestBodyRendererMode = "details" | "edit";

export interface RequestBodyRendererProps {
  mode: RequestBodyRendererMode;
  requestTypeId?: string | null;
  request?: RequestDto;
  form?: FormInstance;
  editMode?: "create" | "edit";
  hideDescription?: boolean;
}

export const RequestBodyRenderer: React.FC<RequestBodyRendererProps> = ({
  mode,
  requestTypeId,
  request,
  form,
  editMode,
  hideDescription,
}) => {
  const profile = getRequestTypeProfile(requestTypeId);

  if (mode === "details") {
    if (!request) return null;
    return <>{profile.renderDetails({ request })}</>;
  }

  if (!form) return null;

  return (
    <>
      {profile.renderEdit({
        form,
        requestTypeId: requestTypeId ?? profile.id,
        mode: editMode ?? "create",
        hideDescription,
      })}
    </>
  );
};

