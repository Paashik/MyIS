import React, { useState } from 'react';
import { Typography, Divider, Row, Col } from 'antd';
import { ItemGroupTreeFilter } from '../../../modules/references/mdm/components/ItemGroupTreeFilter';
import { ItemList } from '../../../modules/references/mdm/components/ItemList';
import { ItemDetails } from '../../../modules/references/mdm/components/ItemDetails';
import { ItemListItemDto } from '../../../core/api/mdmReferencesQueryService';
import { t } from '../../../core/i18n/t';
import "./ItemsPage.css";

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
    if (item.itemGroupId) {
      setSelectedGroupId(item.itemGroupId);
    }
  };

  return (
    <div className="items-page">
      <Title level={2} className="items-page__title">
        {t("references.mdm.items.title")}
      </Title>
      <Divider />
      <Row gutter={24} className="items-page__row">
        <Col span={6}>
          <div className="items-page__panel">
            <Title level={4} className="items-page__panel-title">
              Группы номенклатуры
            </Title>
            <div className="items-page__panel-content">
              <ItemGroupTreeFilter
                onGroupSelect={handleGroupSelect}
                selectedGroupId={selectedGroupId}
                placeholder="Поиск групп..."
              />
            </div>
          </div>
        </Col>
        <Col span={9}>
          <div className="items-page__panel">
            <Title level={4} className="items-page__panel-title">
              Номенклатура
            </Title>
            <div className="items-page__panel-content">
              <ItemList
                selectedGroupId={selectedGroupId}
                onItemSelect={handleItemSelect}
              />
            </div>
          </div>
        </Col>
        <Col span={9}>
          <div className="items-page__panel">
            <Title level={4} className="items-page__panel-title">
              Детали номенклатуры
            </Title>
            <div className="items-page__panel-content">
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
