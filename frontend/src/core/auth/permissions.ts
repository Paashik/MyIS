import { useAuth, User } from "../../auth/AuthContext";

/**
 * Минимальный слой прав для UI.
 *
 * ВАЖНО: это заглушка для Iteration 1.
 * TODO: заменить на полноценную RBAC-модель (permissions от backend).
 */
export function can(permission: string, user: User | null | undefined): boolean {
  if (!user) {
    return false;
  }

  // Iteration S1: минимальная, но осмысленная заглушка.
  // Все `Admin.*` permission'ы считаем доступными только роли ADMIN.
  if (permission.startsWith("Admin.")) {
    return !!user.roles?.some((r) => r === "ADMIN");
  }

  return true;
}

/**
 * Хук для удобной проверки прав в компонентах.
 *
 * Пример:
 *   const canCreate = useCan("Requests.Create");
 *   if (canCreate) { ... }
 */
export function useCan(permission: string): boolean {
  const { user } = useAuth();
  return can(permission, user);
}
