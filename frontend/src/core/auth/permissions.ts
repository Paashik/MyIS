import { useAuth, User } from "../../auth/AuthContext";

/**
 * Минимальный слой прав для UI.
 *
 * ВАЖНО: это заглушка для Iteration 1.
 * TODO: заменить на полноценную RBAC-модель (permissions от backend).
 */
export function can(permission: string, user: User | null | undefined): boolean {
  // На данном этапе считаем, что любой аутентифицированный пользователь
  // имеет доступ ко всем действиям, разрешённым backend'ом.
  // Реальная проверка прав будет добавлена в следующих итерациях.
  return !!user;
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