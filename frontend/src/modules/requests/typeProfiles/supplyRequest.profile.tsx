import React from "react";
import { Form, Input, Typography } from "antd";

import type { RequestTypeProfile, ValidationError } from "./types";
import { t } from "../../../core/i18n/t";
import { SUPPLY_REQUEST_TYPE_ID } from "../requestTypeIds";

const { Text } = Typography;

export const supplyRequestProfile: RequestTypeProfile = {
  id: SUPPLY_REQUEST_TYPE_ID,
  title: t("requests.typeProfile.supply.title"),
  direction: "Outgoing",
  renderDetails: ({ request }) => {
    const description = request.description ?? request.bodyText ?? "";
    if (!description.trim()) {
      return <Text type="secondary">{t("requests.details.value.noDescription")}</Text>;
    }
    return <div style={{ whiteSpace: "pre-wrap" }}>{description}</div>;
  },
  renderEdit: () => {
    return (
      <Form.Item label={t("requests.form.description.label")} name="description">
        <Input.TextArea data-testid="request-form-description-input" rows={4} />
      </Form.Item>
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
