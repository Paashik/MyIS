import React from 'react';
import {
  Card,
  Button,
  Form,
  Input,
  InputNumber,
  Select,
  Tag,
  Tooltip,
} from 'antd';
import {
  PlusOutlined,
  FolderOutlined,
  InfoCircleOutlined,
} from '@ant-design/icons';
import { Link } from 'react-router-dom';

import {
  EbomTreeNodeDto,
  EbomLineDto,
  BomRole,
  UpdateEbomLinePayload,
} from '../../api/types';
import styles from './EbomInspector.module.css';

interface EbomInspectorProps {
  selectedNode: EbomTreeNodeDto | null;
  selectedLine: EbomLineDto | null;
  isEditMode: boolean;
  onAddLine: () => void;
  onAddSubNode: () => void;
  onUpdateLine: (lineId: string, payload: UpdateEbomLinePayload) => void;
  onOpenItemSelect: (lineId: string) => void;
}

const roleLabels: Record<BomRole, string> = {
  Component: 'Компонент',
  Material: 'Материал',
  SubAssembly: 'Сборка',
  Service: 'Услуга',
};

const roleOptions = Object.entries(roleLabels).map(([value, label]) => ({
  value,
  label,
}));

type EbomLineParametersFormProps = {
  selectedLine: EbomLineDto;
  isEditMode: boolean;
  onUpdateLine: (lineId: string, payload: UpdateEbomLinePayload) => void;
};

const EbomLineParametersForm: React.FC<EbomLineParametersFormProps> = ({
  selectedLine,
  isEditMode,
  onUpdateLine,
}) => {
  const [form] = Form.useForm();

  React.useEffect(() => {
    form.setFieldsValue({
      role: selectedLine.role,
      qty: selectedLine.qty,
      positionNo: selectedLine.positionNo,
      notes: selectedLine.notes,
    });
  }, [selectedLine, form]);

  const handleFormChange = React.useCallback(
    (changedValues: Partial<UpdateEbomLinePayload>) => {
      if (!isEditMode) return;
      onUpdateLine(selectedLine.id, changedValues);
    },
    [isEditMode, onUpdateLine, selectedLine.id],
  );

  return (
    <Form
      form={form}
      layout="vertical"
      size="small"
      onValuesChange={handleFormChange}
      disabled={!isEditMode}
    >
      <Form.Item name="role" label="Роль" className={styles.formItem}>
        {isEditMode ? (
          <Select options={roleOptions} />
        ) : (
          <div className={styles.readOnlyValue}>{roleLabels[selectedLine.role]}</div>
        )}
      </Form.Item>

      <Form.Item name="qty" label="Количество" className={styles.formItem}>
        {isEditMode ? (
          <InputNumber min={0} step={0.001} style={{ width: '100%' }} />
        ) : (
          <div className={styles.readOnlyValue}>
            {selectedLine.qty} {selectedLine.uomCode}
          </div>
        )}
      </Form.Item>

      <Form.Item name="positionNo" label="№ позиции" className={styles.formItem}>
        {isEditMode ? (
          <Input />
        ) : (
          <div className={styles.readOnlyValue}>{selectedLine.positionNo || '—'}</div>
        )}
      </Form.Item>

      <Form.Item name="notes" label="Примечание" className={styles.formItem}>
        {isEditMode ? (
          <Input.TextArea rows={2} />
        ) : (
          <div className={styles.readOnlyValue}>{selectedLine.notes || '—'}</div>
        )}
      </Form.Item>
    </Form>
  );
};

