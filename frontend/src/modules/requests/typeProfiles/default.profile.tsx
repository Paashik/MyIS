import React from "react";
import { Form, Input, Typography } from "antd";

import type { RequestTypeProfile } from "./types";
import { t } from "../../../core/i18n/t";

const { Text } = Typography;

export const defaultRequestTypeProfile: RequestTypeProfile = {
  id: "__default__",
  title: t("requests.typeProfile.default.title"),
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
};

