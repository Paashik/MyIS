import React from 'react';
import {
  Button,
  Descriptions,
  Dropdown,
  Skeleton,
  Space,
  Tag,
  Tooltip,
} from 'antd';
import {
  SaveOutlined,
  PlusOutlined,
  SwapOutlined,
  ExportOutlined,
  MoreOutlined,
  PrinterOutlined,
  HistoryOutlined,
} from '@ant-design/icons';
import type { MenuProps } from 'antd';
import dayjs from 'dayjs';

import { ItemDto, EbomVersionDto, EbomStatus, EbomSource } from '../../api/types';
import styles from './EbomHeader.module.css';

interface EbomHeaderProps {
  item: ItemDto | undefined;
  version: EbomVersionDto | undefined;
  isLoading: boolean;
  hasUnsavedChanges: boolean;
  onSave: () => void;
  onCreateVersion: () => void;
  onCompareVersions: () => void;
  onExport: () => void;
}

const statusLabels: Record<EbomStatus, string> = {
  Draft: 'Черновик',
  Released: 'Выпущена',
  Archived: 'Архив',
};

const statusStyles: Record<EbomStatus, string> = {
  Draft: styles.statusDraft,
  Released: styles.statusReleased,
  Archived: styles.statusArchived,
};

const sourceLabels: Record<EbomSource, string> = {
  Component2020: 'Компонент-2020',
  MyIS: 'MyIS',
};

export const EbomHeader: React.FC<EbomHeaderProps> = ({
  item,
  version,
  isLoading,
  hasUnsavedChanges,
  onSave,
  onCreateVersion,
  onCompareVersions,
  onExport,
}) => {
  if (isLoading) {
    return (
      <div className={styles.skeleton}>
        <Skeleton active paragraph={{ rows: 2 }} />
      </div>
    );
  }

  const moreMenuItems: MenuProps['items'] = [
    {
      key: 'print',
      icon: <PrinterOutlined />,
      label: 'Печать',
      disabled: true,
    },
    {
      key: 'history',
      icon: <HistoryOutlined />,
      label: 'История версий',
      disabled: true,
    },
  ];

  const handleMenuClick: MenuProps['onClick'] = ({ key }: { key: string }) => {
    // MVP: остальные действия disabled
    console.log('Menu action:', key);
  };

  return (
    <div className={styles.ebomHeader}>
      <div className={styles.ebomHeaderContent}>
        <div className={styles.ebomHeaderInfo}>
          <div className={styles.ebomHeaderTitle}>
            <h1>
              {item?.code || item?.name || 'Загрузка...'}
              {version && ` — ${version.versionCode}`}
            </h1>
            {version && (
              <Tag className={`${styles.statusTag} ${statusStyles[version.status]}`}>
                {statusLabels[version.status]}
              </Tag>
            )}
            {version && (
              <Tag className={styles.sourceTag}>{sourceLabels[version.source]}</Tag>
            )}
          </div>

          <Descriptions
            className={styles.ebomHeaderDescriptions}
            size="small"
            column={{ xs: 1, sm: 2, md: 3, lg: 4 }}
          >
            {item?.code && (
              <Descriptions.Item label="Код">{item.code}</Descriptions.Item>
            )}
            <Descriptions.Item label="Наименование">
              {item?.name || '—'}
            </Descriptions.Item>
            <Descriptions.Item label="Тип">{item?.itemType || '—'}</Descriptions.Item>
            <Descriptions.Item label="Группа">
              {item?.groupName || '—'}
            </Descriptions.Item>
            {version && (
              <Descriptions.Item label="Обновлено">
                {dayjs(version.updatedAt).format('DD.MM.YYYY HH:mm')}
              </Descriptions.Item>
            )}
          </Descriptions>
        </div>

        <div className={styles.ebomHeaderActions}>
          <Space>
            <Button
              type="primary"
              icon={<SaveOutlined />}
              disabled={!hasUnsavedChanges}
              onClick={onSave}
            >
              Сохранить
            </Button>

            <Tooltip title="MVP позже">
              <Button icon={<PlusOutlined />} disabled onClick={onCreateVersion}>
                Создать версию…
              </Button>
            </Tooltip>

            <Tooltip title="MVP позже">
              <Button icon={<SwapOutlined />} disabled onClick={onCompareVersions}>
                Сравнить версии
              </Button>
            </Tooltip>

            <Button icon={<ExportOutlined />} onClick={onExport}>
              Экспорт
            </Button>

            <Tooltip title="MVP позже">
              <Dropdown
                menu={{ items: moreMenuItems, onClick: handleMenuClick }}
                trigger={['click']}
              >
                <Button icon={<MoreOutlined />} disabled />
              </Dropdown>
            </Tooltip>
          </Space>
        </div>
      </div>
    </div>
  );
};
