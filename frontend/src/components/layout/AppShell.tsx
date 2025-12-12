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
import { useCan } from "../../core/auth/permissions";

const { Header, Sider, Content } = Layout;
const { Title, Text } = Typography;

const AppShell: React.FC = () => {
  const [collapsed, setCollapsed] = useState(false);
  const { token } = theme.useToken();
  const navigate = useNavigate();
  const location = useLocation();
  const { user, logout } = useAuth();

  const canSeeSettings = useCan("Admin.Settings.Access");

  const [openKeys, setOpenKeys] = useState<string[]>([]);

  const selectedKey = useMemo(() => {
    // Requests: подсветка именно /requests/incoming или /requests/outgoing (см. Iteration 3.2)
    if (location.pathname.startsWith("/requests/outgoing")) return "/requests/outgoing";
    if (location.pathname.startsWith("/requests/incoming")) return "/requests/incoming";
    if (location.pathname.startsWith("/requests")) return "/requests";
    if (location.pathname.startsWith("/settings/requests/types"))
      return "/settings/requests/types";
    if (location.pathname.startsWith("/settings/requests/statuses"))
      return "/settings/requests/statuses";
    if (location.pathname.startsWith("/settings/requests/workflow"))
      return "/settings/requests/workflow";
    if (location.pathname.startsWith("/settings/security/employees"))
      return "/settings/security/employees";
    if (location.pathname.startsWith("/settings/security/users"))
      return "/settings/security/users";
    if (location.pathname.startsWith("/settings/security/roles"))
      return "/settings/security/roles";
    if (location.pathname.startsWith("/settings")) return "/settings";
    if (location.pathname.startsWith("/customers")) return "/customers";
    if (location.pathname.startsWith("/procurement")) return "/procurement";
    if (location.pathname.startsWith("/production")) return "/production";
    if (location.pathname.startsWith("/warehouse")) return "/warehouse";
    if (location.pathname.startsWith("/engineering")) return "/engineering";
    if (location.pathname.startsWith("/technology")) return "/technology";
    return "/";
  }, [location.pathname]);

  const computedOpenKeys = useMemo(() => {
    if (collapsed) return [];
    if (location.pathname.startsWith("/requests")) return ["/requests"];
    if (location.pathname.startsWith("/settings/requests"))
      return ["/settings", "/settings/requests"];
    if (location.pathname.startsWith("/settings/security"))
      return ["/settings", "/settings/security"];
    if (location.pathname.startsWith("/settings")) return ["/settings"];
    return [];
  }, [collapsed, location.pathname]);

  const menuItems: MenuProps["items"] = useMemo(() => {
    const items: MenuProps["items"] = [
      {
        key: "/",
        icon: <AppstoreOutlined />,
        label: t("nav.home"),
      },
      {
        key: "/requests",
        icon: <DatabaseOutlined />,
        label: t("nav.requests"),
        children: [
          {
            key: "/requests/incoming",
            label: t("nav.requests.incoming"),
          },
          {
            key: "/requests/outgoing",
            label: t("nav.requests.outgoing"),
          },
        ],
      },
      {
        key: "/customers",
        icon: <TeamOutlined />,
        label: t("nav.customers"),
      },
      {
        key: "/procurement",
        icon: <ShoppingCartOutlined />,
        label: t("nav.procurement"),
      },
      {
        key: "/production",
        icon: <DeploymentUnitOutlined />,
        label: t("nav.production"),
      },
      {
        key: "/warehouse",
        icon: <ApartmentOutlined />,
        label: t("nav.warehouse"),
      },
      {
        key: "/engineering",
        icon: <BuildOutlined />,
        label: t("nav.engineering"),
      },
      {
        key: "/technology",
        icon: <BuildOutlined />,
        label: t("nav.technology"),
      },
    ];

    if (canSeeSettings) {
      items.push({
        key: "/settings",
        icon: <BuildOutlined />,
        label: t("nav.settings"),
        children: [
          {
            key: "/settings/requests",
            label: t("nav.settings.requests"),
            children: [
              {
                key: "/settings/requests/types",
                label: t("nav.settings.requests.types"),
              },
              {
                key: "/settings/requests/statuses",
                label: t("nav.settings.requests.statuses"),
              },
              {
                key: "/settings/requests/workflow",
                label: t("nav.settings.requests.workflow"),
              },
            ],
          },
          {
            key: "/settings/security",
            label: t("nav.settings.security"),
            children: [
              {
                key: "/settings/security/employees",
                label: t("nav.settings.security.employees"),
              },
              {
                key: "/settings/security/users",
                label: t("nav.settings.security.users"),
              },
              {
                key: "/settings/security/roles",
                label: t("nav.settings.security.roles"),
              },
            ],
          },
        ],
      });
    }

    return items;
  }, [canSeeSettings]);

  type MenuClickInfo = { key: React.Key };

  const handleMenuClick = (info: MenuClickInfo) => {
    const path = String(info.key);
    if (path === "/requests") {
      navigate("/requests/incoming?type=all");
      return;
    }
    if (path === "/settings") {
      navigate("/settings/requests/types");
      return;
    }
    if (path === "/settings/requests") {
      navigate("/settings/requests/types");
      return;
    }
    if (path === "/settings/security") {
      navigate("/settings/security/employees");
      return;
    }
    navigate(path);
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
          openKeys={computedOpenKeys.length ? computedOpenKeys : openKeys}
          onOpenChange={(keys: string[]) => setOpenKeys(keys)}
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
