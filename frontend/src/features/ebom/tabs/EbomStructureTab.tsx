import React, { useState, useCallback, useMemo, useEffect } from 'react';
import { message } from 'antd';

import { EbomTreePanel } from '../components/EbomTreePanel/EbomTreePanel';
import { EbomTablePanel } from '../components/EbomTablePanel/EbomTablePanel';
import { EbomInspector } from '../components/EbomInspector/EbomInspector';
import { ItemSelectModal } from '../components/ItemSelectModal/ItemSelectModal';

import {
  useEbomTree,
  useEbomLines,
  useCreateEbomLine,
  useUpdateEbomLine,
  useDeleteEbomLine,
  useValidateEbom,
} from '../api/hooks';
import {
  EbomTreeNodeDto,
  EbomLineDto,
  ItemSearchResultDto,
  UpdateEbomLinePayload,
  BomRole,
} from '../api/types';
import styles from './EbomStructureTab.module.css';

interface EbomStructureTabProps {
  bomVersionId: string;
  initialParentItemId?: string;
  initialLineId?: string;
}

export const EbomStructureTab: React.FC<EbomStructureTabProps> = ({
  bomVersionId,
  initialParentItemId,
  initialLineId,
}) => {
  // UI State
  const [isEditMode, setIsEditMode] = useState(false);
  const [includeLeaves, setIncludeLeaves] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [onlyErrors, setOnlyErrors] = useState(false);
  const [selectedNodeId, setSelectedNodeId] = useState<string | null>(
    initialParentItemId || null
  );
  const [selectedLineId, setSelectedLineId] = useState<string | null>(
    initialLineId || null
  );
  const [itemSelectModalOpen, setItemSelectModalOpen] = useState(false);
  const [editingLineId, setEditingLineId] = useState<string | null>(null);

  // Deep-link: применяем initial params при монтировании/изменении
  useEffect(() => {
    if (initialParentItemId && initialParentItemId !== selectedNodeId) {
      setSelectedNodeId(initialParentItemId);
    }
  }, [initialParentItemId, selectedNodeId]);

  useEffect(() => {
    if (initialLineId && initialLineId !== selectedLineId) {
      setSelectedLineId(initialLineId);
    }
  }, [initialLineId, selectedLineId]);

  // Data fetching
  const {
    data: treeData,
    isLoading: isTreeLoading,
  } = useEbomTree(bomVersionId, { includeLeaves, q: searchQuery });

  const {
    data: lines,
    isLoading: isLinesLoading,
  } = useEbomLines(
    bomVersionId,
    selectedNodeId ? { parentItemId: selectedNodeId, onlyErrors } : undefined
  );

  // Mutations
  const createLineMutation = useCreateEbomLine(bomVersionId);
  const updateLineMutation = useUpdateEbomLine(bomVersionId, selectedNodeId || '');
  const deleteLineMutation = useDeleteEbomLine(bomVersionId, selectedNodeId || '');
  const validateMutation = useValidateEbom(bomVersionId);

  // Derived data
  const selectedNode = useMemo<EbomTreeNodeDto | null>(() => {
    if (!treeData?.nodes || !selectedNodeId) return null;
    return treeData.nodes.find((n: EbomTreeNodeDto) => n.itemId === selectedNodeId) || null;
  }, [treeData?.nodes, selectedNodeId]);

  const selectedLine = useMemo<EbomLineDto | null>(() => {
    if (!lines || !selectedLineId) return null;
    return lines.find((l: EbomLineDto) => l.id === selectedLineId) || null;
  }, [lines, selectedLineId]);

  // Handlers
  const handleSelectNode = useCallback((itemId: string) => {
    setSelectedNodeId(itemId);
    setSelectedLineId(null);
  }, []);

  const handleSelectLine = useCallback((lineId: string | null) => {
    setSelectedLineId(lineId);
  }, []);

  const handleAddLine = useCallback(() => {
    if (!selectedNodeId) {
      message.warning('Сначала выберите узел в дереве');
      return;
    }
    setEditingLineId(null);
    setItemSelectModalOpen(true);
  }, [selectedNodeId]);

  const handleOpenItemSelect = useCallback((lineId: string) => {
    setEditingLineId(lineId);
    setItemSelectModalOpen(true);
  }, []);

  const handleItemSelect = useCallback(
    (item: ItemSearchResultDto) => {
      if (editingLineId) {
        // Обновляем существующую строку
        updateLineMutation.mutate({
          lineId: editingLineId,
          payload: { itemId: item.id },
        });
      } else if (selectedNodeId) {
        // Создаём новую строку
        createLineMutation.mutate({
          parentItemId: selectedNodeId,
          itemId: item.id,
          role: 'Component' as BomRole,
          qty: 1,
        });
      }
      setItemSelectModalOpen(false);
      setEditingLineId(null);
    },
    [editingLineId, selectedNodeId, updateLineMutation, createLineMutation]
  );

  const handleUpdateLine = useCallback(
    (lineId: string, payload: UpdateEbomLinePayload) => {
      updateLineMutation.mutate({ lineId, payload });
    },
    [updateLineMutation]
  );

  const handleDeleteLine = useCallback(
    (lineId: string) => {
      deleteLineMutation.mutate(lineId);
      if (selectedLineId === lineId) {
        setSelectedLineId(null);
      }
    },
    [deleteLineMutation, selectedLineId]
  );

  const handleValidate = useCallback(() => {
    validateMutation.mutate();
  }, [validateMutation]);

  const handleAddSubNode = useCallback(() => {
    // MVP: placeholder
    message.info('Функция создания подузла будет доступна позже');
  }, []);

  const handleAddComponent = useCallback((parentItemId: string) => {
    setSelectedNodeId(parentItemId);
    setEditingLineId(null);
    setItemSelectModalOpen(true);
  }, []);

  const handleAddSubAssembly = useCallback(() => {
    message.info('Функция создания сборки будет доступна позже');
  }, []);

  const handleDeleteNode = useCallback(() => {
    message.info('Функция удаления узла будет доступна позже');
  }, []);

  return (
    <div className={styles.structureTab}>
      <div className={styles.leftSider}>
        <EbomTreePanel
          nodes={treeData?.nodes || []}
          rootItemId={treeData?.rootItemId}
          isLoading={isTreeLoading}
          selectedNodeId={selectedNodeId}
          includeLeaves={includeLeaves}
          searchQuery={searchQuery}
          isEditMode={isEditMode}
          onSelectNode={handleSelectNode}
          onIncludeLeavesChange={setIncludeLeaves}
          onSearchChange={setSearchQuery}
          onAddComponent={handleAddComponent}
          onAddSubAssembly={handleAddSubAssembly}
          onDeleteNode={handleDeleteNode}
        />
      </div>

      <div className={styles.centerContent}>
        <EbomTablePanel
          lines={lines || []}
          isLoading={isLinesLoading}
          isEditMode={isEditMode}
          onlyErrors={onlyErrors}
          selectedLineId={selectedLineId}
          onEditModeChange={setIsEditMode}
          onOnlyErrorsChange={setOnlyErrors}
          onSelectLine={handleSelectLine}
          onAddLine={handleAddLine}
          onUpdateLine={handleUpdateLine}
          onDeleteLine={handleDeleteLine}
          onValidate={handleValidate}
          onOpenItemSelect={handleOpenItemSelect}
        />
      </div>

      <div className={styles.rightSider}>
        <EbomInspector
          selectedNode={selectedNode}
          selectedLine={selectedLine}
          isEditMode={isEditMode}
          onAddLine={handleAddLine}
          onAddSubNode={handleAddSubNode}
          onUpdateLine={handleUpdateLine}
          onOpenItemSelect={handleOpenItemSelect}
        />
      </div>

      <ItemSelectModal
        open={itemSelectModalOpen}
        onSelect={handleItemSelect}
        onCancel={() => {
          setItemSelectModalOpen(false);
          setEditingLineId(null);
        }}
      />
    </div>
  );
};
