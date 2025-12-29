import { useState } from 'react';

export interface ItemListItemDto {
  id: string;
  code: string;
  nomenclatureNo: string;
  name: string;
  designation?: string;
  itemKind?: string | null;
  isActive: boolean;
  hasPhoto?: boolean;
  unitOfMeasureName?: string;
  itemGroupId?: string | null;
  itemGroupName?: string;
}

export interface GetItemsResult {
  items: ItemListItemDto[];
  totalCount: number;
}

export interface GetItemsParams {
  groupId?: string | null;
  searchText?: string;
  isActive?: boolean;
  pageNumber: number;
  pageSize: number;
}

export const useMdmReferencesQueryService = () => {
  const [loading, setLoading] = useState<boolean>(false);

  const getItems = async (params: GetItemsParams): Promise<GetItemsResult> => {
    setLoading(true);
    try {
      const queryParams = new URLSearchParams();
      if (params.groupId) {
        queryParams.append('groupId', params.groupId);
      }
      if (params.searchText) {
        queryParams.append('q', params.searchText);
      }
      if (typeof params.isActive === 'boolean') {
        queryParams.append('isActive', String(params.isActive));
      }
      queryParams.append('skip', ((params.pageNumber - 1) * params.pageSize).toString());
      queryParams.append('take', params.pageSize.toString());

      const url = `/api/admin/references/mdm/items?${queryParams.toString()}`;
      const response = await fetch(url);
      if (!response.ok) {
        throw new Error('Failed to fetch items');
      }
      const data = await response.json();
      const totalCount = Number(data?.totalCount ?? data?.total ?? 0);
      return {
        items: Array.isArray(data?.items) ? data.items : [],
        totalCount,
      };
    } finally {
      setLoading(false);
    }
  };

  return {
    getItems,
    loading,
  };
};
