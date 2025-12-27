import React, { useState, useEffect } from 'react';
import { Table, Input, Button, Space, Card, Select } from 'antd';
import { UserOutlined } from '@ant-design/icons';
import Pagination from 'antd/es/pagination';
import { useNavigate } from 'react-router-dom';
import { useMdmReferencesQueryService, ItemListItemDto } from '../../../../core/api/mdmReferencesQueryService';

const { Search } = Input;
const { Option } = Select;

interface ItemListProps {
  selectedGroupId?: string | null;
  onItemSelect?: (item: ItemListItemDto) => void;
}

export const ItemList: React.FC<ItemListProps> = ({ selectedGroupId, onItemSelect }) => {
  const [items, setItems] = useState<ItemListItemDto[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [searchText, setSearchText] = useState<string>('');
  const [currentPage, setCurrentPage] = useState<number>(1);
  const [pageSize, setPageSize] = useState<number>(10);
  const [totalItems, setTotalItems] = useState<number>(0);
  const mdmReferencesQueryService = useMdmReferencesQueryService();
  const navigate = useNavigate();

  useEffect(() => {
    fetchItems();
  }, [selectedGroupId, searchText, currentPage, pageSize]);

  const fetchItems = async () => {
    setLoading(true);
    try {
      const trimmedSearch = searchText.trim();
      const result = await mdmReferencesQueryService.getItems({
        groupId: trimmedSearch.length > 0 ? null : selectedGroupId,
        searchText: trimmedSearch,
        pageNumber: currentPage,
        pageSize: pageSize,
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

  const handlePageChange = (page: number, pageSize?: number) => {
    setCurrentPage(page);
    if (pageSize) {
      setPageSize(pageSize);
    }
  };

  const handleRowClick = (record: ItemListItemDto) => {
    if (onItemSelect) {
      onItemSelect(record);
    }
  };

  const handleRowDoubleClick = (record: ItemListItemDto) => {
    navigate(`/references/mdm/items/${record.id}`);
  };

  const columns = [
    {
      title: 'Код',
      dataIndex: 'code',
      key: 'code',
    },
    {
      title: 'Номенклатурный номер',
      dataIndex: 'nomenclatureNo',
      key: 'nomenclatureNo',
    },
    {
      title: 'Название',
      dataIndex: 'name',
      key: 'name',
    },
    {
      title: 'Обозначение',
      dataIndex: 'designation',
      key: 'designation',
    },
    {
      title: 'Единица измерения',
      dataIndex: 'unitOfMeasureName',
      key: 'unitOfMeasureName',
    },
    {
      title: 'Группа',
      dataIndex: 'itemGroupName',
      key: 'itemGroupName',
    },
  ];

  return (
    <Card title="Список номенклатуры" loading={loading}>
      <Space direction="vertical" size="middle" style={{ width: '100%' }}>
        <Search
          placeholder="Поиск по коду или названию"
          allowClear
          enterButton={<Button icon={<UserOutlined />} />}
          onSearch={handleSearch}
          style={{ width: '100%' }}
        />
        <Table
          columns={columns}
          dataSource={items}
          rowKey="id"
          onRow={(record: ItemListItemDto) => ({
            onClick: () => handleRowClick(record),
            onDoubleClick: () => handleRowDoubleClick(record),
          })}
          pagination={false}
        />
        <Pagination
          current={currentPage}
          pageSize={pageSize}
          total={totalItems}
          onChange={handlePageChange}
          showSizeChanger
          showQuickJumper
        />
      </Space>
    </Card>
  );
};
