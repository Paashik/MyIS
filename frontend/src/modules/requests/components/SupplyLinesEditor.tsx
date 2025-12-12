import React from "react";
import { Button, Card, Col, DatePicker, Form, Input, InputNumber, Row, Space } from "antd";

import { t } from "../../../core/i18n/t";

export interface SupplyLinesEditorProps {
  name: string;
}

/**
 * Минимальный редактор строк SupplyRequest.
 * v0.1: без выбора номенклатуры, только базовые поля из ТЗ.
 */
export const SupplyLinesEditor: React.FC<SupplyLinesEditorProps> = ({ name }) => {
  // В Ant Design v5 Form.List существует, но типы иногда не пробрасываются корректно.
  // Делаем безопасный cast, чтобы не ломать strict TS.
  const FormList = (Form as any).List as React.FC<any>;

  return (
    <FormList name={name}>
      {(fields: any[], { add, remove }: { add: (initialValue?: any) => void; remove: (index: number) => void }) => (
        <div>
          <Space style={{ marginBottom: 12 }}>
            <Button
              data-testid="supply-lines-add"
              type="dashed"
              onClick={() => add({ quantity: 1 })}
            >
              {t("requests.supply.lines.actions.add")}
            </Button>
          </Space>

          <Space direction="vertical" style={{ width: "100%" }} size={12}>
            {fields.map((field: any) => (
              <Card
                key={field.key}
                size="small"
                title={t("requests.supply.lines.card.title", { no: field.name + 1 })}
                extra={
                  <Button
                    data-testid={`supply-lines-remove-${field.name}`}
                    danger
                    onClick={() => remove(field.name)}
                  >
                    {t("requests.supply.lines.actions.remove")}
                  </Button>
                }
              >
                <Row gutter={12}>
                  <Col span={24}>
                    <Form.Item
                      label={t("requests.supply.lines.fields.description")}
                      name={[field.name, "description"]}
                    >
                      <Input
                        data-testid={`supply-lines-${field.name}-description`}
                        placeholder={t("requests.supply.lines.placeholders.description")}
                      />
                    </Form.Item>
                  </Col>

                  <Col span={8}>
                    <Form.Item
                      label={t("requests.supply.lines.fields.quantity")}
                      name={[field.name, "quantity"]}
                      rules={[
                        {
                          required: true,
                          message: t("requests.supply.validation.quantityRequired"),
                        },
                      ]}
                    >
                      <InputNumber
                        data-testid={`supply-lines-${field.name}-quantity`}
                        min={0}
                        style={{ width: "100%" }}
                      />
                    </Form.Item>
                  </Col>

                  <Col span={16}>
                    <Form.Item
                      label={t("requests.supply.lines.fields.needByDate")}
                      name={[field.name, "needByDate"]}
                    >
                      <DatePicker
                        data-testid={`supply-lines-${field.name}-needByDate`}
                        style={{ width: "100%" }}
                        showTime
                      />
                    </Form.Item>
                  </Col>

                  <Col span={12}>
                    <Form.Item
                      label={t("requests.supply.lines.fields.supplierName")}
                      name={[field.name, "supplierName"]}
                    >
                      <Input data-testid={`supply-lines-${field.name}-supplierName`} />
                    </Form.Item>
                  </Col>

                  <Col span={12}>
                    <Form.Item
                      label={t("requests.supply.lines.fields.supplierContact")}
                      name={[field.name, "supplierContact"]}
                    >
                      <Input data-testid={`supply-lines-${field.name}-supplierContact`} />
                    </Form.Item>
                  </Col>
                </Row>
              </Card>
            ))}
          </Space>
        </div>
      )}
    </FormList>
  );
};

