import React, { useCallback, useEffect, useMemo, useState } from "react";
import {
  Alert,
  Button,
  Dropdown,
  Form,
  Input,
  InputNumber,
  Modal,
  Select,
  Space,
  Switch,
  Table,
  Tag,
  Typography,
  message,
} from "antd";
import type { ColumnsType } from "antd/es/table";
import type { MenuProps } from "antd";
import { useNavigate, useSearchParams } from "react-router-dom";

import { CommandBar } from "../../../../components/ui/CommandBar";
import { useCan } from "../../../../core/auth/permissions";
import { t } from "../../../../core/i18n/t";
import {
  getComponent2020Connection,
  runComponent2020Sync,
} from "../../../settings/integrations/component2020/api/adminComponent2020Api";
import { Component2020SyncMode, Component2020SyncScope } from "../../../settings/integrations/component2020/api/types";
import {
  archiveStatus,
  archiveStatusGroup,
  createStatus,
  createStatusGroup,
  getStatusGroups,
  getStatuses,
  updateStatus,
  updateStatusGroup,
} from "../api/statusDictionaryApi";
import type { StatusDto, StatusGroupDto } from "../api/types";

const REQUEST_FINAL_FLAG = 1;

type StatusFormState = {
  groupId?: string;
  name?: string;
  color?: number | null;
  flags?: number | null;
  sortOrder?: number | null;
  isActive?: boolean;
};

type GroupFormState = {
  name?: string;
  description?: string | null;
  sortOrder?: number | null;
  isActive?: boolean;
};

type StatusTreeRow = {
  key: string;
  rowType: "group" | "status";
  id: string;
  name: string;
  groupId?: string;
  groupName?: string | null;
  description?: string | null;
  color?: number | null;
  sortOrder?: number | null;
  isActive: boolean;
  status?: StatusDto;
  children?: StatusTreeRow[];
};

