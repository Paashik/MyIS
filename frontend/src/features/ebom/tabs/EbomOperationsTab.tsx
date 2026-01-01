import React, { useState, useCallback, useMemo } from 'react';
import { Table, Card, Empty, Spin, Tag, Divider } from 'antd';
import { ToolOutlined } from '@ant-design/icons';

import { useEbomOperations } from '../api/hooks';
import { EbomOperationDto, OperationStatus } from '../api/types';

interface EbomOperationsTabProps {
  bomVersionId: string;
}

const statusLabels: Record<OperationStatus, string> = {
  Active: 'Активна',
  Inactive: 'Неактивна',
  Draft: 'Черновик',
};

const statusColors: Record<OperationStatus, string> = {
  Active: 'success',
  Inactive: 'default',
  Draft: 'warning',
};

export const EbomOperationsTab: React.FC<EbomOperationsTabProps> = ({
  bomVersionId,
}) => {
  const { data: operations, isLoading } = useEbomOperations(bomVersionId);
  const [selectedOperationId, setSelectedOperationId] = useState<string | null>(
    null
  );

  const selectedOperation = useMemo<EbomOperationDto | null>(() => {
    if (!operations || !selectedOperationId) return null;
    return (
      operations.find((op: EbomOperationDto) => op.id === selectedOperationId) || null
    );
  }, [operations, selectedOperationId]);

  const handleRowClick = useCallback((record: EbomOperationDto) => {
    setSelectedOperationId(record.id);
  }, []);

  const columns = [
    {
      title: 'Код',
      dataIndex: 'code',
      key: 'code',
      width: 100,
    },
    {
      title: 'Наименование',
      dataIndex: 'name',
      key: 'name',
      ellipsis: true,
    },
    {
      title: 'Участок',
      dataIndex: 'areaName',
      key: 'areaName',
      width: 150,
      render: (value: string | null) => value || '—',
    },
    {
      title: 'Время, мин',
      dataIndex: 'durationMin',
      key: 'durationMin',
      width: 100,
      render: (value: number | null) => (value !== null ? value : '—'),
    },
    {
      title: 'Статус',
      dataIndex: 'status',
      key: 'status',
      width: 100,
      render: (value: OperationStatus) => (
        <Tag color={statusColors[value]}>{statusLabels[value]}</Tag>
      ),
    },
  ];

  if (isLoading) {
    return (
      <div
        style={{
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'center',
          height: 300,
        }}
      >
        <Spin />
      </div>
    );
  }

  if (!operations || operations.length === 0) {
    return (
      <Empty
        image={Empty.PRESENTED_IMAGE_SIMPLE}
        description="Операции не найдены"
        style={{ marginTop: 48 }}
      />
    );
  }

  return (
    <div style={{ display: 'flex', gap: 24, padding: 16 }}>
      <div style={{ flex: 1, minWidth: 0 }}>
        <Card title="Список операций" size="small">
          <Table
            dataSource={operations}
            columns={columns}
            rowKey="id"
            size="small"
            pagination={false}
            scroll={{ y: 400 }}
            rowClassName={(record: EbomOperationDto) =>
              record.id === selectedOperationId ? 'ant-table-row-selected' : ''
            }
            onRow={(record: EbomOperationDto) => ({
              onClick: () => handleRowClick(record),
            })}
          />
        </Card>
      </div>

      <div style={{ width: 360, flexShrink: 0 }}>
        <Card
          title={
            <span>
              <ToolOutlined style={{ marginRight: 8 }} />
              Карточка операции
            </span>
          }
          size="small"
        >
          {selectedOperation ? (
            <div>
              <div style={{ marginBottom: 12 }}>
                <div style={{ color: '#8c8c8c', fontSize: 12 }}>Код</div>
                <div>{selectedOperation.code}</div>
              </div>
              <div style={{ marginBottom: 12 }}>
                <div style={{ color: '#8c8c8c', fontSize: 12 }}>
                  Наименование
                </div>
                <div>{selectedOperation.name}</div>
              </div>
              <div style={{ marginBottom: 12 }}>
                <div style={{ color: '#8c8c8c', fontSize: 12 }}>Участок</div>
                <div>{selectedOperation.areaName || '—'}</div>
              </div>
              <div style={{ marginBottom: 12 }}>
                <div style={{ color: '#8c8c8c', fontSize: 12 }}>
                  Длительность
                </div>
                <div>
                  {selectedOperation.durationMin !== null
                    ? `${selectedOperation.durationMin} мин`
                    : '—'}
                </div>
              </div>
              <div style={{ marginBottom: 12 }}>
                <div style={{ color: '#8c8c8c', fontSize: 12 }}>Статус</div>
                <Tag color={statusColors[selectedOperation.status]}>
                  {statusLabels[selectedOperation.status]}
                </Tag>
              </div>

              <Divider style={{ margin: '16px 0' }} />

              <div>
                <div
                  style={{
                    color: '#8c8c8c',
                    fontSize: 12,
                    marginBottom: 8,
                  }}
                >
                  Привязанные позиции eBOM
                </div>
                <Empty
                  image={Empty.PRESENTED_IMAGE_SIMPLE}
                  description="MVP позже"
                  style={{ margin: 0 }}
                />
              </div>
            </div>
          ) : (
            <Empty
              image={Empty.PRESENTED_IMAGE_SIMPLE}
              description="Выберите операцию"
            />
          )}
        </Card>
      </div>
    </div>
  );
};
