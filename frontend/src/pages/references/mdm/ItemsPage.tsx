import React, { useEffect, useMemo, useState } from 'react';
import { Button, Dropdown, Modal, Space, Table, Tag, Typography, message, Tabs } from 'antd';
import Tooltip from 'antd/es/tooltip';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { CommandBar } from '../../../components/ui/CommandBar';
import { useCan } from '../../../core/auth/permissions';
import { ItemGroupTreeFilter } from '../../../modules/references/mdm/components/ItemGroupTreeFilter';
import { ItemList } from '../../../modules/references/mdm/components/ItemList';
import { ItemListItemDto } from '../../../core/api/mdmReferencesQueryService';
import { t } from '../../../core/i18n/t';
import { purgeMdmItems } from '../../../modules/references/mdm/api/adminMdmReferencesApi';
import {
  getComponent2020Connection,
  getComponent2020ImportPreview,
  runComponent2020Sync,
} from '../../../modules/settings/integrations/component2020/api/adminComponent2020Api';
import {
  Component2020ImportPreviewItem,
  Component2020ImportPreviewResponse,
  Component2020SyncMode,
  Component2020SyncScope,
} from '../../../modules/settings/integrations/component2020/api/types';
import "./ItemsPage.css";

const { Title, Text } = Typography;

