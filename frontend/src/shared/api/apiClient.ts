/**
 * Базовый HTTP-клиент для API запросов.
 * - baseUrl: /api
 * - credentials: include (cookie-based auth)
 * - Автоматический JSON parsing
 * - Централизованная обработка 401: редирект на /login
 */
const BASE_URL = '/api';

export class ApiError extends Error {
  constructor(
    public status: number,
    public statusText: string,
    message: string
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

export interface RequestOptions extends Omit<RequestInit, 'body'> {
  body?: unknown;
}

function redirectToLogin(): void {
  // Для Vite/SPA достаточно жёсткого редиректа.
  // Это гарантирует, что мы выйдем из текущего защищённого раздела.
  if (typeof window === 'undefined') return;

  const isOnLoginPage = window.location.pathname === '/login';
  if (!isOnLoginPage) {
    window.location.assign('/login');
  }
}

async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const text = await response.text().catch(() => '');

    if (response.status === 401) {
      redirectToLogin();
      throw new ApiError(401, 'Unauthorized', 'Требуется аутентификация');
    }

    if (response.status === 403) {
      throw new ApiError(403, 'Forbidden', 'Доступ запрещён');
    }

    const message = text || `HTTP ${response.status}: ${response.statusText}`;
    throw new ApiError(response.status, response.statusText, message);
  }

  // Для 204 No Content возвращаем undefined
  if (response.status === 204) {
    return undefined as T;
  }

  const contentType = response.headers.get('content-type') ?? '';
  if (!contentType.toLowerCase().includes('application/json')) {
    const text = await response.text().catch(() => '');
    throw new ApiError(
      response.status,
      'Invalid Content-Type',
      `Ожидался JSON, получен: ${contentType}. Ответ: ${text.slice(0, 200)}`
    );
  }

  return response.json() as Promise<T>;
}

export async function apiClient<T>(
  endpoint: string,
  options: RequestOptions = {}
): Promise<T> {
  const { body, headers, ...restOptions } = options;

  const config: RequestInit = {
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      ...headers,
    },
    ...restOptions,
  };

  if (body !== undefined) {
    config.body = JSON.stringify(body);
  }

  const url = endpoint.startsWith('/') ? `${BASE_URL}${endpoint}` : `${BASE_URL}/${endpoint}`;
  const response = await fetch(url, config);
  
  return handleResponse<T>(response);
}

// Удобные методы
export const api = {
  get: <T>(endpoint: string, options?: Omit<RequestOptions, 'method'>) =>
    apiClient<T>(endpoint, { ...options, method: 'GET' }),

  post: <T>(endpoint: string, body?: unknown, options?: Omit<RequestOptions, 'method' | 'body'>) =>
    apiClient<T>(endpoint, { ...options, method: 'POST', body }),

  put: <T>(endpoint: string, body?: unknown, options?: Omit<RequestOptions, 'method' | 'body'>) =>
    apiClient<T>(endpoint, { ...options, method: 'PUT', body }),

  patch: <T>(endpoint: string, body?: unknown, options?: Omit<RequestOptions, 'method' | 'body'>) =>
    apiClient<T>(endpoint, { ...options, method: 'PATCH', body }),

  delete: <T>(endpoint: string, options?: Omit<RequestOptions, 'method'>) =>
    apiClient<T>(endpoint, { ...options, method: 'DELETE' }),
};
