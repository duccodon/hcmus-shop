using System;
using System.Collections.Generic;
using System.Text;

namespace hcmus_shop.GraphQL.Operations
{
    public static class BrandQueries
    {
        public const string GetAll = @"
            query {
                brands {
                    brandId
                    name
                    description
                    productCount
                }
            }";

        public const string GetById = @"
            query Brand($brandId: Int!) {
                brand(brandId: $brandId) {
                    brandId
                    name
                    description
                    series { seriesId name targetSegment }
                }
            }";

        public const string Create = @"
            mutation CreateBrand($name: String!, $description: String) {
                createBrand(name: $name, description: $description) {
                    brandId
                    name
                    description
                }
            }";

        public const string Update = @"
            mutation UpdateBrand($brandId: Int!, $name: String, $description: String) {
                updateBrand(brandId: $brandId, name: $name, description: $description) {
                    brandId
                    name
                    description
                }
            }";

        public const string Delete = @"
            mutation DeleteBrand($brandId: Int!) {
                deleteBrand(brandId: $brandId) {
                    brandId
                }
            }";
    }
}
