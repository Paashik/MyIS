import React from "react";
import { Alert, Button, Modal, Space, Typography } from "antd";
import { ExclamationCircleOutlined } from "@ant-design/icons";

interface ImportBomModalProps {
  bomVersionId: string;
  visible: boolean;
  onCancel: () => void;
  onSuccess: () => void;
}

/**
 * MVP: импорт BOM из Component2020 отключён, т.к. эндпоинт не входит в утверждённый список контрактов.
 * Оставляем модалку как заглушку “MVP позже”, без сетевых вызовов.
 */
export const ImportBomModal: React.FC<ImportBomModalProps> = ({ visible, onCancel }) => {
  return (
    <Modal
      title={
        <Space>
          <ExclamationCircleOutlined style={{ color: "#1890ff" }} />
          Импорт BOM из Component2020
        </Space>
      }
      open={visible}
      onCancel={onCancel}
      footer={null}
      width={520}
    >
      <Alert
        message="MVP позже"
        description={
          <div>
            <Typography.Paragraph style={{ marginBottom: 0 }}>
              Импорт из Component2020 будет добавлен в следующих итерациях. В текущем MVP кнопка
              оставлена для сохранения UX-структуры, но функциональность отключена.
            </Typography.Paragraph>
          </div>
        }
        type="info"
        showIcon
        style={{ marginBottom: 16 }}
      />

      <div style={{ display: "flex", justifyContent: "flex-end" }}>
        <Space>
          <Button onClick={onCancel}>Закрыть</Button>
          <Button type="primary" disabled>
            Начать импорт
          </Button>
        </Space>
      </div>
    </Modal>
  );
};