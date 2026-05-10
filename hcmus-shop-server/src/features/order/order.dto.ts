export interface OrderFilterDto {
  status?: string;
  fromDate?: string;
  toDate?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface ProductInstanceFilterDto {
  search?: string;
  productId?: number;
  page?: number;
  pageSize?: number;
}

export interface OrderItemInputDto {
  instanceId: number;
  quantity: number;
}

export interface CreateOrderDto {
  customerId: string;
  promotionCode?: string;
  items: OrderItemInputDto[];
  notes?: string;
}

export interface UpdateOrderDto {
  customerId?: string;
  promotionCode?: string | null;
  items?: OrderItemInputDto[];
  notes?: string | null;
}
