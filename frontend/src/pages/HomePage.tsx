import React from "react";
import { Typography } from "antd";

const { Title, Paragraph } = Typography;

const HomePage: React.FC = () => {
  return (
    <Typography>
      <Title level={2}>Добро пожаловать в MyIS</Title>
      <Paragraph>
        Выберите раздел в меню слева, чтобы перейти к соответствующему домену системы.
      </Paragraph>
    </Typography>
  );
};

export { HomePage };