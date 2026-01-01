import React, { useState, useCallback, useMemo, useEffect } from 'react';
import { useParams, useSearchParams, useNavigate } from 'react-router-dom';
import { Tabs, Spin, Result, Button, message, Space } from 'antd';
import {
  PartitionOutlined,
  ToolOutlined,
  ExclamationCircleOutlined,
  HistoryOutlined,
} from '@ant-design/icons';

import { EbomHeader } from '../components/EbomHeader/EbomHeader';
import { EbomStructureTab } from '../tabs/EbomStructureTab';
import { EbomOperationsTab } from '../tabs/EbomOperationsTab';
import { EbomValidationTab } from '../tabs/EbomValidationTab';
import { EbomHistoryTab } from '../tabs/EbomHistoryTab';

import { useItem, useEbomVersion, useEbomVersions } from '../api/hooks';
import { ValidationTargetType } from '../api/types';
import styles from './EbomEditorPage.module.css';

type TabKey = 'structure' | 'operations' | 'validation' | 'history';

/**
 * Страница редактора eBOM.
 * Поддерживает два режима:
 * 1. /mdm/items/:itemId/bom — открытие через карточку Item
 * 2. /engineering/ebom/:bomVersionId — прямая ссылка на версию
 */
export const EbomEditorPage: React.FC = () => {
  const navigate = useNavigate();
  const params = useParams<{ itemId?: string; bomVersionId?: string }>();
  const [searchParams, setSearchParams] = useSearchParams();

  // Определяем режим работы
  const isDirectVersionMode = !!params.bomVersionId;
  const itemIdFromParams = params.itemId;
  const bomVersionIdFromParams = params.bomVersionId;

  // MVP: демо-режим для демонстрации без реальных данных
  const isDemoMode = !itemIdFromParams && !bomVersionIdFromParams;

  // State
  const [activeTab, setActiveTab] = useState<TabKey>('structure');
  const [selectedBomVersionId, setSelectedBomVersionId] = useState<string | null>(
    bomVersionIdFromParams || null
  );

  // Deep link params
  const initialParentItemId = searchParams.get('parentItemId') || undefined;
  const initialLineId = searchParams.get('lineId') || undefined;

  // Загрузка версии напрямую (режим /engineering/ebom/:bomVersionId)
  const {
    data: directVersion,
    isLoading: isDirectVersionLoading,
    error: directVersionError,
  } = useEbomVersion(isDirectVersionMode ? bomVersionIdFromParams : undefined);

  // Определяем itemId
  const itemId = isDirectVersionMode ? directVersion?.itemId : itemIdFromParams;

  // Загрузка Item
  const {
    data: item,
    isLoading: isItemLoading,
    error: itemError,
  } = useItem(itemId);

  // Загрузка списка версий (для режима через Item)
  const {
    data: versions,
    isLoading: isVersionsLoading,
  } = useEbomVersions(itemId);

  // Автовыбор первой версии, если не выбрана
  useEffect(() => {
    if (!isDirectVersionMode && versions && versions.length > 0 && !selectedBomVersionId) {
      setSelectedBomVersionId(versions[0].id);
    }
  }, [isDirectVersionMode, versions, selectedBomVersionId]);

  // Загрузка выбранной версии
  const {
    data: selectedVersion,
    isLoading: isSelectedVersionLoading,
  } = useEbomVersion(
    isDirectVersionMode ? bomVersionIdFromParams : selectedBomVersionId || undefined
  );


  // Финальные данные
  const version = isDirectVersionMode ? directVersion : selectedVersion;
  const bomVersionId = isDirectVersionMode
    ? bomVersionIdFromParams
    : selectedBomVersionId;

  const isLoading =
    isItemLoading ||
    isVersionsLoading ||
    isDirectVersionLoading ||
    isSelectedVersionLoading;

  const error = itemError || directVersionError;

  // Handlers
  const handleSave = useCallback(() => {
    // В текущей реализации изменения сохраняются сразу через mutations
    message.success('Все изменения сохранены');
  }, []);

  const handleCreateVersion = useCallback(() => {
    message.info('Функция создания версии будет доступна позже');
  }, []);

  const handleCompareVersions = useCallback(() => {
    message.info('Функция сравнения версий будет доступна позже');
  }, []);

  const handleExport = useCallback(() => {
    message.info('Экспорт будет доступен позже');
  }, []);


  const handleNavigateToTarget = useCallback(
    (targetType: ValidationTargetType, targetId: string) => {
      setActiveTab('structure');
      // Обновляем URL с параметрами для deep link
      const newParams = new URLSearchParams(searchParams);
      if (targetType === 'Node') {
        newParams.set('parentItemId', targetId);
        newParams.delete('lineId');
      } else {
        newParams.set('lineId', targetId);
      }
      setSearchParams(newParams);
    },
    [searchParams, setSearchParams]
  );

  const tabItems = useMemo(
    () => [
      {
        key: 'structure',
        label: (
          <span>
            <PartitionOutlined />
            Структура
          </span>
        ),
        children: bomVersionId ? (
          <EbomStructureTab
            bomVersionId={bomVersionId}
            initialParentItemId={initialParentItemId}
            initialLineId={initialLineId}
          />
        ) : null,
      },
      {
        key: 'operations',
        label: (
          <span>
            <ToolOutlined />
            Операции
          </span>
        ),
        children: bomVersionId ? (
          <EbomOperationsTab bomVersionId={bomVersionId} />
        ) : null,
      },
      {
        key: 'validation',
        label: (
          <span>
            <ExclamationCircleOutlined />
            Ошибки/проверки
          </span>
        ),
        children: bomVersionId ? (
          <EbomValidationTab
            bomVersionId={bomVersionId}
            onNavigateToTarget={handleNavigateToTarget}
          />
        ) : null,
      },
      {
        key: 'history',
        label: (
          <span>
            <HistoryOutlined />
            История
          </span>
        ),
        children: bomVersionId ? (
          <EbomHistoryTab bomVersionId={bomVersionId} />
        ) : null,
      },
    ],
    [bomVersionId, initialParentItemId, initialLineId, handleNavigateToTarget]
  );

  // Loading state
  if (isLoading && !item && !version) {
    return (
      <div className={styles.ebomEditorPage}>
        <div className={styles.loadingContainer}>
          <Spin size="large" />
          <span>Загрузка данных...</span>
        </div>
      </div>
    );
  }

  // Error state
  if (error) {
    return (
      <div className={styles.ebomEditorPage}>
        <div className={styles.errorContainer}>
          <Result
            status="error"
            title="Ошибка загрузки"
            subTitle={error instanceof Error ? error.message : 'Неизвестная ошибка'}
            extra={
              <Button type="primary" onClick={() => navigate(-1)}>
                Назад
              </Button>
            }
          />
        </div>
      </div>
    );
  }

  // Demo mode
  if (isDemoMode) {
    return (
      <div className={styles.ebomEditorPage}>
        <div className={styles.errorContainer}>
          <Result
            status="info"
            title="eBOM Editor — MVP демонстрация"
            subTitle={
              <div>
                <p>Это стартовая страница редактора eBOM.</p>
                <p>Чтобы открыть реальную спецификацию:</p>
                <ul style={{ textAlign: 'left', marginTop: 16 }}>
                  <li>Перейдите в <strong>Engineering</strong> → найдите изделие → нажмите «Открыть BOM»</li>
                  <li>Или откройте напрямую: <code>/mdm/items/{'<itemId>'}/bom</code></li>
                </ul>
                <p style={{ marginTop: 16 }}>
                  <strong>Функциональность:</strong> дерево структуры, таблица строк, инспектор, inline-редактирование, валидация
                </p>
              </div>
            }
            extra={
              <Space>
                <Button type="primary" onClick={() => navigate('/engineering')}>
                  Перейти к списку изделий
                </Button>
                <Button onClick={() => navigate('/references/mdm/items')}>
                  Открыть справочник номенклатуры
                </Button>
              </Space>
            }
          />
        </div>
      </div>
    );
  }

  // No version available
  if (!bomVersionId && versions && versions.length === 0) {
    return (
      <div className={styles.ebomEditorPage}>
        <EbomHeader
          item={item}
          version={undefined}
          isLoading={isLoading}
          hasUnsavedChanges={false}
          onSave={handleSave}
          onCreateVersion={handleCreateVersion}
          onCompareVersions={handleCompareVersions}
          onExport={handleExport}
        />
        <div className={styles.errorContainer}>
          <Result
            status="info"
            title="Нет версий eBOM"
            subTitle="Для данного изделия ещё не создано ни одной версии спецификации"
            extra={
              <Button type="primary" onClick={handleCreateVersion}>
                Создать версию
              </Button>
            }
          />
        </div>
      </div>
    );
  }

  return (
    <div className={styles.ebomEditorPage}>
      <EbomHeader
        item={item}
        version={version}
        isLoading={isLoading}
        hasUnsavedChanges={false}
        onSave={handleSave}
        onCreateVersion={handleCreateVersion}
        onCompareVersions={handleCompareVersions}
        onExport={handleExport}
      />

      <div className={styles.ebomContent}>
        <div className={styles.tabsContainer}>
          <Tabs
            activeKey={activeTab}
            onChange={(key: string) => setActiveTab(key as TabKey)}
            items={tabItems}
            size="middle"
          />
        </div>
      </div>
    </div>
  );
};
