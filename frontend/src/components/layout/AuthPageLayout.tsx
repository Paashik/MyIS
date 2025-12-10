import React from "react";
import { Card, Typography } from "antd";

const { Title, Paragraph } = Typography;

interface AuthPageLayoutProps {
  title: React.ReactNode;
  description?: React.ReactNode;
  children: React.ReactNode;
  cardWidth?: number;
}

const AuthPageLayout: React.FC<AuthPageLayoutProps> = ({
  title,
  description,
  children,
  cardWidth = 380,
}) => {
  return (
    <div
      style={{
        minHeight: "100vh",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        padding: 16,
        background: "#f5f5f5",
      }}
    >
      <Card style={{ width: cardWidth }}>
        <Title
          level={3}
          style={{
            textAlign: "center",
            marginBottom: description ? 8 : 24,
          }}
        >
          {title}
        </Title>
        {description && (
          <Paragraph
            type="secondary"
            style={{
              textAlign: "center",
              marginBottom: 24,
              marginTop: 0,
            }}
          >
            {description}
          </Paragraph>
        )}
        {children}
      </Card>
    </div>
  );
};

export { AuthPageLayout };