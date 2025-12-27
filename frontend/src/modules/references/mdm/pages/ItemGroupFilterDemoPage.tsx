import React, { useState } from "react";
import { Card, Typography, Alert } from "antd";
import { ItemGroupTreeFilter } from "../components/ItemGroupTreeFilter";
import { t } from "../../../../core/i18n/t";
import "./ItemGroupFilterDemoPage.css";

const { Title, Text } = Typography;

export const ItemGroupFilterDemoPage: React.FC = () => {
  const [selectedGroupId, setSelectedGroupId] = useState<string | null>(null);
  const [selectedGroupName, setSelectedGroupName] = useState<string | null>(null);

  const handleGroupSelect = (groupId: string | null, groupName: string | null) => {
    setSelectedGroupId(groupId);
    setSelectedGroupName(groupName);
  };

  return (
    <div className="item-group-filter-demo">
      <Card>
        <Title level={3}>{t("references.mdm.itemGroups.title")}</Title>
        
        <div className="item-group-filter-demo__selected">
          <Text strong>Выбранная группа: </Text>
          {selectedGroupId ? (
            <Text code>{selectedGroupName} (ID: {selectedGroupId})</Text>
          ) : (
            <Text type="secondary">Не выбрано</Text>
          )}
        </div>

        {selectedGroupId && (
          <Alert
            type="info"
            message={`Фильтрация по группе: ${selectedGroupName}`}
            description={`ID группы: ${selectedGroupId}`}
            className="item-group-filter-demo__alert"
          />
        )}

        <ItemGroupTreeFilter
          onGroupSelect={handleGroupSelect}
          selectedGroupId={selectedGroupId}
          placeholder="Поиск групп номенклатуры..."
        />

        <div className="item-group-filter-demo__note">
          <Text type="secondary">
            Этот компонент демонстрирует использование ItemGroupTreeFilter для фильтрации номенклатуры по группам.
            Выбранная группа может быть использована для фильтрации списка номенклатуры.
          </Text>
        </div>
      </Card>
    </div>
  );
};
