import React, { useCallback, useEffect, useMemo, useState } from "react";
import {
  Alert,
  Button,
  Card,
  Checkbox,
  Form,
  Input,
  Select,
  Space,
  Table,
  Tag,
  Typography,
  message,
} from "antd";

import Modal from "antd/es/modal";

import type { ColumnsType } from "antd/es/table";

import { t } from "../../../../../core/i18n/t";
import { useCan } from "../../../../../core/auth/permissions";
import { useNavigate } from "react-router-dom";
import {
  getComponent2020Connection,
  getComponent2020FsEntries,
  getComponent2020Status,
  getComponent2020SyncRuns,
  runComponent2020Sync,
  saveComponent2020Connection,
  testComponent2020Connection,
} from "../api/adminComponent2020Api";
import type {
  Component2020ConnectionDto,
  Component2020FsEntryDto,
  Component2020StatusResponse,
  Component2020MdbFileDto,
  Component2020SyncRunDto,
  Component2020SyncScope,
  RunComponent2020SyncRequest,
} from "../api/types";

const MDB_PATH_STORAGE_KEY = "myis:component2020:mdbPath";

function safeLocalStorageGetItem(key: string): string | null {
  try {
    if (typeof localStorage === "undefined") return null;
    if (typeof localStorage.getItem !== "function") return null;
    return localStorage.getItem(key);
  } catch {
    return null;
  }
}

function safeLocalStorageSetItem(key: string, value: string): void {
  try {
    if (typeof localStorage === "undefined") return;
    if (typeof localStorage.setItem !== "function") return;
    localStorage.setItem(key, value);
  } catch {
    // ignore (quota, privacy mode, test env)
  }
}

