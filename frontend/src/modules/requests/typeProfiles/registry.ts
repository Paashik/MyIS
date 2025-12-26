import type { RequestTypeId, RequestTypeProfile } from "./types";
import { defaultRequestTypeProfile } from "./default.profile";
import { supplyRequestProfile } from "./supplyRequest.profile";

const registry: Record<RequestTypeId, RequestTypeProfile> = {
  [supplyRequestProfile.id]: supplyRequestProfile,
};

export function getRequestTypeProfile(typeId: RequestTypeId | null | undefined): RequestTypeProfile {
  if (!typeId) {
    return defaultRequestTypeProfile;
  }

  return registry[typeId] ?? defaultRequestTypeProfile;
}

