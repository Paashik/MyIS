import React, { useMemo } from "react";
import { Alert, Card, Table, Tag, Typography } from "antd";
import type { ColumnsType } from "antd/es/table";
import { useNavigate } from "react-router-dom";

import { CommandBar } from "../../components/ui/CommandBar";
import { t } from "../../core/i18n/t";
import { useCan } from "../../core/auth/permissions";

const { Title, Text } = Typography;

type OwnershipRow = {
  key: string;
  dictionary: string;
  entity: string;
  owner: string;
  mode: "ExternalMaster" | "Hybrid" | "MyISMaster";
  source: string;
  editable: boolean;
};

export const MdmOwnershipPage: React.FC = () => {
  const navigate = useNavigate();
  const canView = useCan("Admin.Integration.View");

  const rows: OwnershipRow[] = useMemo(
    () => [
      {
        key: "mdm.units",
        dictionary: t("references.mdm.units.title"),
        entity: "mdm.units",
        owner: "Component-2020",
        mode: "ExternalMaster",
        source: t("nav.administration.integrations.component2020"),
        editable: false,
      },
      {
        key: "mdm.counterparties",
        dictionary: t("references.mdm.counterparties.title"),
        entity: "mdm.counterparties",
        owner: "Component-2020",
        mode: "ExternalMaster",
        source: t("nav.administration.integrations.component2020"),
        editable: false,
      },
      {
        key: "mdm.items",
        dictionary: t("references.mdm.items.title"),
        entity: "mdm.items",
        owner: "Component-2020",
        mode: "ExternalMaster",
        source: t("nav.administration.integrations.component2020"),
        editable: false,
      },
      {
        key: "mdm.manufacturers",
        dictionary: t("references.mdm.manufacturers.title"),
        entity: "mdm.manufacturers",
        owner: "Component-2020",
        mode: "ExternalMaster",
        source: t("nav.administration.integrations.component2020"),
        editable: false,
      },
      {
        key: "mdm.body_types",
        dictionary: t("references.mdm.bodyTypes.title"),
        entity: "mdm.body_types",
        owner: "Component-2020",
        mode: "ExternalMaster",
        source: t("nav.administration.integrations.component2020"),
        editable: false,
      },
      {
        key: "mdm.currencies",
        dictionary: t("references.mdm.currencies.title"),
        entity: "mdm.currencies",
        owner: "Component-2020",
        mode: "ExternalMaster",
        source: t("nav.administration.integrations.component2020"),
        editable: false,
      },
      {
        key: "mdm.technical_parameters",
        dictionary: t("references.mdm.technicalParameters.title"),
        entity: "mdm.technical_parameters",
        owner: "Component-2020",
        mode: "ExternalMaster",
        source: t("nav.administration.integrations.component2020"),
        editable: false,
      },
      {
        key: "mdm.parameter_sets",
        dictionary: t("references.mdm.parameterSets.title"),
        entity: "mdm.parameter_sets",
        owner: "Component-2020",
        mode: "ExternalMaster",
        source: t("nav.administration.integrations.component2020"),
        editable: false,
      },
      {
        key: "mdm.symbols",
        dictionary: t("references.mdm.symbols.title"),
        entity: "mdm.symbols",
        owner: "Component-2020",
        mode: "ExternalMaster",
        source: t("nav.administration.integrations.component2020"),
        editable: false,
      },
    ],
    []
  );

  const columns: ColumnsType<OwnershipRow> = useMemo(
    () => [
      { title: t("administration.mdm.columns.dictionary"), dataIndex: "dictionary", key: "dictionary" },
      { title: t("administration.mdm.columns.entity"), dataIndex: "entity", key: "entity" },
      { title: t("administration.mdm.columns.owner"), dataIndex: "owner", key: "owner" },
      {
        title: t("administration.mdm.columns.mode"),
        dataIndex: "mode",
        key: "mode",
        render: (v: OwnershipRow["mode"]) => <Tag>{v}</Tag>,
      },
      {
        title: t("administration.mdm.columns.editable"),
        dataIndex: "editable",
        key: "editable",
        render: (v: boolean) => (v ? <Tag color="green">YES</Tag> : <Tag>NO</Tag>),
      },
      { title: t("administration.mdm.columns.source"), dataIndex: "source", key: "source" },
    ],
    []
  );

  return (
    <div>
      <CommandBar
        left={
          <Title level={2} style={{ margin: 0 }}>
            {t("administration.mdm.title")}
          </Title>
        }
        right={
          <a onClick={() => navigate("/administration")}>{t("common.actions.back")}</a>
        }
      />

      {!canView && (
        <Alert type="warning" showIcon message={t("settings.forbidden")} style={{ marginBottom: 12 }} />
      )}

      <Alert
        type="info"
        showIcon
        message={t("administration.mdm.readOnly.title")}
        description={
          <Text>
            {t("administration.mdm.readOnly.description")}
          </Text>
        }
        style={{ marginBottom: 12 }}
      />

      <Card>
        <Table rowKey={(r: OwnershipRow) => r.key} columns={columns} dataSource={rows} pagination={false} />
      </Card>
    </div>
  );
};
