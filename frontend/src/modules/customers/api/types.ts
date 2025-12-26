export interface PagedResultDto<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export interface CustomerOrderListItemDto {
  id: string;
  number?: string | null;
  orderDate?: string | null;
  deliveryDate?: string | null;
  state?: number | null;
  customerId?: string | null;
  customerName?: string | null;
  personId?: string | null;
  personName?: string | null;
  contract?: string | null;
  note?: string | null;
  statusName?: string | null;
  statusColor?: number | null;
  createdAt: string;
  updatedAt: string;
}

export interface GetCustomerOrdersParams {
  q?: string;
  customerId?: string;
  pageNumber?: number;
  pageSize?: number;
}
