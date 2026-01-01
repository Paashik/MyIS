/**
 * API функции для eBOM
 */

import { api } from '../../../shared/api/apiClient';

// Mock data for demonstration
const mockItem: any = {
  id: 'demo-item-id',
  code: 'DEMO-001',
  name: 'Демонстрационное изделие',
  itemType: 'Assembly',
  groupName: 'Электроника',
  isActive: true,
};

const mockVersions: any[] = [
  {
    id: 'demo-version-id',
    itemId: 'demo-item-id',
    versionCode: 'v1.0',
    status: 'Draft',
    source: 'MyIS',
    updatedAt: new Date().toISOString(),
  },
];

const mockVersion: any = mockVersions[0];

const mockTree: any = {
  rootItemId: 'demo-item-id',
  nodes: [
    {
      itemId: 'demo-item-id',
      parentItemId: null,
      code: 'DEMO-001',
      name: 'Демонстрационное изделие',
      itemType: 'Assembly',
      hasErrors: false,
    },
    {
      itemId: 'comp-1',
      parentItemId: 'demo-item-id',
      code: 'COMP-001',
      name: 'Компонент 1',
      itemType: 'Component',
      hasErrors: false,
    },
    {
      itemId: 'comp-2',
      parentItemId: 'demo-item-id',
      code: 'COMP-002',
      name: 'Компонент 2',
      itemType: 'Component',
      hasErrors: true,
    },
  ],
};

const mockLines: any[] = [
  {
    id: 'line-1',
    parentItemId: 'demo-item-id',
    itemId: 'comp-1',
    itemCode: 'COMP-001',
    itemName: 'Компонент 1',
    role: 'Component',
    qty: 2,
    uomCode: 'шт',
    positionNo: '1',
    notes: 'Основной компонент',
    lineStatus: 'Valid',
  },
  {
    id: 'line-2',
    parentItemId: 'demo-item-id',
    itemId: 'comp-2',
    itemCode: 'COMP-002',
    itemName: 'Компонент 2',
    role: 'Component',
    qty: 1,
    uomCode: 'шт',
    positionNo: '2',
    notes: 'Дополнительный компонент',
    lineStatus: 'Error',
  },
];

const mockOperations: any[] = [
  {
    id: 'op-1',
    code: 'OP-001',
    name: 'Сборка компонентов',
    areaName: 'Цех сборки',
    durationMin: 30,
    status: 'Active',
  },
  {
    id: 'op-2',
    code: 'OP-002',
    name: 'Тестирование',
    areaName: 'Цех тестирования',
    durationMin: 15,
    status: 'Active',
  },
];

const mockValidationResults: any[] = [
  {
    severity: 'Error',
    targetType: 'Line',
    targetId: 'line-2',
    message: 'Компонент COMP-002 не найден в справочнике',
  },
  {
    severity: 'Warning',
    targetType: 'Node',
    targetId: 'comp-2',
    message: 'Узел имеет ошибки в дочерних элементах',
  },
];


// Mock delay for realistic loading
const mockDelay = (ms: number = 500) => new Promise(resolve => setTimeout(resolve, ms));

// Use mock data flag - set to true for demo without backend
const USE_MOCK_DATA = false;
import {
  ItemDto,
  ItemSearchResultDto,
  EbomVersionDto,
  EbomVersionListItemDto,
  EbomTreeDto,
  EbomLineDto,
  CreateEbomLinePayload,
  UpdateEbomLinePayload,
  ValidationResultDto,
  EbomOperationDto,
  GetEbomTreeParams,
  GetEbomLinesParams,
} from './types';

// ============ Item (MDM) ============

/**
 * Получить данные Item по ID
 * GET /api/mdm/items/{itemId}
 */
export async function getItem(itemId: string): Promise<ItemDto> {
  if (USE_MOCK_DATA) {
    await mockDelay();
    return mockItem;
  }
  return api.get<ItemDto>(`/mdm/items/${encodeURIComponent(itemId)}`);
}

/**
 * Поиск номенклатуры
 * GET /api/mdm/items/search?q={text}&take=20
 */
export async function searchItems(q: string, take = 20): Promise<ItemSearchResultDto[]> {
  if (USE_MOCK_DATA) {
    await mockDelay();
    return [
      { id: 'comp-1', code: 'COMP-001', name: 'Компонент 1', itemType: 'Component' as const, groupName: 'Электроника', isActive: true },
      { id: 'comp-2', code: 'COMP-002', name: 'Компонент 2', itemType: 'Component' as const, groupName: 'Электроника', isActive: true },
      { id: 'comp-3', code: 'COMP-003', name: 'Компонент 3', itemType: 'Component' as const, groupName: 'Механика', isActive: true },
    ].filter(item => item.name.toLowerCase().includes(q.toLowerCase()) || item.code.toLowerCase().includes(q.toLowerCase())).slice(0, take);
  }
  const params = new URLSearchParams();
  if (q) params.set('q', q);
  params.set('take', String(take));
  return api.get<ItemSearchResultDto[]>(`/mdm/items/search?${params.toString()}`);
}

// ============ eBOM Versions ============

/**
 * Получить список версий eBOM для Item
 * GET /api/engineering/ebom/versions?itemId={itemId}
 */
export async function getEbomVersions(itemId: string): Promise<EbomVersionListItemDto[]> {
  if (USE_MOCK_DATA) {
    await mockDelay();
    return mockVersions;
  }
  return api.get<EbomVersionListItemDto[]>(
    `/engineering/ebom/versions?itemId=${encodeURIComponent(itemId)}`
  );
}

