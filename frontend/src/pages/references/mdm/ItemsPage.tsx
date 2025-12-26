import React, { useState } from 'react';
import { Typography, Divider, Row, Col } from 'antd';
import { ItemGroupTreeFilter } from '../../../modules/references/mdm/components/ItemGroupTreeFilter';
import { ItemList } from '../../../modules/references/mdm/components/ItemList';
import { ItemDetails } from '../../../modules/references/mdm/components/ItemDetails';
import { ItemListItemDto } from '../../../core/api/mdmReferencesQueryService';
import { t } from '../../../core/i18n/t';

const { Title } = Typography;

export const ItemsPage: React.FC = () => {
  const [selectedGroupId, setSelectedGroupId] = useState<string | null>(null);
  const [selectedItemId, setSelectedItemId] = useState<string | null>(null);

  const handleGroupSelect = (groupId: string | null, groupName: string | null) => {
    setSelectedGroupId(groupId);
    setSelectedItemId(null); // Сбрасываем выбранный item при смене группы
  };

  const handleItemSelect = (item: ItemListItemDto) => {
    setSelectedItemId(item.id);
  };

  return (
    <div style={{ padding: 24 }}>
      <Title level={2} style={{ marginBottom: 16 }}>
        {t("references.mdm.items.title")}
      </Title>
      <Divider />
      <Row gutter={24} style={{ height: 'calc(100vh - 120px)' }}>
        <Col span={6}>
          <div style={{ background: '#fff', border: '1px solid #d9d9d9', borderRadius: 6, height: '100%', padding: '16px' }}>
            <Title level={4} style={{ marginBottom: 16 }}>
              Группы номенклатуры
            </Title>
            <div style={{ height: 'calc(100% - 60px)', overflow: 'auto' }}>
              <ItemGroupTreeFilter
                onGroupSelect={handleGroupSelect}
                selectedGroupId={selectedGroupId}
                placeholder="Поиск групп..."
              />
            </div>
          </div>
        </Col>
        <Col span={9}>
          <div style={{ background: '#fff', border: '1px solid #d9d9d9', borderRadius: 6, height: '100%', padding: '16px' }}>
            <Title level={4} style={{ marginBottom: 16 }}>
              Номенклатура
            </Title>
            <div style={{ height: 'calc(100% - 60px)', overflow: 'auto' }}>
              <ItemList
                selectedGroupId={selectedGroupId}
                onItemSelect={handleItemSelect}
              />
            </div>
          </div>
        </Col>
        <Col span={9}>
          <div style={{ background: '#fff', border: '1px solid #d9d9d9', borderRadius: 6, height: '100%', padding: '16px' }}>
            <Title level={4} style={{ marginBottom: 16 }}>
              Детали номенклатуры
            </Title>
            <div style={{ height: 'calc(100% - 60px)', overflow: 'auto' }}>
              <ItemDetails
                itemId={selectedItemId}
              />
            </div>
          </div>
        </Col>
      </Row>
    </div>
  );
};
