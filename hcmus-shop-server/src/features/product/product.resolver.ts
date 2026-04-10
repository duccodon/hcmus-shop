import { Prisma } from "@prisma/client";
import { productService } from "./product.service";
import {
  ProductFilterDto,
  CreateProductDto,
  UpdateProductDto,
} from "./product.dto";

export const productResolver = {
  Product: {
    brand: (parent: { brandId: number }) =>
      productService.findBrand(parent.brandId),
    series: (parent: { seriesId: number | null }) =>
      parent.seriesId ? productService.findSeries(parent.seriesId) : null,
    categories: (parent: { productId: number }) =>
      productService.findCategories(parent.productId),
    images: (parent: { productId: number }) =>
      productService.findImages(parent.productId),
    importPrice: (parent: { importPrice: Prisma.Decimal }) =>
      Number(parent.importPrice),
    sellingPrice: (parent: { sellingPrice: Prisma.Decimal }) =>
      Number(parent.sellingPrice),
  },

  Query: {
    products: (_: unknown, args: ProductFilterDto) =>
      productService.findAll(args),
    product: (_: unknown, { productId }: { productId: number }) =>
      productService.findById(productId),
  },

  Mutation: {
    createProduct: (_: unknown, { input }: { input: CreateProductDto }) => {
      return productService.create(input);
    },

    updateProduct: (
      _: unknown,
      { productId, input }: { productId: number; input: UpdateProductDto }
    ) => {
      return productService.update(productId, input);
    },

    deleteProduct: (_: unknown, { productId }: { productId: number }) => {
      return productService.delete(productId);
    },
  },
};
