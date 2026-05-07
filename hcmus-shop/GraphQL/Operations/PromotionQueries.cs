namespace hcmus_shop.GraphQL.Operations
{
    public static class PromotionQueries
    {
        public const string GetPromotions = @"
            query Promotions($search: String, $page: Int, $pageSize: Int) {
                promotions(search: $search, page: $page, pageSize: $pageSize) {
                    items {
                        promotionId
                        code
                        discountPercent
                        discountAmount
                        startDate
                        endDate
                        isActive
                        createdAt
                        updatedAt
                    }
                    totalCount
                    page
                    pageSize
                }
            }";

        public const string GetPromotion = @"
            query Promotion($promotionId: Int!) {
                promotion(promotionId: $promotionId) {
                    promotionId
                    code
                    discountPercent
                    discountAmount
                    startDate
                    endDate
                    isActive
                    createdAt
                    updatedAt
                }
            }";

        public const string ValidatePromotion = @"
            query ValidatePromotion($code: String!) {
                validatePromotion(code: $code) {
                    isValid
                    message
                    promotion {
                        promotionId
                        code
                        discountPercent
                        discountAmount
                        startDate
                        endDate
                        isActive
                        createdAt
                        updatedAt
                    }
                }
            }";

        public const string CreatePromotion = @"
            mutation CreatePromotion($input: CreatePromotionInput!) {
                createPromotion(input: $input) {
                    promotionId
                    code
                    discountPercent
                    discountAmount
                    startDate
                    endDate
                    isActive
                    createdAt
                    updatedAt
                }
            }";

        public const string UpdatePromotion = @"
            mutation UpdatePromotion($promotionId: Int!, $input: UpdatePromotionInput!) {
                updatePromotion(promotionId: $promotionId, input: $input) {
                    promotionId
                    code
                    discountPercent
                    discountAmount
                    startDate
                    endDate
                    isActive
                    createdAt
                    updatedAt
                }
            }";

        public const string DeletePromotion = @"
            mutation DeletePromotion($promotionId: Int!) {
                deletePromotion(promotionId: $promotionId) {
                    promotionId
                }
            }";
    }
}
