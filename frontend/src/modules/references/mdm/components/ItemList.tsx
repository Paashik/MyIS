import React, { useEffect, useMemo, useRef, useState } from 'react';
import { Checkbox, Empty, Input, Select, Space, Spin, Tag, Tooltip } from 'antd';
import {
  ApiOutlined,
  AppstoreOutlined,
  BuildOutlined,
  ClusterOutlined,
  ExperimentOutlined,
  ShoppingOutlined,
} from '@ant-design/icons';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useMdmReferencesQueryService, ItemListItemDto } from '../../../../core/api/mdmReferencesQueryService';
import { t } from '../../../../core/i18n/t';
import './ItemList.css';

const { Search } = Input;

interface ItemListProps {
  selectedGroupId?: string | null;
  onItemSelect?: (item: ItemListItemDto) => void;
  reloadToken?: number;
}

type ItemTypeMeta = {
  code: string;
  label: string;
  icon: React.ReactNode;
  tone: 'blue' | 'green' | 'orange' | 'purple' | 'default';
};

const ITEM_TYPE_META: ItemTypeMeta[] = [
  { code: 'CMP', label: t('references.mdm.items.type.component'), icon: <ApiOutlined />, tone: 'blue' },
  { code: 'MAT', label: t('references.mdm.items.type.material'), icon: <ExperimentOutlined />, tone: 'orange' },
  { code: 'PRT', label: t('references.mdm.items.type.part'), icon: <BuildOutlined />, tone: 'purple' },
  { code: 'ASM', label: t('references.mdm.items.type.assembly'), icon: <ClusterOutlined />, tone: 'green' },
  { code: 'PRD', label: t('references.mdm.items.type.product'), icon: <ShoppingOutlined />, tone: 'blue' },
];

const normalizeText = (value?: string | null) => (value ?? '').trim().toLowerCase();

const parseBoolean = (value: string | null) => value === 'true';

const parseNumber = (value: string | null, fallback: number) => {
  if (!value) return fallback;
  const parsed = Number(value);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback;
};

const resolveItemType = (itemKind?: string | null): ItemTypeMeta => {
  const kind = normalizeText(itemKind);
  if (kind.includes('component') || kind.includes('компонент') || kind.includes('электро')) {
    return ITEM_TYPE_META[0];
  }
  if (kind.includes('material') || kind.includes('материал')) {
    return ITEM_TYPE_META[1];
  }
  if (kind.includes('detail') || kind.includes('part') || kind.includes('детал')) {
    return ITEM_TYPE_META[2];
  }
  if (kind.includes('assembly') || kind.includes('сборк')) {
    return ITEM_TYPE_META[3];
  }
  if (kind.includes('product') || kind.includes('готов') || kind.includes('издел')) {
    return ITEM_TYPE_META[4];
  }
  return {
    code: (itemKind ?? '').slice(0, 3).toUpperCase() || 'MDM',
    label: itemKind ?? t('references.mdm.items.type.unknown'),
    icon: <AppstoreOutlined />,
    tone: 'default',
  };
};

const resolveProcurementType = (itemKind?: string | null): 'purchased' | 'produced' | 'unknown' => {
  const kind = normalizeText(itemKind);
  if (kind.includes('component') || kind.includes('компонент') || kind.includes('материал')) {
    return 'purchased';
  }
  if (kind.includes('assembly') || kind.includes('сборк') || kind.includes('product') || kind.includes('издел') || kind.includes('детал')) {
    return 'produced';
  }
  return 'unknown';
};

