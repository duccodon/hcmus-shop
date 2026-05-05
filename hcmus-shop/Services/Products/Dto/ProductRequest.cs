using System;
using System.Collections.Generic;
using System.Text;

namespace hcmus_shop.Services.Products.Dto
{
    public class GetProductsRequest
    {
        public string? Search { get; set; }
        public int? CategoryId { get; set; }
        public int? BrandId { get; set; }
        public double? MinPrice { get; set; }
        public double? MaxPrice { get; set; }
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class GetProductByIdRequest
    {
        public int ProductId { get; set; }
    }

    public class DeleteProductRequest
    {
        public int ProductId { get; set; }
    }
}
