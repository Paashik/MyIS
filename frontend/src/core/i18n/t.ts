import { ru, type I18nKey } from "./ru";

export type I18nParams = Record<string, string | number | boolean | null | undefined>;

const tokenRegex = /\{([^}]+)\}/g;

export function t(key: I18nKey, params?: I18nParams): string {
  const template = ru[key] ?? String(key);
  if (!params) return template;

  return template.replace(tokenRegex, (_m, rawName: string) => {
    const name = String(rawName);
    const value = params[name];
    if (value === null || value === undefined) return "";
    return String(value);
  });
}

