using System;
using System.Collections.Generic;
using System.Text;

namespace hcmus_shop.GraphQL.Operations
{
    public static class CategoryQueries
    {
        public const string GetAll = @"
            query {
                categories {
                    categoryId
                    name
                    description
                    productCount
                }
            }";

        public const string GetById = @"
            query Category($categoryId: Int!) {
                category(categoryId: $categoryId) {
                    categoryId
                    name
                    description
                }
            }";

        public const string Create = @"
            mutation CreateCategory($name: String!, $description: String) {
                createCategory(name: $name, description: $description) {
                    categoryId
                    name
                    description
                }
            }";

        public const string Update = @"
            mutation UpdateCategory($categoryId: Int!, $name: String, $description: String) {
                updateCategory(categoryId: $categoryId, name: $name, description: $description) {
                    categoryId
                    name
                    description
                }
            }";

        public const string Delete = @"
            mutation DeleteCategory($categoryId: Int!) {
                deleteCategory(categoryId: $categoryId) {
                    categoryId
                }
            }";
    }
}
