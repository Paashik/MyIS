import React, { useMemo, useState } from "react";
import {
  Layout,
  Menu,
  Avatar,
  Typography,
  Dropdown,
  Space,
  Button,
  theme,
} from "antd";
import type { MenuProps } from "antd";
import {
  MenuFoldOutlined,
  MenuUnfoldOutlined,
  UserOutlined,
  ShoppingCartOutlined,
  TeamOutlined,
  DeploymentUnitOutlined,
  BuildOutlined,
  AppstoreOutlined,
  DatabaseOutlined,
  ApartmentOutlined,
} from "@ant-design/icons";
import { Outlet, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../../auth/AuthContext";
import { t } from "../../core/i18n/t";

const { Header, Sider, Content } = Layout;
const { Title, Text } = Typography;

const menuItems: MenuProps["items"] = [
  {
    key: "home",
    icon: <AppstoreOutlined />,
    label: t("nav.home"),
  },
  {
    key: "requests",
    icon: <DatabaseOutlined />,
    label: t("nav.requests"),
  },
  {
    key: "customers",
    icon: <TeamOutlined />,
    label: t("nav.customers"),
  },
  {
    key: "procurement",
    icon: <ShoppingCartOutlined />,
    label: t("nav.procurement"),
  },
  {
    key: "production",
    icon: <DeploymentUnitOutlined />,
    label: t("nav.production"),
  },
  {
    key: "warehouse",
    icon: <ApartmentOutlined />,
    label: t("nav.warehouse"),
  },
  {
    key: "engineering",
    icon: <BuildOutlined />,
    label: t("nav.engineering"),
  },
  {
    key: "technology",
    icon: <BuildOutlined />,
    label: t("nav.technology"),
  },
];

const AppShell: React.FC = () => {
  const [collapsed, setCollapsed] = useState(false);
  const { token } = theme.useToken();
  const navigate = useNavigate();
  const location = useLocation();
  const { user, logout } = useAuth();

  const selectedKey = useMemo(() => {
    if (location.pathname.startsWith("/requests")) return "requests";
    if (location.pathname.startsWith("/customers")) return "customers";
    if (location.pathname.startsWith("/procurement")) return "procurement";
    if (location.pathname.startsWith("/production")) return "production";
    if (location.pathname.startsWith("/warehouse")) return "warehouse";
    if (location.pathname.startsWith("/engineering")) return "engineering";
    if (location.pathname.startsWith("/technology")) return "technology";
    return "home";
  }, [location.pathname]);

  type MenuClickInfo = { key: React.Key };

  const handleMenuClick = (info: MenuClickInfo) => {
    switch (info.key) {
      case "home":
        navigate("/");
        break;
      default:
        navigate(`/${info.key}`);
        break;
    }
  };

  const userMenuItems: MenuProps["items"] = [
    {
      key: "logout",
      icon: <UserOutlined />,
      label: <span data-testid="user-menu-logout">{t("nav.logout")}</span>,
      onClick: () => {
        void logout();
      },
    },
  ];

  const userName = user?.fullName || user?.login || t("nav.user.unknown");
  const userRoles = user?.roles?.length ? user.roles.join(", ") : t("nav.roles.none");

  return (
    <Layout style={{ minHeight: "100vh" }}>
      <Sider collapsible collapsed={collapsed} onCollapse={setCollapsed}>
        <div
          style={{
            height: 64,
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            color: "#fff",
            fontWeight: 600,
            fontSize: 18,
          }}
        >
          MyIS
        </div>
        <Menu
          theme="dark"
          mode="inline"
          selectedKeys={[selectedKey]}
          items={menuItems}
          onClick={handleMenuClick}
        />
      </Sider>
      <Layout>
        <Header
          style={{
            padding: "0 24px",
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            background: token.colorBgContainer,
          }}
        >
          <Space align="center">
            <Button
              type="text"
              icon={collapsed ? <MenuUnfoldOutlined /> : <MenuFoldOutlined />}
              onClick={() => setCollapsed((prev) => !prev)}
            />
            <Title level={4} style={{ margin: 0 }}>
              MyIS
            </Title>
          </Space>

          <Dropdown menu={{ items: userMenuItems }} trigger={["click"]}>
            <Space align="center" style={{ cursor: "pointer" }}>
              <Avatar icon={<UserOutlined />} />
              <div style={{ display: "flex", flexDirection: "column" }}>
                <Text strong>{userName}</Text>
                <Text type="secondary" style={{ fontSize: 12 }}>
                  {userRoles}
                </Text>
              </div>
            </Space>
          </Dropdown>
        </Header>
        <Content
          style={{
            margin: 16,
            padding: 24,
            background: token.colorBgContainer,
          }}
        >
          <Outlet />
        </Content>
      </Layout>
    </Layout>
  );
};

export { AppShell };
