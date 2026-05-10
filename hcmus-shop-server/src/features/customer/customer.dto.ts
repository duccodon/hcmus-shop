export interface CustomerFilterDto {
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface CreateCustomerDto {
  name: string;
  phone?: string;
  email?: string;
}

export interface UpdateCustomerDto {
  name?: string;
  phone?: string | null;
  email?: string | null;
}
