import React, { useEffect } from "react";
import { Form, Input, DatePicker, Button, Space } from "antd";
import { RequestTypeDto } from "../api/types";
import { t } from "../../../core/i18n/t";

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
      data-testid="request-form"
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
        label={t("requests.form.type.label")}
        name="requestTypeId"
        rules={[{ required: true, message: t("requests.form.type.required") }]}
      >
        <select
          data-testid="request-form-type-select"
          style={{ width: "100%", padding: 8 }}
        >
          <option value="">{t("requests.form.type.placeholder")}</option>
          {requestTypes.map((t) => (
            <option key={t.id} value={t.id}>
              {t.code} — {t.name}
            </option>
          ))}
        </select>
      </Form.Item>

      <Form.Item
        label={t("requests.form.title.label")}
        name="title"
        rules={[{ required: true, message: t("requests.form.title.required") }]}
      >
        <Input data-testid="request-form-title-input" />
      </Form.Item>

      <Form.Item label={t("requests.form.description.label")} name="description">
        <Input.TextArea data-testid="request-form-description-input" rows={4} />
      </Form.Item>

      <Form.Item label={t("requests.form.dueDate.label")} name="dueDate">
        <DatePicker data-testid="request-form-due-date-input" style={{ width: "100%" }} />
      </Form.Item>

      <Form.Item label={t("requests.form.relatedType.label")} name="relatedEntityType">
        <Input data-testid="request-form-related-type-input" />
      </Form.Item>

      <Form.Item label={t("requests.form.relatedId.label")} name="relatedEntityId">
        <Input data-testid="request-form-related-id-input" />
      </Form.Item>

      <Form.Item label={t("requests.form.externalId.label")} name="externalReferenceId">
        <Input data-testid="request-form-external-id-input" />
      </Form.Item>

      <Form.Item>
        <Space style={{ display: "flex", justifyContent: "flex-end" }}>
          {onCancel && (
            <Button
              data-testid="request-form-cancel-button"
              htmlType="button"
              onClick={onCancel}
            >
              {t("common.actions.cancel")}
            </Button>
          )}
          <Button
            data-testid="request-form-submit-button"
            type="primary"
            htmlType="submit"
            loading={submitting}
          >
            {mode === "create" ? t("common.actions.create") : t("common.actions.save")}
          </Button>
        </Space>
      </Form.Item>
    </Form>
  );
};
