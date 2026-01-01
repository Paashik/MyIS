/**
 * DTO типы для eBOM API
 */

// ============ Item (MDM) ============

export type ItemType = 'Component' | 'Material' | 'Assembly' | 'Product' | 'Service';

export interface ItemDto {
  id: string;
  code: string | null;
  name: string;
  itemType: ItemType;
  groupName: string | null;
  isActive: boolean;
}

export interface ItemSearchResultDto {
  id: string;
  code: string | null;
  name: string;
  itemType: ItemType;
  groupName: string | null;
  isActive: boolean;
}

// ============ eBOM Version ============

export type EbomStatus = 'Draft' | 'Released' | 'Archived';
export type EbomSource = 'Component2020' | 'MyIS';

export interface EbomVersionDto {
  id: string;
  itemId: string;
  versionCode: string;
  status: EbomStatus;
  source: EbomSource;
  updatedAt: string;
}

export interface EbomVersionListItemDto {
  id: string;
  itemId: string;
  versionCode: string;
  status: EbomStatus;
  source: EbomSource;
  updatedAt: string;
}

// ============ eBOM Tree ============

export type TreeItemType = 'Assembly' | 'Part' | 'Component' | 'Material';

export interface EbomTreeNodeDto {
  itemId: string;
  parentItemId: string | null;
  code: string | null;
  name: string;
  itemType: TreeItemType;
  hasErrors: boolean;
}

export interface EbomTreeDto {
  rootItemId: string;
  nodes: EbomTreeNodeDto[];
}

// ============ eBOM Lines ============

export type BomRole = 'Component' | 'Material' | 'SubAssembly' | 'Service';
export type LineStatus = 'Valid' | 'Warning' | 'Error' | 'Archived';

export interface EbomLineDto {
  id: string;
  parentItemId: string;
  itemId: string;
  itemCode: string | null;
  itemName: string;
  role: BomRole;
  qty: number;
  uomCode: string;
  positionNo: string | null;
  notes: string | null;
  lineStatus: LineStatus;
}

export interface CreateEbomLinePayload {
  parentItemId: string;
  itemId: string;
  role: BomRole;
  qty: number;
  positionNo?: string;
  notes?: string;
}

export interface UpdateEbomLinePayload {
  role?: BomRole;
  qty?: number;
  positionNo?: string;
  notes?: string;
  itemId?: string;
}

// ============ Validation ============

export type ValidationSeverity = 'Error' | 'Warning' | 'Info';
export type ValidationTargetType = 'Node' | 'Line';

export interface ValidationResultDto {
  severity: ValidationSeverity;
  targetType: ValidationTargetType;
  targetId: string;
  message: string;
}

// ============ Operations ============

export type OperationStatus = 'Active' | 'Inactive' | 'Draft';

export interface EbomOperationDto {
  id: string;
  code: string;
  name: string;
  areaName: string | null;
  durationMin: number | null;
  status: OperationStatus;
}

// ============ History ============

export interface EbomHistoryItemDto {
  id: string;
  timestamp: string;
  userId: string;
  userName: string;
  action: string;
  details: string | null;
  comment: string | null;
}

// ============ Query Params ============

export interface GetEbomTreeParams {
  includeLeaves?: boolean;
  q?: string;
}

export interface GetEbomLinesParams {
  parentItemId: string;
  onlyErrors?: boolean;
}

// ============ Import ============

export type Component2020SyncMode = 'Delta' | 'SnapshotUpsert' | 'Overwrite';

export interface ImportBomFromComponent2020Payload {
  bomVersionId: string;
  connectionId?: string;
  syncMode?: Component2020SyncMode;
}

export interface ImportBomFromComponent2020Response {
  success: boolean;
  message: string;
  processedCount?: number;
}