export const EbomInspector: React.FC<EbomInspectorProps> = ({
  selectedNode,
  selectedLine,
  isEditMode,
  onAddLine,
  onAddSubNode,
  onUpdateLine,
  onOpenItemSelect,
}) => {
  // Пустое состояние
  if (!selectedNode && !selectedLine) {
    return (
      <div className={styles.inspector}>
        <div className={styles.emptyState}>
          <InfoCircleOutlined className={styles.emptyStateIcon} />
          <div className={styles.emptyStateText}>
            Выберите узел в дереве или строку в таблице для просмотра деталей
          </div>
        </div>
      </div>
    );
  }

  // Отображение информации о выбранном узле дерева
  if (selectedNode && !selectedLine) {
    return (
      <div className={styles.inspector}>
        <div className={styles.inspectorHeader}>
          <h3 className={styles.inspectorTitle}>Узел структуры</h3>
          <p className={styles.inspectorSubtitle}>
            {selectedNode.code || selectedNode.name}
          </p>
        </div>

        <div className={styles.inspectorContent}>
          <Card className={styles.inspectorCard} size="small" title="Информация">
            <div className={styles.nodeInfo}>
              <div className={styles.nodeInfoItem}>
                <span className={styles.nodeInfoLabel}>Код:</span>
                <span className={styles.nodeInfoValue}>
                  {selectedNode.code || '—'}
                </span>
              </div>
              <div className={styles.nodeInfoItem}>
                <span className={styles.nodeInfoLabel}>Наименование:</span>
                <span className={styles.nodeInfoValue}>{selectedNode.name}</span>
              </div>
              <div className={styles.nodeInfoItem}>
                <span className={styles.nodeInfoLabel}>Тип:</span>
                <span className={styles.nodeInfoValue}>
                  {selectedNode.itemType}
                </span>
              </div>
              {selectedNode.hasErrors && (
                <div className={styles.nodeInfoItem}>
                  <span className={styles.nodeInfoLabel}>Статус:</span>
                  <Tag color="error">Есть ошибки</Tag>
                </div>
              )}
            </div>
          </Card>

          <Card className={styles.inspectorCard} size="small" title="Действия">
            <div className={styles.inspectorActions}>
              <Button
                icon={<PlusOutlined />}
                disabled={!isEditMode}
                onClick={onAddLine}
              >
                Добавить строку
              </Button>
              <Tooltip title="MVP позже">
                <Button
                  icon={<FolderOutlined />}
                  disabled
                  onClick={onAddSubNode}
                >
                  Создать подузел
                </Button>
              </Tooltip>
            </div>
          </Card>
        </div>
      </div>
    );
  }

  // Отображение формы редактирования строки
  if (selectedLine) {
    return (
      <div className={styles.inspector}>
        <div className={styles.inspectorHeader}>
          <h3 className={styles.inspectorTitle}>Строка eBOM</h3>
          <p className={styles.inspectorSubtitle}>
            {selectedLine.itemCode || selectedLine.itemName}
          </p>
        </div>

        <div className={styles.inspectorContent}>
          <Card className={styles.inspectorCard} size="small" title="Номенклатура">
            <div className={styles.nodeInfo}>
              <div className={styles.nodeInfoItem}>
                <span className={styles.nodeInfoLabel}>Код:</span>
                <span className={styles.nodeInfoValue}>
                  {selectedLine.itemCode ? (
                    <Link
                      to={`/references/mdm/items/${selectedLine.itemId}`}
                      className={styles.itemLink}
                    >
                      {selectedLine.itemCode}
                    </Link>
                  ) : (
                    '—'
                  )}
                </span>
              </div>
              <div className={styles.nodeInfoItem}>
                <span className={styles.nodeInfoLabel}>Наименование:</span>
                <span className={styles.nodeInfoValue}>
                  {selectedLine.itemName}
                </span>
              </div>
              {isEditMode && (
                <Button
                  size="small"
                  onClick={() => onOpenItemSelect(selectedLine.id)}
                  style={{ marginTop: 8 }}
                >
                  Изменить номенклатуру
                </Button>
              )}
            </div>
          </Card>

          <Card className={styles.inspectorCard} size="small" title="Параметры">
            <EbomLineParametersForm
              selectedLine={selectedLine}
              isEditMode={isEditMode}
              onUpdateLine={onUpdateLine}
            />
          </Card>

          <Card className={styles.inspectorCard} size="small" title="Статус">
            <div className={styles.statusBadges}>
              <Tag
                color={
                  selectedLine.lineStatus === 'Valid'
                    ? 'success'
                    : selectedLine.lineStatus === 'Warning'
                    ? 'warning'
                    : selectedLine.lineStatus === 'Error'
                    ? 'error'
                    : 'default'
                }
              >
                {selectedLine.lineStatus}
              </Tag>
            </div>
          </Card>
        </div>
      </div>
    );
  }

  return null;
};
