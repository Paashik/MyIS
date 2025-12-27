import React, { useEffect, useMemo, useState } from "react";
import { Button, DatePicker, Form, Input, Select, Space, Tabs, Typography } from "antd";
import type { FormInstance } from "rc-field-form";
import dayjs from "dayjs";

import type { RequestCounterpartyLookupDto, RequestLineInputDto, RequestTypeDto } from "../api/types";
import { getRequestCounterparties } from "../api/requestsApi";
import { RequestBodyRenderer } from "./RequestBodyRenderer";
import { SupplyLinesEditor } from "./SupplyLinesEditor";
import { getRequestTypeProfile } from "../typeProfiles/registry";
import { t } from "../../../core/i18n/t";

const { Text } = Typography;

export interface RequestFormValues {
  requestTypeId: string;
  title: string;
  description?: string;
  lines?: RequestLineInputDto[];
  dueDate?: string;
  relatedEntityType?: string;
  relatedEntityId?: string;
  relatedEntityName?: string;
  externalReferenceId?: string;
  targetEntityType?: string;
  targetEntityId?: string;
  targetEntityName?: string;
}

export type RequestFormInitialValues =
  Omit<RequestFormValues, "requestTypeId"> & { requestTypeId?: string };

export interface RequestFormProps {
  mode: "create" | "edit";
  initialValues?: RequestFormInitialValues;
  requestTypes: RequestTypeDto[];
  form?: FormInstance;
  showActions?: boolean;
  submitting: boolean;
  onSubmit: (values: RequestFormValues) => Promise<void> | void;
  onCancel?: () => void;
}

