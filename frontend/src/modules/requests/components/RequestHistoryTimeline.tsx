import React from "react";
import { List, Typography } from "antd";
import { RequestHistoryItemDto } from "../api/types";

const { Text } = Typography;

export interface RequestHistoryTimelineProps {
  items: RequestHistoryItemDto[];
  loading: boolean;
}

/**
 * Простое отображение истории изменений заявки в виде списка.
 * Сортировка по времени выполняется на уровне UI и не вносит бизнес-логики.
 */
export const RequestHistoryTimeline: React.FC<RequestHistoryTimelineProps> = ({
  items,
  loading,
}) => {
  const sorted = [...items].sort(
    (a, b) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime()
  );

  return (
    <List
      loading={loading}
      dataSource={sorted}
      renderItem={(item: RequestHistoryItemDto) => {
        const date = new Date(item.timestamp);
        return (
          <List.Item>
            <List.Item.Meta
              title={
                <span>
                  <Text strong>{item.action}</Text>{" "}
                  <Text type="secondary">
                    {date.toLocaleDateString()} {date.toLocaleTimeString()}
                  </Text>
                </span>
              }
              description={
                <div>
                  <div>
                    <Text type="secondary">Пользователь:</Text>{" "}
                    {item.performedByFullName || item.performedBy}
                  </div>
                  <div>
                    <Text type="secondary">Было:</Text> {item.oldValue}
                  </div>
                  <div>
                    <Text type="secondary">Стало:</Text> {item.newValue}
                  </div>
                  {item.comment && (
                    <div>
                      <Text type="secondary">Комментарий:</Text> {item.comment}
                    </div>
                  )}
                </div>
              }
            />
          </List.Item>
        );
      }}
    />
  );
};