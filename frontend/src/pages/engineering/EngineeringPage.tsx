import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Table, Input, Button, Space, Typography, Card, Empty, Spin } from 'antd';
import { SearchOutlined, FolderOpenOutlined } from '@ant-design/icons';
import { useItemSearch } from '../../features/ebom/api/hooks';
import type { ItemSearchResultDto } from '../../features/ebom/api/types';

const { Title, Text } = Typography;
const { Search } = Input;

/**
 * Engineering Page - список изделий с возможностью открытия BOM
 * Использует существующий хук useItemSearch для поиска изделий
 */
export const EngineeringPage: React.FC = () => {
  const navigate = useNavigate();
  const [searchTerm, setSearchTerm] = useState('');
  const { data: items, isLoading } = useItemSearch(searchTerm);

  const handleOpenBom = (itemId: string) => {
    navigate(`/mdm/items/${itemId}/bom`);
  };

  const columns = [
    {
      title: 'Код',
      dataIndex: 'code',
      key: 'code',
      width: 150,
      sorter: (a: ItemSearchResultDto, b: ItemSearchResultDto) => 
        (a.code || '').localeCompare(b.code || ''),
    },
    {
      title: 'Наименование',
      dataIndex: 'name',
      key: 'name',
      sorter: (a: ItemSearchResultDto, b: ItemSearchResultDto) => 
        a.name.localeCompare(b.name),
    },
    {
      title: 'Тип',
      dataIndex: 'itemType',
      key: 'itemType',
      width: 120,
      render: (type: string) => {
        const typeMap: Record<string, string> = {
          Component: 'Компонент',
          Material: 'Материал',
          Assembly: 'Сборка',
          Product: 'Изделие',
          Service: 'Услуга',
        };
        return typeMap[type] || type;
      },
    },
    {
      title: 'Группа',
      dataIndex: 'groupName',
      key: 'groupName',
      width: 200,
    },
    {
      title: 'Действия',
      key: 'actions',
      width: 150,
      render: (_: unknown, record: ItemSearchResultDto) => (
        <Space>
          <Button
            type="primary"
            size="small"
            icon={<FolderOpenOutlined />}
            onClick={() => handleOpenBom(record.id)}
          >
            Открыть BOM
          </Button>
        </Space>
      ),
    },
  ];

  return (
    <div style={{ padding: 24 }}>
      <Card>
        <Space direction="vertical" size="large" style={{ width: '100%' }}>
          <div>
            <Title level={2}>Инженерный модуль</Title>
            <Text type="secondary">
              Поиск изделий и управление спецификациями (eBOM)
            </Text>
          </div>

          <Search
            placeholder="Поиск изделия по коду или наименованию (минимум 2 символа)..."
            prefix={<SearchOutlined />}
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            allowClear
            size="large"
            style={{ maxWidth: 600 }}
          />

          {isLoading ? (
            <div style={{ textAlign: 'center', padding: 40 }}>
              <Spin size="large" />
              <div style={{ marginTop: 16 }}>
                <Text type="secondary">Загрузка...</Text>
              </div>
            </div>
          ) : searchTerm.length < 2 ? (
            <Empty
              description="Введите минимум 2 символа для поиска изделий"
              style={{ padding: 40 }}
            />
          ) : !items || items.length === 0 ? (
            <Empty
              description="Изделия не найдены"
              style={{ padding: 40 }}
            />
          ) : (
            <Table
              columns={columns}
              dataSource={items}
              rowKey="id"
              pagination={{
                pageSize: 20,
                showSizeChanger: true,
                showTotal: (total) => `Всего: ${total}`,
              }}
            />
          )}
        </Space>
      </Card>
    </div>
  );
};