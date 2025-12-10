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

const { Header, Sider, Content } = Layout;
const { Title, Text } = Typography;

const menuItems: MenuProps["items"] = [
  {
    key: "home",
    icon: <AppstoreOutlined />,
    label: "Главная",
  },
  {
    key: "requests",
    icon: <DatabaseOutlined />,
    label: "Requests",
  },
  {
    key: "customers",
    icon: <TeamOutlined />,
    label: "Customers",
  },
  {
    key: "procurement",
    icon: <ShoppingCartOutlined />,
    label: "Procurement",
  },
  {
    key: "production",
    icon: <DeploymentUnitOutlined />,
    label: "Production",
  },
  {
    key: "warehouse",
    icon: <ApartmentOutlined />,
    label: "Warehouse",
  },
  {
    key: "engineering",
    icon: <BuildOutlined />,
    label: "Engineering",
  },
  {
    key: "technology",
    icon: <BuildOutlined />,
    label: "Technology",
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
      label: "Выйти",
      onClick: () => {
        void logout();
      },
    },
  ];

  const userName = user?.fullName || user?.login || "Неизвестный пользователь";
  const userRoles = user?.roles?.length ? user.roles.join(", ") : "Нет ролей";

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