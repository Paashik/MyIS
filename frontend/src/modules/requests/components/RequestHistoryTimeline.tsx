import React from "react";
import { List, Typography } from "antd";
import { RequestHistoryItemDto } from "../api/types";
import { t } from "../../../core/i18n/t";

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
      data-testid="request-history-list"
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
                    <Text type="secondary">{t("requests.history.field.user")}</Text>{" "}
                    {item.performedByFullName || item.performedBy}
                  </div>
                  <div>
                    <Text type="secondary">{t("requests.history.field.was")}</Text> {item.oldValue}
                  </div>
                  <div>
                    <Text type="secondary">{t("requests.history.field.became")}</Text> {item.newValue}
                  </div>
                  {item.comment && (
                    <div>
                      <Text type="secondary">{t("requests.history.field.comment")}</Text> {item.comment}
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