/**
 * Получить версию eBOM по ID
 * GET /api/engineering/ebom/versions/{bomVersionId}
 */
export async function getEbomVersion(bomVersionId: string): Promise<EbomVersionDto> {
  if (USE_MOCK_DATA) {
    await mockDelay();
    return mockVersion;
  }
  return api.get<EbomVersionDto>(
    `/engineering/ebom/versions/${encodeURIComponent(bomVersionId)}`
  );
}

// ============ eBOM Tree ============

/**
 * Получить дерево eBOM
 * GET /api/engineering/ebom/{bomVersionId}/tree?includeLeaves={true|false}&q={search?}
 */
export async function getEbomTree(
  bomVersionId: string,
  params?: GetEbomTreeParams
): Promise<EbomTreeDto> {
  if (USE_MOCK_DATA) {
    await mockDelay();
    return mockTree;
  }
  const searchParams = new URLSearchParams();
  if (params?.includeLeaves !== undefined) {
    searchParams.set('includeLeaves', String(params.includeLeaves));
  }
  if (params?.q) {
    searchParams.set('q', params.q);
  }
  const query = searchParams.toString();
  return api.get<EbomTreeDto>(
    `/engineering/ebom/${encodeURIComponent(bomVersionId)}/tree${query ? `?${query}` : ''}`
  );
}

// ============ eBOM Lines ============

/**
 * Получить строки узла eBOM
 * GET /api/engineering/ebom/{bomVersionId}/lines?parentItemId={itemId}&onlyErrors={true|false}
 */
export async function getEbomLines(
  bomVersionId: string,
  params: GetEbomLinesParams
): Promise<EbomLineDto[]> {
  if (USE_MOCK_DATA) {
    await mockDelay();
    return mockLines.filter(line => line.parentItemId === params.parentItemId);
  }
  const searchParams = new URLSearchParams();
  searchParams.set('parentItemId', params.parentItemId);
  if (params.onlyErrors !== undefined) {
    searchParams.set('onlyErrors', String(params.onlyErrors));
  }
  return api.get<EbomLineDto[]>(
    `/engineering/ebom/${encodeURIComponent(bomVersionId)}/lines?${searchParams.toString()}`
  );
}

/**
 * Создать строку eBOM
 * POST /api/engineering/ebom/{bomVersionId}/lines
 */
export async function createEbomLine(
  bomVersionId: string,
  payload: CreateEbomLinePayload
): Promise<EbomLineDto> {
  if (USE_MOCK_DATA) {
    await mockDelay();
    const newLine: EbomLineDto = {
      id: `line-${Date.now()}`,
      parentItemId: payload.parentItemId,
      itemId: payload.itemId,
      itemCode: 'NEW-COMP',
      itemName: 'Новый компонент',
      role: payload.role,
      qty: payload.qty,
      uomCode: 'шт',
      positionNo: payload.positionNo || '99',
      notes: payload.notes ?? null,
      lineStatus: 'Valid',
    };
    mockLines.push(newLine);
    return newLine;
  }
  return api.post<EbomLineDto>(
    `/engineering/ebom/${encodeURIComponent(bomVersionId)}/lines`,
    payload
  );
}

/**
 * Обновить строку eBOM
 * PUT /api/engineering/ebom/lines/{lineId}
 */
export async function updateEbomLine(
  lineId: string,
  payload: UpdateEbomLinePayload
): Promise<EbomLineDto> {
  if (USE_MOCK_DATA) {
    await mockDelay();
    const index = mockLines.findIndex(l => l.id === lineId);
    if (index !== -1) {
      mockLines[index] = { ...mockLines[index], ...payload };
      return mockLines[index];
    }
    throw new Error('Line not found');
  }
  return api.put<EbomLineDto>(
    `/engineering/ebom/lines/${encodeURIComponent(lineId)}`,
    payload
  );
}

/**
 * Удалить строку eBOM
 * DELETE /api/engineering/ebom/lines/{lineId}
 */
export async function deleteEbomLine(lineId: string): Promise<void> {
  if (USE_MOCK_DATA) {
    await mockDelay();
    const index = mockLines.findIndex(l => l.id === lineId);
    if (index !== -1) {
      mockLines.splice(index, 1);
    }
    return;
  }
  return api.delete<void>(`/engineering/ebom/lines/${encodeURIComponent(lineId)}`);
}

// ============ Validation ============

/**
 * Запустить валидацию eBOM
 * POST /api/engineering/ebom/{bomVersionId}/validate
 */
export async function validateEbom(bomVersionId: string): Promise<ValidationResultDto[]> {
  if (USE_MOCK_DATA) {
    await mockDelay();
    return mockValidationResults;
  }
  return api.post<ValidationResultDto[]>(
    `/engineering/ebom/${encodeURIComponent(bomVersionId)}/validate`
  );
}

// ============ Operations ============

/**
 * Получить операции eBOM
 * GET /api/engineering/ebom/{bomVersionId}/operations
 */
export async function getEbomOperations(bomVersionId: string): Promise<EbomOperationDto[]> {
  if (USE_MOCK_DATA) {
    await mockDelay();
    return mockOperations;
  }
  return api.get<EbomOperationDto[]>(
    `/engineering/ebom/${encodeURIComponent(bomVersionId)}/operations`
  );
}

// ============ Products ============

