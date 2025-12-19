import React from "react";
import { Button, Card, Space, Typography } from "antd";
import { useNavigate } from "react-router-dom";
import { t } from "../../core/i18n/t";
import { useCan } from "../../core/auth/permissions";

const { Title } = Typography;

const AdministrationPage: React.FC = () => {
  const navigate = useNavigate();
  const canSeeIntegrations = useCan("Admin.Integration.View");
  return (
    <div>
      <Title level={2} style={{ marginTop: 0 }}>
        {t("nav.administration")}
      </Title>
      <Space direction="vertical" style={{ width: "100%" }}>
        <Card title={t("administration.mdm.title")}>
          <Button type="primary" onClick={() => navigate("/administration/mdm")}>
            {t("common.actions.open")}
          </Button>
        </Card>
        <Card title={t("nav.administration.security.users")}>
          <Button type="primary" onClick={() => navigate("/administration/security/users")}>
            {t("common.actions.open")}
          </Button>
        </Card>
        <Card title={t("nav.administration.security.roles")}>
          <Button type="primary" onClick={() => navigate("/administration/security/roles")}>
            {t("common.actions.open")}
          </Button>
        </Card>
        <Card title={t("nav.administration.security.employees")}>
          <Button type="primary" onClick={() => navigate("/administration/security/employees")}>
            {t("common.actions.open")}
          </Button>
        </Card>
        <Card title={t("nav.administration.requests.workflow")}>
          <Button type="primary" onClick={() => navigate("/administration/requests/workflow")}>
            {t("common.actions.open")}
          </Button>
        </Card>
        <Card title={t("nav.administration.system.paths")}>
          <Button type="primary" onClick={() => navigate("/administration/system/paths")}>
            {t("common.actions.open")}
          </Button>
        </Card>
        {canSeeIntegrations && (
          <Card title={t("nav.administration.integrations.component2020")}>
            <Button
              type="primary"
              onClick={() => navigate("/administration/integrations/component2020")}
            >
              {t("common.actions.open")}
            </Button>
          </Card>
        )}
      </Space>
    </div>
  );
};

export { AdministrationPage };
