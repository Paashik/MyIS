import React from "react";
import { Button, Card, Space, Typography } from "antd";
import { useNavigate } from "react-router-dom";
import { t } from "../../core/i18n/t";

const { Title } = Typography;

const ReferencesPage: React.FC = () => {
  const navigate = useNavigate();
  return (
    <div>
      <Title level={2} style={{ marginTop: 0 }}>
        {t("nav.references")}
      </Title>
      <Space direction="vertical" style={{ width: "100%" }}>
        <Card title={t("references.group.mdm")}>
          <Space wrap>
            <Button type="primary" onClick={() => navigate("/references/mdm/units")}>
              {t("references.mdm.units.title")}
            </Button>
            <Button type="primary" onClick={() => navigate("/references/mdm/counterparties")}>
              {t("references.mdm.counterparties.title")}
            </Button>
            <Button type="primary" onClick={() => navigate("/references/mdm/items")}>
              {t("references.mdm.items.title")}
            </Button>
            <Button onClick={() => navigate("/references/mdm/manufacturers")}>
              {t("references.mdm.manufacturers.title")}
            </Button>
            <Button onClick={() => navigate("/references/mdm/body-types")}>
              {t("references.mdm.bodyTypes.title")}
            </Button>
            <Button onClick={() => navigate("/references/mdm/currencies")}>
              {t("references.mdm.currencies.title")}
            </Button>
            <Button onClick={() => navigate("/references/mdm/technical-parameters")}>
              {t("references.mdm.technicalParameters.title")}
            </Button>
            <Button onClick={() => navigate("/references/mdm/parameter-sets")}>
              {t("references.mdm.parameterSets.title")}
            </Button>
            <Button onClick={() => navigate("/references/mdm/symbols")}>
              {t("references.mdm.symbols.title")}
            </Button>
          </Space>
        </Card>

        <Card title={t("nav.references.requests.types")}>
          <Button type="primary" onClick={() => navigate("/references/requests/types")}>
            {t("common.actions.open")}
          </Button>
        </Card>
        <Card title={t("nav.references.requests.statuses")}>
          <Button type="primary" onClick={() => navigate("/references/requests/statuses")}>
            {t("common.actions.open")}
          </Button>
        </Card>
      </Space>
    </div>
  );
};

export { ReferencesPage };
