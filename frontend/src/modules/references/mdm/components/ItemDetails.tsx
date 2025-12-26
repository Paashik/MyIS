import React, { useEffect, useState } from 'react';
import { Card, Descriptions, Spin } from 'antd';
import Empty from 'antd/es/empty';

interface ItemDetailsDto {
  id: string;
  code: string;
  nomenclatureNo: string;
  name: string;
  designation?: string;
  itemKind: string;
  isEskd: boolean;
  isEskdDocument?: boolean;
  manufacturerPartNumber?: string;
  isActive: boolean;
  unitOfMeasureName?: string;
  itemGroupName?: string;
}

interface ItemDetailsProps {
  itemId: string | null;
}

export const ItemDetails: React.FC<ItemDetailsProps> = ({ itemId }) => {
  const [loading, setLoading] = useState(false);
  const [item, setItem] = useState<ItemDetailsDto | null>(null);

  useEffect(() => {
    if (itemId) {
      fetchItemDetails(itemId);
    } else {
      setItem(null);
    }
  }, [itemId]);

  const fetchItemDetails = async (id: string) => {
    setLoading(true);
    try {
      const response = await fetch(`/api/admin/references/mdm/items/${id}`);
      if (!response.ok) {
        throw new Error('Failed to fetch item details');
      }
      const data = await response.json();
      setItem(data);
    } catch (error) {
      console.error('Failed to fetch item details:', error);
      setItem(null);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <Card title="Детали номенклатуры" style={{ height: '100%' }}>
        <div style={{ textAlign: 'center', padding: '24px 0' }}>
          <Spin size="large" />
        </div>
      </Card>
    );
  }

  if (!item) {
    return (
      <Card title="Детали номенклатуры" style={{ height: '100%' }}>
        <Empty description="Выберите номенклатуру для просмотра деталей" />
      </Card>
    );
  }

  return (
    <Card title="Детали номенклатуры" style={{ height: '100%' }}>
      <Descriptions column={1} bordered>
        <Descriptions.Item label="Код">{item.code}</Descriptions.Item>
        <Descriptions.Item label="Номенклатурный номер">{item.nomenclatureNo}</Descriptions.Item>
        <Descriptions.Item label="Наименование">{item.name}</Descriptions.Item>
        <Descriptions.Item label="Вид номенклатуры">{item.itemKind}</Descriptions.Item>
        {item.unitOfMeasureName && (
          <Descriptions.Item label="Единица измерения">{item.unitOfMeasureName}</Descriptions.Item>
        )}
        {item.designation && (
          <Descriptions.Item label="Обозначение">{item.designation}</Descriptions.Item>
        )}
        {item.manufacturerPartNumber && (
          <Descriptions.Item label="Заводской номер">{item.manufacturerPartNumber}</Descriptions.Item>
        )}
        {item.itemGroupName && (
          <Descriptions.Item label="Группа">{item.itemGroupName}</Descriptions.Item>
        )}
        <Descriptions.Item label="ЕСКД">
          {item.isEskd ? (item.isEskdDocument ? 'Документ' : 'Да') : 'Нет'}
        </Descriptions.Item>
        <Descriptions.Item label="Активен">{item.isActive ? 'Да' : 'Нет'}</Descriptions.Item>
      </Descriptions>
    </Card>
  );
};