export const ItemsPage: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const canExecute = useCan('Admin.Integration.Execute');
  const defaultPreviewPageSize = 200;
  const [selectedGroupId, setSelectedGroupId] = useState<string | null>(null);
  const [reloadToken, setReloadToken] = useState(0);
  const [importLoading, setImportLoading] = useState(false);
  const [purgeLoading, setPurgeLoading] = useState(false);
  const [previewVisible, setPreviewVisible] = useState(false);
  const [previewLoading, setPreviewLoading] = useState(false);
  const [previewMode, setPreviewMode] = useState<Component2020SyncMode>(Component2020SyncMode.SnapshotUpsert);
  const [previewData, setPreviewData] = useState<Component2020ImportPreviewResponse | null>(null);
  const [previewPage, setPreviewPage] = useState(1);
  const [previewPageSize, setPreviewPageSize] = useState(defaultPreviewPageSize);
  const [previewTab, setPreviewTab] = useState<'all' | 'matches' | 'conflicts'>('all');

  useEffect(() => {
    const groupId = searchParams.get('groupId');
    setSelectedGroupId((prev) => (prev === groupId ? prev : groupId));
  }, [searchParams]);

  useEffect(() => {
    document.body.classList.add('mdm-items-page-scroll');
    return () => {
      document.body.classList.remove('mdm-items-page-scroll');
    };
  }, []);

  const updateGroupParam = (groupId: string | null) => {
    const next = new URLSearchParams(searchParams);
    if (groupId) {
      next.set('groupId', groupId);
    } else {
      next.delete('groupId');
    }
    setSearchParams(next, { replace: true });
  };

  const handleGroupSelect = (groupId: string | null, _groupName: string | null) => {
    setSelectedGroupId(groupId);
    updateGroupParam(groupId);
  };

  const handleItemSelect = (item: ItemListItemDto) => {
    if (item.itemGroupId) {
      setSelectedGroupId(item.itemGroupId);
      updateGroupParam(item.itemGroupId);
    }
  };

  const importMenuItems = useMemo(
    () => [
      { key: 'delta', label: t('references.mdm.import.delta') },
      { key: 'snapshotUpsert', label: t('references.mdm.import.snapshotUpsert') },
      { key: 'overwrite', label: t('references.mdm.import.overwrite'), danger: true },
    ],
    []
  );

  const runImport = async (syncMode: Component2020SyncMode) => {
    setImportLoading(true);
    try {
      const toastKey = 'mdm-items-import';
      message.loading({ key: toastKey, content: t('references.mdm.import.running'), duration: 0 });

      const connection = await getComponent2020Connection();
      const connectionId = connection.id;

      if (!connectionId || connection.isActive === false) {
        message.error({ key: toastKey, content: t('references.mdm.import.noActiveConnection'), duration: 6 });
        return;
      }

      const productsResp = await runComponent2020Sync({
        connectionId,
        scope: Component2020SyncScope.Products,
        dryRun: false,
        syncMode,
      });

      if (productsResp.status === 'Failed') {
        message.error({
          key: toastKey,
          duration: 8,
          content: t('references.mdm.import.started', { status: productsResp.status }),
        });
        return;
      }

      const itemsResp = await runComponent2020Sync({
        connectionId,
        scope: Component2020SyncScope.Items,
        dryRun: false,
        syncMode,
      });

      if (itemsResp.status === 'Failed') {
        message.error({
          key: toastKey,
          duration: 8,
          content: t('references.mdm.import.started', { status: itemsResp.status }),
        });
        return;
      }

      message.success({
        key: toastKey,
        duration: 6,
        content: (
          <Space size={8}>
            <span>
              {t('references.mdm.import.started', { status: productsResp.status })}{' '}
              (Products: {productsResp.processedCount}, Components: {itemsResp.processedCount}) [{String(syncMode)}]
            </span>
            <Button
              type="link"
              size="small"
              onClick={() =>
                navigate(
                  `/administration/integrations/component2020/runs/${encodeURIComponent(itemsResp.runId)}`
                )
              }
            >
              {t('common.actions.open')}
            </Button>
          </Space>
        ),
      });

      setReloadToken((prev) => prev + 1);
    } catch (e) {
      message.error({ key: 'mdm-items-import', content: (e as Error).message, duration: 6 });
    } finally {
      setImportLoading(false);
    }
  };

  const runPurge = async () => {
    setPurgeLoading(true);
    try {
      const toastKey = 'mdm-items-purge';
      message.loading({ key: toastKey, content: t('references.mdm.items.purge.running'), duration: 0 });
      const resp = await purgeMdmItems();
      message.success({
        key: toastKey,
        duration: 6,
        content: t('references.mdm.items.purge.success', {
          items: resp.deletedItems,
          links: resp.deletedLinks,
          attributes: resp.deletedAttributeValues,
        }),
      });
      setReloadToken((prev) => prev + 1);
    } catch (e) {
      message.error({ key: 'mdm-items-purge', content: (e as Error).message, duration: 6 });
    } finally {
      setPurgeLoading(false);
    }
  };

  const confirmPurge = () => {
    Modal.confirm({
      title: t('references.mdm.items.purge.confirmTitle'),
      content: t('references.mdm.items.purge.confirmBody'),
      okText: t('references.mdm.items.purge.confirmOk'),
      okType: 'danger',
      closable: true,
      cancelText: t('common.actions.cancel'),
      onOk: runPurge,
    });
  };

  const loadPreview = async (mode: Component2020SyncMode, page: number, pageSize: number) => {
    setPreviewLoading(true);
    try {
      const connection = await getComponent2020Connection();
      const connectionId = connection.id;

      if (!connectionId || connection.isActive === false) {
        message.error(t('references.mdm.import.noActiveConnection'));
        return;
      }

      const result = await getComponent2020ImportPreview({
        connectionId,
        syncMode: mode,
        page,
        pageSize,
      });
      setPreviewData(result);
      setPreviewPage(result.page);
      setPreviewPageSize(result.pageSize);
    } catch (e) {
      message.error((e as Error).message);
    } finally {
      setPreviewLoading(false);
    }
  };

  const openPreview = (mode: Component2020SyncMode) => {
    setPreviewMode(mode);
    setPreviewTab('all');
    setPreviewVisible(true);
    void loadPreview(mode, 1, defaultPreviewPageSize);
  };

  const runImportFromPreview = () => {
    const mode = previewMode;
    const run = async () => {
      setPreviewVisible(false);
      await runImport(mode);
    };

    if (mode === Component2020SyncMode.Overwrite) {
      Modal.confirm({
        title: t('references.mdm.import.overwrite.confirmTitle'),
        content: t('references.mdm.import.overwrite.confirmBody'),
        okText: t('references.mdm.import.overwrite.confirmOk'),
        okType: 'danger',
        closable: true,
        cancelText: t('common.actions.cancel'),
        onOk: run,
      });
      return;
    }

    void run();
  };

  const onImportMenuClick = ({ key }: { key: string }) => {
    if (key === 'delta') {
      openPreview(Component2020SyncMode.Delta);
      return;
    }

    if (key === 'snapshotUpsert') {
      openPreview(Component2020SyncMode.SnapshotUpsert);
      return;
    }

    if (key === 'overwrite') {
      openPreview(Component2020SyncMode.Overwrite);
    }
  };

  const previewColumns = useMemo(() => {
    return [
      {
        title: 'Источник',
        dataIndex: 'source',
        width: 110,
        render: (value: string) => <Tag>{value}</Tag>,
      },
      {
        title: 'ExternalId',
        dataIndex: 'externalId',
        width: 90,
      },
      {
        title: 'Действие',
        dataIndex: 'action',
        width: 110,
        render: (value: string) => {
          const label = value === 'Create'
            ? 'Создать'
            : value === 'Update'
              ? 'Обновить'
              : value === 'Merge'
                ? 'Склейка'
                : value === 'Review'
                  ? 'На ревью'
                  : value;
          const color = value === 'Review' ? 'red' : value === 'Merge' ? 'gold' : value === 'Create' ? 'green' : 'blue';
          return <Tag color={color}>{label}</Tag>;
        },
      },
      {
        title: 'Тип',
        dataIndex: 'itemKind',
        width: 130,
      },
      {
        title: 'Группа',
        dataIndex: 'itemGroupName',
        width: 180,
        render: (value: string | null, record: Component2020ImportPreviewItem) => (
          <span>
            {value ?? '-'}
            {record.rootGroupAbbreviation ? ` (${record.rootGroupAbbreviation})` : ''}
          </span>
        ),
      },
      {
        title: 'Обозначение',
        dataIndex: 'designation',
        width: 160,
      },
      {
        title: 'Наименование',
        dataIndex: 'name',
        width: 260,
      },
      {
        title: 'PartNumber',
        dataIndex: 'partNumber',
        width: 160,
      },
      {
        title: 'Код',
        dataIndex: 'code',
        width: 120,
      },
      {
        title: 'Группа (Component)',
        dataIndex: 'externalGroupName',
        width: 200,
        render: (value: string | null, record: Component2020ImportPreviewItem) => (
          <span>
            {value ?? '-'}
            {record.externalGroupId ? ` (${record.externalGroupId})` : ''}
          </span>
        ),
      },
      {
        title: 'Кандидаты ЕСКД',
        dataIndex: 'designationCandidates',
        width: 200,
        render: (value: string[], record: Component2020ImportPreviewItem) => (
          <span>
            {value?.length ? value.join(', ') : '-'}
            {record.designationSource ? ` (${record.designationSource})` : ''}
          </span>
        ),
      },
      {
        title: 'Причины',
        dataIndex: 'reasons',
        width: 360,
        render: (value: string[]) => (value?.length ? value.join('; ') : '-'),
      },
      {
        title: 'Совпадение',
        dataIndex: 'matchedItemId',
        width: 220,
        render: (_: unknown, record: Component2020ImportPreviewItem) => {
          const matched = record.matchedItemId
            ? `Matched: ${record.matchedItemId} (${record.matchedItemKind ?? '-'})`
            : null;
          const existing = record.existingItemId
            ? `Existing: ${record.existingItemId} (${record.existingItemKind ?? '-'})`
            : null;
          return (
            <span>
              {matched ?? existing ?? '-'}
            </span>
          );
        },
      },
    ];
  }, []);

  const filteredPreviewItems = useMemo(() => {
    const items = previewData?.items ?? [];
    if (previewTab === 'all') {
      return items;
    }
    if (previewTab === 'matches') {
      return items.filter((item) => item.source === 'Component' && item.action === 'Merge');
    }
    return items.filter((item) =>
      item.source === 'Component'
      && (item.reasons ?? []).some((reason) => reason.includes('Classification conflict'))
    );
  }, [previewData, previewTab]);

  const previewCounts = useMemo(() => {
    const items = previewData?.items ?? [];
    const matches = items.filter((item) => item.source === 'Component' && item.action === 'Merge').length;
    const conflicts = items.filter((item) =>
      item.source === 'Component'
      && (item.reasons ?? []).some((reason) => reason.includes('Classification conflict'))
    ).length;
    return { matches, conflicts };
  }, [previewData]);

  const handlePreviewTabChange = (key: string) => {
    const nextTab = key as typeof previewTab;
    setPreviewTab(nextTab);
    setPreviewPage(1);
    if (nextTab === 'all') {
      setPreviewPageSize(defaultPreviewPageSize);
      void loadPreview(previewMode, 1, defaultPreviewPageSize);
      return;
    }

    const total = previewData?.total ?? defaultPreviewPageSize;
    const nextPageSize = Math.min(total, 5000);
    setPreviewPageSize(nextPageSize);
    void loadPreview(previewMode, 1, nextPageSize);
  };

  return (
    <div className="items-page">
      <CommandBar
        left={
          <Space direction="vertical" size={0}>
            <Title level={2} style={{ margin: 0 }}>
              {t('references.mdm.items.title')}
            </Title>
            <Text type="secondary">{t('references.mdm.items.journal.subtitle')}</Text>
          </Space>
        }
        right={
          <Space>
            <Tooltip title={!canExecute ? t('settings.forbidden') : undefined}>
              <Dropdown.Button
                trigger={["click"]}
                loading={importLoading}
                disabled={!canExecute}
                menu={{ items: importMenuItems, onClick: onImportMenuClick }}
                onClick={() => openPreview(Component2020SyncMode.SnapshotUpsert)}
                data-testid="mdm-items-import"
              >
                {t('references.mdm.import.button')}
              </Dropdown.Button>
            </Tooltip>
            <Tooltip title={!canExecute ? t('settings.forbidden') : undefined}>
              <Button
                danger
                loading={purgeLoading}
                disabled={!canExecute}
                onClick={confirmPurge}
                data-testid="mdm-items-purge"
              >
                {t('references.mdm.items.purge.button')}
              </Button>
            </Tooltip>
          </Space>
        }
      />

      <div className="items-page__layout">
        <aside className="items-page__sidebar">
          <div className="items-page__panel">
            <Title level={5} className="items-page__panel-title">
              {t('references.mdm.itemGroups.title')}
            </Title>
            <div className="items-page__panel-content">
              <ItemGroupTreeFilter
                onGroupSelect={handleGroupSelect}
                selectedGroupId={selectedGroupId}
              />
            </div>
          </div>
        </aside>

        <section className="items-page__content">
          <ItemList
            selectedGroupId={selectedGroupId}
            onItemSelect={handleItemSelect}
            reloadToken={reloadToken}
          />
        </section>
      </div>

      <Modal
        title="Предпросмотр импорта номенклатуры"
        open={previewVisible}
        onCancel={() => setPreviewVisible(false)}
        width={1400}
        footer={
          <Space>
            <Button onClick={() => setPreviewVisible(false)}>{t('common.actions.cancel')}</Button>
            <Button
              type="primary"
              danger={previewMode === Component2020SyncMode.Overwrite}
              onClick={runImportFromPreview}
              loading={importLoading}
            >
              {previewMode === Component2020SyncMode.Overwrite
                ? t('references.mdm.import.overwrite.confirmOk')
                : t('references.mdm.import.button')}
            </Button>
          </Space>
        }
      >
        <Space direction="vertical" size={12} style={{ width: '100%' }}>
          <div className="items-page__preview-summary">
            <Text type="secondary">
              Режим: {String(previewMode)} · Всего: {previewData?.summary.total ?? 0} · Product: {previewData?.summary.products ?? 0} · Component: {previewData?.summary.components ?? 0}
            </Text>
            <div className="items-page__preview-tags">
              <Tag color="green">Создать: {previewData?.summary.create ?? 0}</Tag>
              <Tag color="blue">Обновить: {previewData?.summary.update ?? 0}</Tag>
              <Tag color="gold">Склейка: {previewData?.summary.merge ?? 0}</Tag>
              <Tag color="red">Ревью: {previewData?.summary.review ?? 0}</Tag>
            </div>
          </div>
          <Tabs
            activeKey={previewTab}
            onChange={handlePreviewTabChange}
            items={[
              { key: 'all', label: `Все (${previewData?.items.length ?? 0})` },
              { key: 'matches', label: `Совпадения (Component): ${previewCounts.matches}` },
              { key: 'conflicts', label: `Конфликты (Component): ${previewCounts.conflicts}` },
            ]}
          />
          <Table<Component2020ImportPreviewItem>
            rowKey={(record) => `${record.source}-${record.externalId}`}
            columns={previewColumns}
            dataSource={filteredPreviewItems}
            loading={previewLoading}
            pagination={
              previewTab === 'all'
                ? {
                    current: previewPage,
                    pageSize: previewPageSize,
                    total: previewData?.total ?? 0,
                    showSizeChanger: true,
                    onChange: (page, pageSize) => {
                      setPreviewPage(page);
                      setPreviewPageSize(pageSize);
                      void loadPreview(previewMode, page, pageSize);
                    },
                  }
                : false
            }
            scroll={{ x: 1600, y: 520 }}
            size="small"
          />
        </Space>
      </Modal>
    </div>
  );
};
