/**
 * React Query hooks для eBOM
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { message } from 'antd';
import {
  getItem,
  searchItems,
  getEbomVersions,
  getEbomVersion,
  getEbomTree,
  getEbomLines,
  createEbomLine,
  updateEbomLine,
  deleteEbomLine,
  validateEbom,
  getEbomOperations,
} from './ebomApi';
import {
  CreateEbomLinePayload,
  UpdateEbomLinePayload,
  GetEbomTreeParams,
  GetEbomLinesParams,
  ValidationResultDto,
} from './types';

// ============ Query Keys ============

export const ebomKeys = {
  all: ['ebom'] as const,
  item: (itemId: string) => ['mdm', 'item', itemId] as const,
  itemSearch: (q: string) => ['mdm', 'items', 'search', q] as const,
  versions: (itemId: string) => [...ebomKeys.all, 'versions', itemId] as const,
  version: (bomVersionId: string) => [...ebomKeys.all, 'version', bomVersionId] as const,
  tree: (bomVersionId: string, params?: GetEbomTreeParams) =>
    [...ebomKeys.all, 'tree', bomVersionId, params] as const,
  lines: (bomVersionId: string, params: GetEbomLinesParams) =>
    [...ebomKeys.all, 'lines', bomVersionId, params] as const,
  validation: (bomVersionId: string) => [...ebomKeys.all, 'validation', bomVersionId] as const,
  operations: (bomVersionId: string) => [...ebomKeys.all, 'operations', bomVersionId] as const,
};

// ============ Item Hooks ============

export function useItem(itemId: string | undefined) {
  return useQuery({
    queryKey: ebomKeys.item(itemId ?? ''),
    queryFn: () => getItem(itemId!),
    enabled: !!itemId,
  });
}

export function useItemSearch(q: string, enabled = true) {
  return useQuery({
    queryKey: ebomKeys.itemSearch(q),
    queryFn: () => searchItems(q),
    enabled: enabled && q.length >= 2,
    staleTime: 1000 * 30, // 30 секунд
  });
}

// ============ Version Hooks ============

export function useEbomVersions(itemId: string | undefined) {
  return useQuery({
    queryKey: ebomKeys.versions(itemId ?? ''),
    queryFn: () => getEbomVersions(itemId!),
    enabled: !!itemId,
  });
}

export function useEbomVersion(bomVersionId: string | undefined) {
  return useQuery({
    queryKey: ebomKeys.version(bomVersionId ?? ''),
    queryFn: () => getEbomVersion(bomVersionId!),
    enabled: !!bomVersionId,
  });
}

// ============ Tree Hook ============

export function useEbomTree(bomVersionId: string | undefined, params?: GetEbomTreeParams) {
  return useQuery({
    queryKey: ebomKeys.tree(bomVersionId ?? '', params),
    queryFn: () => getEbomTree(bomVersionId!, params),
    enabled: !!bomVersionId,
  });
}

// ============ Lines Hooks ============

export function useEbomLines(
  bomVersionId: string | undefined,
  params: GetEbomLinesParams | undefined
) {
  return useQuery({
    queryKey: ebomKeys.lines(bomVersionId ?? '', params ?? { parentItemId: '' }),
    queryFn: () => getEbomLines(bomVersionId!, params!),
    enabled: !!bomVersionId && !!params?.parentItemId,
  });
}

export function useCreateEbomLine(bomVersionId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: CreateEbomLinePayload) => createEbomLine(bomVersionId, payload),
    onSuccess: (_data: unknown, variables: CreateEbomLinePayload) => {
      message.success('Строка добавлена');
      // Инвалидируем строки для родительского узла
      queryClient.invalidateQueries({
        queryKey: ebomKeys.lines(bomVersionId, { parentItemId: variables.parentItemId }),
      });
      // Инвалидируем дерево (могли появиться ошибки)
      queryClient.invalidateQueries({
        queryKey: ebomKeys.tree(bomVersionId),
        exact: false,
      });
    },
  });
}

export function useUpdateEbomLine(bomVersionId: string, parentItemId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ lineId, payload }: { lineId: string; payload: UpdateEbomLinePayload }) =>
      updateEbomLine(lineId, payload),
    onSuccess: () => {
      message.success('Строка обновлена');
      queryClient.invalidateQueries({
        queryKey: ebomKeys.lines(bomVersionId, { parentItemId }),
      });
      queryClient.invalidateQueries({
        queryKey: ebomKeys.tree(bomVersionId),
        exact: false,
      });
    },
  });
}

export function useDeleteEbomLine(bomVersionId: string, parentItemId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (lineId: string) => deleteEbomLine(lineId),
    onSuccess: () => {
      message.success('Строка удалена');
      queryClient.invalidateQueries({
        queryKey: ebomKeys.lines(bomVersionId, { parentItemId }),
      });
      queryClient.invalidateQueries({
        queryKey: ebomKeys.tree(bomVersionId),
        exact: false,
      });
    },
  });
}

// ============ Validation Hook ============

export function useValidateEbom(bomVersionId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => validateEbom(bomVersionId),
    onSuccess: (data: ValidationResultDto[]) => {
      const errorCount = data.filter((r: ValidationResultDto) => r.severity === 'Error').length;
      const warningCount = data.filter((r: ValidationResultDto) => r.severity === 'Warning').length;

      if (errorCount === 0 && warningCount === 0) {
        message.success('Проверка завершена. Ошибок не найдено.');
      } else {
        message.warning(`Найдено ошибок: ${errorCount}, предупреждений: ${warningCount}`);
      }

      queryClient.setQueryData(ebomKeys.validation(bomVersionId), data);
      // Обновляем дерево для отображения ошибок
      queryClient.invalidateQueries({
        queryKey: ebomKeys.tree(bomVersionId),
        exact: false,
      });
    },
  });
}

export function useEbomValidationResults(bomVersionId: string | undefined) {
  return useQuery({
    queryKey: ebomKeys.validation(bomVersionId ?? ''),
    queryFn: () => [] as Awaited<ReturnType<typeof validateEbom>>,
    enabled: false, // Данные устанавливаются через mutation
    staleTime: Infinity,
  });
}

// ============ Operations Hook ============

export function useEbomOperations(bomVersionId: string | undefined) {
  return useQuery({
    queryKey: ebomKeys.operations(bomVersionId ?? ''),
    queryFn: () => getEbomOperations(bomVersionId!),
    enabled: !!bomVersionId,
  });
}

