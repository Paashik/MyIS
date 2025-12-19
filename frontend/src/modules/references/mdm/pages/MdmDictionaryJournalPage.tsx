import React, { useEffect, useMemo, useState } from "react";
import { Alert, Button, Card, Dropdown, Input, Modal, Select, Space, Table, Tag, Typography, message } from "antd";
import Tooltip from "antd/es/tooltip";
import type { ColumnsType } from "antd/es/table";
import { useNavigate, useParams } from "react-router-dom";
import type { MenuProps } from "antd";

import { CommandBar } from "../../../../components/ui/CommandBar";
import { t } from "../../../../core/i18n/t";
import { useCan } from "../../../../core/auth/permissions";
import {
  getComponent2020Connection,
  runComponent2020Sync,
} from "../../../settings/integrations/component2020/api/adminComponent2020Api";
import { Component2020SyncMode, Component2020SyncScope } from "../../../settings/integrations/component2020/api/types";
import type {
  MdmDictionaryKey,
  MdmCounterpartyReferenceDto,
  MdmCurrencyReferenceDto,
  MdmItemReferenceDto,
  MdmManufacturerReferenceDto,
  MdmSimpleReferenceDto,
  MdmSupplierReferenceDto,
  MdmUnitReferenceDto,
} from "../api/adminMdmReferencesApi";
import { getMdmDictionaryList } from "../api/adminMdmReferencesApi";

const { Title, Text } = Typography;

type DictKeyParam = { dict?: string };

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

function dictToComponent2020Scope(dict: MdmDictionaryKey): Component2020SyncScope {
  switch (dict) {
    case "units":
      return Component2020SyncScope.Units;
    case "counterparties":
      return Component2020SyncScope.Counterparties;
    case "suppliers":
      return Component2020SyncScope.Suppliers;
    case "items":
      return Component2020SyncScope.Items;
    case "manufacturers":
      return Component2020SyncScope.Manufacturers;
    case "body-types":
      return Component2020SyncScope.BodyTypes;
    case "currencies":
      return Component2020SyncScope.Currencies;
    case "technical-parameters":
      return Component2020SyncScope.TechnicalParameters;
    case "parameter-sets":
      return Component2020SyncScope.ParameterSets;
    case "symbols":
      return Component2020SyncScope.Symbols;
  }
}