export const StatusDictionaryPage: React.FC = () => {
  const canEdit = useCan("Admin.Integration.Execute");
  const canView = useCan("Admin.Integration.View");
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const defaultGroupKey = searchParams.get("group")?.toLowerCase();

  const [groups, setGroups] = useState<StatusGroupDto[]>([]);
  const [groupsLoading, setGroupsLoading] = useState(false);
  const [groupsError, setGroupsError] = useState<string | null>(null);

  const [statuses, setStatuses] = useState<StatusDto[]>([]);
  const [statusesLoading, setStatusesLoading] = useState(false);
  const [statusesError, setStatusesError] = useState<string | null>(null);
  const [statusSearch, setStatusSearch] = useState("");
  const [statusActiveFilter, setStatusActiveFilter] = useState<"all" | "active" | "inactive">("all");
  const [statusGroupId, setStatusGroupId] = useState<string | undefined>();

  const [groupModalOpen, setGroupModalOpen] = useState(false);
  const [editingGroup, setEditingGroup] = useState<StatusGroupDto | null>(null);
  const [groupForm] = Form.useForm<GroupFormState>();

  const [statusModalOpen, setStatusModalOpen] = useState(false);
  const [editingStatus, setEditingStatus] = useState<StatusDto | null>(null);
  const [statusForm] = Form.useForm<StatusFormState>();
  const [importLoading, setImportLoading] = useState(false);

  const loadGroups = useCallback(async () => {
    setGroupsLoading(true);
    setGroupsError(null);
    try {
      const data = await getStatusGroups({
        q: undefined,
        isActive: undefined,
        skip: 0,
        take: 500,
      });
      setGroups(data.items);
    } catch (e) {
      setGroupsError((e as Error).message || t("common.error.unknownNetwork"));
    } finally {
      setGroupsLoading(false);
    }
  }, []);

  const loadStatuses = useCallback(async () => {
    setStatusesLoading(true);
    setStatusesError(null);
    try {
      const data = await getStatuses({
        q: statusSearch.trim() || undefined,
        groupId: statusGroupId,
        isActive: statusActiveFilter === "all" ? undefined : statusActiveFilter === "active",
        skip: 0,
        take: 500,
      });
      setStatuses(data.items);
    } catch (e) {
      setStatusesError((e as Error).message || t("common.error.unknownNetwork"));
    } finally {
      setStatusesLoading(false);
    }
  }, [statusActiveFilter, statusGroupId, statusSearch]);

  useEffect(() => {
    void loadGroups();
  }, [loadGroups]);

  useEffect(() => {
    void loadStatuses();
  }, [loadStatuses]);

  useEffect(() => {
    if (!defaultGroupKey || groups.length === 0 || statusGroupId) return;
    if (defaultGroupKey === "requests") {
      const match = groups.find((g) => g.isRequestsGroup);
      if (match) {
        setStatusGroupId(match.id);
      }
    }
  }, [defaultGroupKey, groups, statusGroupId]);

  const groupOptions = useMemo(
    () =>
      groups
        .slice()
        .sort((a, b) => (a.sortOrder ?? 9999) - (b.sortOrder ?? 9999) || a.name.localeCompare(b.name)),
    [groups]
  );

  const statusTreeData = useMemo<StatusTreeRow[]>(() => {
    if (statuses.length === 0) {
      return [];
    }

    const groupById = new Map(groupOptions.map((g) => [g.id, g]));
    const grouped = new Map<string, StatusDto[]>();
    const ungroupedKey = "group:ungrouped";

    statuses.forEach((status) => {
      const key = status.groupId ?? ungroupedKey;
      if (!grouped.has(key)) {
        grouped.set(key, []);
      }
      grouped.get(key)?.push(status);
    });

    const rows: StatusTreeRow[] = [];

    groupOptions.forEach((group) => {
      const groupStatuses = grouped.get(group.id);
      const children = (groupStatuses ?? [])
        .slice()
        .sort((a, b) => (a.sortOrder ?? 9999) - (b.sortOrder ?? 9999) || a.name.localeCompare(b.name))
        .map((status) => ({
          key: `status:${status.id}`,
          rowType: "status" as const,
          id: status.id,
          name: status.name,
          groupId: status.groupId,
          groupName: status.groupName,
          color: status.color ?? null,
          sortOrder: status.sortOrder ?? null,
          isActive: status.isActive,
          status,
        }));

      rows.push({
        key: `group:${group.id}`,
        rowType: "group",
        id: group.id,
        name: group.name,
        description: group.description ?? null,
        sortOrder: group.sortOrder ?? null,
        isActive: group.isActive,
        children,
      });
    });

    const ungrouped = grouped.get(ungroupedKey);
    if (ungrouped && ungrouped.length > 0) {
      rows.push({
        key: ungroupedKey,
        rowType: "group",
        id: ungroupedKey,
        name: "No group",
        isActive: true,
        children: ungrouped
          .slice()
          .sort((a, b) => (a.sortOrder ?? 9999) - (b.sortOrder ?? 9999) || a.name.localeCompare(b.name))
          .map((status) => ({
            key: `status:${status.id}`,
            rowType: "status" as const,
            id: status.id,
            name: status.name,
            groupId: status.groupId,
            groupName: status.groupName,
            color: status.color ?? null,
            sortOrder: status.sortOrder ?? null,
            isActive: status.isActive,
            status,
          })),
      });
    }

    return rows;
  }, [groupOptions, statuses]);

  const currentGroupForStatusForm = useMemo(() => {
    const groupId = statusForm.getFieldValue("groupId") as string | undefined;
    return groupOptions.find((g) => g.id === groupId) ?? null;
  }, [groupOptions, statusForm]);

  const isFinal = ((statusForm.getFieldValue("flags") ?? 0) & REQUEST_FINAL_FLAG) === REQUEST_FINAL_FLAG;
  const showFinalFlag = currentGroupForStatusForm?.isRequestsGroup === true;

  const updateFinalFlag = (checked: boolean) => {
    const current = statusForm.getFieldValue("flags") ?? 0;
    const next = checked ? (current | REQUEST_FINAL_FLAG) : (current & ~REQUEST_FINAL_FLAG);
    statusForm.setFieldsValue({ flags: next });
  };

  const openCreateGroup = () => {
    setEditingGroup(null);
    groupForm.resetFields();
    groupForm.setFieldsValue({ isActive: true });
    setGroupModalOpen(true);
  };

  const openEditGroup = (group: StatusGroupDto) => {
    setEditingGroup(group);
    groupForm.setFieldsValue({
      name: group.name,
      description: group.description ?? null,
      sortOrder: group.sortOrder ?? null,
      isActive: group.isActive,
    });
    setGroupModalOpen(true);
  };

  const saveGroup = async () => {
    const values = await groupForm.validateFields();
    try {
      if (editingGroup) {
        await updateStatusGroup(editingGroup.id, values);
      } else {
        await createStatusGroup(values);
      }
      setGroupModalOpen(false);
      await loadGroups();
      message.success(t("common.actions.save"));
    } catch (e) {
      message.error((e as Error).message);
    }
  };

  const confirmArchiveGroup = (group: StatusGroupDto) => {
    Modal.confirm({
      title: t("common.actions.archive"),
      content: `${group.name} (${group.id})`,
      okText: t("common.actions.archive"),
      okType: "danger",
      cancelText: t("common.actions.cancel"),
      onOk: async () => {
        try {
          await archiveStatusGroup(group.id);
          await loadGroups();
          message.success(t("common.actions.save"));
        } catch (e) {
          message.error((e as Error).message);
        }
      },
    });
  };

  const openCreateStatus = () => {
    setEditingStatus(null);
    statusForm.resetFields();
    statusForm.setFieldsValue({
      groupId: statusGroupId,
      isActive: true,
      flags: 0,
    });
    setStatusModalOpen(true);
  };

  const openEditStatus = (status: StatusDto) => {
    setEditingStatus(status);
    statusForm.setFieldsValue({
      groupId: status.groupId,
      name: status.name,
      color: status.color ?? null,
      flags: status.flags ?? 0,
      sortOrder: status.sortOrder ?? null,
      isActive: status.isActive,
    });
    setStatusModalOpen(true);
  };

  const saveStatus = async () => {
    const values = await statusForm.validateFields();
    try {
      if (editingStatus) {
        await updateStatus(editingStatus.id, values);
      } else {
        await createStatus(values);
      }
      setStatusModalOpen(false);
      await loadStatuses();
      message.success(t("common.actions.save"));
    } catch (e) {
      message.error((e as Error).message);
    }
  };

  const confirmArchiveStatus = (status: StatusDto) => {
    Modal.confirm({
      title: t("common.actions.archive"),
      content: `${status.name} (${status.id})`,
      okText: t("common.actions.archive"),
      okType: "danger",
      cancelText: t("common.actions.cancel"),
      onOk: async () => {
        try {
          await archiveStatus(status.id);
          await loadStatuses();
          message.success(t("common.actions.save"));
        } catch (e) {
          message.error((e as Error).message);
        }
      },
    });
  };

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
      const toastKey = "statuses-import";
      message.loading({ key: toastKey, content: t("references.mdm.import.running"), duration: 0 });

      const connection = await getComponent2020Connection();
      const connectionId = connection.id;

      if (!connectionId || connection.isActive === false) {
        message.error({ key: toastKey, content: t("references.mdm.import.noActiveConnection"), duration: 6 });
        return;
      }

      const resp = await runComponent2020Sync({
        connectionId,
        scope: Component2020SyncScope.Statuses,
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
              ({resp.processedCount}) [{String(syncMode)}/{String(Component2020SyncScope.Statuses)}]
            </span>
            <Button
              type="link"
              size="small"
              onClick={() =>
                navigate(`/administration/integrations/component2020/runs/${encodeURIComponent(resp.runId)}`)
              }
            >
              {t("common.actions.open")}
            </Button>
          </Space>
        ),
      });

      await Promise.all([loadGroups(), loadStatuses()]);
    } catch (e) {
      message.error({ key: "statuses-import", content: (e as Error).message, duration: 6 });
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

  const renderCellText = (value: React.ReactNode, isActive: boolean) =>
    isActive ? value : <Typography.Text type="secondary">{value}</Typography.Text>;

  const statusColumns: ColumnsType<StatusTreeRow> = [
    {
      title: t("references.columns.name"),
      dataIndex: "name",
      key: "name",
      render: (value: string, r: StatusTreeRow) => {
        const content = r.rowType === "group" ? <strong>{value}</strong> : value;
        return renderCellText(content, r.isActive);
      },
    },
    {
      title: t("references.statuses.columns.group"),
      key: "group",
      render: (_: unknown, r: StatusTreeRow) =>
        renderCellText(r.rowType === "status" ? r.groupName ?? "-" : "-", r.isActive),
    },
    {
      title: t("references.statuses.columns.color"),
      dataIndex: "color",
      key: "color",
      render: (v?: number | null, r?: StatusTreeRow) =>
        renderCellText(
          r?.rowType === "status" && typeof v === "number" ? v : "-",
          r?.isActive ?? true
        ),
    },
    {
      title: t("references.columns.sortOrder"),
      dataIndex: "sortOrder",
      key: "sortOrder",
      render: (v?: number | null, r?: StatusTreeRow) =>
        renderCellText(typeof v === "number" ? v : "-", r?.isActive ?? true),
    },
    {
      title: t("references.columns.isActive"),
      dataIndex: "isActive",
      key: "isActive",
      render: (v: boolean, r: StatusTreeRow) =>
        renderCellText(v ? <Tag color="green">ON</Tag> : <Tag>OFF</Tag>, r.isActive),
    },
    {
      title: t("references.columns.actions"),
      key: "actions",
      render: (_: unknown, r: StatusTreeRow) =>
        r.rowType === "group"
          ? (() => {
              const group = groups.find((g) => g.id === r.id);
              if (!group) return "-";
              return (
                <Space>
                  <Button size="small" onClick={() => openEditGroup(group)} disabled={!canEdit}>
                    {t("common.actions.edit")}
                  </Button>
                  <Button
                    size="small"
                    danger
                    disabled={!canEdit || !group.isActive}
                    onClick={() => confirmArchiveGroup(group)}
                  >
                    {t("common.actions.archive")}
                  </Button>
                </Space>
              );
            })()
          : r.status
            ? (
                <Space>
                  <Button size="small" onClick={() => openEditStatus(r.status!)} disabled={!canEdit}>
                    {t("common.actions.edit")}
                  </Button>
                  <Button
                    size="small"
                    danger
                    disabled={!canEdit || !r.status.isActive}
                    onClick={() => confirmArchiveStatus(r.status!)}
                  >
                    {t("common.actions.archive")}
                  </Button>
                </Space>
              )
            : "-",
    },
  ];

  return (
    <div>
      <CommandBar
        left={
          <Typography.Title level={2} style={{ margin: 0 }}>
            {t("references.statuses.title")}
          </Typography.Title>
        }
        right={
          <Space>
            <Dropdown.Button
              trigger={["click"]}
              loading={importLoading}
              disabled={!canEdit}
              menu={{ items: importMenuItems, onClick: onImportMenuClick }}
              onClick={() => void runImport(Component2020SyncMode.SnapshotUpsert)}
              data-testid="statuses-import"
            >
              {t("references.mdm.import.button")}
            </Dropdown.Button>
            <Button type="primary" onClick={openCreateGroup} disabled={!canEdit}>
              {t("references.statuses.actions.createGroup")}
            </Button>
            <Button type="primary" onClick={openCreateStatus} disabled={!canEdit}>
              {t("references.statuses.actions.createStatus")}
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

      {groupsError && <Alert type="error" showIcon message={groupsError} style={{ marginBottom: 12 }} />}
      {statusesError && <Alert type="error" showIcon message={statusesError} style={{ marginBottom: 12 }} />}

      <Space style={{ marginBottom: 12 }} wrap>
        <Input
          placeholder={t("common.search")}
          value={statusSearch}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => setStatusSearch(e.target.value)}
          style={{ width: 260 }}
        />
        <Select
          allowClear
          showSearch
          placeholder={t("references.statuses.filters.group")}
          value={statusGroupId}
          onChange={(value) => setStatusGroupId(value)}
          options={groupOptions.map((g) => ({
            value: g.id,
            label: g.name,
          }))}
          style={{ width: 260 }}
        />
        <Select
          value={statusActiveFilter}
          style={{ width: 160 }}
          options={[
            { value: "all", label: t("references.filters.all") },
            { value: "active", label: t("references.filters.active") },
            { value: "inactive", label: t("references.filters.inactive") },
          ]}
          onChange={(v: "all" | "active" | "inactive") => setStatusActiveFilter(v)}
        />
        <Button onClick={() => void loadStatuses()}>{t("common.actions.refresh")}</Button>
      </Space>
      <Table
        rowKey={(r: StatusTreeRow) => r.key}
        loading={statusesLoading || groupsLoading}
        columns={statusColumns}
        dataSource={statusTreeData}
        pagination={false}
        expandable={{ defaultExpandAllRows: true }}
      />

      <Modal
        open={groupModalOpen}
        onCancel={() => setGroupModalOpen(false)}
        onOk={saveGroup}
        okText={t("common.actions.save")}
        title={editingGroup ? t("references.statuses.actions.editGroup") : t("references.statuses.actions.createGroup")}
      >
        <Form form={groupForm} layout="vertical">
          <Form.Item name="name" label={t("references.columns.name")} rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Form.Item name="description" label={t("references.columns.description")}>
            <Input.TextArea rows={3} />
          </Form.Item>
          <Form.Item name="sortOrder" label={t("references.columns.sortOrder")}>
            <InputNumber style={{ width: "100%" }} />
          </Form.Item>
          <Form.Item name="isActive" label={t("references.columns.isActive")} valuePropName="checked">
            <Switch />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        open={statusModalOpen}
        onCancel={() => setStatusModalOpen(false)}
        onOk={saveStatus}
        okText={t("common.actions.save")}
        title={editingStatus ? t("references.statuses.actions.editStatus") : t("references.statuses.actions.createStatus")}
      >
        <Form
          form={statusForm}
          layout="vertical"
          onValuesChange={(changed) => {
            if ("flags" in changed) {
              // keep checkbox in sync with flags
              statusForm.setFieldsValue({ flags: changed.flags });
            }
          }}
        >
          <Form.Item name="groupId" label={t("references.statuses.fields.group")} rules={[{ required: true }]}>
            <Select
              options={groupOptions.map((g) => ({
                value: g.id,
                label: g.name,
              }))}
            />
          </Form.Item>
          <Form.Item name="name" label={t("references.columns.name")} rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Form.Item name="color" label={t("references.statuses.columns.color")}>
            <InputNumber style={{ width: "100%" }} />
          </Form.Item>
          <Form.Item name="flags" label={t("references.statuses.columns.flags")}>
            <InputNumber style={{ width: "100%" }} />
          </Form.Item>
          {showFinalFlag && (
            <Form.Item label={t("references.statuses.fields.isFinal")}>
              <Switch checked={isFinal} onChange={updateFinalFlag} />
            </Form.Item>
          )}
          <Form.Item name="sortOrder" label={t("references.columns.sortOrder")}>
            <InputNumber style={{ width: "100%" }} />
          </Form.Item>
          <Form.Item name="isActive" label={t("references.columns.isActive")} valuePropName="checked">
            <Switch />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};
