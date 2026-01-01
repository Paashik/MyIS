import React, { useEffect, useMemo, useState } from "react";
import { Button, Col, DatePicker, Form, Input, Row, Select, Space, Tabs, Typography } from "antd";
import type { FormInstance } from "rc-field-form";
import dayjs from "dayjs";

import type {
  RequestCounterpartyLookupDto,
  RequestLineInputDto,
  RequestOrgUnitLookupDto,
  RequestBasisCustomerOrderLookupDto,
  RequestBasisIncomingRequestLookupDto,
  RequestBasisType,
  RequestTypeDto,
} from "../api/types";
import {
  getCustomerOrders,
  getIncomingRequests,
  getRequestCounterparties,
  getRequestOrgUnits,
} from "../api/requestsApi";
import { getOrgUnit } from "../../organization/api/orgUnitsApi";
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
  targetEntityType?: string;
  targetEntityId?: string;
  targetEntityName?: string;
  basisType?: RequestBasisType;
  basisRequestId?: string;
  basisCustomerOrderId?: string;
  basisDescription?: string;
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

type RecipientKind = "counterparty" | "department";

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

  const [selectedTypeId, setSelectedTypeId] = useState<string | undefined>(
    initialValues?.requestTypeId
  );
  const [counterpartySearch, setCounterpartySearch] = useState("");
  const [counterpartyLoading, setCounterpartyLoading] = useState(false);
  const [counterpartyOptions, setCounterpartyOptions] = useState<RequestCounterpartyLookupDto[]>([]);
  const [recipientKind, setRecipientKind] = useState<RecipientKind>("counterparty");
  const [orgUnitSearch, setOrgUnitSearch] = useState("");
  const [orgUnitLoading, setOrgUnitLoading] = useState(false);
  const [orgUnitOptions, setOrgUnitOptions] = useState<RequestOrgUnitLookupDto[]>([]);
  const [basisType, setBasisType] = useState<RequestBasisType | undefined>(
    initialValues?.basisType
  );
  const [incomingSearch, setIncomingSearch] = useState("");
  const [incomingLoading, setIncomingLoading] = useState(false);
  const [incomingOptions, setIncomingOptions] =
    useState<RequestBasisIncomingRequestLookupDto[]>([]);
  const [customerOrderSearch, setCustomerOrderSearch] = useState("");
  const [customerOrderLoading, setCustomerOrderLoading] = useState(false);
  const [customerOrderOptions, setCustomerOrderOptions] =
    useState<RequestBasisCustomerOrderLookupDto[]>([]);


  const selectedTypeIdValue = useMemo(
    () => selectedTypeId ?? initialValues?.requestTypeId,
    [initialValues?.requestTypeId, selectedTypeId]
  );

  const selectedType = useMemo(
    () => requestTypes.find((t) => t.id === selectedTypeIdValue),
    [requestTypes, selectedTypeIdValue]
  );

  const isOutgoing = selectedType?.direction === "Outgoing";
  const isIncoming = selectedType?.direction === "Incoming";
  const recipientFieldPrefix = isOutgoing ? "target" : "related";
  const recipientLabel = isOutgoing
    ? t("requests.form.recipient.outgoing")
    : t("requests.form.recipient.incoming");

  const resolveRecipientKind = (
    entityType?: string,
    entityId?: string,
    entityName?: string
  ): RecipientKind => {
    const normalizedType = (entityType ?? "").trim().toLowerCase();
    if (normalizedType === "department") return "department";
    if (normalizedType === "counterparty") return "counterparty";
    if (!!entityId) return "counterparty";
    if (!!entityName) return "department";
    return "counterparty";
  };

  const counterpartySelectOptions = useMemo(() => {
    const seen = new Set<string>();
    const unique = counterpartyOptions.filter((item) => {
      if (seen.has(item.id)) return false;
      seen.add(item.id);
      return true;
    });

    return unique.map((c) => ({
      value: c.id,
      label: c.fullName || c.name,
    }));
  }, [counterpartyOptions]);

  const orgUnitSelectOptions = useMemo(() => {
    const seen = new Set<string>();
    const unique = orgUnitOptions.filter((item) => {
      if (seen.has(item.id)) return false;
      seen.add(item.id);
      return true;
    });

    return unique.map((u) => ({
      value: u.id,
      label: u.code ? `${u.code} — ${u.name}` : u.name,
    }));
  }, [orgUnitOptions]);

  useEffect(() => {
    if (!initialValues) return;

    form.setFieldsValue({
      ...initialValues,
      dueDate: initialValues.dueDate ? dayjs(initialValues.dueDate) : undefined,
      lines: initialValues.lines ?? [],
    });
    setSelectedTypeId(initialValues.requestTypeId);
    setBasisType(initialValues.basisType ?? undefined);
  }, [initialValues, form]);

  useEffect(() => {
    if (!initialValues) return;

    const entityId = isOutgoing
      ? initialValues.targetEntityId
      : initialValues.relatedEntityId;
    const entityName = isOutgoing
      ? initialValues.targetEntityName
      : initialValues.relatedEntityName;
    const entityType = (isOutgoing
      ? initialValues.targetEntityType
      : initialValues.relatedEntityType) ?? "";

    if (recipientKind === "counterparty") {
      if (entityId && entityName) {
        setCounterpartyOptions((prev) => {
          if (prev.some((item) => item.id === entityId)) return prev;
          return [...prev, { id: entityId, name: entityName, fullName: entityName }];
        });
      }
      return;
    }

    if (recipientKind === "department" && entityId && entityType.toLowerCase() === "department") {
      let cancelled = false;
      const load = async () => {
        try {
          const orgUnit = await getOrgUnit(entityId);
          if (cancelled) return;
          setOrgUnitOptions((prev) => {
            if (prev.some((item) => item.id === orgUnit.id)) return prev;
            return [
              ...prev,
              {
                id: orgUnit.id,
                name: orgUnit.name,
                code: orgUnit.code ?? undefined,
                parentId: orgUnit.parentId ?? undefined,
                phone: orgUnit.phone ?? undefined,
                email: orgUnit.email ?? undefined,
              },
            ];
          });
        } catch {
          // ignore lookup errors
        }
      };
      void load();
      return () => {
        cancelled = true;
      };
    }
  }, [initialValues, isOutgoing, recipientKind]);

  useEffect(() => {
    if (!initialValues?.basisType) return;

    if (
      initialValues.basisType === "IncomingRequest" &&
      initialValues.basisRequestId &&
      initialValues.basisDescription
    ) {
      const basisRequestId = initialValues.basisRequestId!;
      const basisDescription = initialValues.basisDescription!;

      setIncomingOptions((prev) => {
        if (prev.some((item) => item.id === basisRequestId)) return prev;
        return [
          ...prev,
          {
            id: basisRequestId,
            title: basisDescription,
            requestTypeName: undefined,
          },
        ];
      });
    }

    if (
      initialValues.basisType === "CustomerOrder" &&
      initialValues.basisCustomerOrderId &&
      initialValues.basisDescription
    ) {
      const basisCustomerOrderId = initialValues.basisCustomerOrderId!;
      const basisDescription = initialValues.basisDescription!;

      setCustomerOrderOptions((prev) => {
        if (prev.some((item) => item.id === basisCustomerOrderId)) return prev;
        return [
          ...prev,
          {
            id: basisCustomerOrderId,
            number: basisDescription,
            customerName: undefined,
          },
        ];
      });
    }
  }, [initialValues]);

  useEffect(() => {
    if (!isOutgoing && !isIncoming) return;

    if (mode === "create") {
      form.setFieldsValue({
        relatedEntityType: undefined,
        relatedEntityId: undefined,
        relatedEntityName: undefined,
        targetEntityType: undefined,
        targetEntityId: undefined,
        targetEntityName: undefined,
      });
      setRecipientKind("counterparty");
      return;
    }

    const entityType = isOutgoing
      ? initialValues?.targetEntityType
      : initialValues?.relatedEntityType;
    const entityId = isOutgoing
      ? initialValues?.targetEntityId
      : initialValues?.relatedEntityId;
    const entityName = isOutgoing
      ? initialValues?.targetEntityName
      : initialValues?.relatedEntityName;
    setRecipientKind(resolveRecipientKind(entityType, entityId, entityName));
  }, [form, initialValues, isIncoming, isOutgoing, mode]);


  useEffect(() => {
    if (!isOutgoing && !isIncoming) return;
    if (recipientKind !== "counterparty") return;

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
  }, [counterpartySearch, isIncoming, isOutgoing, recipientKind]);

  useEffect(() => {
    if (!isOutgoing && !isIncoming) return;
    if (recipientKind !== "department") return;

    let cancelled = false;

    const load = async () => {
      setOrgUnitLoading(true);
      try {
        const items = await getRequestOrgUnits(orgUnitSearch.trim() || undefined);
        if (!cancelled) {
          setOrgUnitOptions(items);
        }
      } finally {
        if (!cancelled) {
          setOrgUnitLoading(false);
        }
      }
    };

    void load();

    return () => {
      cancelled = true;
    };
  }, [isIncoming, isOutgoing, orgUnitSearch, recipientKind]);

  useEffect(() => {
    if (basisType !== "IncomingRequest") return;

    let cancelled = false;

    const load = async () => {
      setIncomingLoading(true);
      try {
        const items = await getIncomingRequests(incomingSearch.trim() || undefined);
        if (!cancelled) {
          setIncomingOptions(items);
        }
      } finally {
        if (!cancelled) {
          setIncomingLoading(false);
        }
      }
    };

    void load();

    return () => {
      cancelled = true;
    };
  }, [basisType, incomingSearch]);

  useEffect(() => {
    if (basisType !== "CustomerOrder") return;

    let cancelled = false;

    const load = async () => {
      setCustomerOrderLoading(true);
      try {
        const items = await getCustomerOrders(customerOrderSearch.trim() || undefined);
        if (!cancelled) {
          setCustomerOrderOptions(items);
        }
      } finally {
        if (!cancelled) {
          setCustomerOrderLoading(false);
        }
      }
    };

    void load();

    return () => {
      cancelled = true;
    };
  }, [basisType, customerOrderSearch]);

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

    const resolvedBasisType =
      values.basisType ??
      basisType ??
      (typeof values.basisDescription === "string" && values.basisDescription.trim()
        ? "Other"
        : undefined);

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
      basisType: resolvedBasisType,
      basisRequestId:
        typeof values.basisRequestId === "string" && values.basisRequestId.trim()
          ? values.basisRequestId.trim()
          : undefined,
      basisCustomerOrderId:
        typeof values.basisCustomerOrderId === "string" && values.basisCustomerOrderId.trim()
          ? values.basisCustomerOrderId.trim()
          : undefined,
      basisDescription:
        typeof values.basisDescription === "string" && values.basisDescription.trim()
          ? values.basisDescription.trim()
          : undefined,
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
        targetEntityType: initialValues?.targetEntityType ?? "",
        targetEntityId: initialValues?.targetEntityId ?? "",
        targetEntityName: initialValues?.targetEntityName ?? "",
        basisType: initialValues?.basisType ?? undefined,
        basisRequestId: initialValues?.basisRequestId ?? undefined,
        basisCustomerOrderId: initialValues?.basisCustomerOrderId ?? undefined,
        basisDescription: initialValues?.basisDescription ?? "",
      }}
      onFinish={handleFinish}
    >
      <Row gutter={[16, 0]}>
        <Col xs={24} md={12}>
          <Form.Item label={t("requests.form.title.label")} name="title">
            <Input
              data-testid="request-form-title-input"
              disabled
              placeholder={mode === "create" ? t("requests.form.title.auto") : undefined}
            />
          </Form.Item>
        </Col>
        <Col xs={24} md={12}>
          <Form.Item
            label={t("requests.form.type.label")}
            name="requestTypeId"
            rules={[{ required: true, message: t("requests.form.type.required") }]}
          >
            <Select
              data-testid="request-form-type-select"
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
        </Col>
      </Row>

      {(isOutgoing || isIncoming) && (
        <Row gutter={[16, 0]}>
          <Col xs={24} md={12}>
            <Form.Item label={t("requests.form.recipient.kind.label")}>
              <Select
                data-testid="request-form-recipient-kind"
                value={recipientKind}
                onChange={(next: RecipientKind) => {
                  setRecipientKind(next);
                  const typeField = `${recipientFieldPrefix}EntityType`;
                  const idField = `${recipientFieldPrefix}EntityId`;
                  const nameField = `${recipientFieldPrefix}EntityName`;
                  if (next === "counterparty") {
                    form.setFieldsValue({
                      [typeField]: "Counterparty",
                      [idField]: undefined,
                      [nameField]: undefined,
                    });
                  } else {
                    form.setFieldsValue({
                      [typeField]: "Department",
                      [idField]: undefined,
                      [nameField]: undefined,
                    });
                  }
                }}
                options={[
                  { label: t("requests.form.recipient.kind.counterparty"), value: "counterparty" },
                  { label: t("requests.form.recipient.kind.department"), value: "department" },
                ]}
              />
            </Form.Item>
          </Col>
          <Col xs={24} md={12}>
            {recipientKind === "counterparty" ? (
              <>
                <Form.Item label={recipientLabel} name={`${recipientFieldPrefix}EntityId`}>
                  <Select
                    data-testid="request-form-recipient-counterparty"
                    showSearch
                    allowClear
                    filterOption={false}
                    loading={counterpartyLoading}
                    onSearch={(value: string) => setCounterpartySearch(value)}
                    onChange={(value: string | null, option: any) => {
                      const typeField = `${recipientFieldPrefix}EntityType`;
                      const idField = `${recipientFieldPrefix}EntityId`;
                      const nameField = `${recipientFieldPrefix}EntityName`;
                      if (!value) {
                        form.setFieldsValue({
                          [idField]: undefined,
                          [nameField]: undefined,
                          [typeField]: "Counterparty",
                        });
                        return;
                      }
                      const label = option?.label as string | undefined;
                      form.setFieldsValue({
                        [idField]: value,
                        [nameField]: label ?? "",
                        [typeField]: "Counterparty",
                      });
                    }}
                    options={counterpartySelectOptions}
                  />
                </Form.Item>
                <Form.Item name={`${recipientFieldPrefix}EntityName`} hidden>
                  <Input />
                </Form.Item>
                <Form.Item name={`${recipientFieldPrefix}EntityType`} hidden>
                  <Input />
                </Form.Item>
              </>
            ) : (
              <>
                <Form.Item label={recipientLabel} name={`${recipientFieldPrefix}EntityId`}>
                  <Select
                    data-testid="request-form-recipient-department"
                    showSearch
                    allowClear
                    filterOption={false}
                    loading={orgUnitLoading}
                    onSearch={(value: string) => setOrgUnitSearch(value)}
                    onChange={(value: string | null, option: any) => {
                      const typeField = `${recipientFieldPrefix}EntityType`;
                      const idField = `${recipientFieldPrefix}EntityId`;
                      const nameField = `${recipientFieldPrefix}EntityName`;
                      if (!value) {
                        form.setFieldsValue({
                          [idField]: undefined,
                          [nameField]: undefined,
                          [typeField]: "Department",
                        });
                        return;
                      }
                      const label = option?.label as string | undefined;
                      form.setFieldsValue({
                        [idField]: value,
                        [nameField]: label ?? "",
                        [typeField]: "Department",
                      });
                    }}
                    options={orgUnitSelectOptions}
                  />
                </Form.Item>
                <Form.Item name={`${recipientFieldPrefix}EntityName`} hidden>
                  <Input />
                </Form.Item>
                <Form.Item name={`${recipientFieldPrefix}EntityType`} hidden>
                  <Input />
                </Form.Item>
              </>
            )}
          </Col>
        </Row>
      )}

      <Row gutter={[16, 0]}>
        <Col xs={24} md={12}>
          <Form.Item label={t("requests.form.basis.type.label")} name="basisType">
            <Select
              data-testid="request-form-basis-type"
              allowClear
              value={basisType}
              onChange={(value: RequestBasisType | undefined) => {
                setBasisType(value);
                form.setFieldsValue({
                  basisType: value,
                  basisRequestId: undefined,
                  basisCustomerOrderId: undefined,
                  basisDescription: undefined,
                });
              }}
              options={[
                { value: "IncomingRequest", label: t("requests.form.basis.type.incoming") },
                { value: "CustomerOrder", label: t("requests.form.basis.type.customerOrder") },
                { value: "ProductionOrder", label: t("requests.form.basis.type.productionOrder") },
                { value: "Other", label: t("requests.form.basis.type.other") },
              ]}
            />
          </Form.Item>
        </Col>
        <Col xs={24} md={12}>
          {basisType === "IncomingRequest" && (
            <>
              <Form.Item label={t("requests.form.basis.incoming.label")} name="basisRequestId">
                <Select
                  data-testid="request-form-basis-incoming"
                  showSearch
                  allowClear
                  filterOption={false}
                  loading={incomingLoading}
                  onSearch={(value: string) => setIncomingSearch(value)}
                  onChange={(value: string | null, option: any) => {
                    if (!value) {
                      form.setFieldsValue({
                        basisRequestId: undefined,
                        basisDescription: undefined,
                      });
                      return;
                    }
                    const label = option?.label as string | undefined;
                    form.setFieldsValue({
                      basisRequestId: value,
                      basisDescription: label ?? "",
                    });
                  }}
                  options={incomingOptions.map((item) => ({
                    value: item.id,
                    label: item.requestTypeName
                      ? `${item.title} · ${item.requestTypeName}`
                      : item.title,
                  }))}
                />
              </Form.Item>
              <Form.Item name="basisDescription" hidden>
                <Input />
              </Form.Item>
            </>
          )}
          {basisType === "CustomerOrder" && (
            <>
              <Form.Item
                label={t("requests.form.basis.customerOrder.label")}
                name="basisCustomerOrderId"
              >
                <Select
                  data-testid="request-form-basis-customer-order"
                  showSearch
                  allowClear
                  filterOption={false}
                  loading={customerOrderLoading}
                  onSearch={(value: string) => setCustomerOrderSearch(value)}
                  onChange={(value: string | null, option: any) => {
                    if (!value) {
                      form.setFieldsValue({
                        basisCustomerOrderId: undefined,
                        basisDescription: undefined,
                      });
                      return;
                    }
                    const label = option?.label as string | undefined;
                    form.setFieldsValue({
                      basisCustomerOrderId: value,
                      basisDescription: label ?? "",
                    });
                  }}
                  options={customerOrderOptions.map((item) => {
                    const number = item.number ?? t("requests.form.basis.customerOrder.unknown");
                    const label = item.customerName ? `${number} · ${item.customerName}` : number;
                    return { value: item.id, label };
                  })}
                />
              </Form.Item>
              <Form.Item name="basisDescription" hidden>
                <Input />
              </Form.Item>
            </>
          )}
          {(basisType === "ProductionOrder" || basisType === "Other") && (
            <Form.Item label={t("requests.form.basis.description.label")} name="basisDescription">
              <Input
                data-testid="request-form-basis-description"
                placeholder={t("requests.form.basis.description.placeholder")}
              />
            </Form.Item>
          )}
          {!basisType && (
            <Form.Item label={t("requests.form.basis.description.label")} name="basisDescription">
              <Input
                data-testid="request-form-basis-description"
                placeholder={t("requests.form.basis.description.placeholder")}
              />
            </Form.Item>
          )}
        </Col>
      </Row>

      <Form.Item label={t("requests.form.description.label")} name="description">
        <Input.TextArea data-testid="request-form-description-input" rows={4} />
      </Form.Item>

      <Form.Item label={t("requests.form.dueDate.label")} name="dueDate">
        <DatePicker
          data-testid="request-form-due-date-input"
          style={{ width: "100%" }}
          format="DD.MM.YYYY"
        />
      </Form.Item>

      <Tabs
        items={[
          {
            key: "composition",
            label: t("requests.card.tabs.composition"),
            children: (
              <>
                <RequestBodyRenderer
                  mode="edit"
                  requestTypeId={selectedTypeIdValue}
                  form={form}
                  editMode={mode}
                  hideDescription
                />
                <SupplyLinesEditor name="lines" form={form} />
              </>
            ),
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


