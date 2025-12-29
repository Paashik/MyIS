import React, { useEffect, useMemo, useState } from "react";
import {
  Button,
  Card,
  Checkbox,
  Col,
  Form,
  Input,
  Row,
  Select,
  Space,
  Tree,
  Typography,
  message,
} from "antd";
import type { DataNode } from "antd/es/tree";

import { CommandBar } from "../../components/ui/CommandBar";
import { t } from "../../core/i18n/t";
import { useCan } from "../../core/auth/permissions";
import {
  createAdminOrgUnit,
  deleteAdminOrgUnit,
  getAdminOrgUnit,
  getAdminOrgUnits,
  updateAdminOrgUnit,
} from "../../modules/organization/api/adminOrgUnitsApi";
import type { OrgUnitDetailsDto, OrgUnitListItemDto } from "../../modules/organization/api/types";
import { getAdminEmployees } from "../../modules/settings/security/api/adminSecurityApi";
import type { AdminEmployeeDto } from "../../modules/settings/security/api/types";

type Mode = "view" | "create";

export const OrgStructurePage: React.FC = () => {
  const canEdit = useCan("Admin.Organization.Edit");
  const [orgUnits, setOrgUnits] = useState<OrgUnitListItemDto[]>([]);
  const [employees, setEmployees] = useState<AdminEmployeeDto[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [details, setDetails] = useState<OrgUnitDetailsDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [mode, setMode] = useState<Mode>("view");
  const [form] = Form.useForm();

  const load = async () => {
    setLoading(true);
    try {
      const [units, employeeList] = await Promise.all([
        getAdminOrgUnits(),
        getAdminEmployees({ isActive: true }),
      ]);
      setOrgUnits(units);
      setEmployees(employeeList);
    } catch (error) {
      message.error((error as Error).message || t("common.error.unknownNetwork"));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  const employeeOptions = useMemo(
    () =>
      employees.map((e) => ({
        value: e.id,
        label: e.fullName || e.shortName || e.email || e.id,
      })),
    [employees]
  );

  const unitOptions = useMemo(
    () =>
      orgUnits.map((u) => ({
        value: u.id,
        label: u.code ? `${u.code} — ${u.name}` : u.name,
      })),
    [orgUnits]
  );

  const treeData = useMemo(() => {
    const byId = new Map<string, DataNode>();
    const roots: DataNode[] = [];

    for (const unit of orgUnits) {
      byId.set(unit.id, {
        key: unit.id,
        title: unit.code ? `${unit.code} — ${unit.name}` : unit.name,
        children: [],
      });
    }

    for (const unit of orgUnits) {
      const node = byId.get(unit.id)!;
      if (unit.parentId && byId.has(unit.parentId)) {
        (byId.get(unit.parentId)!.children as DataNode[]).push(node);
      } else {
        roots.push(node);
      }
    }

    return roots;
  }, [orgUnits]);

  const resetFormForCreate = (parentId?: string | null) => {
    setMode("create");
    setSelectedId(null);
    setDetails(null);
    form.setFieldsValue({
      name: "",
      code: "",
      parentId: parentId ?? null,
      managerEmployeeId: null,
      phone: "",
      email: "",
      isActive: true,
      sortOrder: 0,
      contacts: [],
    });
  };

  const loadDetails = async (id: string) => {
    setLoading(true);
    try {
      const dto = await getAdminOrgUnit(id);
      setDetails(dto);
      setMode("view");
      form.setFieldsValue({
        name: dto.name,
        code: dto.code ?? "",
        parentId: dto.parentId ?? null,
        managerEmployeeId: dto.managerEmployeeId ?? null,
        phone: dto.phone ?? "",
        email: dto.email ?? "",
        isActive: dto.isActive,
        sortOrder: dto.sortOrder ?? 0,
        contacts: (dto.contacts ?? []).map((c) => ({
          employeeId: c.employeeId,
          includeInRequest: c.includeInRequest,
          sortOrder: c.sortOrder ?? 0,
        })),
      });
    } catch (error) {
      message.error((error as Error).message || t("common.error.unknownNetwork"));
    } finally {
      setLoading(false);
    }
  };

  const handleSelect = (keys: React.Key[]) => {
    const key = keys.length ? String(keys[0]) : null;
    if (!key) return;
    setSelectedId(key);
    void loadDetails(key);
  };

  const handleSave = async () => {
    try {
      const values = await form.validateFields();
      setSaving(true);
      const payload = {
        name: values.name,
        code: values.code || undefined,
        parentId: values.parentId || null,
        managerEmployeeId: values.managerEmployeeId || null,
        phone: values.phone || undefined,
        email: values.email || undefined,
        isActive: values.isActive !== false,
        sortOrder: Number(values.sortOrder ?? 0),
        contacts: (values.contacts ?? []).map((c: any) => ({
          employeeId: c.employeeId,
          includeInRequest: !!c.includeInRequest,
          sortOrder: Number(c.sortOrder ?? 0),
        })),
      };

      let result: OrgUnitDetailsDto;
      if (mode === "create") {
        result = await createAdminOrgUnit(payload);
      } else if (selectedId) {
        result = await updateAdminOrgUnit(selectedId, payload);
      } else {
        return;
      }

      await load();
      setSelectedId(result.id);
      await loadDetails(result.id);
      message.success(t("common.actions.save"));
    } catch (error) {
      if (error instanceof Error) {
        message.error(error.message);
      }
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async () => {
    if (!selectedId) return;
    setSaving(true);
    try {
      await deleteAdminOrgUnit(selectedId);
      setSelectedId(null);
      setDetails(null);
      form.resetFields();
      await load();
      message.success(t("common.actions.delete"));
    } catch (error) {
      message.error((error as Error).message || t("common.error.unknownNetwork"));
    } finally {
      setSaving(false);
    }
  };

  return (
    <div data-testid="administration-org-structure-page">
      <CommandBar
        left={
          <Typography.Title level={2} style={{ margin: 0 }}>
            {t("settings.organization.structure.title")}
          </Typography.Title>
        }
        right={
          <Space>
            <Button onClick={() => void load()} loading={loading}>
              {t("common.actions.refresh")}
            </Button>
            <Button
              type="primary"
              disabled={!canEdit}
              onClick={() => resetFormForCreate(null)}
            >
              {t("common.actions.add")}
            </Button>
            <Button
              disabled={!canEdit || !selectedId}
              onClick={() => resetFormForCreate(selectedId)}
            >
              {t("settings.organization.structure.actions.addChild")}
            </Button>
          </Space>
        }
      />

      <Row gutter={16}>
        <Col xs={24} md={8}>
          <Card size="small" title={t("settings.organization.structure.tree.title")}>
            <Tree
              treeData={treeData}
              selectedKeys={selectedId ? [selectedId] : []}
              onSelect={handleSelect}
            />
          </Card>
        </Col>
        <Col xs={24} md={16}>
          <Card
            size="small"
            title={
              mode === "create"
                ? t("settings.organization.structure.form.new")
                : t("settings.organization.structure.form.title")
            }
          >
            <Form layout="vertical" form={form}>
              <Row gutter={16}>
                <Col xs={24} md={12}>
                  <Form.Item
                    label={t("settings.organization.structure.form.name")}
                    name="name"
                    rules={[
                      {
                        required: true,
                        message: t("settings.organization.structure.form.name.required"),
                      },
                    ]}
                  >
                    <Input />
                  </Form.Item>
                </Col>
                <Col xs={24} md={12}>
                  <Form.Item label={t("settings.organization.structure.form.code")} name="code">
                    <Input />
                  </Form.Item>
                </Col>
              </Row>

              <Row gutter={16}>
                <Col xs={24} md={12}>
                  <Form.Item
                    label={t("settings.organization.structure.form.parent")}
                    name="parentId"
                  >
                    <Select allowClear options={unitOptions} />
                  </Form.Item>
                </Col>
                <Col xs={24} md={12}>
                  <Form.Item
                    label={t("settings.organization.structure.form.manager")}
                    name="managerEmployeeId"
                  >
                    <Select allowClear options={employeeOptions} />
                  </Form.Item>
                </Col>
              </Row>

              <Row gutter={16}>
                <Col xs={24} md={12}>
                  <Form.Item
                    label={t("settings.organization.structure.form.phone")}
                    name="phone"
                  >
                    <Input />
                  </Form.Item>
                </Col>
                <Col xs={24} md={12}>
                  <Form.Item
                    label={t("settings.organization.structure.form.email")}
                    name="email"
                  >
                    <Input />
                  </Form.Item>
                </Col>
              </Row>

              <Row gutter={16}>
                <Col xs={24} md={8}>
                  <Form.Item
                    label={t("settings.organization.structure.form.sortOrder")}
                    name="sortOrder"
                  >
                    <Input type="number" />
                  </Form.Item>
                </Col>
                <Col xs={24} md={8}>
                  <Form.Item
                    label={t("settings.organization.structure.form.isActive")}
                    name="isActive"
                    valuePropName="checked"
                  >
                    <Checkbox />
                  </Form.Item>
                </Col>
              </Row>

              <Form.List name="contacts">
                {(fields, { add, remove }) => (
                  <div>
                    <Typography.Title level={5}>
                      {t("settings.organization.structure.form.contacts.title")}
                    </Typography.Title>
                    {fields.map((field) => (
                      <Row key={field.key} gutter={12}>
                        <Col xs={24} md={12}>
                          <Form.Item
                            {...field}
                            label={t("settings.organization.structure.form.contacts.employee")}
                            name={[field.name, "employeeId"]}
                            rules={[
                              {
                                required: true,
                                message: t(
                                  "settings.organization.structure.form.contacts.employee.required"
                                ),
                              },
                            ]}
                          >
                            <Select options={employeeOptions} />
                          </Form.Item>
                        </Col>
                        <Col xs={12} md={5}>
                          <Form.Item
                            label={t("settings.organization.structure.form.contacts.includeInRequest")}
                            name={[field.name, "includeInRequest"]}
                            valuePropName="checked"
                          >
                            <Checkbox />
                          </Form.Item>
                        </Col>
                        <Col xs={12} md={5}>
                          <Form.Item
                            label={t("settings.organization.structure.form.contacts.sortOrder")}
                            name={[field.name, "sortOrder"]}
                          >
                            <Input type="number" />
                          </Form.Item>
                        </Col>
                        <Col xs={24} md={2}>
                          <Button onClick={() => remove(field.name)} danger>
                            {t("common.actions.delete")}
                          </Button>
                        </Col>
                      </Row>
                    ))}
                    <Button onClick={() => add()}>{t("common.actions.add")}</Button>
                  </div>
                )}
              </Form.List>

              <Space style={{ marginTop: 16 }}>
                <Button type="primary" onClick={handleSave} loading={saving} disabled={!canEdit}>
                  {t("common.actions.save")}
                </Button>
                <Button danger onClick={handleDelete} disabled={!canEdit || !selectedId} loading={saving}>
                  {t("common.actions.delete")}
                </Button>
              </Space>
            </Form>
          </Card>
        </Col>
      </Row>
    </div>
  );
};
