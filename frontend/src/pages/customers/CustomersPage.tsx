import React from "react";
import { Typography } from "antd";

const { Title } = Typography;

const CustomersPage: React.FC = () => {
  return (
    <div>
      <Title level={2}>Customers</Title>
    </div>
  );
};

export { CustomersPage };