import React, { useState, useCallback } from 'react';
import { Modal, Input, Table, Spin, Empty, Tag } from 'antd';
import { SearchOutlined } from '@ant-design/icons';

import { useItemSearch } from '../../api/hooks';
import { ItemSearchResultDto, ItemType } from '../../api/types';

interface ItemSelectModalProps {
  open: boolean;
  onSelect: (item: ItemSearchResultDto) => void;
  onCancel: () => void;
}

const itemTypeLabels: Record<ItemType, string> = {
  Component: 'Компонент',
  Material: 'Материал',
  Assembly: 'Сборка',
  Product: 'Изделие',
  Service: 'Услуга',
};

const itemTypeColors: Record<ItemType, string> = {
  Component: 'blue',
  Material: 'orange',
  Assembly: 'green',
  Product: 'purple',
  Service: 'cyan',
};

export const ItemSelectModal: React.FC<ItemSelectModalProps> = ({
  open,
  onSelect,
  onCancel,
}) => {
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedItem, setSelectedItem] = useState<ItemSearchResultDto | null>(
    null
  );

  const { data: items, isLoading } = useItemSearch(searchQuery, open);

  const handleSearch = useCallback((value: string) => {
    setSearchQuery(value);
    setSelectedItem(null);
  }, []);

  const handleRowClick = useCallback((record: ItemSearchResultDto) => {
    setSelectedItem(record);
  }, []);

  const handleOk = useCallback(() => {
    if (selectedItem) {
      onSelect(selectedItem);
      setSearchQuery('');
      setSelectedItem(null);
    }
  }, [selectedItem, onSelect]);

  const handleCancel = useCallback(() => {
    setSearchQuery('');
    setSelectedItem(null);
    onCancel();
  }, [onCancel]);

  const columns = [
    {
      title: 'Код',
      dataIndex: 'code',
      key: 'code',
      width: 120,
      render: (value: string | null) => value || '—',
    },
    {
      title: 'Наименование',
      dataIndex: 'name',
      key: 'name',
      ellipsis: true,
    },
    {
      title: 'Тип',
      dataIndex: 'itemType',
      key: 'itemType',
      width: 100,
      render: (value: ItemType) => (
        <Tag color={itemTypeColors[value]}>{itemTypeLabels[value]}</Tag>
      ),
    },
    {
      title: 'Группа',
      dataIndex: 'groupName',
      key: 'groupName',
      width: 150,
      ellipsis: true,
      render: (value: string | null) => value || '—',
    },
    {
      title: 'Активен',
      dataIndex: 'isActive',
      key: 'isActive',
      width: 80,
      render: (value: boolean) => (
        <Tag color={value ? 'success' : 'default'}>
          {value ? 'Да' : 'Нет'}
        </Tag>
      ),
    },
  ];

  return (
    <Modal
      title="Выбор номенклатуры"
      open={open}
      onOk={handleOk}
      onCancel={handleCancel}
      okText="Выбрать"
      cancelText="Отмена"
      okButtonProps={{ disabled: !selectedItem }}
      width={800}
      styles={{ body: { padding: '16px 0' } }}
    >
      <div style={{ marginBottom: 16, padding: '0 24px' }}>
        <Input
          placeholder="Введите код или наименование для поиска..."
          prefix={<SearchOutlined />}
          value={searchQuery}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
            handleSearch(e.target.value)
          }
          allowClear
          autoFocus
        />
        {searchQuery.length > 0 && searchQuery.length < 2 && (
          <div style={{ marginTop: 8, color: '#8c8c8c', fontSize: 12 }}>
            Введите минимум 2 символа для поиска
          </div>
        )}
      </div>

      <div style={{ minHeight: 300 }}>
        {isLoading ? (
          <div
            style={{
              display: 'flex',
              justifyContent: 'center',
              alignItems: 'center',
              height: 200,
            }}
          >
            <Spin />
          </div>
        ) : !items || items.length === 0 ? (
          <Empty
            description={
              searchQuery.length >= 2
                ? 'Ничего не найдено'
                : 'Начните вводить для поиска'
            }
          />
        ) : (
          <Table
            dataSource={items}
            columns={columns}
            rowKey="id"
            size="small"
            pagination={{ pageSize: 10, showSizeChanger: false }}
            scroll={{ y: 300 }}
            rowClassName={(record: ItemSearchResultDto) =>
              record.id === selectedItem?.id ? 'ant-table-row-selected' : ''
            }
            onRow={(record: ItemSearchResultDto) => ({
              onClick: () => handleRowClick(record),
              onDoubleClick: () => {
                setSelectedItem(record);
                onSelect(record);
              },
            })}
          />
        )}
      </div>
    </Modal>
  );
};
