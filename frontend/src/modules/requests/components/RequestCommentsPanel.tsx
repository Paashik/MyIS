import React, { useState } from "react";
import { List, Typography, Input, Button, Alert, Space } from "antd";
import { RequestCommentDto } from "../api/types";

const { Text } = Typography;
const { TextArea } = Input;

export interface RequestCommentsPanelProps {
  comments: RequestCommentDto[];
  loading: boolean;
  adding: boolean;
  error?: string | null;
  onAddComment: (text: string) => Promise<void> | void;
}

/**
 * Панель комментариев к заявке:
 * - отображает список комментариев;
 * - даёт форму для добавления нового комментария (делегирует сохранение наверх через onAddComment).
 */
export const RequestCommentsPanel: React.FC<RequestCommentsPanelProps> = ({
  comments,
  loading,
  adding,
  error,
  onAddComment,
}) => {
  const [text, setText] = useState("");

  const handleSubmit = async () => {
    const trimmed = text.trim();
    if (!trimmed) {
      return;
    }

    await onAddComment(trimmed);
    setText("");
  };

  const handlePressEnter = (e: any) => {
    if (e.ctrlKey || e.metaKey) {
      e.preventDefault();
      void handleSubmit();
    }
  };

  return (
    <div>
      {error && (
        <Alert
          type="error"
          message="Не удалось загрузить комментарии"
          description={error}
          showIcon
          style={{ marginBottom: 16 }}
        />
      )}

      <List
        loading={loading}
        dataSource={comments}
        locale={{ emptyText: "Комментариев пока нет" }}
        style={{ marginBottom: 16 }}
        renderItem={(item: RequestCommentDto) => {
          const date = new Date(item.createdAt);
          return (
            <List.Item>
              <List.Item.Meta
                title={
                  <span>
                    <Text strong>
                      {item.authorFullName || item.authorId}
                    </Text>{" "}
                    <Text type="secondary">
                      {date.toLocaleDateString()} {date.toLocaleTimeString()}
                    </Text>
                  </span>
                }
                description={item.text}
              />
            </List.Item>
          );
        }}
      />

      <Space direction="vertical" style={{ width: "100%" }}>
        <TextArea
          rows={3}
          placeholder="Добавить комментарий..."
          value={text}
          onChange={(e: any) => setText(e.target.value)}
          onPressEnter={handlePressEnter}
        />
        <div style={{ display: "flex", justifyContent: "flex-end" }}>
          <Button
            type="primary"
            onClick={() => void handleSubmit()}
            loading={adding}
            disabled={!text.trim()}
          >
            Добавить
          </Button>
        </div>
      </Space>
    </div>
  );
};