export const RequestForm: React.FC<RequestFormProps> = ({
  mode,
  initialValues,
  requestTypes,
  form: externalForm,
  showActions = true,
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
  const [counterpartySearch, setCounterpartySearch] = useState("");
  const [counterpartyLoading, setCounterpartyLoading] = useState(false);
  const [counterpartyOptions, setCounterpartyOptions] = useState<RequestCounterpartyLookupDto[]>([]);


  const selectedTypeIdValue = useMemo(() => {
    if (mode === "edit" && initialValues?.requestTypeId) {
      return initialValues.requestTypeId;
    }

    return selectedTypeId;
  }, [initialValues?.requestTypeId, mode, selectedTypeId]);

  const selectedType = useMemo(
    () => requestTypes.find((t) => t.id === selectedTypeIdValue),
    [requestTypes, selectedTypeIdValue]
  );

  const isOutgoing = selectedType?.direction === "Outgoing";

  useEffect(() => {
    if (!initialValues) return;

    form.setFieldsValue({
      ...initialValues,
      dueDate: initialValues.dueDate ? dayjs(initialValues.dueDate) : undefined,
      lines: initialValues.lines ?? [],
    });
    setSelectedTypeId(initialValues.requestTypeId);
  }, [initialValues, form]);

  useEffect(() => {
    if (!isOutgoing) {
      form.setFieldsValue({
        targetEntityType: undefined,
        targetEntityId: undefined,
        targetEntityName: undefined,
      });
      return;
    }

    const currentType = form.getFieldValue("targetEntityType");
    if (!currentType) {
      form.setFieldsValue({ targetEntityType: "Counterparty" });
    }
  }, [form, isOutgoing]);

  useEffect(() => {
    if (!isOutgoing) return;

    let cancelled = false;

    const load = async () => {
      setCounterpartyLoading(true);
      try {
        const items = await getRequestCounterparties(counterpartySearch.trim() || undefined);
        if (!cancelled) {
          setCounterpartyOptions(items);
        }
      } finally {
        if (!cancelled) {
          setCounterpartyLoading(false);
        }
      }
    };

    void load();

    return () => {
      cancelled = true;
    };
  }, [counterpartySearch, isOutgoing]);

  const handleFinish = async (values: any) => {
    const toIso = (v: any): string | undefined => {
      if (!v) return undefined;
      if (typeof v === "string") return v;
      if (typeof v === "object" && "toISOString" in v) {
        try {
          return v.toISOString();
        } catch {
          return undefined;
        }
      }
      return undefined;
    };

    const dueDate = toIso(values.dueDate);

    const typeId = selectedTypeIdValue ?? values.requestTypeId;
    if (!typeId) {
      return;
    }

    const rawLines: any[] | undefined = Array.isArray(values.lines) ? values.lines : undefined;
    const lines: RequestLineInputDto[] | undefined = rawLines
      ? rawLines.map((l, idx) => ({
          lineNo: idx + 1,
          itemId: l?.itemId || undefined,
          externalItemCode: l?.externalItemCode || undefined,
          description: l?.description || undefined,
          quantity: typeof l?.quantity === "number" ? l.quantity : Number(l?.quantity ?? 0),
          unitOfMeasureId: l?.unitOfMeasureId || undefined,
          needByDate: toIso(l?.needByDate),
          supplierName: l?.supplierName || undefined,
          supplierContact: l?.supplierContact || undefined,
          externalRowReferenceId: l?.externalRowReferenceId || undefined,
        }))
      : undefined;

    const profile = getRequestTypeProfile(typeId);
    if (profile.validate) {
      const errors = profile.validate({
        requestTypeId: typeId,
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
      requestTypeId: typeId,
      title:
        typeof values.title === "string" && values.title.trim()
          ? values.title.trim()
          : "AUTO",
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
      relatedEntityName:
        typeof values.relatedEntityName === "string" && values.relatedEntityName.trim()
          ? values.relatedEntityName.trim()
          : undefined,
      externalReferenceId:
        typeof values.externalReferenceId === "string" && values.externalReferenceId.trim()
          ? values.externalReferenceId.trim()
          : undefined,
      targetEntityType:
        typeof values.targetEntityType === "string" && values.targetEntityType.trim()
          ? values.targetEntityType.trim()
          : undefined,
      targetEntityId:
        typeof values.targetEntityId === "string" && values.targetEntityId.trim()
          ? values.targetEntityId.trim()
          : undefined,
      targetEntityName:
        typeof values.targetEntityName === "string" && values.targetEntityName.trim()
          ? values.targetEntityName.trim()
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
        requestTypeId: initialValues?.requestTypeId,
        title: initialValues?.title ?? "",
        description: initialValues?.description ?? "",
        lines: initialValues?.lines ?? [],
        relatedEntityType: initialValues?.relatedEntityType ?? "",
        relatedEntityId: initialValues?.relatedEntityId ?? "",
        relatedEntityName: initialValues?.relatedEntityName ?? "",
        externalReferenceId: initialValues?.externalReferenceId ?? "",
        targetEntityType: initialValues?.targetEntityType ?? "",
        targetEntityId: initialValues?.targetEntityId ?? "",
        targetEntityName: initialValues?.targetEntityName ?? "",
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
                      label: t.name,
                    }))}
                    onChange={(next: string) => {
                      setSelectedTypeId(next || undefined);
                      form.setFieldValue("requestTypeId", next);
                    }}
                  />
                </Form.Item>

                <Form.Item
                  label={t("requests.form.title.label")}
                  name="title"
                >
                  <Input
                    data-testid="request-form-title-input"
                    disabled
                    placeholder={mode === "create" ? t("requests.form.title.auto") : undefined}
                  />
                </Form.Item>

                <RequestBodyRenderer
                  mode="edit"
                  requestTypeId={selectedTypeIdValue}
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

                <Form.Item label={t("requests.form.relatedName.label")} name="relatedEntityName">
                  <Input data-testid="request-form-related-name-input" />
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

                {isOutgoing && (
                  <>
                    <Form.Item label={t("requests.form.targetType.label")} name="targetEntityType">
                      <Select
                        data-testid="request-form-target-type-select"
                        options={[
                          { value: "Counterparty", label: t("requests.form.targetType.option.counterparty") },
                          { value: "OrgStructure", label: t("requests.form.targetType.option.orgStructure"), disabled: true },
                        ]}
                      />
                    </Form.Item>

                    <Form.Item label={t("requests.form.targetName.label")} name="targetEntityId">
                      <Select
                        data-testid="request-form-target-name-select"
                        showSearch
                        allowClear
                        filterOption={false}
                        loading={counterpartyLoading}
                        onSearch={(value: string) => setCounterpartySearch(value)}
                        onChange={(value: string | null, option: any) => {
                          if (!value) {
                            form.setFieldsValue({
                              targetEntityId: undefined,
                              targetEntityName: undefined,
                            });
                            return;
                          }
                          const label = option?.label as string | undefined;
                          form.setFieldsValue({
                            targetEntityId: value,
                            targetEntityName: label ?? "",
                            targetEntityType: "Counterparty",
                          });
                        }}
                        options={counterpartyOptions.map((c) => ({
                          value: c.id,
                          label: c.fullName || c.name,
                        }))}
                      />
                    </Form.Item>
                    <Form.Item name="targetEntityName" hidden>
                      <Input />
                    </Form.Item>
                  </>
                )}
              </>
            ),
          },
          {
            key: "composition",
            label: t("requests.card.tabs.composition"),
            children: <SupplyLinesEditor name="lines" form={form} />,
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
