import React, { useEffect, useState } from "react";
import { Button, DatePicker, Form, Input, InputNumber, Select, Space, Table } from "antd";
import type { FormInstance } from "rc-field-form";
import type { ColumnsType } from "antd/es/table";
import type { Dayjs } from "dayjs";
import dayjs from "dayjs";
import { useMdmReferencesQueryService, type ItemListItemDto } from "../../../core/api/mdmReferencesQueryService";

import { t } from "../../../core/i18n/t";
import type { RequestLineInputDto } from "../api/types";

export interface SupplyLinesEditorProps {
  name: string;
  form: FormInstance;
}

/**
 * Редактор строк SupplyRequest с использованием Table для производительности.
 * Поддерживает большое количество строк без зависания.
 */
export const SupplyLinesEditor: React.FC<SupplyLinesEditorProps> = ({ name, form }) => {
  const [lines, setLines] = useState<RequestLineInputDto[]>([]);
  const [itemOptions, setItemOptions] = useState<{ label: string; value: string }[]>([]);
  const [currentItems, setCurrentItems] = useState<ItemListItemDto[]>([]);
  const { getItems } = useMdmReferencesQueryService();

  useEffect(() => {
    const current = form.getFieldValue(name) || [];
    setLines(current);
  }, [form, name]);

  const updateForm = (newLines: RequestLineInputDto[]) => {
    form.setFieldsValue({ [name]: newLines });
  };

  const handleAdd = () => {
    const newLine: RequestLineInputDto = {
      lineNo: lines.length + 1,
      quantity: 1,
      description: "",
      needByDate: undefined,
      supplierName: "",
      supplierContact: "",
    };
    const updated = [...lines, newLine];
    setLines(updated);
    updateForm(updated);
  };

  const handleDelete = (index: number) => {
    const updated = lines.filter((_, i) => i !== index);
    setLines(updated);
    updateForm(updated);
  };

  const handleChange = (index: number, key: keyof RequestLineInputDto, value: any) => {
    const updated = lines.map((l, i) => (i === index ? { ...l, [key]: value } : l));
    setLines(updated);
    updateForm(updated);
  };

  const handleItemSearch = async (search: string) => {
    if (!search.trim()) {
      setItemOptions([]);
      return;
    }
    try {
      const result = await getItems({ searchText: search, pageNumber: 1, pageSize: 10 });
      setCurrentItems(result.items);
      setItemOptions(result.items.map(item => ({
        label: `${item.code} - ${item.name}`,
        value: item.code,
      })));
    } catch (error) {
      console.error('Failed to search items:', error);
    }
  };

  const handleItemSelect = (index: number, value?: string) => {
    if (!value) {
      handleChange(index, "itemId", undefined);
      handleChange(index, "externalItemCode", undefined);
      return;
    }
    const item = currentItems.find(i => i.code === value);
    if (item) {
      handleChange(index, "itemId", item.id);
      handleChange(index, "externalItemCode", value);
      return;
    }
    handleChange(index, "itemId", undefined);
    handleChange(index, "externalItemCode", value);
  };

  const columns: ColumnsType<RequestLineInputDto> = [
    {
      title: "No",
      dataIndex: "lineNo",
      render: (value) => value,
    },
    {
      title: "Item",
      dataIndex: "externalItemCode",
      render: (value, record, index) => (
        <Select
          value={value || undefined}
          options={itemOptions}
          showSearch
          filterOption={false}
          onSearch={handleItemSearch}
          onSelect={(val: string | undefined) => handleItemSelect(index, val)}
          onChange={(val?: string) => handleItemSelect(index, val)}
          placeholder="Search and select item"
          style={{ width: "100%" }}
          allowClear
        />
      ),
    },
    {
      title: "Description",
      dataIndex: "description",
      render: (value, record, index) => (
        <Input
          value={value || ""}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleChange(index, "description", e.target.value)}
          placeholder="Enter description"
        />
      ),
    },
    {
      title: "Quantity",
      dataIndex: "quantity",
      render: (value, record, index) => (
        <InputNumber
          value={value || 0}
          min={0}
          onChange={(val: number | null) => handleChange(index, "quantity", val)}
          style={{ width: "100%" }}
        />
      ),
    },
    {
      title: "Need by Date",
      dataIndex: "needByDate",
      render: (value, record, index) => (
        <DatePicker
          value={value ? dayjs(value) : undefined}
          onChange={(date: Dayjs | null) => handleChange(index, "needByDate", date ? date.toISOString() : undefined)}
          style={{ width: "100%" }}
          showTime
        />
      ),
    },
    {
      title: "Supplier Name",
      dataIndex: "supplierName",
      render: (value, record, index) => (
        <Input
          value={value || ""}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleChange(index, "supplierName", e.target.value)}
        />
      ),
    },
    {
      title: "Supplier Contact",
      dataIndex: "supplierContact",
      render: (value, record, index) => (
        <Input
          value={value || ""}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleChange(index, "supplierContact", e.target.value)}
        />
      ),
    },
    {
      title: "Actions",
      key: "actions",
      render: (_, record, index) => (
        <Button
          danger
          onClick={() => handleDelete(index)}
          size="small"
        >
          Remove
        </Button>
      ),
    },
  ];

  return (
    <div>
      <Space style={{ marginBottom: 12 }}>
        <Button
          data-testid="supply-lines-add"
          type="dashed"
          onClick={handleAdd}
        >
          {t("requests.supply.lines.actions.add")}
        </Button>
      </Space>

      <Table
        dataSource={lines}
        columns={columns}
        rowKey={(record: RequestLineInputDto, index?: number) => `line-${index ?? 0}`}
        pagination={false}
        scroll={{ y: 400, x: 'max-content' }}
        size="small"
      />
    </div>
  );
};

