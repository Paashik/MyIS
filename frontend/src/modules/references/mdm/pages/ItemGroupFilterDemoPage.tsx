import React, { useState } from "react";
import { Card, Typography, Alert } from "antd";
import { ItemGroupTreeFilter } from "../components/ItemGroupTreeFilter";
import { t } from "../../../../core/i18n/t";

const { Title, Text } = Typography;

export const ItemGroupFilterDemoPage: React.FC = () => {
  const [selectedGroupId, setSelectedGroupId] = useState<string | null>(null);
  const [selectedGroupName, setSelectedGroupName] = useState<string | null>(null);

  const handleGroupSelect = (groupId: string | null, groupName: string | null) => {
    setSelectedGroupId(groupId);
    setSelectedGroupName(groupName);
  };

  return (
    <div style={{ padding: 24 }}>
      <Card>
        <Title level={3}>{t("references.mdm.itemGroups.title")}</Title>
        
        <div style={{ marginBottom: 16 }}>
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
            style={{ marginBottom: 16 }}
          />
        )}

        <ItemGroupTreeFilter
          onGroupSelect={handleGroupSelect}
          selectedGroupId={selectedGroupId}
          placeholder="Поиск групп номенклатуры..."
        />

        <div style={{ marginTop: 16 }}>
          <Text type="secondary">
            Этот компонент демонстрирует использование ItemGroupTreeFilter для фильтрации номенклатуры по группам.
            Выбранная группа может быть использована для фильтрации списка номенклатуры.
          </Text>
        </div>
      </Card>
    </div>
  );
};