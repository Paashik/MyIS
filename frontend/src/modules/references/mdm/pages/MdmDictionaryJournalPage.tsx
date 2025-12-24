import React, { useCallback, useEffect, useMemo, useState } from "react";
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
  MdmExternalEntityLinkDto,
  MdmItemGroupReferenceDto,
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
    v === "item-groups" ||
    v === "items" ||
    v === "manufacturers" ||
    v === "body-types" ||
    v === "currencies" ||
    v === "technical-parameters" ||
    v === "parameter-sets" ||
    v === "symbols" ||
    v === "external-links"
  );
}

function dictTitle(dict: MdmDictionaryKey): string {
  switch (dict) {
    case "units":
      return t("references.mdm.units.title");
    case "counterparties":
    case "suppliers":
      return t("references.mdm.counterparties.title");
    case "item-groups":
      return t("references.mdm.itemGroups.title");
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
    case "external-links":
      return t("references.mdm.externalLinks.title");
  }
}

function dictEditPermission(dict: MdmDictionaryKey): string {
  switch (dict) {
    case "units":
      return "Admin.Mdm.EditUnits";
    case "counterparties":
    case "suppliers":
      return "Admin.Mdm.EditSuppliers";
    case "item-groups":
      return "Admin.Mdm.EditItems";
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
    case "external-links":
      return "Admin.Integration.View";
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
    case "item-groups":
      return Component2020SyncScope.ItemGroups;
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
    case "external-links":
      throw new Error("External links do not support import.");
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

type ItemGroupTreeRow = MdmItemGroupReferenceDto & { children?: ItemGroupTreeRow[] };

function normalizeTextForSearch(v: string): string {
  return v.trim().toLowerCase();
}

function buildItemGroupsTreeRows(flat: MdmItemGroupReferenceDto[], q: string): ItemGroupTreeRow[] {

  
  const ids = flat.map(r => r.id);

  const uniqueIds = new Set(ids);
  if (uniqueIds.size !== ids.length) {

    const seen = new Set<string>();
    flat = flat.filter(r => {
      const id = String(r.id ?? "").trim();
      if (seen.has(id)) return false;
      seen.add(id);
      return true;
    });
  }

  const active = flat.find(r => r.name.includes('Активные'));
  const pokup = flat.find(r => r.name.includes('Покупные'));



  const normalizedQuery = normalizeTextForSearch(q);

  const byId = new Map<string, ItemGroupTreeRow>();
  const parentById = new Map<string, string | null>();

  for (const r of flat) {
    const id = String(r.id ?? "").trim();
    if (!id) continue;

    const treeNode: ItemGroupTreeRow = { ...r, children: [] };
    byId.set(id, treeNode);

    const parentIdValue = r.parentId;
    const normalizedParent: string | null = parentIdValue == null || String(parentIdValue).trim() === "0" 
      ? null 
      : String(parentIdValue).trim();

    parentById.set(id, normalizedParent);
  }

  let allowedIds: Set<string> | null = null;
  if (normalizedQuery) {
    allowedIds = new Set<string>();

    const isMatch = (r: MdmItemGroupReferenceDto) => {
      if (normalizeTextForSearch(r.name).includes(normalizedQuery)) return true;
      if (r.abbreviation && normalizeTextForSearch(r.abbreviation).includes(normalizedQuery)) return true;
      return false;
    };

    for (const r of flat) {
      if (!isMatch(r)) continue;

      let currentId: string | null = String(r.id ?? "").trim();
      const visited = new Set<string>();

      while (currentId && !visited.has(currentId)) {
        visited.add(currentId);
        allowedIds.add(currentId);

        const parentId = parentById.get(currentId);
        if (!parentId || parentId === currentId) break;
        currentId = parentId;
      }
    }
  }

  const roots: ItemGroupTreeRow[] = [];

  for (const [id, node] of byId.entries()) {
    if (allowedIds && !allowedIds.has(id)) continue;

    const parentId = parentById.get(id);

    // Родитель существует в данных только если parentId не null и есть в byId
    const parentNode = parentId != null ? byId.get(parentId) ?? null : null;

    let hasCycle = false;
    if (parentId != null) {
      let currentId: string | null = parentId;
      const visited = new Set<string>();
      while (currentId) {
        if (currentId === id) {
          hasCycle = true;
          break;
        }
        if (visited.has(currentId)) break;
        visited.add(currentId);
        currentId = parentById.get(currentId) ?? null;
      }
    }

    const parentAllowed = !allowedIds || (parentId != null && allowedIds.has(parentId));



    if (!hasCycle && parentNode && parentAllowed) {
      parentNode.children!.push(node);
      continue;
    }

    roots.push(node);
  }

  const sortRecursive = (nodes: ItemGroupTreeRow[]) => {
    nodes.sort((a, b) => 
      (a.name ?? "").localeCompare(b.name ?? "") || 
      String(a.id).localeCompare(String(b.id))
    );
    for (const n of nodes) {
      if (n.children?.length) {
        sortRecursive(n.children);
      }
    }
  };

  sortRecursive(roots);






  return roots;
}

function collectExpandableRowKeys(rows: ItemGroupTreeRow[]): React.Key[] {
  const keys: React.Key[] = [];

  const visit = (nodes: ItemGroupTreeRow[]) => {
    for (const n of nodes) {
      if (n.children?.length) {
        keys.push(n.id);
        visit(n.children);
      }
    }
  };

  visit(rows);
  return keys;
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
  const supportsImport = dict !== "external-links";
  const supportsIsActive = dict !== "external-links";
  const isItemGroups = dict === "item-groups";
  const supportsRowOpen = dict !== "external-links" && !isItemGroups;

  const [loading, setLoading] = useState(false);
  const [importLoading, setImportLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [q, setQ] = useState("");
  const [isActive, setIsActive] = useState<"all" | "active" | "inactive">("active");
  const [roleType, setRoleType] = useState<"all" | "1" | "2">("all");

  const [items, setItems] = useState<unknown[]>([]);
  const [total, setTotal] = useState(0);
  const [itemGroupsExpandedRowKeys, setItemGroupsExpandedRowKeys] = useState<React.Key[]>([]);

  useEffect(() => {
    if (dict === "item-groups" && isActive !== "all") {
      setIsActive("all");
    }
  }, [dict, isActive]);

  const importMenuItems: MenuProps["items"] = useMemo(
    () => [
      { key: "delta", label: t("references.mdm.import.delta") },
      { key: "snapshotUpsert", label: t("references.mdm.import.snapshotUpsert") },
      { key: "overwrite", label: t("references.mdm.import.overwrite"), danger: true },
    ],
    []
  );

  const onOpen = useCallback(
    (id: string) => {
      if (!dict || dict === "external-links") return;
      navigate(`/references/mdm/${dict}/${encodeURIComponent(id)}`);
    },
    [dict, navigate]
  );

  const columns: ColumnsType<any> = useMemo(() => {
    if (!dict) return [];

    if (dict === "items") {
      const cols: ColumnsType<MdmItemReferenceDto> = [
        { title: t("references.columns.code"), dataIndex: "nomenclatureNo", key: "nomenclatureNo", width: 160 },
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

    if (dict === "item-groups") {
      const cols: ColumnsType<MdmItemGroupReferenceDto> = [
        {
          title: t("references.columns.name"),
          dataIndex: "name",
          key: "name",
          render: (_: unknown, r: MdmItemGroupReferenceDto) => (
            <Space direction="vertical" size={0}>
              <Typography.Link
                onClick={(e: React.MouseEvent<HTMLElement>) => { e.stopPropagation(); onOpen(r.id); }}
              >
                <Text strong={!r.parentId}>{r.name}</Text>
              </Typography.Link>
              {r.abbreviation ? <Text type="secondary">{r.abbreviation}</Text> : null}
            </Space>
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

    if (dict === "external-links") {
      const cols: ColumnsType<MdmExternalEntityLinkDto> = [
        { title: t("references.mdm.externalLinks.columns.entityType"), dataIndex: "entityType", key: "entityType", width: 160 },
        {
          title: t("references.mdm.externalLinks.columns.entityId"),
          dataIndex: "entityId",
          key: "entityId",
          width: 220,
          render: (v: string) => <Text code>{v}</Text>,
        },
        { title: t("references.mdm.externalLinks.columns.externalSystem"), dataIndex: "externalSystem", key: "externalSystem", width: 160 },
        { title: t("references.mdm.externalLinks.columns.externalEntity"), dataIndex: "externalEntity", key: "externalEntity", width: 160 },
        { title: t("references.mdm.externalLinks.columns.externalId"), dataIndex: "externalId", key: "externalId", width: 180 },
        {
          title: t("references.mdm.externalLinks.columns.sourceType"),
          dataIndex: "sourceType",
          key: "sourceType",
          width: 120,
          render: (v: number | null | undefined) => (typeof v === "number" ? String(v) : "-"),
        },
        { title: t("references.mdm.externalLinks.columns.syncedAt"), dataIndex: "syncedAt", key: "syncedAt", width: 190 },
        { title: t("references.mdm.externalLinks.columns.updatedAt"), dataIndex: "updatedAt", key: "updatedAt", width: 190 },
      ];
      return cols as ColumnsType<any>;
    }

    if (dict === "counterparties") {
      const cols: ColumnsType<MdmCounterpartyReferenceDto> = [
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
  }, [dict, onOpen]);

  const itemGroupsTreeData = useMemo(() => {
    if (!isItemGroups) return null;
    return buildItemGroupsTreeRows((items as MdmItemGroupReferenceDto[]) ?? [], q);
  }, [isItemGroups, items, q]);

  const itemGroupsRootCount = useMemo(() => {
    if (!isItemGroups) return 0;
    return ((items as MdmItemGroupReferenceDto[]) ?? []).filter((x) => !x.parentId).length;
  }, [isItemGroups, items]);

  const itemGroupsExpandableKeys = useMemo(() => {
    if (!itemGroupsTreeData) return [];
    return collectExpandableRowKeys(itemGroupsTreeData);
  }, [itemGroupsTreeData]);

  useEffect(() => {
    if (!isItemGroups) return;
    if (q.trim()) {
      setItemGroupsExpandedRowKeys(itemGroupsExpandableKeys);
      return;
    }
    setItemGroupsExpandedRowKeys([]); // Collapsed by default
  }, [isItemGroups, q, itemGroupsExpandableKeys]);

  const load = async () => {
    if (!dict) return;
    setLoading(true);
    setError(null);
    try {
      const activeFilter = supportsIsActive
        ? isActive === "all"
          ? undefined
          : isActive === "active"
            ? true
            : false
        : undefined;
      const roleTypeFilter = dict === "counterparties" && roleType !== "all" ? roleType : undefined;

      if (dict === "item-groups") {
        const take = 1000;
        let skip = 0;
        let totalCount = 0;
        let all: unknown[] = [];

        while (true) {
          const page = await getMdmDictionaryList<any>(dict, { isActive: activeFilter, skip, take });
          if (skip === 0) totalCount = page.total;
          all = all.concat(page.items);

          if (all.length >= page.total || page.items.length === 0) break;

          skip += take;
          if (skip > 200_000) break;
        }

        setItems(all);
        setTotal(totalCount);
        return;
      }

      const resp = await getMdmDictionaryList<any>(dict, {
        q: q.trim() || undefined,
        isActive: activeFilter,
        roleType: roleTypeFilter,
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
    if (!dict || dict === "external-links") return;

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
            {supportsImport && (
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
            )}
            {isItemGroups && (
              <Space>
                <Button
                  onClick={() => setItemGroupsExpandedRowKeys(itemGroupsExpandableKeys)}
                  disabled={!itemGroupsExpandableKeys.length}
                >
                  {t("common.actions.expandAll")}
                </Button>
                <Button
                  onClick={() => setItemGroupsExpandedRowKeys([])}
                  disabled={!itemGroupsExpandedRowKeys.length}
                >
                  {t("common.actions.collapseAll")}
                </Button>
              </Space>
            )}
            <Input.Search
              allowClear
              value={q}
              onChange={(e: React.ChangeEvent<HTMLInputElement>) => setQ(e.target.value)}
              onSearch={() => { if (!isItemGroups) void load(); }}
              placeholder={t("common.search")}
              style={{ width: 280 }}
            />
            {supportsIsActive && (
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
            )}
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
            {canEdit && dict !== "external-links" && (
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
      {supportsImport && canView && !canExecute && (
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

      {isItemGroups && items.length > 0 && itemGroupsRootCount === 0 && (
        <Alert
          type="warning"
          showIcon
          message="Не найдены корневые группы"
          description="В данных нет записей с пустым ParentId (корней). Проверьте Access.Groups.Parent (для корня должно быть 0/пусто) и импорт/синхронизацию."
          style={{ marginBottom: 12 }}
        />
      )}

      <Card>
        <Table
          data-testid="mdm-dict-table"
          rowKey={(r: any) => r.id}
          loading={loading}
          columns={columns}
          dataSource={isItemGroups ? (itemGroupsTreeData ?? []) : items}
          pagination={isItemGroups ? false : { pageSize: 50, total, showSizeChanger: false }}
          expandable={
            isItemGroups
              ? {
                  expandedRowKeys: itemGroupsExpandedRowKeys,
                  onExpandedRowsChange: (keys: readonly React.Key[]) => setItemGroupsExpandedRowKeys(Array.from(keys)),
                }
              : undefined
          }
          onRow={
            supportsRowOpen
              ? (record: any) => ({
                  onClick: () => onOpen(String(record.id)),
                })
              : undefined
          }
        />
      </Card>
    </div>
  );
};
