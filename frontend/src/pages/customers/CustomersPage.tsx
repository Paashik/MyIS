import React, { useCallback, useEffect, useMemo, useState } from "react";
import { Alert, Button, Dropdown, Input, Modal, Select, Space, Table, Typography, message } from "antd";
import type { ColumnsType } from "antd/es/table";
import type { MenuProps } from "antd";
import { useNavigate } from "react-router-dom";

import { CommandBar } from "../../components/ui/CommandBar";
import { useCan } from "../../core/auth/permissions";
import { t } from "../../core/i18n/t";
import { getCustomerOrders } from "../../modules/customers/api/customerOrdersApi";
import type { CustomerOrderListItemDto } from "../../modules/customers/api/types";
import { getRequestCounterparties } from "../../modules/requests/api/requestsApi";
import type { RequestCounterpartyLookupDto } from "../../modules/requests/api/types";
import {
  getComponent2020Connection,
  runComponent2020Sync,
} from "../../modules/settings/integrations/component2020/api/adminComponent2020Api";
import { Component2020SyncMode, Component2020SyncScope } from "../../modules/settings/integrations/component2020/api/types";

const CustomersPage: React.FC = () => {
  const canExecuteImport = useCan("Admin.Integration.Execute");
  const navigate = useNavigate();

  const [items, setItems] = useState<CustomerOrderListItemDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [importLoading, setImportLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [search, setSearch] = useState("");
  const [customerFilter, setCustomerFilter] = useState<string | undefined>();
  const [customerOptions, setCustomerOptions] = useState<RequestCounterpartyLookupDto[]>([]);
  const [customersLoading, setCustomersLoading] = useState(false);
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [totalCount, setTotalCount] = useState(0);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await getCustomerOrders({
        q: search.trim() ? search.trim() : undefined,
        pageNumber,
        pageSize,
        customerId: customerFilter,
      });
      setItems(data.items);
      setTotalCount(data.totalCount);
    } catch (e) {
      setError((e as Error).message || t("common.error.unknownNetwork"));
    } finally {
      setLoading(false);
    }
  }, [customerFilter, pageNumber, pageSize, search]);

  const loadCustomers = useCallback(async (query?: string) => {
    setCustomersLoading(true);
    try {
      const items = await getRequestCounterparties(query, true);
      const deduped = new Map<string, RequestCounterpartyLookupDto>();
      for (const item of items) {
        const key = (item.name ?? "").trim().toLowerCase() || item.id;
        if (!deduped.has(key)) {
          deduped.set(key, item);
        }
      }
      setCustomerOptions(Array.from(deduped.values()));
    } catch {
      setCustomerOptions([]);
    } finally {
      setCustomersLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const importMenuItems: MenuProps["items"] = useMemo(
    () => [
      { key: "delta", label: t("references.mdm.import.delta") },
      { key: "snapshotUpsert", label: t("references.mdm.import.snapshotUpsert") },
      { key: "overwrite", label: t("references.mdm.import.overwrite"), danger: true },
    ],
    []
  );

  const runImport = async (syncMode: Component2020SyncMode) => {
    setImportLoading(true);
    try {
      const toastKey = "customer-orders-import";
      message.loading({ key: toastKey, content: t("references.mdm.import.running"), duration: 0 });

      const connection = await getComponent2020Connection();
      const connectionId = connection.id;

      if (!connectionId || connection.isActive === false) {
        message.error({ key: toastKey, content: t("references.mdm.import.noActiveConnection"), duration: 6 });
        return;
      }

      const resp = await runComponent2020Sync({
        connectionId,
        scope: Component2020SyncScope.CustomerOrders,
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
              ({resp.processedCount}) [{String(syncMode)}/{String(Component2020SyncScope.CustomerOrders)}]
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
      message.error({ key: "customer-orders-import", content: (e as Error).message, duration: 6 });
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

  const columns: ColumnsType<CustomerOrderListItemDto> = useMemo(
    () => [
      {
        title: t("customers.orders.columns.number"),
        dataIndex: "number",
        key: "number",
      },
      {
        title: t("customers.orders.columns.customer"),
        dataIndex: "customerName",
        key: "customerName",
      },
      {
        title: t("customers.orders.columns.createdAt"),
        dataIndex: "orderDate",
        key: "orderDate",
        render: (v?: string | null) => (v ? new Date(v).toLocaleDateString() : "-"),
      },
      {
        title: t("customers.orders.columns.contract"),
        dataIndex: "contract",
        key: "contract",
      },
      {
        title: t("customers.orders.columns.note"),
        dataIndex: "note",
        key: "note",
      },
      {
        title: t("customers.orders.columns.deliveryDate"),
        dataIndex: "deliveryDate",
        key: "deliveryDate",
        render: (v?: string | null) => (v ? new Date(v).toLocaleDateString() : "-"),
      },
      {
        title: t("customers.orders.columns.state"),
        dataIndex: "state",
        key: "state",
        render: (_v?: number | null, r?: CustomerOrderListItemDto) =>
          r?.statusName ? r.statusName : r?.state ?? "-",
      },
      {
        title: t("customers.orders.columns.person"),
        dataIndex: "personName",
        key: "personName",
      },
    ],
    []
  );

  return (
    <div>
      <CommandBar
        left={
          <Typography.Title level={2} style={{ margin: 0 }}>
            {t("customers.orders.title")}
          </Typography.Title>
        }
        right={
          <Space align="center">
            <Dropdown.Button
              trigger={["click"]}
              loading={importLoading}
              disabled={!canExecuteImport}
              menu={{ items: importMenuItems, onClick: onImportMenuClick }}
              onClick={() => void runImport(Component2020SyncMode.SnapshotUpsert)}
              data-testid="customers-import"
            >
              {t("customers.orders.import")}
            </Dropdown.Button>
            <Input
              placeholder={t("common.search")}
              value={search}
              onChange={(e: React.ChangeEvent<HTMLInputElement>) => {
                setPageNumber(1);
                setSearch(e.target.value);
              }}
              style={{ width: 260 }}
              data-testid="customers-orders-search"
            />
            <Select
              allowClear
              showSearch
              placeholder={t("customers.orders.filters.customer")}
              value={customerFilter}
              onChange={(value) => {
                setPageNumber(1);
                setCustomerFilter(value);
              }}
              onSearch={(value) => void loadCustomers(value)}
              onDropdownVisibleChange={(open) => {
                if (open && customerOptions.length === 0) {
                  void loadCustomers();
                }
              }}
              filterOption={false}
              loading={customersLoading}
              style={{ width: 260 }}
              options={customerOptions.map((c) => ({
                value: c.id,
                label: c.name,
              }))}
              data-testid="customers-orders-customer-filter"
            />
            <Button onClick={() => void load()} data-testid="customers-orders-refresh">
              {t("common.actions.refresh")}
            </Button>
          </Space>
        }
      />

      {error && <Alert type="error" showIcon message={error} style={{ marginBottom: 12 }} />}

      <Table
        data-testid="customers-orders-table"
        rowKey={(r: CustomerOrderListItemDto) => r.id}
        loading={loading}
        columns={columns}
        dataSource={items}
        pagination={{
          current: pageNumber,
          pageSize,
          total: totalCount,
          showSizeChanger: true,
        }}
        onChange={(pagination) => {
          setPageNumber(pagination.current ?? 1);
          setPageSize(pagination.pageSize ?? 20);
        }}
      />
    </div>
  );
};

export { CustomersPage };
