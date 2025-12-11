import React from "react";

export interface RequestStatusBadgeProps {
  statusCode: string;
  statusName: string;
}

function getStatusColor(statusCode: string): string {
  const normalized = statusCode.toLowerCase();

  if (normalized.includes("draft")) {
    return "#d9d9d9"; // grey
  }
  if (normalized.includes("onapproval") || normalized.includes("review")) {
    return "#4096ff"; // blue
  }
  if (normalized.includes("approved") || normalized.includes("agreed")) {
    return "#52c41a"; // green
  }
  if (normalized.includes("inwork") || normalized.includes("active")) {
    return "#4096ff"; // blue
  }
  if (normalized.includes("closed") || normalized.includes("done")) {
    return "#52c41a"; // green
  }
  if (normalized.includes("rejected") || normalized.includes("declined")) {
    return "#ff4d4f"; // red
  }

  return "#d9d9d9";
}

/**
 * Визуальное отображение статуса заявки без зависимости от конкретных
 * компонентов Ant Design (используем только стили и span),
 * чтобы избежать проблем с типами и tree-shaking.
 */
export const RequestStatusBadge: React.FC<RequestStatusBadgeProps> = ({
  statusCode,
  statusName,
}) => {
  const color = getStatusColor(statusCode);

  return (
    <span
      style={{
        display: "inline-flex",
        alignItems: "center",
        gap: 6,
        fontSize: 12,
      }}
    >
      <span
        style={{
          width: 8,
          height: 8,
          borderRadius: "50%",
          backgroundColor: color,
          display: "inline-block",
        }}
      />
      <span>{statusName}</span>
    </span>
  );
};