import React, { useEffect, useMemo, useState } from "react";
import { Alert, Button, Card, Descriptions, Space, Tabs, Tag, Typography } from "antd";
import Tooltip from "antd/es/tooltip";
import { useNavigate, useParams } from "react-router-dom";

import { CommandBar } from "../../../../components/ui/CommandBar";
import { useCan } from "../../../../core/auth/permissions";
import { t } from "../../../../core/i18n/t";
import type { MdmItemReferenceDto } from "../api/adminMdmReferencesApi";
import { getMdmDictionaryById } from "../api/adminMdmReferencesApi";

const { Title, Text } = Typography;

type Params = { id?: string };

function formatGroupLabel(groupName?: string | null, groupCode?: string | null): string {
  const name = (groupName ?? "").trim();
  const code = (groupCode ?? "").trim();
  if (name && code) return `${name} (${code})`;
  return name || code || "-";
}

export const MdmItemCardPage: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<Params>();

  const canView = useCan("Admin.Integration.View");
  const canEdit = useCan("Admin.Mdm.EditItems") || useCan("Admin.Mdm.Edit");
  const isReadOnly = true; // ExternalMaster for now

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [entity, setEntity] = useState<MdmItemReferenceDto | null>(null);

  const load = async () => {
    if (!id) return;
    setLoading(true);
    setError(null);
    try {
      const data = await getMdmDictionaryById<MdmItemReferenceDto>("items", id);
      setEntity(data);
    } catch (e) {
      setError((e as Error).message);
      setEntity(null);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  const flags = useMemo(() => {
    if (!entity) return [];
    const items: { key: string; label: string; color?: string }[] = [];

    items.push({
      key: "isActive",
      label: entity.isActive ? t("references.mdm.items.flags.active") : t("references.mdm.items.flags.inactive"),
      color: entity.isActive ? "green" : undefined,
    });

    if (entity.isEskd) {
      items.push({ key: "isEskd", label: t("references.mdm.items.flags.eskd"), color: "blue" });
    }

    if (entity.isEskdDocument) {
      items.push({ key: "isEskdDocument", label: t("references.mdm.items.flags.eskdDocument"), color: "geekblue" });
    }

    if ((entity.manufacturerPartNumber ?? "").trim()) {
      items.push({ key: "hasMpn", label: t("references.mdm.items.flags.hasMpn"), color: "purple" });
    }

    return items;
  }, [entity]);

  if (!id) {
    return <Alert type="error" showIcon message={t("common.error.notFound")} />;
  }

  const titleCode = entity?.nomenclatureNo ? ` ${entity.nomenclatureNo}` : "";
  const isRootGroup = Boolean(entity?.itemGroupId && entity?.categoryId && entity.itemGroupId === entity.categoryId);

  return (
    <div data-testid="mdm-item-card-page">
      <CommandBar
        left={
          <Space direction="vertical" size={0}>
            <Title level={2} style={{ margin: 0 }}>
              {t("references.mdm.items.title")}
              {titleCode}
            </Title>
            <Text type="secondary">
              {entity?.name ?? t("references.mdm.items.card.loadingName")}
            </Text>
          </Space>
        }
        right={
          <Space>
            <Button onClick={() => void load()} loading={loading} data-testid="mdm-item-card-refresh">
              {t("common.actions.refresh")}
            </Button>
            {canEdit && (
              <Tooltip title={isReadOnly ? t("references.mdm.actions.disabledExternalMaster") : undefined}>
                <Button disabled={isReadOnly} data-testid="mdm-item-card-edit">
                  {t("common.actions.edit")}
                </Button>
              </Tooltip>
            )}
            <Button onClick={() => navigate("/references/mdm/items")} data-testid="mdm-item-card-back">
              {t("common.actions.back")}
            </Button>
          </Space>
        }
      />

      {!canView && (
        <Alert
          type="warning"
          showIcon
          message={t("settings.forbidden")}
          style={{ marginBottom: 12 }}
        />
      )}

      {error && <Alert type="error" showIcon message={error} style={{ marginBottom: 12 }} />}

      <Card loading={loading} style={{ marginBottom: 12 }}>
        <Descriptions bordered size="small" column={2}>
          <Descriptions.Item label={t("references.mdm.items.fields.nomenclatureNo")}>
            {entity?.nomenclatureNo ? <Text code>{entity.nomenclatureNo}</Text> : "-"}
          </Descriptions.Item>
          <Descriptions.Item label={t("references.columns.name")}>{entity?.name ?? "-"}</Descriptions.Item>

          <Descriptions.Item label={t("references.mdm.items.fields.itemType")}>
            {formatGroupLabel(entity?.categoryName ?? null, null)}
          </Descriptions.Item>

          <Descriptions.Item label={t("references.mdm.items.fields.designation")}>
            {entity?.designation ?? "-"}
          </Descriptions.Item>
          <Descriptions.Item label={t("references.mdm.items.fields.group")}>
            {entity && !isRootGroup ? formatGroupLabel(entity.itemGroupName ?? null, null) : "-"}
          </Descriptions.Item>

          <Descriptions.Item label={t("references.mdm.items.columns.uom")}>
            {entity?.unitOfMeasureName
              ? formatGroupLabel(entity.unitOfMeasureName, entity.unitOfMeasureSymbol ?? entity.unitOfMeasureCode ?? null)
              : (entity?.unitOfMeasureId ? <Text code>{entity.unitOfMeasureId}</Text> : "-")}
          </Descriptions.Item>

          <Descriptions.Item label={t("references.mdm.items.fields.lifecycleStatus")}>
            {entity
              ? (
                <Space size={8}>
                  {entity.isActive
                    ? <Tag color="green">{t("references.mdm.items.lifecycle.active")}</Tag>
                    : <Tag>{t("references.mdm.items.lifecycle.archived")}</Tag>}
                  <Text type="secondary">{t("references.mdm.items.fields.lifecycleStatus.hint")}</Text>
                </Space>
              )
              : "-"}
          </Descriptions.Item>

          <Descriptions.Item label={t("references.mdm.items.fields.preferredSupplier")}>-</Descriptions.Item>
          <Descriptions.Item label={t("references.mdm.items.fields.preferredManufacturer")}>-</Descriptions.Item>

          <Descriptions.Item label={t("references.mdm.items.fields.flags")} span={2}>
            {flags.length
              ? (
                <Space size={4} wrap>
                  {flags.map((x) => (
                    <Tag key={x.key} color={x.color}>
                      {x.label}
                    </Tag>
                  ))}
                </Space>
              )
              : "-"}
          </Descriptions.Item>
        </Descriptions>
      </Card>

      <Tabs
        data-testid="mdm-item-card-tabs"
        defaultActiveKey="procurement"
        items={[
          {
            key: "ecad",
            label: t("references.mdm.items.tabs.ecad"),
            children: (
              <Card>
                <Typography.Text type="secondary">{t("references.mdm.items.tabs.ecad.empty")}</Typography.Text>
              </Card>
            ),
          },
          {
            key: "mcad",
            label: t("references.mdm.items.tabs.mcad"),
            children: (
              <Card>
                <Typography.Text type="secondary">{t("references.mdm.items.tabs.mcad.empty")}</Typography.Text>
              </Card>
            ),
          },
          {
            key: "procurement",
            label: t("references.mdm.items.tabs.procurement"),
            children: (
              <Card>
                <Descriptions bordered size="small" column={2}>
                  <Descriptions.Item label={t("references.mdm.items.columns.mpn")}>
                    {entity?.manufacturerPartNumber ?? "-"}
                  </Descriptions.Item>
                  <Descriptions.Item label={t("references.mdm.items.fields.preferredManufacturer")}>
                    -
                  </Descriptions.Item>
                  <Descriptions.Item label={t("references.mdm.items.fields.preferredSupplier")} span={2}>
                    -
                  </Descriptions.Item>
                </Descriptions>
              </Card>
            ),
          },
          {
            key: "storage",
            label: t("references.mdm.items.tabs.storage"),
            children: (
              <Card>
                <Typography.Text type="secondary">{t("references.mdm.items.tabs.storage.empty")}</Typography.Text>
              </Card>
            ),
          },
          {
            key: "accounting",
            label: t("references.mdm.items.tabs.accounting"),
            children: (
              <Card>
                <Typography.Text type="secondary">{t("references.mdm.items.tabs.accounting.empty")}</Typography.Text>
              </Card>
            ),
          },
          {
            key: "substitutes",
            label: t("references.mdm.items.tabs.substitutes"),
            children: (
              <Card>
                <Typography.Text type="secondary">{t("references.mdm.items.tabs.substitutes.empty")}</Typography.Text>
              </Card>
            ),
          },
          {
            key: "documents",
            label: t("references.mdm.items.tabs.documents"),
            children: (
              <Card>
                <Typography.Text type="secondary">{t("references.mdm.items.tabs.documents.empty")}</Typography.Text>
              </Card>
            ),
          },
          {
            key: "history",
            label: t("references.mdm.items.tabs.history"),
            children: (
              <Card>
                <Descriptions bordered size="small" column={2}>
                  <Descriptions.Item label={t("references.mdm.items.fields.createdAt")}>
                    {entity?.createdAt ?? "-"}
                  </Descriptions.Item>
                  <Descriptions.Item label={t("references.mdm.items.fields.updatedAt")}>
                    {entity?.updatedAt ?? "-"}
                  </Descriptions.Item>
                  <Descriptions.Item label={t("references.mdm.items.fields.syncedAt")}>
                    {entity?.syncedAt ?? "-"}
                  </Descriptions.Item>
                  <Descriptions.Item label={t("references.mdm.items.columns.external")}>
                    {entity?.externalSystem || entity?.externalId
                      ? (
                        <Text code>{`${entity.externalSystem ?? ""}:${entity.externalId ?? ""}`}</Text>
                      )
                      : "-"}
                  </Descriptions.Item>
                  <Descriptions.Item label={t("references.columns.code")}>
                    {entity?.code ? <Text code>{entity.code}</Text> : "-"}
                  </Descriptions.Item>
                  <Descriptions.Item label="Id" span={2}>
                    {entity?.id ? <Text code>{entity.id}</Text> : "-"}
                  </Descriptions.Item>
                </Descriptions>
              </Card>
            ),
          },
        ]}
      />
    </div>
  );
};
