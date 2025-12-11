import React, { useEffect } from "react";
import { Form, Input, DatePicker, Button, Space } from "antd";
import { RequestTypeDto } from "../api/types";

export interface RequestFormValues {
  requestTypeId: string;
  title: string;
  description?: string;
  dueDate?: string;
  relatedEntityType?: string;
  relatedEntityId?: string;
  externalReferenceId?: string;
}

export interface RequestFormProps {
  mode: "create" | "edit";
  initialValues?: RequestFormValues;
  requestTypes: RequestTypeDto[];
  submitting: boolean;
  onSubmit: (values: RequestFormValues) => Promise<void> | void;
  onCancel?: () => void;
}

/**
 * Форма создания/редактирования заявки.
 *
 * Несёт только UI-валидацию (обязательность полей и базовые проверки формата),
 * без доменной логики статусов или workflow.
 */
export const RequestForm: React.FC<RequestFormProps> = ({
  mode,
  initialValues,
  requestTypes,
  submitting,
  onSubmit,
  onCancel,
}) => {
  const [form] = Form.useForm();

  useEffect(() => {
    if (initialValues) {
      form.setFieldsValue({
        ...initialValues,
        // dueDate оставляем как есть (backend ожидает строку/ISO),
        // преобразование в строку делаем при сабмите.
      });
    }
  }, [initialValues, form]);

  const handleFinish = async (values: any) => {
    const due = values.dueDate;
    let dueDate: string | undefined;

    if (due) {
      if (typeof due === "string") {
        dueDate = due;
      } else if (typeof due === "object" && "toISOString" in due) {
        // dayjs или Date
        try {
          // @ts-ignore
          dueDate = due.toISOString();
        } catch {
          // игнорируем, пусть backend сам разберётся
          dueDate = undefined;
        }
      }
    }

    const payload: RequestFormValues = {
      requestTypeId: values.requestTypeId,
      title: values.title,
      description: values.description || undefined,
      relatedEntityType: values.relatedEntityType || undefined,
      relatedEntityId: values.relatedEntityId || undefined,
      externalReferenceId: values.externalReferenceId || undefined,
      dueDate,
    };

    await onSubmit(payload);
  };

  return (
    <Form
      layout="vertical"
      form={form}
      initialValues={{
        requestTypeId: initialValues?.requestTypeId,
        title: initialValues?.title ?? "",
        description: initialValues?.description ?? "",
        relatedEntityType: initialValues?.relatedEntityType ?? "",
        relatedEntityId: initialValues?.relatedEntityId ?? "",
        externalReferenceId: initialValues?.externalReferenceId ?? "",
      }}
      onFinish={handleFinish}
    >
      <Form.Item
        label="Тип заявки"
        name="requestTypeId"
        rules={[{ required: true, message: "Выберите тип заявки" }]}
      >
        <select style={{ width: "100%", padding: 8 }}>
          <option value="">Выберите тип</option>
          {requestTypes.map((t) => (
            <option key={t.id} value={t.id}>
              {t.code} — {t.name}
            </option>
          ))}
        </select>
      </Form.Item>

      <Form.Item
        label="Заголовок"
        name="title"
        rules={[{ required: true, message: "Введите заголовок" }]}
      >
        <Input />
      </Form.Item>

      <Form.Item label="Описание" name="description">
        <Input.TextArea rows={4} />
      </Form.Item>

      <Form.Item label="Срок" name="dueDate">
        <DatePicker style={{ width: "100%" }} />
      </Form.Item>

      <Form.Item label="Связанный объект — тип" name="relatedEntityType">
        <Input />
      </Form.Item>

      <Form.Item label="Связанный объект — идентификатор" name="relatedEntityId">
        <Input />
      </Form.Item>

      <Form.Item label="Внешний идентификатор" name="externalReferenceId">
        <Input />
      </Form.Item>

      <Form.Item>
        <Space style={{ display: "flex", justifyContent: "flex-end" }}>
          {onCancel && (
            <Button htmlType="button" onClick={onCancel}>
              Отмена
            </Button>
          )}
          <Button type="primary" htmlType="submit" loading={submitting}>
            {mode === "create" ? "Создать" : "Сохранить"}
          </Button>
        </Space>
      </Form.Item>
    </Form>
  );
};