function toHttpUrl(raw: string): string | null {
  const v = raw.trim();
  if (!v) return null;
  if (/^javascript:/i.test(v)) return null;
  if (/^https?:\/\//i.test(v)) return v;
  if (v.startsWith("//")) return `https:${v}`;
  return `https://${v}`;
}

export const MdmDictionaryJournalPage: React.FC = () => {
  const canView = useCan("Admin.Integration.View");
  const canExecute = useCan("Admin.Integration.Execute");
  const navigate = useNavigate();
  const { dict: dictParam } = useParams<DictKeyParam>();

  useEffect(() => {
    const v = (dictParam || "").trim().toLowerCase();
    if (v === "suppliers") {
      navigate("/references/mdm/counterparties", { replace: true });
    }
  }, [dictParam, navigate]);

  const dict = useMemo(() => {
    const v = (dictParam || "").trim().toLowerCase();
    if (v === "suppliers") return "counterparties";
    return isDictionaryKey(v) ? v : null;
  }, [dictParam]);

  const canEdit = useCan(dict ? dictEditPermission(dict) : "Admin.Mdm.Edit");
  const isReadOnly = true; // ExternalMaster for now

  const [loading, setLoading] = useState(false);
  const [importLoading, setImportLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [q, setQ] = useState("");
  const [isActive, setIsActive] = useState<"all" | "active" | "inactive">("active");
  const [roleType, setRoleType] = useState<"all" | "1" | "2">("all");

  const [items, setItems] = useState<unknown[]>([]);
  const [total, setTotal] = useState(0);

  const importMenuItems: MenuProps["items"] = useMemo(
    () => [
      { key: "delta", label: t("references.mdm.import.delta") },
      { key: "snapshotUpsert", label: t("references.mdm.import.snapshotUpsert") },
      { key: "overwrite", label: t("references.mdm.import.overwrite"), danger: true },
    ],
    []
  );

  const columns: ColumnsType<any> = useMemo(() => {
    if (!dict) return [];

	    if (dict === "items") {
	      const cols: ColumnsType<MdmItemReferenceDto> = [
        { title: t("references.columns.code"), dataIndex: "code", key: "code", width: 140 },
        { title: t("references.columns.name"), dataIndex: "name", key: "name" },
        { title: t("references.mdm.items.columns.kind"), dataIndex: "itemKind", key: "itemKind", width: 160 },
        {
          title: t("references.mdm.items.columns.uom"),
          key: "uom",
          width: 220,
          render: (_: unknown, r: MdmItemReferenceDto) => (
            <Text type="secondary">
              {r.unitOfMeasureCode
                ? `${r.unitOfMeasureSymbol ?? r.unitOfMeasureCode} — ${r.unitOfMeasureName ?? ""}`
                : "-"}
            </Text>
          ),
        },
        {
          title: t("references.columns.isActive"),
          dataIndex: "isActive",
          key: "isActive",
          width: 110,
          render: (v: boolean) => (v ? <Tag color="green">ON</Tag> : <Tag>OFF</Tag>),
        },
	      ];
	      return cols as ColumnsType<any>;
	    }

	    if (dict === "units") {
	      const cols: ColumnsType<MdmUnitReferenceDto> = [
	        { title: t("references.columns.name"), dataIndex: "name", key: "name" },
	        { title: t("references.mdm.units.columns.symbol"), dataIndex: "symbol", key: "symbol", width: 140 },
	        {
	          title: t("references.columns.code"),
	          dataIndex: "code",
	          key: "code",
	          width: 120,
	          render: (v: string | null | undefined) => v ?? "-",
	        },
	        {
	          title: t("references.columns.isActive"),
	          dataIndex: "isActive",
	          key: "isActive",
	          width: 110,
	          render: (v: boolean) => (v ? <Tag color="green">ON</Tag> : <Tag>OFF</Tag>),
	        },
	      ];
	      return cols as ColumnsType<any>;
	    }

	    if (dict === "counterparties") {
	      const cols: ColumnsType<MdmCounterpartyReferenceDto> = [
          {
            title: t("references.columns.code"),
            dataIndex: "code",
            key: "code",
            width: 160,
            render: (v: string | null | undefined) => v ?? "-",
          },
          { title: t("references.columns.name"), dataIndex: "name", key: "name" },
          { title: t("references.mdm.suppliers.columns.fullName"), dataIndex: "fullName", key: "fullName" },
          { title: t("references.mdm.suppliers.columns.inn"), dataIndex: "inn", key: "inn", width: 140 },
          { title: t("references.mdm.counterparties.columns.city"), dataIndex: "city", key: "city", width: 140 },
          { title: t("references.mdm.counterparties.columns.address"), dataIndex: "address", key: "address" },
          { title: t("references.mdm.counterparties.columns.email"), dataIndex: "email", key: "email", width: 160 },
          {
            title: t("references.mdm.counterparties.columns.site"),
            dataIndex: "site",
            key: "site",
            width: 160,
            render: (v: string | null | undefined) => {
              const href = v ? toHttpUrl(v) : null;
              if (!v || !href) return <Text type="secondary">-</Text>;
              return (
                <a
                  href={href}
                  target="_blank"
                  rel="noreferrer"
                  onClick={(e) => e.stopPropagation()}
                >
                  {v}
                </a>
              );
            },
          },
          { title: t("references.mdm.counterparties.columns.phone"), dataIndex: "phone", key: "phone", width: 140 },
          { title: t("references.mdm.counterparties.columns.note"), dataIndex: "note", key: "note" },
          {
            title: t("references.columns.isActive"),
            dataIndex: "isActive",
            key: "isActive",
            width: 110,
            render: (v: boolean) => (v ? <Tag color="green">ON</Tag> : <Tag>OFF</Tag>),
          },
        ];
        return cols as ColumnsType<any>;
	    }

	    if (dict === "currencies") {
	      const cols: ColumnsType<MdmCurrencyReferenceDto> = [
          {
            title: t("references.columns.code"),
            dataIndex: "code",
            key: "code",
            width: 120,
            render: (v: string | null | undefined) => v ?? "-",
          },
          { title: t("references.columns.name"), dataIndex: "name", key: "name", width: 240 },
          { title: t("references.mdm.currencies.columns.symbol"), dataIndex: "symbol", key: "symbol", width: 90, render: (v: string | null | undefined) => v ?? "-" },
          { title: t("references.mdm.currencies.columns.rate"), dataIndex: "rate", key: "rate", width: 110, render: (v: number | null | undefined) => (typeof v === "number" ? String(v) : "-") },
          {
            title: t("references.columns.isActive"),
            dataIndex: "isActive",
            key: "isActive",
            width: 110,
            render: (v: boolean) => (v ? <Tag color="green">ON</Tag> : <Tag>OFF</Tag>),
          },
          { title: t("references.mdm.currencies.columns.updatedAt"), dataIndex: "updatedAt", key: "updatedAt", width: 190 },
        ];
        return cols as ColumnsType<any>;
	    }

	    if (dict === "manufacturers") {
	      const cols: ColumnsType<MdmManufacturerReferenceDto> = [
          { title: t("references.columns.code"), dataIndex: "code", key: "code", width: 160, render: (v: string | null | undefined) => v ?? "-" },
          { title: t("references.columns.name"), dataIndex: "name", key: "name", width: 220 },
          { title: t("references.mdm.manufacturers.columns.fullName"), dataIndex: "fullName", key: "fullName" },
          {
            title: t("references.mdm.manufacturers.columns.site"),
            dataIndex: "site",
            key: "site",
            width: 180,
            render: (v: string | null | undefined) => {
              const href = v ? toHttpUrl(v) : null;
              if (!v || !href) return <Text type="secondary">-</Text>;
              return (
                <a href={href} target="_blank" rel="noreferrer" onClick={(e) => e.stopPropagation()}>
                  {v}
                </a>
              );
            },
          },
          { title: t("references.mdm.manufacturers.columns.note"), dataIndex: "note", key: "note" },
          {
            title: t("references.columns.isActive"),
            dataIndex: "isActive",
            key: "isActive",
            width: 110,
            render: (v: boolean) => (v ? <Tag color="green">ON</Tag> : <Tag>OFF</Tag>),
          },
        ];
        return cols as ColumnsType<any>;
	    }

	    if (dict === "suppliers") {
	      const cols: ColumnsType<MdmSupplierReferenceDto> = [
        { title: t("references.columns.code"), dataIndex: "code", key: "code", width: 160 },
        { title: t("references.columns.name"), dataIndex: "name", key: "name" },
        { title: t("references.mdm.suppliers.columns.inn"), dataIndex: "inn", key: "inn", width: 140 },
        {
          title: t("references.columns.isActive"),
          dataIndex: "isActive",
          key: "isActive",
          width: 110,
          render: (v: boolean) => (v ? <Tag color="green">ON</Tag> : <Tag>OFF</Tag>),
        },
      ];
      return cols as ColumnsType<any>;
    }

    const cols: ColumnsType<MdmSimpleReferenceDto> = [
      { title: t("references.columns.code"), dataIndex: "code", key: "code", width: 180 },
      { title: t("references.columns.name"), dataIndex: "name", key: "name" },
      {
        title: t("references.columns.isActive"),
        dataIndex: "isActive",
        key: "isActive",
        width: 110,
        render: (v: boolean) => (v ? <Tag color="green">ON</Tag> : <Tag>OFF</Tag>),
      },
    ];
    return cols as ColumnsType<any>;
  }, [dict]);

  const load = async () => {
    if (!dict) return;
    setLoading(true);
    setError(null);
    try {
      const activeFilter =
        isActive === "all" ? undefined : isActive === "active" ? true : false;
      const resp = await getMdmDictionaryList<any>(dict, {
        q: q.trim() || undefined,
        isActive: activeFilter,
        roleType: dict === "counterparties" && roleType !== "all" ? roleType : undefined,
      });
      setItems(resp.items);
      setTotal(resp.total);
    } catch (e) {
      setError((e as Error).message);
      setItems([]);
      setTotal(0);
    } finally {
      setLoading(false);
    }
  };

  const runImport = async (syncMode: Component2020SyncMode) => {
    if (!dict) return;

    setImportLoading(true);
    try {
      const toastKey = "mdm-import";
      message.loading({ key: toastKey, content: t("references.mdm.import.running"), duration: 0 });

      const connection = await getComponent2020Connection();
      const connectionId = connection.id;

      if (!connectionId || connection.isActive === false) {
        message.error({ key: toastKey, content: t("references.mdm.import.noActiveConnection"), duration: 6 });
        return;
      }

      const scope = dictToComponent2020Scope(dict);
      const resp = await runComponent2020Sync({
        connectionId,
        scope,
        dryRun: false,
        syncMode,
      });

      message.success({
        key: toastKey,
        duration: 6,
        content: (
          <Space size={8}>
            <span>
              {t("references.mdm.import.started", { status: resp.status })}{" "}
              ({resp.processedCount}) [{String(syncMode)}/{String(scope)}]
            </span>
            <Button
              type="link"
              size="small"
              onClick={() =>
                navigate(
                  `/administration/integrations/component2020/runs/${encodeURIComponent(resp.runId)}`
                )
              }
            >
              {t("common.actions.open")}
            </Button>
          </Space>
        ),
      });
      await load();
    } catch (e) {
      message.error({ key: "mdm-import", content: (e as Error).message, duration: 6 });
    } finally {
      setImportLoading(false);
    }
  };

  const confirmOverwrite = () => {
    Modal.confirm({
      title: t("references.mdm.import.overwrite.confirmTitle"),
      content: t("references.mdm.import.overwrite.confirmBody"),
      okText: t("references.mdm.import.overwrite.confirmOk"),
      okType: "danger",
      closable: true,
      cancelText: t("common.actions.cancel"),
      onOk: async () => runImport(Component2020SyncMode.Overwrite),
    });
  };

  const onImportMenuClick: MenuProps["onClick"] = ({ key }: { key: string }) => {
    if (key === "delta") {
      void runImport(Component2020SyncMode.Delta);
      return;
    }

    if (key === "snapshotUpsert") {
      void runImport(Component2020SyncMode.SnapshotUpsert);
      return;
    }

    if (key === "overwrite") {
      confirmOverwrite();
    }
  };

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [dict, isActive, roleType]);

  const onOpen = (id: string) => {
    if (!dict) return;
    navigate(`/references/mdm/${dict}/${encodeURIComponent(id)}`);
  };

  if (!dict) {
    return (
      <Alert type="error" showIcon message={t("common.error.notFound")} />
    );
  }

  return (
    <div>
      <CommandBar
        left={
          <Space direction="vertical" size={0}>
            <Title level={2} style={{ margin: 0 }}>
              {dictTitle(dict)}
            </Title>
            <Text type="secondary">{t("references.mdm.readOnlyHint")}</Text>
          </Space>
        }
        right={
          <Space>
            <Tooltip
              title={!canExecute ? t("settings.forbidden") : undefined}
              placement="bottom"
            >
              <Dropdown.Button
                trigger={["click"]}
                loading={importLoading}
                disabled={!canExecute}
                menu={{ items: importMenuItems, onClick: onImportMenuClick }}
                onClick={() => void runImport(Component2020SyncMode.SnapshotUpsert)}
                data-testid="mdm-dict-import"
              >
                {t("references.mdm.import.button")}
              </Dropdown.Button>
            </Tooltip>
            <Input.Search
              allowClear
              value={q}
              onChange={(e: React.ChangeEvent<HTMLInputElement>) => setQ(e.target.value)}
              onSearch={() => void load()}
              placeholder={t("common.search")}
              style={{ width: 280 }}
            />
            <Select
              value={isActive}
              style={{ width: 160 }}
              options={[
                { value: "all", label: t("references.filters.all") },
                { value: "active", label: t("references.filters.active") },
                { value: "inactive", label: t("references.filters.inactive") },
              ]}
              onChange={(v: "all" | "active" | "inactive") => setIsActive(v)}
            />
            {dict === "counterparties" && (
              <Select
                value={roleType}
                style={{ width: 180 }}
                options={[
                  { value: "all", label: "Все роли" },
                  { value: "1", label: "Поставщик" },
                  { value: "2", label: "Заказчик" },
                ]}
                onChange={(v: "all" | "1" | "2") => setRoleType(v)}
              />
            )}
            <Button onClick={() => void load()} data-testid="mdm-dict-refresh">
              {t("common.actions.refresh")}
            </Button>
            {canEdit && (
              <Tooltip title={isReadOnly ? t("references.mdm.actions.disabledExternalMaster") : undefined}>
                <Button type="primary" disabled={isReadOnly} data-testid="mdm-dict-create">
                  {t("common.actions.create")}
                </Button>
              </Tooltip>
            )}
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
      {canView && !canExecute && (
        <Alert
          type="warning"
          showIcon
          message={t("settings.forbidden")}
          description={t("references.mdm.import.noPermission")}
          style={{ marginBottom: 12 }}
        />
      )}

      {error && (
        <Alert type="error" showIcon message={error} style={{ marginBottom: 12 }} />
      )}

      {isReadOnly && (
        <Alert
          type="info"
          showIcon
          message={t("references.mdm.externalMaster.title")}
          description={t("references.mdm.externalMaster.description")}
          style={{ marginBottom: 12 }}
        />
      )}

      <Card>
        <Table
          data-testid="mdm-dict-table"
          rowKey={(r: any) => r.id}
          loading={loading}
          columns={columns}
          dataSource={items}
          pagination={{ pageSize: 50, total, showSizeChanger: false }}
          onRow={(record: any) => ({
            onClick: () => onOpen(String(record.id)),
          })}
        />
      </Card>
    </div>
  );
};
