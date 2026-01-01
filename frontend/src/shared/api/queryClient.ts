import { QueryClient } from '@tanstack/react-query';
import { message } from 'antd';
import { ApiError } from './apiClient';

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 1000 * 60 * 5, // 5 минут
      retry: (failureCount: number, error: Error) => {
        // Не повторять при 401/403
        if (error instanceof ApiError && (error.status === 401 || error.status === 403)) {
          return false;
        }
        return failureCount < 2;
      },
    },
    mutations: {
      onError: (error: Error) => {
        if (error instanceof ApiError) {
          // 401 обрабатывается централизованно в apiClient (редирект на /login)
          if (error.status === 401) {
            return; // Не показываем message, т.к. уже произошёл редирект
          }
          if (error.status === 403) {
            message.error('Доступ запрещён');
          } else {
            message.error(error.message);
          }
        } else if (error instanceof Error) {
          message.error(error.message);
        } else {
          message.error('Произошла неизвестная ошибка');
        }
      },
    },
  },
});
