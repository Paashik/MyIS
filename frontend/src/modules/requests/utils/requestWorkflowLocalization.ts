import type { I18nKey } from "../../../core/i18n/ru";
import { t } from "../../../core/i18n/t";

const statusCodeKeyMap: Partial<Record<string, I18nKey>> = {
  Draft: "requests.statuses.code.Draft",
  Submitted: "requests.statuses.code.Submitted",
  InReview: "requests.statuses.code.InReview",
  Approved: "requests.statuses.code.Approved",
  Rejected: "requests.statuses.code.Rejected",
  InWork: "requests.statuses.code.InWork",
  Done: "requests.statuses.code.Done",
  Closed: "requests.statuses.code.Closed",
};

const actionCodeKeyMap: Partial<Record<string, I18nKey>> = {
  Submit: "requests.workflow.action.Submit",
  StartReview: "requests.workflow.action.StartReview",
  Approve: "requests.workflow.action.Approve",
  Reject: "requests.workflow.action.Reject",
  StartWork: "requests.workflow.action.StartWork",
  Complete: "requests.workflow.action.Complete",
  Close: "requests.workflow.action.Close",
};

export function getRequestStatusLabel(code: string, fallback?: string): string {
  const key = statusCodeKeyMap[code];
  if (key) return t(key);
  return fallback ?? code;
}

export function getRequestActionLabel(code: string): string {
  const key = actionCodeKeyMap[code];
  if (key) return t(key);
  return code;
}
