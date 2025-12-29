import React, { useCallback, useEffect, useMemo, useState } from "react";
import { Alert, Button, Checkbox, Dropdown, Empty, Input, Modal, Pagination, Select, Space, Spin, Tag, Typography, message } from "antd";
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
import "./CustomersPage.css";

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
  const [listView, setListView] = useState(false);

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

  const formatDate = (value?: string | null) => (value ? new Date(value).toLocaleDateString() : "-");

  return (
    <div className="customers-orders-page">
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
            <Checkbox checked={listView} onChange={(event) => setListView(event.target.checked)}>
              {t("references.mdm.items.view.list")}
            </Checkbox>
            <Button onClick={() => void load()} data-testid="customers-orders-refresh">
              {t("common.actions.refresh")}
            </Button>
          </Space>
        }
      />

      {error && <Alert type="error" showIcon message={error} style={{ marginBottom: 12 }} />}

      <div className="customers-orders__scroll">
        {loading ? (
        <div className="customers-orders__state">
          <Spin size="large" />
        </div>
      ) : items.length === 0 ? (
        <div className="customers-orders__state">
          <Empty description={t("references.mdm.items.empty")} />
        </div>
      ) : listView ? (
        <div className="customers-orders__list" data-testid="customers-orders-table">
          {items.map((order) => {
            const statusLabel = order.statusName ?? (order.state != null ? String(order.state) : "-");
            return (
              <div key={order.id} className="customers-orders__row">
                <div className="customers-orders__row-top">
                  <div className="customers-orders__row-title">{order.number ?? "-"}</div>
                  <Tag color="blue">{statusLabel}</Tag>
                </div>
                <div className="customers-orders__row-line">{order.customerName ?? "-"}</div>
                <div className="customers-orders__row-line">
                  {t("customers.orders.columns.createdAt")}: {formatDate(order.orderDate)} · {t("customers.orders.columns.deliveryDate")}: {formatDate(order.deliveryDate)}
                </div>
                <div className="customers-orders__row-line">
                  {t("customers.orders.columns.contract")}: {order.contract ?? "-"} · {t("customers.orders.columns.person")}: {order.personName ?? "-"}
                </div>
              </div>
            );
          })}
        </div>
      ) : (
        <div className="customers-orders__grid" data-testid="customers-orders-table">
          {items.map((order) => {
            const statusLabel = order.statusName ?? (order.state != null ? String(order.state) : "-");
            return (
              <div key={order.id} className="customers-orders__card">
                <div className="customers-orders__card-header">
                  <div>
                    <div className="customers-orders__card-title">{order.number ?? "-"}</div>
                    <div className="customers-orders__card-subtitle">{order.customerName ?? "-"}</div>
                  </div>
                  <Tag color="blue">{statusLabel}</Tag>
                </div>
                <div className="customers-orders__card-meta">
                  <span>{t("customers.orders.columns.createdAt")}: {formatDate(order.orderDate)}</span>
                  <span>{t("customers.orders.columns.deliveryDate")}: {formatDate(order.deliveryDate)}</span>
                </div>
                <div className="customers-orders__card-row">
                  <span className="customers-orders__card-label">{t("customers.orders.columns.contract")}</span>
                  <span>{order.contract ?? "-"}</span>
                </div>
                <div className="customers-orders__card-row">
                  <span className="customers-orders__card-label">{t("customers.orders.columns.person")}</span>
                  <span>{order.personName ?? "-"}</span>
                </div>
                <div className="customers-orders__card-row customers-orders__card-row--note">
                  <span className="customers-orders__card-label">{t("customers.orders.columns.note")}</span>
                  <span>{order.note ?? "-"}</span>
                </div>
              </div>
            );
          })}
        </div>
      )}
      </div>

      <div className="customers-orders__pagination">
        <Pagination
          current={pageNumber}
          pageSize={pageSize}
          total={totalCount}
          showSizeChanger
          onChange={(page, size) => {
            setPageNumber(page);
            setPageSize(size);
          }}
        />
      </div>
    </div>
  );
};

export { CustomersPage };
