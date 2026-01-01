import React, { useState, useCallback, useMemo } from 'react';
import {
  Button,
  Table,
  Switch,
  Tag,
  Spin,
  Empty,
  Popconfirm,
  InputNumber,
  Select,
  Input,
  Tooltip,
} from 'antd';
import {
  PlusOutlined,
  ImportOutlined,
  FilterOutlined,
  CheckCircleOutlined,
  WarningOutlined,
  ExclamationCircleOutlined,
  EditOutlined,
  DeleteOutlined,
  ReloadOutlined,
} from '@ant-design/icons';

import { EbomLineDto, BomRole, LineStatus, UpdateEbomLinePayload } from '../../api/types';
import styles from './EbomTablePanel.module.css';

interface EbomTablePanelProps {
  lines: EbomLineDto[];
  isLoading: boolean;
  isEditMode: boolean;
  onlyErrors: boolean;
  selectedLineId: string | null;
  onEditModeChange: (value: boolean) => void;
  onOnlyErrorsChange: (value: boolean) => void;
  onSelectLine: (lineId: string | null) => void;
  onAddLine: () => void;
  onUpdateLine: (lineId: string, payload: UpdateEbomLinePayload) => void;
  onDeleteLine: (lineId: string) => void;
  onValidate: () => void;
  onOpenItemSelect: (lineId: string) => void;
}

const roleLabels: Record<BomRole, string> = {
  Component: 'Компонент',
  Material: 'Материал',
  SubAssembly: 'Сборка',
  Service: 'Услуга',
};

const roleStyles: Record<BomRole, string> = {
  Component: styles.roleComponent,
  Material: styles.roleMaterial,
  SubAssembly: styles.roleSubAssembly,
  Service: styles.roleService,
};

const statusIcons: Record<LineStatus, React.ReactNode> = {
  Valid: <CheckCircleOutlined className={styles.statusValid} />,
  Warning: <WarningOutlined className={styles.statusWarning} />,
  Error: <ExclamationCircleOutlined className={styles.statusError} />,
  Archived: <ExclamationCircleOutlined className={styles.statusArchived} />,
};

const roleOptions = Object.entries(roleLabels).map(([value, label]) => ({
  value,
  label,
}));

