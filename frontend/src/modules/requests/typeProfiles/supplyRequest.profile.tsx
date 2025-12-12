import React from "react";
import { Form, Input, Tabs, Table, Typography } from "antd";

import type { RequestLineDto } from "../api/types";
import type { RequestTypeProfile, ValidationError } from "./types";
import { SupplyLinesEditor } from "../components/SupplyLinesEditor";
import { t } from "../../../core/i18n/t";

const { Text } = Typography;

export const supplyRequestProfile: RequestTypeProfile = {
  code: "SupplyRequest",
  title: t("requests.typeProfile.supply.title"),
  direction: "Outgoing",
  renderDetails: ({ request }) => {
    const columns = [
      {
        title: t("requests.supply.lines.columns.description"),
        dataIndex: "description",
        key: "description",
        render: (value: string | null | undefined, record: RequestLineDto) =>
          value || record.externalItemCode || "",
      },
      {
        title: t("requests.supply.lines.columns.quantity"),
        dataIndex: "quantity",
        key: "quantity",
        width: 120,
      },
      {
        title: t("requests.supply.lines.columns.needByDate"),
        dataIndex: "needByDate",
        key: "needByDate",
        width: 180,
        render: (value?: string | null) => {
          if (!value) return <Text type="secondary">{t("requests.details.value.notSet")}</Text>;
          const date = new Date(value);
          return `${date.toLocaleDateString()} ${date.toLocaleTimeString()}`;
        },
      },
      {
        title: t("requests.supply.lines.columns.supplierName"),
        dataIndex: "supplierName",
        key: "supplierName",
        width: 220,
      },
      {
        title: t("requests.supply.lines.columns.supplierContact"),
        dataIndex: "supplierContact",
        key: "supplierContact",
        width: 220,
      },
    ];

    const description = request.description ?? request.bodyText ?? "";

    return (
      <Tabs
        data-testid="request-details-supply-body-tabs"
        items={[
          {
            key: "lines",
            label: t("requests.supply.tabs.lines"),
            children: (
              <Table
                data-testid="request-details-supply-lines-table"
                rowKey={(r: RequestLineDto) => r.id}
                size="small"
                pagination={false}
                columns={columns as any}
                dataSource={request.lines ?? []}
              />
            ),
          },
          {
            key: "description",
            label: t("requests.supply.tabs.description"),
            children: description.trim() ? (
              <div style={{ whiteSpace: "pre-wrap" }}>{description}</div>
            ) : (
              <Text type="secondary">{t("requests.details.value.noDescription")}</Text>
            ),
          },
        ]}
      />
    );
  },
  renderEdit: ({ form }) => {
    return (
      <Tabs
        data-testid="request-form-supply-body-tabs"
        items={[
          {
            key: "lines",
            label: t("requests.supply.tabs.lines"),
            children: <SupplyLinesEditor name="lines" />,
          },
          {
            key: "description",
            label: t("requests.supply.tabs.description"),
            children: (
              <Form.Item label={t("requests.form.description.label")} name="description">
                <Input.TextArea data-testid="request-form-description-input" rows={4} />
              </Form.Item>
            ),
          },
        ]}
      />
    );
  },
  validate: (model) => {
    const errors: ValidationError[] = [];

    const description = (model.description ?? "").trim();
    const lines = model.lines ?? [];

    if (lines.length === 0 && !description) {
      errors.push({
        path: ["description"],
        message: t("requests.supply.validation.linesOrDescription"),
      });
      errors.push({
        path: ["lines"],
        message: t("requests.supply.validation.linesOrDescription"),
      });
    }

    for (let i = 0; i < lines.length; i++) {
      const l = lines[i];
      if (!l.quantity || l.quantity <= 0) {
        errors.push({
          path: ["lines", i, "quantity"],
          message: t("requests.supply.validation.quantityPositive"),
        });
      }
    }

    return errors;
  },
};

