export interface Component2020ConnectionDto {
  id?: string;
  mdbPath: string;
  login?: string;
  isActive?: boolean;

  /**
   * Пароль никогда не возвращается из API.
   * Если true — пароль уже сохранён на backend.
   */
  hasPassword?: boolean;

  /**
   * Для сохранения/теста передаётся только от UI к backend.
   */
  password?: string;

  /**
   * Явная очистка сохранённого пароля.
   */
  clearPassword?: boolean;
}

export interface Component2020MdbFileDto {
  name: string;
  relativePath: string;
  fullPath: string;
  sizeBytes: number;
  lastWriteTimeUtc: string; // ISO
}

export interface GetComponent2020MdbFilesResponse {
  databasesRoot: string;
  files: Component2020MdbFileDto[];
}

export interface Component2020FsEntryDto {
  name: string;
  relativePath: string;
  fullPath: string;
  isDirectory: boolean;
  sizeBytes?: number | null;
  lastWriteTimeUtc?: string | null; // ISO
}

export interface GetComponent2020FsEntriesResponse {
  databasesRoot: string;
  currentRelativePath: string;
  entries: Component2020FsEntryDto[];
}

export interface Component2020SyncRunDto {
  id: string;
  startedAt: string; // ISO
  finishedAt?: string; // ISO
  startedByUserId?: string;
  scope: string;
  mode: string;
  status: string;
  processedCount: number;
  errorCount: number;
  countersJson?: string;
  summary?: string;
}

export enum Component2020SyncScope {
  Units = 'Units',
  Counterparties = 'Counterparties',
  Suppliers = 'Suppliers',
  ItemGroups = 'ItemGroups',
  Items = 'Items',
  Products = 'Products',
  Manufacturers = 'Manufacturers',
  BodyTypes = 'BodyTypes',
  Currencies = 'Currencies',
  TechnicalParameters = 'TechnicalParameters',
  ParameterSets = 'ParameterSets',
  Symbols = 'Symbols',
  Employees = 'Employees',
  Users = 'Users',
  CustomerOrders = 'CustomerOrders',
  Statuses = 'Statuses',
  All = 'All'
}

export interface Component2020StatusResponse {
  isConnected: boolean;
  connectionError?: string;
  isSchedulerActive: boolean;
  lastSuccessfulSync?: string;
  lastSyncStatus?: string;
}

export interface GetComponent2020SyncRunsResponse {
  runs: Component2020SyncRunDto[];
  totalCount: number;
}

export interface RunComponent2020SyncRequest {
  connectionId: string;
  scope: Component2020SyncScope;
  dryRun: boolean;
  syncMode?: Component2020SyncMode;
}

export enum Component2020SyncMode {
  Delta = "Delta",
  SnapshotUpsert = "SnapshotUpsert",
  Overwrite = "Overwrite",
}

export interface ScheduleComponent2020SyncRequest {
  scope: Component2020SyncScope;
  dryRun: boolean;
  cronExpression: string;
  isActive: boolean;
}

export interface RunComponent2020SyncResponse {
  runId: string;
  status: string;
  errorMessage?: string;
  processedCount: number;
}

export interface Component2020SyncErrorDto {
  id: string;
  entityType: string;
  externalEntity?: string;
  externalKey?: string;
  message: string;
  details?: string;
  createdAt: string;
}

export interface GetComponent2020SyncRunErrorsResponse {
  errors: Component2020SyncErrorDto[];
}

export interface ScheduleComponent2020SyncResponse {
  scheduleId: string;
  status: string;
}

export interface Component2020ImportPreviewRequest {
  connectionId: string;
  syncMode?: Component2020SyncMode;
  page?: number;
  pageSize?: number;
}

export interface Component2020ImportPreviewSummary {
  total: number;
  products: number;
  components: number;
  create: number;
  update: number;
  merge: number;
  review: number;
}

export interface Component2020ImportPreviewItem {
  source: string;
  externalId: number;
  externalGroupId?: string | null;
  externalGroupName?: string | null;
  code?: string | null;
  partNumber?: string | null;
  designation?: string | null;
  designationSource?: string | null;
  designationCandidates: string[];
  name: string;
  description?: string | null;
  unitName?: string | null;
  unitSymbol?: string | null;
  itemKind: string;
  itemGroupId?: string | null;
  itemGroupName?: string | null;
  rootGroupAbbreviation?: string | null;
  action: string;
  reasons: string[];
  existingItemId?: string | null;
  existingItemKind?: string | null;
  existingItemGroup?: string | null;
  matchedItemId?: string | null;
  matchedItemKind?: string | null;
  matchedItemGroup?: string | null;
  isTooling: boolean;
}

export interface Component2020ImportPreviewResponse {
  total: number;
  page: number;
  pageSize: number;
  summary: Component2020ImportPreviewSummary;
  items: Component2020ImportPreviewItem[];
}
