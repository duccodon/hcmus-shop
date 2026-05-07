export interface PromotionFilterDto {
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface CreatePromotionDto {
  code: string;
  discountPercent?: number | null;
  discountAmount?: number | null;
  startDate: string;
  endDate: string;
  isActive?: boolean;
}

export interface UpdatePromotionDto {
  code?: string;
  discountPercent?: number | null;
  discountAmount?: number | null;
  startDate?: string;
  endDate?: string;
  isActive?: boolean;
}

export interface PromotionValidationResultDto {
  isValid: boolean;
  message: string;
  promotion: unknown | null;
}
