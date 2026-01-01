import React, { useEffect, useMemo, useState } from "react";
import { Alert, Button, Card, Descriptions, Space, Tabs, Tag, Typography } from "antd";
import Tooltip from "antd/es/tooltip";
import { useNavigate, useParams } from "react-router-dom";

import { CommandBar } from "../../../../components/ui/CommandBar";
import { useCan } from "../../../../core/auth/permissions";
import { t } from "../../../../core/i18n/t";
import type { MdmItemReferenceDto } from "../api/adminMdmReferencesApi";
import { getMdmDictionaryById } from "../api/adminMdmReferencesApi";
import "./MdmItemCardPage.css";

const { Title, Text } = Typography;

type Params = { id?: string };

type ItemTypeMeta = {
  code: string;
  label: string;
};

const normalizeText = (value?: string | null) => (value ?? "").trim().toLowerCase();

const resolveItemType = (itemKind?: string | null): ItemTypeMeta => {
  const kind = normalizeText(itemKind);
  if (kind.includes("component") || kind.includes("компонент") || kind.includes("электро")) {
    return { code: "CMP", label: t("references.mdm.items.type.component") };
  }
  if (kind.includes("material") || kind.includes("материал")) {
    return { code: "MAT", label: t("references.mdm.items.type.material") };
  }
  if (kind.includes("detail") || kind.includes("part") || kind.includes("детал")) {
    return { code: "PRT", label: t("references.mdm.items.type.part") };
  }
  if (kind.includes("assembly") || kind.includes("сборк")) {
    return { code: "ASM", label: t("references.mdm.items.type.assembly") };
  }
  if (kind.includes("product") || kind.includes("готов") || kind.includes("издел")) {
    return { code: "PRD", label: t("references.mdm.items.type.product") };
  }
  const fallback = (itemKind ?? "").trim();
  return {
    code: fallback ? fallback.slice(0, 3).toUpperCase() : "MDM",
    label: fallback || t("references.mdm.items.type.unknown"),
  };
};

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

  const itemType = resolveItemType(entity?.itemKind ?? entity?.categoryName ?? null);
  const statusLabel = entity?.isActive
    ? t("references.mdm.items.status.active")
    : t("references.mdm.items.status.archived");
  const headerMeta = entity
    ? `${itemType.label} · ${entity.nomenclatureNo} · ${statusLabel}`
    : t("references.mdm.items.card.loadingName");
  const isRootGroup = Boolean(entity?.itemGroupId && entity?.categoryId && entity.itemGroupId === entity.categoryId);
  const photoUrl = entity?.hasPhoto ? `/api/admin/references/mdm/items/${entity.id}/photo` : null;

  return (
    <div data-testid="mdm-item-card-page" className="mdm-item-card">
      <CommandBar
        left={
          <Space direction="vertical" size={2} className="mdm-item-card__header">
            <Title level={2} style={{ margin: 0 }}>
              {entity?.name ?? t("references.mdm.items.card.loadingName")}
            </Title>
            <Text type="secondary">{headerMeta}</Text>
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
            <Tooltip title={isReadOnly ? t("references.mdm.actions.disabledExternalMaster") : undefined}>
              <Button disabled={isReadOnly} data-testid="mdm-item-card-status">
                {t("references.mdm.items.actions.changeStatus")}
              </Button>
            </Tooltip>
            <Tooltip title={isReadOnly ? t("references.mdm.actions.disabledExternalMaster") : undefined}>
              <Button disabled={isReadOnly} data-testid="mdm-item-card-create-related">
                {t("references.mdm.items.actions.createRelated")}
              </Button>
            </Tooltip>
            <Button onClick={() => navigate(`/mdm/items/${id}/bom`)} data-testid="mdm-item-card-open-bom">
              Открыть BOM
            </Button>
            <Tooltip title={isReadOnly ? t("references.mdm.actions.disabledExternalMaster") : undefined}>
              <Button danger disabled={isReadOnly} data-testid="mdm-item-card-archive">
                {t("references.mdm.items.actions.archive")}
              </Button>
            </Tooltip>
            <Button onClick={() => navigate(-1)} data-testid="mdm-item-card-back">
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

      <Tabs
        data-testid="mdm-item-card-tabs"
        defaultActiveKey="general"
        items={[
          {
            key: "general",
            label: t("references.mdm.items.tabs.general"),
            children: (
              <Card loading={loading} className="mdm-item-card__panel">
                {photoUrl && (
                  <div className="mdm-item-card__photo">
                    <img
                      src={photoUrl}
                      alt={entity?.name ?? "item"}
                      loading="lazy"
                      onError={(event) => { event.currentTarget.style.display = "none"; }}
                    />
                  </div>
                )}
                <Descriptions bordered size="small" column={2}>
                  <Descriptions.Item label={t("references.mdm.items.fields.itemType")}>
                    <Space size={8}>
                      <Tag color="blue">{itemType.code}</Tag>
                      <Text>{itemType.label}</Text>
                    </Space>
                  </Descriptions.Item>
                  <Descriptions.Item label={t("references.columns.name")}>{entity?.name ?? "-"}</Descriptions.Item>

                  <Descriptions.Item label={t("references.mdm.items.fields.nomenclatureNo")}>
                    {entity?.nomenclatureNo ? <Text code>{entity.nomenclatureNo}</Text> : "-"}
                  </Descriptions.Item>
                  <Descriptions.Item label={t("references.columns.code")}>
                    {entity?.code ? <Text code>{entity.code}</Text> : "-"}
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
                    {entity ? (
                      <Tag color={entity.isActive ? "green" : "default"}>{statusLabel}</Tag>
                    ) : "-"}
                  </Descriptions.Item>
                  <Descriptions.Item label={t("references.mdm.items.columns.eskd")}>
                    {entity ? (entity.isEskd ? t("common.yes") : t("common.no")) : "-"}
                  </Descriptions.Item>

                  <Descriptions.Item label={t("references.mdm.items.fields.designation")}>
                    {entity?.designation ?? "-"}
                  </Descriptions.Item>
                  <Descriptions.Item label={t("references.columns.description")}>
                    {"-"}
                  </Descriptions.Item>

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
            ),
          },
          {
            key: "composition",
            label: t("references.mdm.items.tabs.composition"),
            children: (
              <Card className="mdm-item-card__panel">
                <Text type="secondary">{t("references.mdm.items.tabs.composition.empty")}</Text>
              </Card>
            ),
          },
          {
            key: "documents",
            label: t("references.mdm.items.tabs.documents"),
            children: (
              <Card className="mdm-item-card__panel">
                <Text type="secondary">{t("references.mdm.items.tabs.documents.empty")}</Text>
              </Card>
            ),
          },
          {
            key: "history",
            label: t("references.mdm.items.tabs.history"),
            children: (
              <Card className="mdm-item-card__panel">
                <Descriptions bordered size="small" column={2}>
                  <Descriptions.Item label={t("references.mdm.items.fields.createdAt")}>
                    {entity?.createdAt ?? "-"}
                  </Descriptions.Item>
                  <Descriptions.Item label={t("references.mdm.items.fields.updatedAt")}>
                    {entity?.updatedAt ?? "-"}
                  </Descriptions.Item>
                </Descriptions>
              </Card>
            ),
          },
          {
            key: "tasks",
            label: t("references.mdm.items.tabs.tasks"),
            children: (
              <Card className="mdm-item-card__panel">
                <Text type="secondary">{t("references.mdm.items.tabs.tasks.empty")}</Text>
              </Card>
            ),
          },
          {
            key: "integrations",
            label: t("references.mdm.items.tabs.integrations"),
            children: (
              <Card className="mdm-item-card__panel">
                <Descriptions bordered size="small" column={2}>
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
                </Descriptions>
              </Card>
            ),
          },
        ]}
      />
    </div>
  );
};