export const EbomTablePanel: React.FC<EbomTablePanelProps> = ({
  lines,
  isLoading,
  isEditMode,
  onlyErrors,
  selectedLineId,
  onEditModeChange,
  onOnlyErrorsChange,
  onSelectLine,
  onAddLine,
  onUpdateLine,
  onDeleteLine,
  onValidate,
  onOpenItemSelect,
}) => {
  const [editingCell, setEditingCell] = useState<{
    lineId: string;
    field: string;
  } | null>(null);
  const [editValue, setEditValue] = useState<string | number>('');

  const handleStartEdit = useCallback(
    (lineId: string, field: string, currentValue: string | number) => {
      if (!isEditMode) return;
      setEditingCell({ lineId, field });
      setEditValue(currentValue);
    },
    [isEditMode]
  );

  const handleFinishEdit = useCallback(() => {
    if (!editingCell) return;

    const payload: UpdateEbomLinePayload = {};
    if (editingCell.field === 'qty') {
      payload.qty = Number(editValue);
    } else if (editingCell.field === 'role') {
      payload.role = editValue as BomRole;
    } else if (editingCell.field === 'notes') {
      payload.notes = editValue as string;
    } else if (editingCell.field === 'positionNo') {
      payload.positionNo = editValue as string;
    }

    onUpdateLine(editingCell.lineId, payload);
    setEditingCell(null);
    setEditValue('');
  }, [editingCell, editValue, onUpdateLine]);

  const handleCancelEdit = useCallback(() => {
    setEditingCell(null);
    setEditValue('');
  }, []);

  const columns = useMemo(
    () => [
      {
        title: '№ поз.',
        dataIndex: 'positionNo',
        key: 'positionNo',
        width: 80,
        render: (value: string | null, record: EbomLineDto) => {
          if (
            editingCell?.lineId === record.id &&
            editingCell?.field === 'positionNo'
          ) {
            return (
              <Input
                size="small"
                value={editValue as string}
                onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                  setEditValue(e.target.value)
                }
                onBlur={handleFinishEdit}
                onPressEnter={handleFinishEdit}
                autoFocus
              />
            );
          }
          return (
            <div
              className={isEditMode ? styles.editableCell : undefined}
              onClick={() =>
                handleStartEdit(record.id, 'positionNo', value || '')
              }
            >
              {value || '—'}
            </div>
          );
        },
      },
      {
        title: 'Роль',
        dataIndex: 'role',
        key: 'role',
        width: 120,
        render: (value: BomRole, record: EbomLineDto) => {
          if (
            editingCell?.lineId === record.id &&
            editingCell?.field === 'role'
          ) {
            return (
              <Select
                size="small"
                value={editValue as string}
                options={roleOptions}
                onChange={(val: string) => {
                  setEditValue(val);
                  setTimeout(handleFinishEdit, 0);
                }}
                onBlur={handleCancelEdit}
                autoFocus
                style={{ width: '100%' }}
              />
            );
          }
          return (
            <div
              className={isEditMode ? styles.editableCell : undefined}
              onClick={() => handleStartEdit(record.id, 'role', value)}
            >
              <Tag className={`${styles.roleTag} ${roleStyles[value]}`}>
                {roleLabels[value]}
              </Tag>
            </div>
          );
        },
      },
      {
        title: 'Код',
        dataIndex: 'itemCode',
        key: 'itemCode',
        width: 120,
        render: (value: string | null, record: EbomLineDto) => (
          <span
            className={styles.itemLink}
            onClick={() => isEditMode && onOpenItemSelect(record.id)}
          >
            {value || '—'}
          </span>
        ),
      },
      {
        title: 'Наименование',
        dataIndex: 'itemName',
        key: 'itemName',
        ellipsis: true,
        render: (value: string, record: EbomLineDto) => (
          <span
            className={isEditMode ? styles.itemLink : undefined}
            onClick={() => isEditMode && onOpenItemSelect(record.id)}
          >
            {value}
          </span>
        ),
      },
      {
        title: 'Кол-во',
        dataIndex: 'qty',
        key: 'qty',
        width: 100,
        render: (value: number, record: EbomLineDto) => {
          if (
            editingCell?.lineId === record.id &&
            editingCell?.field === 'qty'
          ) {
            return (
              <InputNumber
                size="small"
                value={editValue as number}
                min={0}
                step={0.001}
                onChange={(val: number | null) => setEditValue(val ?? 0)}
                onBlur={handleFinishEdit}
                onPressEnter={handleFinishEdit}
                autoFocus
              />
            );
          }
          return (
            <div
              className={isEditMode ? styles.editableCell : undefined}
              onClick={() => handleStartEdit(record.id, 'qty', value)}
            >
              {value}
            </div>
          );
        },
      },
      {
        title: 'Ед.',
        dataIndex: 'uomCode',
        key: 'uomCode',
        width: 60,
      },
      {
        title: 'Примечание',
        dataIndex: 'notes',
        key: 'notes',
        width: 150,
        ellipsis: true,
        render: (value: string | null, record: EbomLineDto) => {
          if (
            editingCell?.lineId === record.id &&
            editingCell?.field === 'notes'
          ) {
            return (
              <Input
                size="small"
                value={editValue as string}
                onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                  setEditValue(e.target.value)
                }
                onBlur={handleFinishEdit}
                onPressEnter={handleFinishEdit}
                autoFocus
              />
            );
          }
          return (
            <div
              className={isEditMode ? styles.editableCell : undefined}
              onClick={() => handleStartEdit(record.id, 'notes', value || '')}
            >
              {value || '—'}
            </div>
          );
        },
      },
      {
        title: 'Статус',
        dataIndex: 'lineStatus',
        key: 'lineStatus',
        width: 70,
        align: 'center' as const,
        render: (value: LineStatus) => (
          <Tooltip title={value}>{statusIcons[value]}</Tooltip>
        ),
      },
      {
        title: '',
        key: 'actions',
        width: 80,
        render: (_: unknown, record: EbomLineDto) => (
          <div className={styles.actionButtons}>
            {isEditMode && (
              <>
                <Tooltip title="Изменить номенклатуру">
                  <Button
                    type="text"
                    size="small"
                    icon={<EditOutlined />}
                    onClick={() => onOpenItemSelect(record.id)}
                  />
                </Tooltip>
                <Popconfirm
                  title="Удалить строку?"
                  onConfirm={() => onDeleteLine(record.id)}
                  okText="Да"
                  cancelText="Нет"
                >
                  <Button
                    type="text"
                    size="small"
                    danger
                    icon={<DeleteOutlined />}
                  />
                </Popconfirm>
              </>
            )}
          </div>
        ),
      },
    ],
    [
      isEditMode,
      editingCell,
      editValue,
      handleStartEdit,
      handleFinishEdit,
      handleCancelEdit,
      onOpenItemSelect,
      onDeleteLine,
    ]
  );

  const handleRowClick = useCallback(
    (record: EbomLineDto) => {
      onSelectLine(record.id);
    },
    [onSelectLine]
  );

  if (isLoading) {
    return (
      <div className={styles.tablePanel}>
        <div className={styles.loadingContainer}>
          <Spin />
        </div>
      </div>
    );
  }

  return (
    <div className={styles.tablePanel}>
      <div className={styles.toolbar}>
        <div className={styles.toolbarLeft}>
          <Button
            type="primary"
            icon={<PlusOutlined />}
            disabled={!isEditMode}
            onClick={onAddLine}
          >
            Добавить строку
          </Button>
          <Tooltip title="MVP позже">
            <Button icon={<ImportOutlined />} disabled>
              Вставить из буфера
            </Button>
          </Tooltip>
          <Tooltip title="MVP позже">
            <Button icon={<ImportOutlined />} disabled>
              Импорт из Excel
            </Button>
          </Tooltip>
          <Button icon={<ReloadOutlined />} onClick={onValidate}>
            Проверить
          </Button>
          <Tooltip title="MVP позже">
            <Button icon={<FilterOutlined />} disabled>
              Фильтры
            </Button>
          </Tooltip>
        </div>

        <div className={styles.toolbarRight}>
          <div className={styles.switchGroup}>
            <Switch
              size="small"
              checked={isEditMode}
              onChange={onEditModeChange}
            />
            <span>Режим редактирования</span>
          </div>
          <div className={styles.switchGroup}>
            <Switch
              size="small"
              checked={onlyErrors}
              onChange={onOnlyErrorsChange}
            />
            <span>Только ошибки</span>
          </div>
        </div>
      </div>

      <div className={styles.tableContainer}>
        {lines.length === 0 ? (
          <div className={styles.emptyContainer}>
            <Empty description="Нет строк в выбранном узле" />
          </div>
        ) : (
          <Table
            dataSource={lines}
            columns={columns}
            rowKey="id"
            size="small"
            pagination={false}
            scroll={{ y: 'calc(100vh - 400px)' }}
            rowClassName={(record: EbomLineDto) =>
              record.id === selectedLineId ? 'ant-table-row-selected' : ''
            }
            onRow={(record: EbomLineDto) => ({
              onClick: () => handleRowClick(record),
            })}
          />
        )}
      </div>
    </div>
  );
};
