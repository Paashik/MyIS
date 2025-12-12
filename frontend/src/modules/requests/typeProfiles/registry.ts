import type { RequestTypeCode, RequestTypeProfile } from "./types";
import { defaultRequestTypeProfile } from "./default.profile";
import { supplyRequestProfile } from "./supplyRequest.profile";

const registry: Record<RequestTypeCode, RequestTypeProfile> = {
  [supplyRequestProfile.code]: supplyRequestProfile,
};

export function getRequestTypeProfile(code: RequestTypeCode | null | undefined): RequestTypeProfile {
  if (!code) {
    return defaultRequestTypeProfile;
  }

  return registry[code] ?? defaultRequestTypeProfile;
}

