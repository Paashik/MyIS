import React, { useMemo, useState, useCallback } from 'react';
import { Input, Switch, Tree, Spin, Empty, Badge, Dropdown } from 'antd';
import {
  SearchOutlined,
  FolderOutlined,
  FileOutlined,
  BlockOutlined,
  InboxOutlined,
  WarningOutlined,
  PlusOutlined,
  DeleteOutlined,
} from '@ant-design/icons';
import type { MenuProps } from 'antd';

import { EbomTreeNodeDto, TreeItemType } from '../../api/types';
import styles from './EbomTreePanel.module.css';

interface EbomTreePanelProps {
  nodes: EbomTreeNodeDto[];
  rootItemId: string | undefined;
  isLoading: boolean;
  selectedNodeId: string | null;
  includeLeaves: boolean;
  searchQuery: string;
  isEditMode: boolean;
  onSelectNode: (itemId: string) => void;
  onIncludeLeavesChange: (value: boolean) => void;
  onSearchChange: (value: string) => void;
  onAddComponent?: (parentItemId: string) => void;
  onAddSubAssembly?: (parentItemId: string) => void;
  onDeleteNode?: (itemId: string) => void;
}

interface TreeDataNode {
  key: string;
  title: React.ReactNode;
  children?: TreeDataNode[];
  isLeaf?: boolean;
  icon?: React.ReactNode;
}

const itemTypeIcons: Record<TreeItemType, React.ReactNode> = {
  Assembly: <FolderOutlined className={styles.treeNodeIconAssembly} />,
  Part: <FileOutlined className={styles.treeNodeIconPart} />,
  Component: <BlockOutlined className={styles.treeNodeIconComponent} />,
  Material: <InboxOutlined className={styles.treeNodeIconMaterial} />,
};

export const EbomTreePanel: React.FC<EbomTreePanelProps> = ({
  nodes,
  rootItemId,
  isLoading,
  selectedNodeId,
  includeLeaves,
  searchQuery,
  isEditMode,
  onSelectNode,
  onIncludeLeavesChange,
  onSearchChange,
  onAddComponent,
  onAddSubAssembly,
  onDeleteNode,
}) => {
  const [expandedKeys, setExpandedKeys] = useState<string[]>([]);
  const [contextMenuNode, setContextMenuNode] = useState<string | null>(null);

  // Построение дерева из плоского списка
  const treeData = useMemo(() => {
    if (!nodes.length || !rootItemId) return [];

    const nodeMap = new Map<string, EbomTreeNodeDto>();
    nodes.forEach((node) => nodeMap.set(node.itemId, node));

    const buildNode = (node: EbomTreeNodeDto): TreeDataNode => {
      const children = nodes.filter((n) => n.parentItemId === node.itemId);
      const hasChildren = children.length > 0;

      const title = (
        <div className={styles.treeNode}>
          {node.code && <span className={styles.treeNodeCode}>{node.code}</span>}
          <span className={styles.treeNodeName}>{node.name}</span>
          {node.hasErrors && (
            <Badge
              className={styles.errorBadge}
              count={<WarningOutlined style={{ color: '#ff4d4f', fontSize: 12 }} />}
            />
          )}
        </div>
      );

      return {
        key: node.itemId,
        title,
        icon: itemTypeIcons[node.itemType],
        isLeaf: !hasChildren,
        children: hasChildren ? children.map(buildNode) : undefined,
      };
    };

    const rootNode = nodeMap.get(rootItemId);
    if (!rootNode) return [];

    return [buildNode(rootNode)];
  }, [nodes, rootItemId]);

  // Автоматическое раскрытие корня
  React.useEffect(() => {
    if (rootItemId && !expandedKeys.includes(rootItemId)) {
      setExpandedKeys([rootItemId]);
    }
  }, [rootItemId]);

  const handleSelect = useCallback(
    (selectedKeys: React.Key[]) => {
      if (selectedKeys.length > 0) {
        onSelectNode(selectedKeys[0] as string);
      }
    },
    [onSelectNode]
  );

  const handleExpand = useCallback((keys: React.Key[]) => {
    setExpandedKeys(keys as string[]);
  }, []);

  const contextMenuItems: MenuProps['items'] = useMemo(
    () => [
      {
        key: 'addComponent',
        icon: <PlusOutlined />,
        label: 'Добавить компонент',
        disabled: !isEditMode,
        onClick: () => {
          if (contextMenuNode && onAddComponent) {
            onAddComponent(contextMenuNode);
          }
        },
      },
      {
        key: 'addSubAssembly',
        icon: <FolderOutlined />,
        label: 'Добавить сборку',
        disabled: !isEditMode,
        onClick: () => {
          if (contextMenuNode && onAddSubAssembly) {
            onAddSubAssembly(contextMenuNode);
          }
        },
      },
      { type: 'divider' as const },
      {
        key: 'delete',
        icon: <DeleteOutlined />,
        label: 'Удалить узел',
        disabled: !isEditMode || contextMenuNode === rootItemId,
        danger: true,
        onClick: () => {
          if (contextMenuNode && onDeleteNode) {
            onDeleteNode(contextMenuNode);
          }
        },
      },
    ],
    [isEditMode, contextMenuNode, rootItemId, onAddComponent, onAddSubAssembly, onDeleteNode]
  );

  const handleRightClick = useCallback(
    ({ node }: { node: { key: React.Key } }) => {
      setContextMenuNode(node.key as string);
    },
    []
  );

  if (isLoading) {
    return (
      <div className={styles.treePanel}>
        <div className={styles.loadingContainer}>
          <Spin />
        </div>
      </div>
    );
  }

  return (
    <div className={styles.treePanel}>
      <div className={styles.treePanelHeader}>
        <Input
          className={styles.searchInput}
          placeholder="Поиск по коду/названию..."
          prefix={<SearchOutlined />}
          value={searchQuery}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => onSearchChange(e.target.value)}
          allowClear
        />
        <div className={styles.treeOptions}>
          <Switch
            size="small"
            checked={includeLeaves}
            onChange={onIncludeLeavesChange}
          />
          <span>Показывать листья</span>
        </div>
      </div>

      <div className={styles.treeContent}>
        {treeData.length === 0 ? (
          <div className={styles.emptyContainer}>
            <Empty description="Структура пуста" />
          </div>
        ) : (
          <Dropdown
            menu={{ items: contextMenuItems }}
            trigger={['contextMenu']}
          >
            <div>
              <Tree
                showIcon
                blockNode
                treeData={treeData}
                selectedKeys={selectedNodeId ? [selectedNodeId] : []}
                expandedKeys={expandedKeys}
                onSelect={handleSelect}
                onExpand={handleExpand}
                onRightClick={handleRightClick}
              />
            </div>
          </Dropdown>
        )}
      </div>
    </div>
  );
};
