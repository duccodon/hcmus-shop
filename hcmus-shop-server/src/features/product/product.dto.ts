export interface ProductSortDto {
  field: string;
  direction: string;
}

export interface ProductFilterDto {
  search?: string;
  name?: string;
  sku?: string;
  categoryId?: number;
  brandId?: number;
  categoryIds?: number[];
  brandIds?: number[];
  minPrice?: number;
  maxPrice?: number;
  inStockOnly?: boolean;
  sorts?: ProductSortDto[];
  sortBy?: string;
  sortOrder?: string;
  page?: number;
  pageSize?: number;
}

export interface CreateProductDto {
  sku: string;
  name: string;
  brandId: number;
  seriesId?: number;
  importPrice: number;
  sellingPrice: number;
  stockQuantity?: number;
  specifications?: unknown;
  description?: string;
  warrantyMonths?: number;
  categoryIds?: number[];
  imageUrls?: string[];
}

export interface UpdateProductDto {
  sku?: string;
  name?: string;
  brandId?: number;
  seriesId?: number;
  importPrice?: number;
  sellingPrice?: number;
  stockQuantity?: number;
  specifications?: unknown;
  description?: string;
  warrantyMonths?: number;
  isActive?: boolean;
  categoryIds?: number[];
  imageUrls?: string[];
}
