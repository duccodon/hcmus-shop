namespace hcmus_shop.GraphQL.Operations
{
	public static class ProductQueries
	{
		public const string GetProducts = @"
            query Products(
                $search: String
                $categoryId: Int
                $brandId: Int
                $minPrice: Float
                $maxPrice: Float
                $sortBy: String
                $sortOrder: String
                $page: Int
                $pageSize: Int
            ) {
                products(
                    search: $search
                    categoryId: $categoryId
                    brandId: $brandId
                    minPrice: $minPrice
                    maxPrice: $maxPrice
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