import React, { useEffect, useMemo, useState } from "react";
import { Alert, Button, Card, Descriptions, Space, Tabs, Tag, Typography } from "antd";
import Tooltip from "antd/es/tooltip";
import { useNavigate, useParams } from "react-router-dom";

import { CommandBar } from "../../../../components/ui/CommandBar";
import { t } from "../../../../core/i18n/t";
import { useCan } from "../../../../core/auth/permissions";
import type {
  MdmDictionaryKey,
  MdmCounterpartyReferenceDto,
  MdmCurrencyReferenceDto,
  MdmItemReferenceDto,
  MdmManufacturerReferenceDto,
  MdmSimpleReferenceDto,
  MdmUnitReferenceDto,
} from "../api/adminMdmReferencesApi";
import { getMdmDictionaryById } from "../api/adminMdmReferencesApi";

const { Title, Text } = Typography;

type Params = { dict?: string; id?: string };

function toHttpUrl(raw: string): string | null {
  const v = raw.trim();
  if (!v) return null;
  if (/^javascript:/i.test(v)) return null;
  if (/^https?:\/\//i.test(v)) return v;
  if (v.startsWith("//")) return `https:${v}`;
  return `https://${v}`;
}

function isDictionaryKey(v: string): v is MdmDictionaryKey {
  return (
    v === "units" ||
    v === "counterparties" ||
    v === "suppliers" ||
    v === "items" ||
    v === "manufacturers" ||
    v === "body-types" ||
    v === "currencies" ||
    v === "technical-parameters" ||
    v === "parameter-sets" ||
    v === "symbols"
  );
}

function dictTitle(dict: MdmDictionaryKey): string {
  switch (dict) {
    case "units":
      return t("references.mdm.units.title");
    case "counterparties":
    case "suppliers":
      return t("references.mdm.counterparties.title");
    case "items":
      return t("references.mdm.items.title");
    case "manufacturers":
      return t("references.mdm.manufacturers.title");
    case "body-types":
      return t("references.mdm.bodyTypes.title");
    case "currencies":
      return t("references.mdm.currencies.title");
    case "technical-parameters":
      return t("references.mdm.technicalParameters.title");
    case "parameter-sets":
      return t("references.mdm.parameterSets.title");
    case "symbols":
      return t("references.mdm.symbols.title");
  }
}

function dictEditPermission(dict: MdmDictionaryKey): string {
  switch (dict) {
    case "units":
      return "Admin.Mdm.EditUnits";
    case "counterparties":
    case "suppliers":
      return "Admin.Mdm.EditSuppliers";
    case "items":
      return "Admin.Mdm.EditItems";
    case "manufacturers":
      return "Admin.Mdm.EditManufacturers";
    case "body-types":
      return "Admin.Mdm.EditBodyTypes";
    case "currencies":
      return "Admin.Mdm.EditCurrencies";
    case "technical-parameters":
      return "Admin.Mdm.EditTechnicalParameters";
    case "parameter-sets":
      return "Admin.Mdm.EditParameterSets";
    case "symbols":
      return "Admin.Mdm.EditSymbols";
  }
}

export const MdmDictionaryCardPage: React.FC = () => {
  const navigate = useNavigate();
  const { dict: dictParam, id } = useParams<Params>();
  const [showSitePassword, setShowSitePassword] = useState(false);

  useEffect(() => {
    const v = (dictParam || "").trim().toLowerCase();
    if (v === "suppliers" && id) {
      navigate(`/references/mdm/counterparties/${encodeURIComponent(id)}`, { replace: true });
    }
  }, [dictParam, id, navigate]);

  const dict = useMemo(() => {
    const v = (dictParam || "").trim().toLowerCase();
    if (v === "suppliers") return "counterparties";
    return isDictionaryKey(v) ? v : null;
  }, [dictParam]);

  const canEdit = useCan(dict ? dictEditPermission(dict) : "Admin.Mdm.Edit");
  const isReadOnly = true; // ExternalMaster for now

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [entity, setEntity] = useState<any>(null);

  const load = async () => {
    if (!dict || !id) return;
    setLoading(true);
    setError(null);
    try {
      const data = await getMdmDictionaryById<any>(dict, id);
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
  }, [dict, id]);

  if (!dict || !id) {
    return <Alert type="error" showIcon message={t("common.error.notFound")} />;
  }

  const title = dictTitle(dict);
  const isActive = Boolean(entity?.isActive);
  const showIsActiveInBody = dict !== "counterparties" && dict !== "suppliers";

  const headerDetails = (() => {
    if (!entity) return null;

    if (dict === "currencies") {
      const r = entity as MdmCurrencyReferenceDto;
      return (
        <Descriptions bordered size="small" column={2}>
          <Descriptions.Item label={t("references.columns.name")}>{r.name ?? "-"}</Descriptions.Item>
          <Descriptions.Item label={t("references.columns.code")}>{r.code ?? "-"}</Descriptions.Item>
          <Descriptions.Item label={t("references.mdm.currencies.columns.symbol")}>{r.symbol ?? "-"}</Descriptions.Item>
          <Descriptions.Item label={t("references.mdm.currencies.columns.rate")}>
            {typeof r.rate === "number" ? String(r.rate) : "-"}
          </Descriptions.Item>
        </Descriptions>
      );
    }

    if (dict === "manufacturers") {
      const r = entity as MdmManufacturerReferenceDto;
      const siteHref = r.site ? toHttpUrl(r.site) : null;
      return (
        <Descriptions bordered size="small" column={2}>
          <Descriptions.Item label={t("references.columns.name")}>{r.name ?? "-"}</Descriptions.Item>
          <Descriptions.Item label={t("references.columns.code")}>{r.code ?? "-"}</Descriptions.Item>
          <Descriptions.Item label={t("references.mdm.manufacturers.columns.fullName")}>{r.fullName ?? "-"}</Descriptions.Item>
          <Descriptions.Item label={t("references.mdm.manufacturers.columns.site")}>
            {r.site && siteHref ? (
              <a href={siteHref} target="_blank" rel="noreferrer">
                {r.site}
              </a>
            ) : (
              "-"
            )}
          </Descriptions.Item>
          <Descriptions.Item label={t("references.mdm.manufacturers.columns.note")}>{r.note ?? "-"}</Descriptions.Item>
        </Descriptions>
      );
    }

    if (dict !== "counterparties" && dict !== "suppliers") return null;

    const r = entity as MdmCounterpartyReferenceDto;
    const roles = (r.roles ?? []).filter((x) => x.isActive);
    const siteHref = r.site ? toHttpUrl(r.site) : null;

    return (
      <Descriptions bordered size="small" column={2}>
        <Descriptions.Item label={t("references.columns.name")}>{r.name ?? "-"}</Descriptions.Item>
        <Descriptions.Item label={t("references.columns.code")}>{r.code ?? "-"}</Descriptions.Item>
        <Descriptions.Item label={t("references.mdm.suppliers.columns.fullName")}>{r.fullName ?? "-"}</Descriptions.Item>
        <Descriptions.Item label={t("references.mdm.suppliers.columns.inn")}>{r.inn ?? "-"}</Descriptions.Item>
        <Descriptions.Item label={t("references.mdm.suppliers.columns.kpp")}>{r.kpp ?? "-"}</Descriptions.Item>
        <Descriptions.Item label={t("references.mdm.counterparties.columns.email")}>{r.email ?? "-"}</Descriptions.Item>
        <Descriptions.Item label={t("references.mdm.counterparties.columns.phone")}>{r.phone ?? "-"}</Descriptions.Item>
        <Descriptions.Item label={t("references.mdm.counterparties.columns.city")}>{r.city ?? "-"}</Descriptions.Item>
        <Descriptions.Item label={t("references.mdm.counterparties.columns.address")}>{r.address ?? "-"}</Descriptions.Item>
        <Descriptions.Item label={t("references.mdm.counterparties.columns.site")}>
          {r.site && siteHref ? (
            <a href={siteHref} target="_blank" rel="noreferrer">
              {r.site}
            </a>
          ) : (
            "-"
          )}
        </Descriptions.Item>
        <Descriptions.Item label={t("references.mdm.counterparties.columns.siteLogin")}>{r.siteLogin ?? "-"}</Descriptions.Item>
        <Descriptions.Item label={t("references.mdm.counterparties.columns.sitePassword")}>
          {r.sitePassword ? (
            <Space size={8}>
              <span>{showSitePassword ? r.sitePassword : "••••••"}</span>
              <a onClick={() => setShowSitePassword((v) => !v)}>{showSitePassword ? "Скрыть" : "Показать"}</a>
            </Space>
          ) : (
            "-"
          )}
        </Descriptions.Item>
        <Descriptions.Item label={t("references.mdm.counterparties.columns.roles")}>
          {roles.length ? (
            <Space size={4} wrap>
              {roles.map((x) => (
                <Tag key={x.roleType}>{x.roleType}</Tag>
              ))}
            </Space>
          ) : (
            "-"
          )}
        </Descriptions.Item>
        <Descriptions.Item label={t("references.mdm.counterparties.columns.note")}>{r.note ?? "-"}</Descriptions.Item>
      </Descriptions>
    );
  })();

  const extraFields = (() => {
    if (!entity) return null;
    if (dict === "items") {
      const r = entity as MdmItemReferenceDto;
      return (
        <>
          <Descriptions.Item label={t("references.mdm.items.columns.kind")}>{r.itemKind}</Descriptions.Item>
          <Descriptions.Item label={t("references.mdm.items.columns.uom")}>
            {r.unitOfMeasureCode ? `${r.unitOfMeasureSymbol ?? r.unitOfMeasureCode} — ${r.unitOfMeasureName ?? ""}` : "-"}
          </Descriptions.Item>
          <Descriptions.Item label={t("references.mdm.items.columns.eskd")}>
            {r.isEskd ? t("common.yes") : t("common.no")}
          </Descriptions.Item>
          <Descriptions.Item label={t("references.mdm.items.columns.mpn")}>{r.manufacturerPartNumber ?? "-"}</Descriptions.Item>
          <Descriptions.Item label={t("references.mdm.items.columns.external")}>
            {r.externalSystem && r.externalId ? `${r.externalSystem}:${r.externalId}` : "-"}
          </Descriptions.Item>
        </>
      );
    }
    if (dict === "counterparties" || dict === "suppliers") {
      const r = entity as MdmCounterpartyReferenceDto;
      const links = r.externalLinks ?? [];
      return (
        <>
          <Descriptions.Item label={t("references.mdm.counterparties.columns.externalLinks")}>
            {links.length ? (
              <Space size={4} wrap>
                {links.map((x) => (
                  <Tag key={`${x.externalSystem}:${x.externalEntity}:${x.externalId}`}>
                    {x.externalSystem}:{x.externalEntity}:{x.externalId}
                  </Tag>
                ))}
              </Space>
            ) : (
              "-"
            )}
          </Descriptions.Item>
        </>
      );
    }
    if (dict === "currencies") {
      const r = entity as MdmCurrencyReferenceDto;
      return (
        <>
          <Descriptions.Item label={t("references.mdm.currencies.columns.syncedAt")}>{r.syncedAt ?? "-"}</Descriptions.Item>
          <Descriptions.Item label="ExternalSystem">{r.externalSystem ?? "-"}</Descriptions.Item>
          <Descriptions.Item label="ExternalId">{r.externalId ?? "-"}</Descriptions.Item>
        </>
      );
    }
    if (dict === "manufacturers") {
      const r = entity as MdmManufacturerReferenceDto;
      return (
        <>
          <Descriptions.Item label={t("references.mdm.manufacturers.columns.syncedAt")}>{r.syncedAt ?? "-"}</Descriptions.Item>
          <Descriptions.Item label="ExternalSystem">{r.externalSystem ?? "-"}</Descriptions.Item>
          <Descriptions.Item label="ExternalId">{r.externalId ?? "-"}</Descriptions.Item>
        </>
      );
    }
    return null;
  })();

  const mainFields = (() => {
    if (dict === "units") {
      const r = entity as MdmUnitReferenceDto | null;
      return (
        <>
          <Descriptions.Item label={t("references.columns.name")}>{r?.name ?? "-"}</Descriptions.Item>
          <Descriptions.Item label={t("references.mdm.units.columns.symbol")}>{r?.symbol ?? "-"}</Descriptions.Item>
          <Descriptions.Item label={t("references.columns.code")}>{r?.code ?? "-"}</Descriptions.Item>
        </>
      );
    }

    if (dict === "counterparties" || dict === "suppliers") {
      return null;
    }

    if (dict === "currencies") {
      return null;
    }

    if (dict === "manufacturers") {
      return null;
    }

    return (
      <>
        <Descriptions.Item label={t("references.columns.code")}>
          {(entity as MdmSimpleReferenceDto | null)?.code ?? "-"}
        </Descriptions.Item>
        <Descriptions.Item label={t("references.columns.name")}>
          {(entity as MdmSimpleReferenceDto | null)?.name ?? "-"}
        </Descriptions.Item>
      </>
    );
  })();

  return (
    <div>
      <CommandBar
        left={
          <Space direction="vertical" size={8}>
            <Space size={8} align="center">
              <Title level={3} style={{ margin: 0 }}>
                {title}{" "}
                {entity?.code ? (
                  <Text type="secondary" style={{ fontSize: 14 }}>
                    <Text code>{String(entity.code)}</Text>
                  </Text>
                ) : null}
              </Title>
              <Tag color={isActive ? "green" : undefined}>{isActive ? "ON" : "OFF"}</Tag>
            </Space>
            <Text type="secondary">
              {t("references.mdm.card.readOnly")}
            </Text>
          </Space>
        }
        right={
          <Space>
            {canEdit && (
              <Tooltip title={isReadOnly ? t("references.mdm.actions.disabledExternalMaster") : undefined}>
                <Button disabled={isReadOnly} data-testid="mdm-card-edit">
                  {t("common.actions.edit")}
                </Button>
              </Tooltip>
            )}
            <Button onClick={() => navigate(`/references/mdm/${dict}`)}>{t("common.actions.back")}</Button>
          </Space>
        }
      />

      {error && <Alert type="error" showIcon message={error} style={{ marginBottom: 12 }} />}

      <Tabs
        defaultActiveKey="general"
        items={[
          {
            key: "general",
            label: "Общее",
            children: (
              <Card loading={loading}>
                {(dict === "counterparties" || dict === "suppliers" || dict === "currencies" || dict === "manufacturers")
                  ? headerDetails
                  : (
                    <Descriptions bordered size="small" column={1}>
                      {mainFields}
                      {showIsActiveInBody && (
                        <Descriptions.Item label={t("references.columns.isActive")}>
                          {isActive ? <Tag color="green">ON</Tag> : <Tag>OFF</Tag>}
                        </Descriptions.Item>
                      )}
                      {extraFields}
                    </Descriptions>
                  )}
              </Card>
            ),
          },
          {
            key: "integrations",
            label: "Интеграции",
            children: (
              <Card loading={loading}>
                <Descriptions bordered size="small" column={2}>
                  <Descriptions.Item label="Id" span={2}>
                    <Text code>{String((entity as any)?.id ?? "")}</Text>
                  </Descriptions.Item>

                  {dict === "units" && (
                    <>
                      <Descriptions.Item label="Обновлено">
                        {(entity as MdmUnitReferenceDto | null)?.updatedAt ?? "-"}
                      </Descriptions.Item>
                      <Descriptions.Item label="ExternalSystem">
                        {(entity as MdmUnitReferenceDto | null)?.externalSystem ?? "-"}
                      </Descriptions.Item>
                      <Descriptions.Item label="ExternalId">
                        {(entity as MdmUnitReferenceDto | null)?.externalId ?? "-"}
                      </Descriptions.Item>
                      <Descriptions.Item label={t("references.mdm.currencies.columns.syncedAt")}>
                        {(entity as MdmUnitReferenceDto | null)?.syncedAt ?? "-"}
                      </Descriptions.Item>
                    </>
                  )}
                  {(dict === "counterparties" || dict === "suppliers") && (
                    <>
                      <Descriptions.Item label={t("references.mdm.counterparties.columns.updatedAt")}>
                        {(entity as MdmCounterpartyReferenceDto | null)?.updatedAt ?? "-"}
                      </Descriptions.Item>
                      <Descriptions.Item label="ExternalSystem">
                        {(entity as MdmCounterpartyReferenceDto | null)?.externalLinks?.[0]?.externalSystem ?? "-"}
                      </Descriptions.Item>
                      <Descriptions.Item label="ExternalId">
                        {(entity as MdmCounterpartyReferenceDto | null)?.externalLinks?.[0]?.externalId ?? "-"}
                      </Descriptions.Item>
                      <Descriptions.Item label="SyncedAt">
                        {(entity as MdmCounterpartyReferenceDto | null)?.externalLinks?.[0]?.syncedAt ?? "-"}
                      </Descriptions.Item>
                    </>
                  )}
                  {dict === "currencies" && (
                    <Descriptions.Item label={t("references.mdm.currencies.columns.updatedAt")}>
                      {(entity as MdmCurrencyReferenceDto | null)?.updatedAt ?? "-"}
                    </Descriptions.Item>
                  )}
                  {dict === "manufacturers" && (
                    <Descriptions.Item label={t("references.mdm.manufacturers.columns.updatedAt")}>
                      {(entity as MdmManufacturerReferenceDto | null)?.updatedAt ?? "-"}
                    </Descriptions.Item>
                  )}

                  {(dict === "counterparties" || dict === "suppliers" || dict === "currencies" || dict === "manufacturers")
                    ? extraFields
                    : null}
                </Descriptions>
              </Card>
            ),
          },
        ]}
      />
    </div>
  );
};
