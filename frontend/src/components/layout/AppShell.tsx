import React, { useEffect, useMemo, useState } from "react";
import {
  Layout,
  Menu,
  Avatar,
  Typography,
  Dropdown,
  Space,
  Button,
  Input,
  Badge,
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
import BellOutlined from "@ant-design/icons/BellOutlined";
import BookOutlined from "@ant-design/icons/BookOutlined";
import SafetyCertificateOutlined from "@ant-design/icons/SafetyCertificateOutlined";
import SettingOutlined from "@ant-design/icons/SettingOutlined";
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

  const canSeeReferences =
    useCan("Admin.Requests.EditTypes") ||
    useCan("Admin.Requests.EditStatuses") ||
    useCan("Admin.Integration.View");
  const canSeeIntegrations = useCan("Admin.Integration.View");
  const canSeeAdministration =
    useCan("Admin.Settings.Access") ||
    useCan("Admin.Security.EditEmployees") ||
    useCan("Admin.Security.EditUsers") ||
    useCan("Admin.Security.EditRoles") ||
    useCan("Admin.Requests.EditWorkflow") ||
    useCan("Admin.Integration.View") ||
    useCan("Admin.Integration.Execute");

  const [openKeys, setOpenKeys] = useState<string[]>([]);

  const selectedKey = useMemo(() => {
    if (location.pathname.startsWith("/requests")) {
      const sp = new URLSearchParams(location.search);
      const direction = sp.get("direction");
      if (direction === "outgoing") return "/requests/outgoing";
      return "/requests/incoming";
    }

    if (location.pathname.startsWith("/references/requests/types"))
      return "/references/requests/types";
    if (location.pathname.startsWith("/references/requests/statuses"))
      return "/references/mdm/statuses";
    if (location.pathname.startsWith("/references/mdm/")) {
      const parts = location.pathname.split("/").filter(Boolean);
      const dict = parts.length >= 3 ? parts[2] : "";
      if (dict) return `/references/mdm/${dict}`;
      return "/references";
    }
    if (location.pathname.startsWith("/references")) return "/references";

    if (location.pathname.startsWith("/administration/security/employees"))
      return "/administration/security/employees";
    if (location.pathname.startsWith("/administration/security/users"))
      return "/administration/security/users";
    if (location.pathname.startsWith("/administration/security/roles"))
      return "/administration/security/roles";
    if (location.pathname.startsWith("/administration/mdm")) return "/administration/mdm";
    if (location.pathname.startsWith("/administration/requests/workflow"))
      return "/administration/requests/workflow";
    if (location.pathname.startsWith("/administration/integrations/component2020"))
      return "/administration/integrations/component2020";
    if (location.pathname.startsWith("/administration/system/paths"))
      return "/administration/system/paths";
    if (location.pathname.startsWith("/administration")) return "/administration";

    if (location.pathname.startsWith("/customers")) return "/customers";
    if (location.pathname.startsWith("/procurement")) return "/procurement";
    if (location.pathname.startsWith("/production")) return "/production";
    if (location.pathname.startsWith("/warehouse")) return "/warehouse";
    if (location.pathname.startsWith("/engineering")) return "/engineering";
    if (location.pathname.startsWith("/technology")) return "/technology";
    if (location.pathname.startsWith("/quality")) return "/quality";
    return "/";
  }, [location.pathname, location.search]);

  useEffect(() => {
    if (collapsed) {
      setOpenKeys([]);
      return;
    }

    if (location.pathname.startsWith("/requests")) {
      setOpenKeys(["/requests"]);
      return;
    }

    if (location.pathname.startsWith("/references")) {
      setOpenKeys(["/references"]);
      return;
    }

    if (location.pathname.startsWith("/administration")) {
      setOpenKeys(["/administration"]);
      return;
    }

    setOpenKeys([]);
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
          { key: "/requests/incoming", label: t("nav.requests.incoming") },
          { key: "/requests/outgoing", label: t("nav.requests.outgoing") },
        ],
      },
      { key: "/customers", icon: <TeamOutlined />, label: t("nav.customers") },
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
      { key: "/warehouse", icon: <ApartmentOutlined />, label: t("nav.warehouse") },
      { key: "/engineering", icon: <BuildOutlined />, label: t("nav.engineering") },
      { key: "/technology", icon: <BuildOutlined />, label: t("nav.technology") },
      {
        key: "/quality",
        icon: <SafetyCertificateOutlined />,
        label: t("nav.quality"),
      },
    ];

    if (canSeeReferences) {
      items.push({
        key: "/references",
        icon: <BookOutlined />,
        label: t("nav.references"),
        children: [
          { key: "/references/requests/types", label: t("nav.references.requests.types") },
          { type: "divider" },
          { key: "/references/mdm/units", label: t("references.mdm.units.title") },
          { key: "/references/mdm/counterparties", label: t("references.mdm.counterparties.title") },
          { key: "/references/mdm/items", label: t("references.mdm.items.title") },
          { key: "/references/mdm/item-groups", label: t("references.mdm.itemGroups.title") },
          { key: "/references/mdm/manufacturers", label: t("references.mdm.manufacturers.title") },
          { key: "/references/mdm/body-types", label: t("references.mdm.bodyTypes.title") },
          { key: "/references/mdm/currencies", label: t("references.mdm.currencies.title") },
          { key: "/references/mdm/technical-parameters", label: t("references.mdm.technicalParameters.title") },
          { key: "/references/mdm/parameter-sets", label: t("references.mdm.parameterSets.title") },
          { key: "/references/mdm/symbols", label: t("references.mdm.symbols.title") },
          { key: "/references/mdm/statuses", label: t("references.statuses.title") },
          { key: "/references/mdm/external-links", label: t("references.mdm.externalLinks.title") },
        ],
      });
    }

    if (canSeeAdministration) {
      items.push({
        key: "/administration",
        icon: <SettingOutlined />,
        label: t("nav.administration"),
        children: [
          { key: "/administration/mdm", label: t("administration.mdm.title") },
          { key: "/administration/security/users", label: t("nav.administration.security.users") },
          { key: "/administration/security/roles", label: t("nav.administration.security.roles") },
          { key: "/administration/security/employees", label: t("nav.administration.security.employees") },
          { key: "/administration/requests/workflow", label: t("nav.administration.requests.workflow") },
          { key: "/administration/system/paths", label: t("nav.administration.system.paths") },
          ...(canSeeIntegrations
            ? [
                {
                  key: "/administration/integrations/component2020",
                  label: t("nav.administration.integrations.component2020"),
                },
              ]
            : []),
        ],
      });
    }

    return items;
  }, [canSeeAdministration, canSeeIntegrations, canSeeReferences]);

  type MenuClickInfo = { key: React.Key };

  const handleMenuClick = (info: MenuClickInfo) => {
    const path = String(info.key);

    if (path === "/requests") {
      navigate("/requests/journal?direction=incoming&type=all");
      return;
    }
    if (path === "/requests/incoming") {
      navigate("/requests/journal?direction=incoming&type=all");
      return;
    }
    if (path === "/requests/outgoing") {
      navigate("/requests/journal?direction=outgoing&type=all");
      return;
    }

    if (path === "/references") {
      navigate("/references");
      return;
    }

    if (path === "/administration") {
      navigate("/administration");
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
          openKeys={openKeys}
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

          <Space align="center" size="middle">
            <Input.Search
              placeholder={t("nav.search.placeholder")}
              allowClear
              style={{ width: 360, maxWidth: "40vw", marginTop: 8 }}
              onSearch={() => {
                // Global search is not implemented yet.
              }}
            />

            <Badge count={0} size="small">
              <Button
                type="text"
                icon={<BellOutlined />}
                aria-label={t("nav.notifications")}
              />
            </Badge>

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
          </Space>
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