export const ItemList: React.FC<ItemListProps> = ({ selectedGroupId, onItemSelect, reloadToken }) => {
  const [searchParams, setSearchParams] = useSearchParams();
  const [items, setItems] = useState<ItemListItemDto[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [searchText, setSearchText] = useState<string>('');
  const [currentPage, setCurrentPage] = useState<number>(1);
  const [pageSize, setPageSize] = useState<number>(12);
  const [totalItems, setTotalItems] = useState<number>(0);
  const [statusFilter, setStatusFilter] = useState<string | null>(null);
  const [typeFilter, setTypeFilter] = useState<string | null>(null);
  const [procurementFilter, setProcurementFilter] = useState<string | null>(null);
  const [archivedOnly, setArchivedOnly] = useState<boolean>(false);
  const [listView, setListView] = useState<boolean>(false);

  const mdmReferencesQueryService = useMdmReferencesQueryService();
  const navigate = useNavigate();
  const isUrlHydrated = useRef(false);

  useEffect(() => {
    const nextSearch = searchParams.get('q') ?? '';
    const nextPage = parseNumber(searchParams.get('page'), 1);
    const nextPageSize = parseNumber(searchParams.get('pageSize'), 12);
    const nextStatus = searchParams.get('status');
    const nextType = searchParams.get('type');
    const nextProcurement = searchParams.get('procurement');
    const nextArchived = parseBoolean(searchParams.get('archived'));
    setSearchText((prev) => (prev === nextSearch ? prev : nextSearch));
    setCurrentPage((prev) => (prev === nextPage ? prev : nextPage));
    setPageSize((prev) => (prev === nextPageSize ? prev : nextPageSize));
    setStatusFilter((prev) => (prev === nextStatus ? prev : nextStatus));
    setTypeFilter((prev) => (prev === nextType ? prev : nextType));
    setProcurementFilter((prev) => (prev === nextProcurement ? prev : nextProcurement));
    setArchivedOnly((prev) => (prev === nextArchived ? prev : nextArchived));
    if (!isUrlHydrated.current) {
      isUrlHydrated.current = true;
    }
  }, [searchParams]);

  useEffect(() => {
    const stored = localStorage.getItem('mdmItemsView');
    if (stored === 'list') {
      setListView(true);
    }
  }, []);

  useEffect(() => {
    localStorage.setItem('mdmItemsView', listView ? 'list' : 'cards');
  }, [listView]);

  useEffect(() => {
    setCurrentPage(1);
  }, [selectedGroupId]);

  useEffect(() => {
    fetchItems();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedGroupId, searchText, currentPage, pageSize, statusFilter, archivedOnly, reloadToken]);

  useEffect(() => {
    if (!isUrlHydrated.current) {
      return;
    }
    const next = new URLSearchParams(searchParams);
    const setOrDelete = (key: string, value: string | null) => {
      if (value === null || value === '') {
        next.delete(key);
        return;
      }
      next.set(key, value);
    };

    setOrDelete('q', searchText.trim() ? searchText.trim() : null);
    setOrDelete('page', String(currentPage));
    setOrDelete('pageSize', String(pageSize));
    setOrDelete('status', statusFilter);
    setOrDelete('type', typeFilter);
    setOrDelete('procurement', procurementFilter);

    if (archivedOnly) {
      next.set('archived', 'true');
    } else {
      next.delete('archived');
    }

    if (next.toString() !== searchParams.toString()) {
      setSearchParams(next, { replace: true });
    }
  }, [
    archivedOnly,
    currentPage,
    listView,
    pageSize,
    procurementFilter,
    searchText,
    setSearchParams,
    searchParams,
    statusFilter,
    typeFilter,
  ]);

  const fetchItems = async () => {
    const trimmedSearch = searchText.trim();
    const groupIdParam = searchParams.get('groupId');
    if (groupIdParam && selectedGroupId !== groupIdParam) {
      return;
    }
    if (!selectedGroupId && trimmedSearch.length === 0) {
      setItems([]);
      setTotalItems(0);
      setLoading(false);
      return;
    }
    setLoading(true);
    try {
      const isActive = archivedOnly
        ? false
        : statusFilter === 'active'
          ? true
          : statusFilter === 'archived'
            ? false
            : undefined;
      const result = await mdmReferencesQueryService.getItems({
        groupId: trimmedSearch.length > 0 ? null : selectedGroupId,
        searchText: trimmedSearch,
        pageNumber: currentPage,
        pageSize: pageSize,
        isActive,
      });
      setItems(result.items);
      setTotalItems(result.totalCount);
    } catch (error) {
      console.error('Failed to fetch items:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = (value: string) => {
    setSearchText(value);
    setCurrentPage(1);
  };

  const handleCardClick = (record: ItemListItemDto) => {
    onItemSelect?.(record);
    navigate(`/references/mdm/items/${record.id}`);
  };

  const displayedItems = useMemo(() => {
    const filteredByType = typeFilter
      ? items.filter((item) => resolveItemType(item.itemKind).code === typeFilter)
      : items;

    if (!procurementFilter) {
      return filteredByType;
    }

    return filteredByType.filter((item) => resolveProcurementType(item.itemKind) === procurementFilter);
  }, [items, typeFilter, procurementFilter]);

  return (
    <div className="mdm-items-journal">
      <div className="mdm-items-journal__toolbar">
        <Space wrap size={8} className="mdm-items-journal__filters">
          <Select
            allowClear
            placeholder={t('references.mdm.items.filter.type')}
            value={typeFilter ?? undefined}
            onChange={(value) => setTypeFilter(value ?? null)}
            options={ITEM_TYPE_META.map((type) => ({
              value: type.code,
              label: `${type.code} · ${type.label}`,
            }))}
            className="mdm-items-journal__filter"
          />
          <Select
            allowClear
            placeholder={t('references.mdm.items.filter.status')}
            value={statusFilter ?? undefined}
            onChange={(value) => {
              setStatusFilter(value ?? null);
              if (value) {
                setArchivedOnly(false);
              }
            }}
            options={[
              { value: 'active', label: t('references.mdm.items.status.active') },
              { value: 'archived', label: t('references.mdm.items.status.archived') },
              { value: 'blocked', label: t('references.mdm.items.status.blocked'), disabled: true },
            ]}
            className="mdm-items-journal__filter"
          />
          <Select
            allowClear
            placeholder={t('references.mdm.items.filter.procurement')}
            value={procurementFilter ?? undefined}
            onChange={(value) => setProcurementFilter(value ?? null)}
            options={[
              { value: 'purchased', label: t('references.mdm.items.procurement.purchased') },
              { value: 'produced', label: t('references.mdm.items.procurement.produced') },
            ]}
            className="mdm-items-journal__filter"
          />
          <Tooltip title={t('references.mdm.items.filter.usedInProductsHint')}>
            <Checkbox disabled>{t('references.mdm.items.filter.usedInProducts')}</Checkbox>
          </Tooltip>
          <Checkbox
            checked={archivedOnly}
            onChange={(event) => {
              setArchivedOnly(event.target.checked);
              if (event.target.checked) {
                setStatusFilter(null);
              }
            }}
          >
            {t('references.mdm.items.filter.archived')}
          </Checkbox>
          <Checkbox
            checked={listView}
            onChange={(event) => setListView(event.target.checked)}
          >
            {t('references.mdm.items.view.list')}
          </Checkbox>
        </Space>
        <Search
          placeholder={t('references.mdm.items.searchPlaceholder')}
          allowClear
          onSearch={handleSearch}
          onChange={(event) => handleSearch(event.target.value)}
          className="mdm-items-journal__search"
        />
      </div>

      <div className="mdm-items-journal__content">
        {loading ? (
          <div className="mdm-items-journal__state">
            <Spin size="large" />
          </div>
        ) : displayedItems.length === 0 ? (
          <div className="mdm-items-journal__state">
            <Empty
              description={
                !selectedGroupId && searchText.trim().length === 0
                  ? t('references.mdm.items.emptySelectGroup')
                  : t('references.mdm.items.empty')
              }
            />
          </div>
        ) : listView ? (
          <div className="mdm-items-list">
            {displayedItems.map((item) => {
              const typeMeta = resolveItemType(item.itemKind);
              const statusLabel = item.isActive
                ? t('references.mdm.items.status.active')
                : t('references.mdm.items.status.archived');
              const designation = item.designation?.trim();
              const displayTitle = designation ? `${designation} ${item.name}` : item.name;
              return (
                <button
                  key={item.id}
                  type="button"
                  className="mdm-items-list__row"
                  onClick={() => handleCardClick(item)}
                >
                  <div className="mdm-items-list__main">
                    <div className="mdm-items-list__title">{displayTitle}</div>
                    <div className="mdm-items-list__meta">
                      <span className="mdm-items-list__code">{item.nomenclatureNo}</span>
                      {item.code && <span className="mdm-items-list__divider">•</span>}
                      {item.code && <span>{item.code}</span>}
                    </div>
                  </div>
                  <div className="mdm-items-list__info">
                    <Tag className={`mdm-items-card__chip mdm-items-card__chip--${typeMeta.tone}`}>
                      <span className="mdm-items-card__chip-icon">{typeMeta.icon}</span>
                      <span>{typeMeta.code}</span>
                    </Tag>
                    <span className="mdm-items-list__group">{item.itemGroupName ?? t('references.mdm.items.card.noGroup')}</span>
                    <span className="mdm-items-list__uom">{item.unitOfMeasureName ?? '-'}</span>
                    <Tag className="mdm-items-card__chip mdm-items-card__chip--status">
                      {statusLabel}
                    </Tag>
                  </div>
                </button>
              );
            })}
          </div>
        ) : (
          <div className="mdm-items-journal__grid">
            {displayedItems.map((item) => {
              const typeMeta = resolveItemType(item.itemKind);
              const statusLabel = item.isActive
                ? t('references.mdm.items.status.active')
                : t('references.mdm.items.status.archived');
              const displayTitle = item.designation?.trim() ? item.designation : item.name;
              const displaySubtitle = item.designation?.trim() ? item.name : null;
              const photoUrl = item.hasPhoto ? `/api/admin/references/mdm/items/${item.id}/photo` : null;
              return (
                <button
                  key={item.id}
                  type="button"
                  className="mdm-items-card"
                  onClick={() => handleCardClick(item)}
                >
                  <div className="mdm-items-card__top">
                    <div className="mdm-items-card__header">
                      <div className="mdm-items-card__title-group">
                        <div className="mdm-items-card__title">{displayTitle}</div>
                        {displaySubtitle && (
                          <div className="mdm-items-card__subtitle">{displaySubtitle}</div>
                        )}
                        <div className="mdm-items-card__meta mdm-items-card__meta--top">
                          <span className="mdm-items-card__code">{item.nomenclatureNo}</span>
                          {item.code && <span className="mdm-items-card__divider">•</span>}
                          {item.code && <span>{item.code}</span>}
                        </div>
                      </div>
                      <Tag className={`mdm-items-card__chip mdm-items-card__chip--${typeMeta.tone}`}>
                        <span className="mdm-items-card__chip-icon">{typeMeta.icon}</span>
                        <span>{typeMeta.code}</span>
                      </Tag>
                    </div>
                    {photoUrl && (
                      <div className="mdm-items-card__media">
                        <img
                          src={photoUrl}
                          alt={item.name}
                          loading="lazy"
                          onError={(event) => { event.currentTarget.style.display = 'none'; }}
                        />
                      </div>
                    )}
                  </div>

                  <div className="mdm-items-card__row">
                    <span className="mdm-items-card__label">{t('references.mdm.items.columns.kind')}</span>
                    <span>{typeMeta.label}</span>
                  </div>

                  <div className="mdm-items-card__row">
                    <span className="mdm-items-card__label">{t('references.mdm.items.card.group')}</span>
                    <span>{item.itemGroupName ?? t('references.mdm.items.card.noGroup')}</span>
                  </div>

                  <div className="mdm-items-card__row">
                    <span className="mdm-items-card__label">{t('references.mdm.items.card.unit')}</span>
                    <span>{item.unitOfMeasureName ?? '-'}</span>
                    <Tag className="mdm-items-card__chip mdm-items-card__chip--status">
                      {statusLabel}
                    </Tag>
                  </div>

                  <div className="mdm-items-card__usage">
                    <Tooltip title={t('references.mdm.items.usage.noData')}>
                      <span className="mdm-items-card__usage-pill">{t('references.mdm.items.usage.inProducts')}</span>
                    </Tooltip>
                    <Tooltip title={t('references.mdm.items.usage.noData')}>
                      <span className="mdm-items-card__usage-pill">{t('references.mdm.items.usage.inProcurement')}</span>
                    </Tooltip>
                    <Tooltip title={t('references.mdm.items.usage.noData')}>
                      <span className="mdm-items-card__usage-pill">{t('references.mdm.items.usage.inStock')}</span>
                    </Tooltip>
                  </div>
                </button>
              );
            })}
          </div>
        )}
      </div>

    </div>
  );
};
