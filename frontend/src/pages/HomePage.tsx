import React from "react";
import { Typography } from "antd";
import { t } from "../core/i18n/t";

const { Title, Paragraph } = Typography;

const HomePage: React.FC = () => {
  return (
    <Typography>
      <Title level={2}>{t("home.title")}</Title>
      <Paragraph>
        {t("home.subtitle")}
      </Paragraph>
    </Typography>
  );
};

export { HomePage };
