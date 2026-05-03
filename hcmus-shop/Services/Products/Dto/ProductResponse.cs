using hcmus_shop.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace hcmus_shop.Services.Products.Dto
{
    public class ProductsResponse
    {
        public ProductPageDto Products { get; set; } = new();
    }

    public class ProductResponse
    {
        public ProductDto? Product { get; set; }
    }

    public class CreateProductResponse
    {
        public ProductDto CreateProduct { get; set; } = new();
    }

    public class UpdateProductResponse
    {
        public ProductDto UpdateProduct { get; set; } = new();
    }

    public class DeleteProductResponse
    {
        public ProductDto DeleteProduct { get; set; } = new();
    }
}
