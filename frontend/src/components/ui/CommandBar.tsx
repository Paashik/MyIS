import React from "react";

export interface CommandBarProps {
  left?: React.ReactNode;
  right?: React.ReactNode;
}

export const CommandBar: React.FC<CommandBarProps> = ({ left, right }) => {
  return (
    <div
      style={{
        marginBottom: 16,
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        gap: 12,
        flexWrap: "wrap",
      }}
    >
      <div style={{ display: "flex", alignItems: "center", gap: 12, flexWrap: "wrap" }}>
        {left}
      </div>
      <div style={{ display: "flex", alignItems: "center", gap: 8, flexWrap: "wrap" }}>
        {right}
      </div>
    </div>
  );
};