export const Component2020SettingsPage: React.FC = () => {
  const canView = useCan("Admin.Integration.View");
  const canExecute = useCan("Admin.Integration.Execute");
  const navigate = useNavigate();

  const [status, setStatus] = useState<Component2020StatusResponse | null>(null);
  const [connection, setConnection] = useState<Component2020ConnectionDto | null>(null);
  const [runs, setRuns] = useState<Component2020SyncRunDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [mdbFilesModalOpen, setMdbFilesModalOpen] = useState(false);
  const [mdbFilesLoading, setMdbFilesLoading] = useState(false);
  const [fsEntries, setFsEntries] = useState<Component2020FsEntryDto[]>([]);
  const [currentFsPath, setCurrentFsPath] = useState<string>("");
  const [databasesRoot, setDatabasesRoot] = useState<string>("");

  const [connectionForm] = Form.useForm();
  const [syncForm] = Form.useForm();

  const loadConnection = useCallback(async () => {
    try {
      const data = await getComponent2020Connection();
      setConnection(data);
      connectionForm.setFieldsValue({
        mdbPath: data.mdbPath,
        login: data.login,
        isActive: data.isActive ?? true,
        // password intentionally not set
        clearPassword: false,
      });
      // Prefill connectionId for sync
      if (data.id) {
        syncForm.setFieldsValue({ connectionId: data.id });
      }
    } catch (e) {
      setError((e as Error).message || t("common.error.unknownNetwork"));
    }
  }, [connectionForm, syncForm]);

  const loadStatus = useCallback(async () => {
    try {
      const data = await getComponent2020Status();
      setStatus(data);
    } catch (e) {
      setError((e as Error).message || t("common.error.unknownNetwork"));
    }
  }, []);

  const loadRuns = useCallback(async () => {
    try {
      const data = await getComponent2020SyncRuns();
      setRuns(data.runs);
    } catch (e) {
      setError((e as Error).message || t("common.error.unknownNetwork"));
    }
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    await Promise.all([loadConnection(), loadStatus(), loadRuns()]);
    setLoading(false);
  }, [loadConnection, loadStatus, loadRuns]);

  useEffect(() => {
    void load();
  }, [load]);

  // Load saved path from localStorage on component mount
  useEffect(() => {
    const savedPath = safeLocalStorageGetItem(MDB_PATH_STORAGE_KEY);
    if (savedPath && !connectionForm.getFieldValue("mdbPath")) {
      connectionForm.setFieldValue("mdbPath", savedPath);
    }
  }, [connectionForm]);

  const loadFsEntries = useCallback(async (path?: string) => {
    setMdbFilesLoading(true);
    try {
      const response = await getComponent2020FsEntries(path);
      setDatabasesRoot(response.databasesRoot);
      setCurrentFsPath(response.currentRelativePath);
      setFsEntries(response.entries);
    } catch (e) {
      message.error((e as Error).message);
      setDatabasesRoot("");
      setCurrentFsPath(path ?? "");
      setFsEntries([]);
    } finally {
      setMdbFilesLoading(false);
    }
  }, []);

  const onSelectMdbFile = useCallback(async () => {
    setMdbFilesModalOpen(true);
    await loadFsEntries("");
  }, [loadFsEntries]);

  /*
    const fileInput = document.createElement("input");
    fileInput.type = "file";
    fileInput.accept = ".mdb,.accdb";
    fileInput.style.display = "none";
    
    fileInput.onchange = (e) => {
      const target = e.target as HTMLInputElement;
      const file = target.files?.[0];
      
      console.log("File selected:", file?.name);
      
      if (file) {
        try {
          // Browsers do not expose the full local file path for security reasons.
          // We can only use a relative path (when selecting a directory) or try to
          // reuse the last manually entered full path to reconstruct the filename.
          let fullPath: string | null = null;

          if (file.webkitRelativePath) {
            fullPath = file.webkitRelativePath;
          } else {
            const lastUsedPath = safeLocalStorageGetItem(MDB_PATH_STORAGE_KEY);
            if (lastUsedPath && (lastUsedPath.includes("/") || lastUsedPath.includes("\\"))) {
              const pathParts = lastUsedPath.split(/[\\/]/);
              pathParts[pathParts.length - 1] = file.name;
              fullPath = pathParts.join(pathParts.includes("\\") ? "\\" : "/");
            }
          }

          if (!fullPath || fullPath === file.name) {
            message.warning(
              t("settings.integrations.component2020.connection.mdbPath.cannotDetectFullPath", {
                fileName: file.name,
              })
            );
            return;
          }
          
          // Update form field with the full path
          connectionForm.setFieldValue("mdbPath", fullPath);

          // Save the path to localStorage for future use
          safeLocalStorageSetItem(MDB_PATH_STORAGE_KEY, fullPath);
          
          // Trigger form validation to update UI
          connectionForm.validateFields(["mdbPath"]).catch(() => {
            // Ignore validation errors during file selection
          });
          
          console.log("Form field updated with path:", fullPath);
          message.success(`Выбран файл: ${fullPath}`);
        } catch (error) {
          console.error("Error updating form field:", error);
          message.error("Ошибка при выборе файла: " + (error as Error).message);
        }
      }
    };
    
    // Handle file dialog cancellation
    fileInput.onclick = () => {
      console.log("File dialog opened");
    };
    
    // Trigger file picker
    try {
      fileInput.click();
    } catch {
      // ignore
    }
  }, [connectionForm]);
  */

  const onPickServerMdb = useCallback(
    (file: Component2020MdbFileDto) => {
      const fullPath = file.fullPath;
      connectionForm.setFieldValue("mdbPath", fullPath);
      safeLocalStorageSetItem(MDB_PATH_STORAGE_KEY, fullPath);
      connectionForm.validateFields(["mdbPath"]).catch(() => {});
      setMdbFilesModalOpen(false);
      message.success(
        `${t("settings.integrations.component2020.connection.mdbPath.picked")}: ${file.relativePath}`
      );
    },
    [connectionForm]
  );

  const mdbFilesColumns: ColumnsType<Component2020FsEntryDto> = useMemo(
    () => [
      {
        title: t("settings.integrations.component2020.connection.mdbPicker.columns.file"),
        key: "name",
        render: (_: unknown, r: Component2020FsEntryDto) => (
          <Space>
            <Typography.Text code>{r.name}</Typography.Text>
            {r.isDirectory && <Tag>DIR</Tag>}
          </Space>
        ),
      },
      {
        title: t("settings.integrations.component2020.connection.mdbPicker.columns.size"),
        dataIndex: "sizeBytes",
        key: "sizeBytes",
        render: (v?: number | null, r?: Component2020FsEntryDto) => {
          if (!r || r.isDirectory) return "-";
          const bytes = v ?? 0;
          const mb = bytes / (1024 * 1024);
          return `${mb.toFixed(1)} MB`;
        },
      },
      {
        title: t("settings.integrations.component2020.connection.mdbPicker.columns.modified"),
        dataIndex: "lastWriteTimeUtc",
        key: "lastWriteTimeUtc",
        render: (v?: string | null) => (v ? new Date(v).toLocaleString() : "-"),
      },
      {
        title: t("settings.integrations.component2020.connection.mdbPicker.columns.actions"),
        key: "actions",
        render: (_: unknown, r: Component2020FsEntryDto) => {
          if (r.isDirectory) {
            return (
              <Button size="small" onClick={() => void loadFsEntries(r.relativePath)}>
                {t("settings.integrations.component2020.connection.mdbPicker.openFolder")}
              </Button>
            );
          }

          const lower = r.name.toLowerCase();
          const isMdb = lower.endsWith(".mdb") || lower.endsWith(".accdb");
          return (
            <Button
              size="small"
              type="primary"
              disabled={!isMdb}
              onClick={() =>
                isMdb &&
                onPickServerMdb({
                  name: r.name,
                  relativePath: r.relativePath,
                  fullPath: r.fullPath,
                  sizeBytes: r.sizeBytes ?? 0,
                  lastWriteTimeUtc: r.lastWriteTimeUtc ?? new Date(0).toISOString(),
                })
              }
            >
              {t("common.actions.select")}
            </Button>
          );
        },
      },
    ],
    [loadFsEntries, onPickServerMdb]
  );

  const onTestConnection = async () => {
    const values = await connectionForm.validateFields();
    try {
      const result = await testComponent2020Connection(values);
      message.success(
        result.isConnected
          ? t("settings.integrations.component2020.connection.test.ok")
          : t("settings.integrations.component2020.connection.test.fail")
      );
    } catch (e) {
      message.error((e as Error).message);
    }
  };

  const onSaveConnection = async () => {
    const values = await connectionForm.validateFields();
    try {
      await saveComponent2020Connection(values);
      message.success(t("settings.integrations.component2020.connection.save.ok"));
      await Promise.all([loadConnection(), loadStatus()]);
    } catch (e) {
      message.error((e as Error).message);
    }
  };

  const onRunSync = async () => {
    const values = await syncForm.validateFields();
    try {
      const result = await runComponent2020Sync(values);
      message.success(
        t("settings.integrations.component2020.sync.run.started", { runId: result.runId })
      );
      await loadRuns();
    } catch (e) {
      message.error((e as Error).message);
    }
  };

  const runsColumns: ColumnsType<Component2020SyncRunDto> = useMemo(
    () => [
      {
        title: t("settings.integrations.component2020.runs.columns.id"),
        dataIndex: "id",
        key: "id",
      },
      {
        title: t("settings.integrations.component2020.runs.columns.startedAt"),
        dataIndex: "startedAt",
        key: "startedAt",
        render: (v: string) => new Date(v).toLocaleString(),
      },
      {
        title: t("settings.integrations.component2020.runs.columns.finishedAt"),
        dataIndex: "finishedAt",
        key: "finishedAt",
        render: (v?: string) => (v ? new Date(v).toLocaleString() : "-"),
      },
      {
        title: t("settings.integrations.component2020.runs.columns.status"),
        dataIndex: "status",
        key: "status",
      },
      {
        title: t("settings.integrations.component2020.runs.columns.scope"),
        dataIndex: "scope",
        key: "scope",
      },
      {
        title: t("settings.integrations.component2020.runs.columns.mode"),
        dataIndex: "mode",
        key: "mode",
      },
      {
        title: t("settings.integrations.component2020.runs.columns.processed"),
        dataIndex: "processedCount",
        key: "processedCount",
      },
      {
        title: t("settings.integrations.component2020.runs.columns.errors"),
        dataIndex: "errorCount",
        key: "errorCount",
        render: (_v: number, r: Component2020SyncRunDto) => (
          <Button
            size="small"
            onClick={() =>
              navigate(`/administration/integrations/component2020/runs/${encodeURIComponent(r.id)}`)
            }
            data-testid={`component2020-run-errors-${r.id}`}
          >
            {t("common.actions.open")}
          </Button>
        ),
      },
    ],
    [navigate]
  );

  return (
    <div>
      <Typography.Title level={3} style={{ marginTop: 0 }}>
        {t("settings.integrations.component2020.title")}
      </Typography.Title>

      {!canView && (
        <Alert
          type="warning"
          showIcon
          message={t("settings.forbidden")}
          style={{ marginBottom: 12 }}
        />
      )}

      {error && (
        <Alert
          type="error"
          showIcon
          message={error}
          style={{ marginBottom: 12 }}
        />
      )}

      <Space style={{ marginBottom: 12 }}>
        <Button onClick={() => void load()} data-testid="component2020-refresh">
          {t("settings.integrations.component2020.actions.refresh")}
        </Button>
      </Space>

      <Card title={t("settings.integrations.component2020.connection.title")} style={{ marginBottom: 16 }}>
        <Form form={connectionForm} layout="vertical">
          <Form.Item
            label={t("settings.integrations.component2020.connection.mdbPath")}
            required
          >
            <Space.Compact data-testid="component2020-mdb-path">
              <Form.Item
                name="mdbPath"
                noStyle
                rules={[{ required: true, message: t("settings.integrations.component2020.connection.mdbPath.required") }]}
              >
                <Input
                  style={{ width: "calc(100% - 120px)" }}
                  data-testid="component2020-mdb-path-input"
                  onChange={(e: React.ChangeEvent<HTMLInputElement>) => {
                    const value = e.target.value;
                    if (value) safeLocalStorageSetItem(MDB_PATH_STORAGE_KEY, value);
                  }}
                placeholder="Выберите файл базы данных или введите путь вручную"
                />
              </Form.Item>
              <Button
                type="primary"
                style={{ width: 120 }}
                onClick={onSelectMdbFile}
                data-testid="component2020-mdb-path-browse"
              >
                Выбрать
              </Button>
            </Space.Compact>
            {databasesRoot && (
              <div style={{ marginTop: 8 }}>
                <Tag>
                  {t("settings.integrations.component2020.connection.mdbPicker.root")}: {databasesRoot}
                </Tag>
              </div>
            )}
          </Form.Item>

          <Form.Item
            label={t("settings.integrations.component2020.connection.login")}
            name="login"
          >
            <Input data-testid="component2020-login" />
          </Form.Item>

          <Form.Item
            label={t("settings.integrations.component2020.connection.password")}
            name="password"
            extra={
              <Space direction="vertical" size={0}>
                <Typography.Text type="secondary">
                  {t("settings.integrations.component2020.connection.password.keepHint")}
                </Typography.Text>
                <Typography.Text type="secondary">
                  {t("settings.integrations.component2020.connection.password.extra")}
                </Typography.Text>
                <Typography.Text type={connection?.hasPassword ? "success" : "secondary"}>
                  {t("settings.integrations.component2020.connection.password.hasPassword")}: {connection?.hasPassword ? t("settings.integrations.component2020.status.value.yes") : t("settings.integrations.component2020.status.value.no")}
                </Typography.Text>
              </Space>
            }
          >
            <Input.Password data-testid="component2020-password" autoComplete="new-password" />
          </Form.Item>

          <Form.Item name="clearPassword" valuePropName="checked">
            <Checkbox data-testid="component2020-clear-password">
              {t("settings.integrations.component2020.connection.password.clear")}
            </Checkbox>
            <Typography.Text type="secondary" style={{ display: "block" }}>
              {t("settings.integrations.component2020.connection.password.clearHint")}
            </Typography.Text>
          </Form.Item>

          <Form.Item name="isActive" valuePropName="checked">
            <Checkbox data-testid="component2020-is-active">
              {t("settings.integrations.component2020.connection.isActive")}
            </Checkbox>
            <Typography.Text type="secondary" style={{ display: "block" }}>
              {t("settings.integrations.component2020.connection.isActiveHint")}
            </Typography.Text>
          </Form.Item>
          <Space>
            <Button onClick={onTestConnection} data-testid="component2020-test-connection">
              {t("settings.integrations.component2020.connection.test")}
            </Button>
            <Button
              type="primary"
              onClick={onSaveConnection}
              disabled={!canExecute}
              data-testid="component2020-save-connection"
            >
              {t("settings.integrations.component2020.connection.save")}
            </Button>
          </Space>
        </Form>
      </Card>

      <Card title={t("settings.integrations.component2020.sync.title")} style={{ marginBottom: 16 }}>
        <Form form={syncForm} layout="vertical">
          <Form.Item
            label={t("settings.integrations.component2020.sync.scope")}
            name="connectionId"
            hidden
          >
            <Input />
          </Form.Item>

          <Form.Item
            label={t("settings.integrations.component2020.sync.scope")}
            name="scope"
            rules={[
              {
                required: true,
                message: t("settings.integrations.component2020.sync.scope.requiredMessage"),
              },
            ]}
          >
            <Select
              options={[
                { value: "Units", label: t("settings.integrations.component2020.sync.scope.option.Units") },
                { value: "Counterparties", label: t("settings.integrations.component2020.sync.scope.option.Counterparties") },
                { value: "ItemGroups", label: t("settings.integrations.component2020.sync.scope.option.ItemGroups") },
                { value: "Items", label: t("settings.integrations.component2020.sync.scope.option.Items") },
                { value: "Products", label: t("settings.integrations.component2020.sync.scope.option.Products") },
                { value: "Manufacturers", label: t("settings.integrations.component2020.sync.scope.option.Manufacturers") },
                { value: "BodyTypes", label: t("settings.integrations.component2020.sync.scope.option.BodyTypes") },
                { value: "Currencies", label: t("settings.integrations.component2020.sync.scope.option.Currencies") },
                { value: "TechnicalParameters", label: t("settings.integrations.component2020.sync.scope.option.TechnicalParameters") },
                { value: "ParameterSets", label: t("settings.integrations.component2020.sync.scope.option.ParameterSets") },
                { value: "Symbols", label: t("settings.integrations.component2020.sync.scope.option.Symbols") },
                { value: "Employees", label: t("settings.integrations.component2020.sync.scope.option.Employees") },
                { value: "Users", label: t("settings.integrations.component2020.sync.scope.option.Users") },
                { value: "CustomerOrders", label: t("settings.integrations.component2020.sync.scope.option.CustomerOrders") },
                { value: "Statuses", label: t("settings.integrations.component2020.sync.scope.option.Statuses") },
                { value: "All", label: t("settings.integrations.component2020.sync.scope.option.All") },
              ]}
              data-testid="component2020-sync-scope"
            />
          </Form.Item>
          <Form.Item
            label={t("settings.integrations.component2020.sync.mode.dryRun")}
            name="dryRun"
            valuePropName="checked"
            initialValue={false}
            extra={t("settings.integrations.component2020.sync.mode.dryRun.extra")}
          >
            <Checkbox data-testid="component2020-dry-run" />
          </Form.Item>
          <Button
            type="primary"
            onClick={onRunSync}
            disabled={!canExecute}
            data-testid="component2020-run-sync"
          >
            {t("settings.integrations.component2020.sync.run")}
          </Button>
        </Form>
      </Card>

      <Card title={t("references.group.mdm")} style={{ marginBottom: 16 }}>
        <Space wrap>
          <Button onClick={() => navigate("/references/mdm/units")}>
            {t("references.mdm.units.title")}
          </Button>
          <Button onClick={() => navigate("/references/mdm/counterparties")}>
            {t("references.mdm.counterparties.title")}
          </Button>
          <Button onClick={() => navigate("/references/mdm/items")}>
            {t("references.mdm.items.title")}
          </Button>
          <Button onClick={() => navigate("/references/mdm/item-groups")}>
            {t("references.mdm.itemGroups.title")}
          </Button>
          <Button onClick={() => navigate("/references/mdm/manufacturers")}>
            {t("references.mdm.manufacturers.title")}
          </Button>
          <Button onClick={() => navigate("/references/mdm/body-types")}>
            {t("references.mdm.bodyTypes.title")}
          </Button>
          <Button onClick={() => navigate("/references/mdm/currencies")}>
            {t("references.mdm.currencies.title")}
          </Button>
          <Button onClick={() => navigate("/references/mdm/technical-parameters")}>
            {t("references.mdm.technicalParameters.title")}
          </Button>
          <Button onClick={() => navigate("/references/mdm/parameter-sets")}>
            {t("references.mdm.parameterSets.title")}
          </Button>
          <Button onClick={() => navigate("/references/mdm/symbols")}>
            {t("references.mdm.symbols.title")}
          </Button>
          <Button onClick={() => navigate("/references/mdm/statuses")}>
            {t("references.statuses.title")}
          </Button>
        </Space>
      </Card>

      <Card title={t("settings.integrations.component2020.status.title")}>
        {status && (
          <div>
            <p>
              {t("settings.integrations.component2020.status.connected")}: {status.isConnected ? t("settings.integrations.component2020.status.value.yes") : t("settings.integrations.component2020.status.value.no")}
            </p>
            <p>
              {t("settings.integrations.component2020.status.scheduler")}: {status.isSchedulerActive ? t("settings.integrations.component2020.status.value.yes") : t("settings.integrations.component2020.status.value.no")}
            </p>
            <p>
              {t("settings.integrations.component2020.status.lastSuccessful")}: {status.lastSuccessfulSync || t("settings.integrations.component2020.status.value.never")}
            </p>
            <p>
              {t("settings.integrations.component2020.status.lastStatus")}: {status.lastSyncStatus || "-"}
            </p>
          </div>
        )}
      </Card>

      <Card title={t("settings.integrations.component2020.runs.title")} style={{ marginTop: 16 }}>
        <Table
          data-testid="component2020-runs-table"
          rowKey={(r: Component2020SyncRunDto) => r.id}
          loading={loading}
          columns={runsColumns}
          dataSource={runs}
          pagination={{ pageSize: 10 }}
          onRow={(record: Component2020SyncRunDto) => ({
            onClick: () =>
              navigate(
                `/administration/integrations/component2020/runs/${encodeURIComponent(record.id)}`
              ),
          })}
        />
      </Card>

      <Modal
        open={mdbFilesModalOpen}
        title={t("settings.integrations.component2020.connection.mdbPicker.title")}
        onCancel={() => setMdbFilesModalOpen(false)}
        footer={null}
        width={1000}
      >
        {!databasesRoot && (
          <Alert
            type="warning"
            showIcon
            message={t("settings.integrations.component2020.connection.mdbPicker.notConfigured")}
            style={{ marginBottom: 12 }}
          />
        )}

        <Space style={{ marginBottom: 12 }} wrap>
          <Button onClick={() => void loadFsEntries("")} disabled={!databasesRoot}>
            {t("settings.integrations.component2020.connection.mdbPicker.toRoot")}
          </Button>
          <Button
            onClick={() => {
              const parts = (currentFsPath || "").split(/[\\/]/).filter(Boolean);
              parts.pop();
              void loadFsEntries(parts.join("\\"));
            }}
            disabled={!currentFsPath}
          >
            {t("settings.integrations.component2020.connection.mdbPicker.up")}
          </Button>
          <Typography.Text type="secondary">
            {t("settings.integrations.component2020.connection.mdbPicker.current")}:{" "}
            <Typography.Text code>{currentFsPath || "\\"}</Typography.Text>
          </Typography.Text>
        </Space>

        <Table
          data-testid="component2020-mdb-files-table"
          rowKey={(r: Component2020FsEntryDto) => r.fullPath}
          loading={mdbFilesLoading}
          columns={mdbFilesColumns}
          dataSource={fsEntries}
          pagination={{ pageSize: 10 }}
        />
      </Modal>
    </div>
  );
};
