import React, { useCallback } from 'react';
import { Table, Button, Empty, Spin, Tag } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import {
  ReloadOutlined,
  ExclamationCircleOutlined,
  WarningOutlined,
  InfoCircleOutlined,
  RightOutlined,
} from '@ant-design/icons';

import { useValidateEbom, useEbomValidationResults } from '../api/hooks';
import { ValidationResultDto, ValidationSeverity, ValidationTargetType } from '../api/types';

interface EbomValidationTabProps {
  bomVersionId: string;
  onNavigateToTarget: (targetType: ValidationTargetType, targetId: string) => void;
}

const severityIcons: Record<ValidationSeverity, React.ReactNode> = {
  Error: <ExclamationCircleOutlined style={{ color: '#ff4d4f' }} />,
  Warning: <WarningOutlined style={{ color: '#faad14' }} />,
  Info: <InfoCircleOutlined style={{ color: '#1890ff' }} />,
};

const severityColors: Record<ValidationSeverity, string> = {
  Error: 'error',
  Warning: 'warning',
  Info: 'processing',
};

const severityLabels: Record<ValidationSeverity, string> = {
  Error: 'Ошибка',
  Warning: 'Предупреждение',
  Info: 'Информация',
};

const targetTypeLabels: Record<ValidationTargetType, string> = {
  Node: 'Узел',
  Line: 'Строка',
};

export const EbomValidationTab: React.FC<EbomValidationTabProps> = ({
  bomVersionId,
  onNavigateToTarget,
}) => {
  const validateMutation = useValidateEbom(bomVersionId);
  const { data: results } = useEbomValidationResults(bomVersionId);

  const handleValidate = useCallback(() => {
    validateMutation.mutate();
  }, [validateMutation]);

  const handleNavigate = useCallback(
    (record: ValidationResultDto) => {
      onNavigateToTarget(record.targetType, record.targetId);
    },
    [onNavigateToTarget]
  );

  const columns: ColumnsType<ValidationResultDto> = [
    {
      title: 'Уровень',
      dataIndex: 'severity',
      key: 'severity',
      width: 140,
      render: (value: ValidationSeverity) => (
        <Tag icon={severityIcons[value]} color={severityColors[value]}>
          {severityLabels[value]}
        </Tag>
      ),
      filters: [
        { text: 'Ошибки', value: 'Error' },
        { text: 'Предупреждения', value: 'Warning' },
        { text: 'Информация', value: 'Info' },
      ],
      onFilter: (value: boolean | React.Key, record: ValidationResultDto) =>
        record.severity === String(value),
    },
    {
      title: 'Объект',
      dataIndex: 'targetType',
      key: 'targetType',
      width: 100,
      render: (value: ValidationTargetType, record: ValidationResultDto) => (
        <span>
          {targetTypeLabels[value]}: {record.targetId.slice(0, 8)}...
        </span>
      ),
    },
    {
      title: 'Сообщение',
      dataIndex: 'message',
      key: 'message',
      ellipsis: true,
    },
    {
      title: '',
      key: 'action',
      width: 100,
      render: (_: unknown, record: ValidationResultDto) => (
        <Button
          type="link"
          size="small"
          icon={<RightOutlined />}
          onClick={() => handleNavigate(record)}
        >
          Перейти
        </Button>
      ),
    },
  ];

  const errorCount = results?.filter((r: ValidationResultDto) => r.severity === 'Error').length || 0;
  const warningCount = results?.filter((r: ValidationResultDto) => r.severity === 'Warning').length || 0;
  const infoCount = results?.filter((r: ValidationResultDto) => r.severity === 'Info').length || 0;

  return (
    <div style={{ padding: 16 }}>
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          marginBottom: 16,
        }}
      >
        <div style={{ display: 'flex', gap: 16 }}>
          <span>
            <ExclamationCircleOutlined style={{ color: '#ff4d4f', marginRight: 4 }} />
            Ошибок: {errorCount}
          </span>
          <span>
            <WarningOutlined style={{ color: '#faad14', marginRight: 4 }} />
            Предупреждений: {warningCount}
          </span>
          <span>
            <InfoCircleOutlined style={{ color: '#1890ff', marginRight: 4 }} />
            Информация: {infoCount}
          </span>
        </div>

        <Button
          type="primary"
          icon={<ReloadOutlined />}
          loading={validateMutation.isPending}
          onClick={handleValidate}
        >
          Запустить проверку
        </Button>
      </div>

      {validateMutation.isPending ? (
        <div
          style={{
            display: 'flex',
            justifyContent: 'center',
            alignItems: 'center',
            height: 200,
          }}
        >
          <Spin tip="Выполняется проверка..." />
        </div>
      ) : !results || results.length === 0 ? (
        <Empty
          image={Empty.PRESENTED_IMAGE_SIMPLE}
          description="Нажмите «Запустить проверку» для анализа структуры"
          style={{ marginTop: 48 }}
        />
      ) : (
        <Table
          dataSource={results}
          columns={columns}
          rowKey={(record: ValidationResultDto) => `${record.targetType}-${record.targetId}-${record.message}`}
          size="small"
          pagination={{ pageSize: 20 }}
        />
      )}
    </div>
  );
};
