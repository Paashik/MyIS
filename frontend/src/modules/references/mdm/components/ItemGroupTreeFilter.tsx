import React, { useEffect, useState, ChangeEvent } from "react";
import { Input, Spin } from "antd";
import Tree from "antd/es/tree";
import Empty from "antd/es/empty";
import type { DataNode } from "antd/es/tree";
import { t } from "../../../../core/i18n/t";
import { getMdmDictionaryList } from "../api/adminMdmReferencesApi";
import type { MdmItemGroupReferenceDto } from "../api/adminMdmReferencesApi";

const { Search } = Input;

export interface ItemGroupTreeFilterProps {
  onGroupSelect?: (groupId: string | null, groupName: string | null) => void;
  selectedGroupId?: string | null;
  placeholder?: string;
}

export const ItemGroupTreeFilter: React.FC<ItemGroupTreeFilterProps> = ({
  onGroupSelect,
  selectedGroupId,
  placeholder = t("references.mdm.itemGroups.searchPlaceholder"),
}) => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState("");
  const [itemGroups, setItemGroups] = useState<MdmItemGroupReferenceDto[]>([]);
  const [treeData, setTreeData] = useState<DataNode[]>([]);
  const [expandedKeys, setExpandedKeys] = useState<React.Key[]>([]);
  const [autoExpandParent, setAutoExpandParent] = useState(true);

  useEffect(() => {
    loadItemGroups();
  }, []);

  useEffect(() => {
    if (itemGroups.length > 0) {
      buildTreeData(itemGroups, searchQuery);
    }
  }, [itemGroups, searchQuery]);

  useEffect(() => {
    if (selectedGroupId && treeData.length > 0) {
      // Find the path to the selected group and expand it
      const findPath = (nodes: DataNode[], targetKey: string, path: React.Key[] = []): React.Key[] | null => {
        for (const node of nodes) {
          const currentPath = [...path, node.key];
          if (node.key === targetKey) {
            return currentPath;
          }
          if (node.children) {
            const result = findPath(node.children, targetKey, currentPath);
            if (result) return result;
          }
        }
        return null;
      };

      const path = findPath(treeData, selectedGroupId);
      if (path) {
        setExpandedKeys(path);
      }
    }
  }, [selectedGroupId, treeData]);

  const loadItemGroups = async () => {
    setLoading(true);
    setError(null);
    try {
      const take = 1000;
      let skip = 0;
      let allGroups: MdmItemGroupReferenceDto[] = [];

      while (true) {
        const page = await getMdmDictionaryList<MdmItemGroupReferenceDto>("item-groups", { skip, take });
        allGroups = allGroups.concat(page.items);
        if (allGroups.length >= page.total || page.items.length === 0) break;
        skip += take;
        if (skip > 200000) break;
      }



      setItemGroups(allGroups);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setLoading(false);
    }
  };

  const buildTreeData = (groups: MdmItemGroupReferenceDto[], query: string) => {
    const byId = new Map<string, { node: MdmItemGroupReferenceDto; children: DataNode[] }>();

    // Сначала добавим ВСЕ группы в карту
    for (const group of groups) {
      byId.set(String(group.id), { node: group, children: [] });
    }

    // Определим, какие узлы "выживают" в результате поиска
    const matchesQuery = (g: MdmItemGroupReferenceDto): boolean => {
      const q = query || '';
      if (q.trim() === '') return true;
      const nameMatch = g.name ? g.name.toLowerCase().includes(q.toLowerCase()) : false;
      const abbrMatch = g.abbreviation ? g.abbreviation.toLowerCase().includes(q.toLowerCase()) : false;
      return nameMatch || abbrMatch;
    };

    // Множество ID, которые должны быть включены (включая их цепочку родителей)
    const includedIds = new Set<string>();

    // Шаг 1: добавим все узлы, совпадающие с запросом
    for (const group of groups) {
      if (matchesQuery(group)) {
        includedIds.add(String(group.id));
      }
    }

    // Шаг 2: включим всех родителей для этих узлов
    for (const group of groups) {
      let current = group;
      while (current.parentId && includedIds.has(String(current.id))) {
        current = groups.find(g => g.id === current.parentId)!;
        if (current) {
          includedIds.add(String(current.id));
        } else {
          break;
        }
      }
    }

    // Шаг 3: построим дерево только для включённых узлов
    const roots: DataNode[] = [];
    const nodeMap = new Map<string, DataNode>();

    // Проходим по всем группам, но создаём узлы только для includedIds
    for (const group of groups) {
      const id = String(group.id);
      if (!includedIds.has(id)) continue;

      const treeNode: DataNode = {
        key: id,
        title: (
          <span>
            {group.name}
            {group.abbreviation && <span style={{ color: "#999", marginLeft: 8 }}>({group.abbreviation})</span>}
          </span>
        ),
        children: undefined,
      };

      nodeMap.set(id, treeNode);

      const parentId = group.parentId ? String(group.parentId) : null;

      if (parentId && includedIds.has(parentId)) {
        const parentEntry = byId.get(parentId);
        if (parentEntry) {
          parentEntry.children.push(treeNode);
        }
      } else {
        roots.push(treeNode);
      }
    }

    // Устанавливаем children для каждого узла
    for (const [id, entry] of byId.entries()) {
      if (includedIds.has(id)) {
        const node = nodeMap.get(id);
        if (node && entry.children.length > 0) {
          node.children = entry.children;
        }
      }
    }

    // Функция сортировки по имени (а не по React-ноде)
    const sortNodes = (nodes: DataNode[]) => {
      nodes.sort((a, b) => {
        const nameA = byId.get(String(a.key))?.node.name || '';
        const nameB = byId.get(String(b.key))?.node.name || '';
        return nameA.localeCompare(nameB);
      });
      nodes.forEach(node => {
        if (node.children) {
          sortNodes(node.children);
        }
      });
    };

    sortNodes(roots);
    setTreeData(roots);
  };

  const onSearch = (value: string) => {
    setSearchQuery(value);
    setAutoExpandParent(true);
  };

  const onExpand = (newExpandedKeys: React.Key[]) => {
    setExpandedKeys(newExpandedKeys);
    setAutoExpandParent(false);
  };

  const onSelect = (selectedKeys: React.Key[], info: any) => {
    if (selectedKeys.length === 0) {
      onGroupSelect?.(null, null);
    } else {
      const selectedKey = selectedKeys[0] as string;
      const group = itemGroups.find(g => String(g.id) === selectedKey);
      if (group) {
        onGroupSelect?.(group.id, group.name);
      }
    }
  };

  return (
    <div style={{ padding: "8px 0" }}>
      <Search
        placeholder={placeholder}
        allowClear
        onSearch={onSearch}
        onChange={(e: ChangeEvent<HTMLInputElement>) => setSearchQuery(e.target.value)}
        style={{ marginBottom: 8 }}
      />

      {loading ? (
        <div style={{ textAlign: "center", padding: "24px 0" }}>
          <Spin />
        </div>
      ) : error ? (
        <div style={{ padding: "12px 0" }}>
          <Empty description={error} />
        </div>
      ) : treeData.length === 0 ? (
        <div style={{ padding: "12px 0" }}>
          <Empty description={t("references.mdm.itemGroups.noGroups")} />
        </div>
      ) : (
        <Tree
          showLine
          showIcon
          expandedKeys={expandedKeys}
          autoExpandParent={autoExpandParent}
          onExpand={onExpand}
          selectedKeys={selectedGroupId ? [selectedGroupId] : []}
          onSelect={onSelect}
          treeData={treeData}
          style={{ height: 400, overflow: "auto", border: "1px solid #f0f0f0", borderRadius: 4, padding: 8 }}
        />
      )}
    </div>
  );
};