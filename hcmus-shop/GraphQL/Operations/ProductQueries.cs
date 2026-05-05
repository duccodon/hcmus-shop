namespace hcmus_shop.GraphQL.Operations
{
	public static class ProductQueries
	{
		public const string GetProducts = @"
            query Products(
                $search: String
                $name: String
                $sku: String
                $categoryId: Int
                $brandId: Int
                $categoryIds: [Int!]
                $brandIds: [Int!]
                $minPrice: Float
                $maxPrice: Float
                $inStockOnly: Boolean
                $sortBy: String
                $sortOrder: String
                $page: Int
                $pageSize: Int
            ) {
                products(
                    search: $search
                    name: $name
                    sku: $sku
                    categoryId: $categoryId
                    brandId: $brandId
                    categoryIds: $categoryIds
                    brandIds: $brandIds
                    minPrice: $minPrice
                    maxPrice: $maxPrice
                    inStockOnly: $inStockOnly
                    sortBy: $sortBy
                    sortOrder: $sortOrder
                    page: $page
                    pageSize: $pageSize
                ) {
                    items {
                        productId
                        sku
                        name
                        sellingPrice
                        importPrice
                        stockQuantity
                        isActive
                        brand { brandId name }
                        categories { categoryId name }
                        images { imageId imageUrl displayOrder }
                    }
                    totalCount
                    page
                    pageSize
                }
            }";

		public const string GetById = @"
            query Product($productId: Int!) {
                product(productId: $productId) {
                    productId
                    sku
                    name
                    description
                    importPrice
                    sellingPrice
                    stockQuantity
                    warrantyMonths
                    isActive
                    brand { brandId name }
                    series { seriesId name }
                    categories { categoryId name }
                    images { imageId imageUrl displayOrder }
                }
            }";

		public const string Create = @"
            mutation CreateProduct($input: CreateProductInput!) {
                createProduct(input: $input) {
                    productId
                    sku
                    name
                    sellingPrice
                    stockQuantity
                }
            }";

		public const string Update = @"
            mutation UpdateProduct($productId: Int!, $input: UpdateProductInput!) {
                updateProduct(productId: $productId, input: $input) {
                    productId
                    sku
                    name
                    sellingPrice
                    stockQuantity
                }
            }";

		public const string Delete = @"
            mutation DeleteProduct($productId: Int!) {
                deleteProduct(productId: $productId) {
                    productId
                }
            }";
	}
}
