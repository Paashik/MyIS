import React, { useEffect, useMemo, useState } from "react";
import { Button, DatePicker, Form, Input, Select, Space, Tabs, Typography } from "antd";
import type { FormInstance } from "rc-field-form";

import type { RequestLineInputDto, RequestTypeDto } from "../api/types";
import { RequestBodyRenderer } from "./RequestBodyRenderer";
import { SupplyLinesEditor } from "./SupplyLinesEditor";
import { getRequestTypeProfile } from "../typeProfiles/registry";
import { t } from "../../../core/i18n/t";

const { Text } = Typography;

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
  form?: FormInstance;
  showActions?: boolean;
  /**
   * Fixed type for create (from list context / URL).
   * If set — type selector is hidden, and the value is taken from this field.
   */
  fixedRequestTypeId?: string;
  submitting: boolean;
  onSubmit: (values: RequestFormValues) => Promise<void> | void;
  onCancel?: () => void;
}

export const RequestForm: React.FC<RequestFormProps> = ({
  mode,
  initialValues,
  requestTypeCode,
  requestTypes,
  form: externalForm,
  showActions = true,
  fixedRequestTypeId,
  submitting,
  onSubmit,
  onCancel,
}) => {
  const [innerForm] = Form.useForm();
  const form = (externalForm ?? innerForm) as any;

  const isGuid = (value: string): boolean => {
    return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(
      value
    );
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
    if (!initialValues) return;

    form.setFieldsValue({
      ...initialValues,
      lines: initialValues.lines ?? [],
    });
    setSelectedTypeId(initialValues.requestTypeId);
  }, [initialValues, form]);

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

    const dueDate = toIso(values.dueDate);

    const typeCode = selectedTypeCode;
    if (!typeCode) {
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
        lines: initialValues?.lines ?? [],
        relatedEntityType: initialValues?.relatedEntityType ?? "",
        relatedEntityId: initialValues?.relatedEntityId ?? "",
        externalReferenceId: initialValues?.externalReferenceId ?? "",
      }}
      onFinish={handleFinish}
    >
      <Tabs
        items={[
          {
            key: "general",
            label: t("requests.card.tabs.general"),
            children: (
              <>
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
                    <Select
                      data-testid="request-form-type-select"
                      disabled={mode === "edit"}
                      showSearch
                      optionFilterProp="label"
                      options={requestTypes.map((t) => ({
                        value: t.id,
                        label: `${t.code} — ${t.name}`,
                      }))}
                      onChange={(next: string) => {
                        setSelectedTypeId(next || undefined);
                        form.setFieldValue("requestTypeId", next);
                      }}
                    />
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
                  <DatePicker
                    data-testid="request-form-due-date-input"
                    style={{ width: "100%" }}
                    showTime
                  />
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
              </>
            ),
          },
          {
            key: "composition",
            label: t("requests.card.tabs.composition"),
            children: <SupplyLinesEditor name="lines" />,
          },
          {
            key: "documents",
            label: t("requests.card.tabs.documents"),
            disabled: mode === "create",
            children: <Text type="secondary">{t("requests.card.tabs.documents.afterCreate")}</Text>,
          },
          {
            key: "history",
            label: t("requests.card.tabs.history"),
            disabled: mode === "create",
            children: <Text type="secondary">{t("requests.card.tabs.history.afterCreate")}</Text>,
          },
          {
            key: "tasks",
            label: t("requests.card.tabs.tasks"),
            disabled: mode === "create",
            children: <Text type="secondary">{t("requests.card.tabs.tasks.afterCreate")}</Text>,
          },
          {
            key: "integrations",
            label: t("requests.card.tabs.integrations"),
            disabled: mode === "create",
            children: (
              <Text type="secondary">{t("requests.card.tabs.integrations.afterCreate")}</Text>
            ),
          },
        ]}
      />

      {showActions && (
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
      )}
    </Form>
  );
};
