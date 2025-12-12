import React, { useEffect, useMemo, useState } from "react";
import { Form, Input, DatePicker, Button, Space } from "antd";
import type { RequestLineInputDto, RequestTypeDto } from "../api/types";
import { RequestBodyRenderer } from "./RequestBodyRenderer";
import { getRequestTypeProfile } from "../typeProfiles/registry";
import { t } from "../../../core/i18n/t";

export interface RequestFormValues {
  requestTypeId: string;
  requestTypeCode?: string;
  title: string;
  description?: string;
  lines?: RequestLineInputDto[];
  dueDate?: string;
  relatedEntityType?: string;
  relatedEntityId?: string;
  externalReferenceId?: string;
}

export interface RequestFormProps {
  mode: "create" | "edit";
  initialValues?: RequestFormValues;
  requestTypeCode?: string;
  requestTypes: RequestTypeDto[];
  /**
   * Фиксированный тип для режима создания (Iteration 3.2).
   * Если задан — выбор типа не показывается, значение берётся из этого поля.
   */
  fixedRequestTypeId?: string;
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
  requestTypeCode,
  requestTypes,
  fixedRequestTypeId,
  submitting,
  onSubmit,
  onCancel,
}) => {
  const [form] = Form.useForm();

  const isGuid = (value: string): boolean => {
    // Accept canonical GUID format: 8-4-4-4-12 hex.
    // Backend expects Guid/Guid? in JSON, so any other value will be rejected with 400.
    return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value);
  };

  const [selectedTypeId, setSelectedTypeId] = useState<string | undefined>(
    initialValues?.requestTypeId
  );

  const selectedTypeCode = useMemo(() => {
    if (mode === "edit" && requestTypeCode) {
      return requestTypeCode;
    }

    if (!selectedTypeId) {
      return undefined;
    }

    const type = requestTypes.find((t) => t.id === selectedTypeId);
    return type?.code;
  }, [mode, requestTypeCode, requestTypes, selectedTypeId]);

  useEffect(() => {
    if (initialValues) {
      form.setFieldsValue({
        ...initialValues,
        // dueDate оставляем как есть (backend ожидает строку/ISO),
        // преобразование в строку делаем при сабмите.
      });

      setSelectedTypeId(initialValues.requestTypeId);
    }
  }, [initialValues, form]);

  // Режим создания с фиксированным типом (из контекста списка/URL)
  useEffect(() => {
    if (mode !== "create") return;
    if (!fixedRequestTypeId) return;

    setSelectedTypeId(fixedRequestTypeId);
    form.setFieldValue("requestTypeId", fixedRequestTypeId);
  }, [fixedRequestTypeId, form, mode]);

  const handleFinish = async (values: any) => {
    const toIso = (v: any): string | undefined => {
      if (!v) return undefined;
      if (typeof v === "string") return v;
      if (typeof v === "object" && "toISOString" in v) {
        try {
          // dayjs or Date
          // @ts-ignore
          return v.toISOString();
        } catch {
          return undefined;
        }
      }
      return undefined;
    };

    const due = values.dueDate;
    const dueDate = toIso(due);

    const typeCode = selectedTypeCode;
    if (!typeCode) {
      // тип не выбран — это поймают rules required на поле типа
      return;
    }

    const rawLines: any[] | undefined = Array.isArray(values.lines) ? values.lines : undefined;
    const lines: RequestLineInputDto[] | undefined = rawLines
      ? rawLines.map((l, idx) => ({
          lineNo: idx + 1,
          description: l?.description || undefined,
          quantity: typeof l?.quantity === "number" ? l.quantity : Number(l?.quantity ?? 0),
          needByDate: toIso(l?.needByDate),
          supplierName: l?.supplierName || undefined,
          supplierContact: l?.supplierContact || undefined,
        }))
      : undefined;

    const profile = getRequestTypeProfile(typeCode);
    if (profile.validate) {
      const errors = profile.validate({
        requestTypeCode: typeCode,
        description: values.description || "",
        lines,
      });

      if (errors.length > 0) {
        // Группируем ошибки по полю и пробрасываем в AntD Form
        const byKey = new Map<string, { name: (string | number)[]; errors: string[] }>();
        for (const e of errors) {
          const key = e.path.join(".");
          const existing = byKey.get(key);
          if (existing) {
            existing.errors.push(e.message);
          } else {
            byKey.set(key, { name: e.path, errors: [e.message] });
          }
        }
        form.setFields(Array.from(byKey.values()));
        return;
      }
    }

    const payload: RequestFormValues = {
      requestTypeId: values.requestTypeId,
      requestTypeCode: typeCode,
      title: values.title,
      description: values.description || undefined,
      lines,
      relatedEntityType:
        typeof values.relatedEntityType === "string" && values.relatedEntityType.trim()
          ? values.relatedEntityType.trim()
          : undefined,
      relatedEntityId:
        typeof values.relatedEntityId === "string" && values.relatedEntityId.trim()
          ? values.relatedEntityId.trim()
          : undefined,
      externalReferenceId:
        typeof values.externalReferenceId === "string" && values.externalReferenceId.trim()
          ? values.externalReferenceId.trim()
          : undefined,
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
        requestTypeId: fixedRequestTypeId ?? initialValues?.requestTypeId,
        title: initialValues?.title ?? "",
        description: initialValues?.description ?? "",
        relatedEntityType: initialValues?.relatedEntityType ?? "",
        relatedEntityId: initialValues?.relatedEntityId ?? "",
        externalReferenceId: initialValues?.externalReferenceId ?? "",
      }}
      onFinish={handleFinish}
    >
      {mode === "create" && fixedRequestTypeId ? (
        <Form.Item
          name="requestTypeId"
          hidden
          rules={[{ required: true, message: t("requests.form.type.required") }]}
        >
          <Input />
        </Form.Item>
      ) : (
        <Form.Item
          label={t("requests.form.type.label")}
          name="requestTypeId"
          rules={[{ required: true, message: t("requests.form.type.required") }]}
        >
          <select
            data-testid="request-form-type-select"
            style={{ width: "100%", padding: 8 }}
            disabled={mode === "edit"}
            onChange={(e) => {
              const next = e.target.value;
              setSelectedTypeId(next || undefined);
              // синхронизируем с antd form
              form.setFieldValue("requestTypeId", next);
            }}
          >
            <option value="">{t("requests.form.type.placeholder")}</option>
            {requestTypes.map((t) => (
              <option key={t.id} value={t.id}>
                {t.code} — {t.name}
              </option>
            ))}
          </select>
        </Form.Item>
      )}

      <Form.Item
        label={t("requests.form.title.label")}
        name="title"
        rules={[{ required: true, message: t("requests.form.title.required") }]}
      >
        <Input data-testid="request-form-title-input" />
      </Form.Item>

      <RequestBodyRenderer
        mode="edit"
        requestTypeCode={selectedTypeCode}
        form={form}
        editMode={mode}
      />

      <Form.Item label={t("requests.form.dueDate.label")} name="dueDate">
        <DatePicker data-testid="request-form-due-date-input" style={{ width: "100%" }} />
      </Form.Item>

      <Form.Item label={t("requests.form.relatedType.label")} name="relatedEntityType">
        <Input data-testid="request-form-related-type-input" />
      </Form.Item>

      <Form.Item
        label={t("requests.form.relatedId.label")}
        name="relatedEntityId"
        rules={[
          {
            validator: async (_rule: any, value: any) => {
              if (value === null || value === undefined) return;
              if (typeof value !== "string") return;

              const trimmed = value.trim();
              if (!trimmed) return;

              if (!isGuid(trimmed)) {
                throw new Error(t("requests.form.relatedId.invalidGuid"));
              }
            },
          },
        ]}
      >
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
