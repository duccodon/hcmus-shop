import { Prisma } from "@prisma/client";
import { productRepository } from "./product.repository";
import {
  ProductFilterDto,
  CreateProductDto,
  UpdateProductDto,
} from "./product.dto";

export class ProductService {
  findAll(filter: ProductFilterDto) {
    return productRepository.findAll(filter);
  }

  findById(productId: number) {
    return productRepository.findById(productId);
  }

  async create(dto: CreateProductDto) {
    const { categoryIds, imageUrls, ...data } = dto;
    console.log("[ProductService] createProduct input", {
      sku: dto.sku,
      name: dto.name,
      brandId: dto.brandId,
      seriesId: dto.seriesId ?? null,
      categoryCount: categoryIds?.length ?? 0,
      imageCount: imageUrls?.length ?? 0,
    });

    try {
      const product = await productRepository.create({
        ...data,
        specifications: data.specifications as Prisma.InputJsonValue,
        brand: { connect: { brandId: data.brandId } },
        series: data.seriesId
          ? { connect: { seriesId: data.seriesId } }
          : undefined,
        categories: categoryIds?.length
          ? { create: categoryIds.map((categoryId) => ({ categoryId })) }
          : undefined,
        images: imageUrls?.length
          ? {
              create: imageUrls.map((imageUrl, i) => ({
                imageUrl,
                displayOrder: i,
              })),
            }
          : undefined,
      });

      console.log("[ProductService] createProduct success", {
        productId: product.productId,
        sku: product.sku,
      });
      return product;
    } catch (error) {
      console.error("[ProductService] createProduct failed", error);
      throw error;
    }
  }

  async update(productId: number, dto: UpdateProductDto) {
    const { categoryIds, imageUrls, ...data } = dto;
    console.log("[ProductService] updateProduct input", {
      productId,
      sku: dto.sku,
      name: dto.name,
      brandId: dto.brandId,
      seriesId: dto.seriesId ?? null,
      categoryCount: categoryIds?.length,
      imageCount: imageUrls?.length,
    });

    try {
      if (categoryIds !== undefined) {
        await productRepository.replaceCategories(productId, categoryIds);
      }

      if (imageUrls !== undefined) {
        await productRepository.replaceImages(productId, imageUrls);
      }

      const product = await productRepository.update(productId, {
        ...data,
        specifications: data.specifications as Prisma.InputJsonValue,
      });

      console.log("[ProductService] updateProduct success", {
        productId: product.productId,
        sku: product.sku,
      });
      return product;
    } catch (error) {
      console.error("[ProductService] updateProduct failed", { productId, error });
      throw error;
    }
  }

  async delete(productId: number) {
    console.log("[ProductService] deleteProduct input", { productId });

    try {
      const product = await productRepository.softDelete(productId);
      console.log("[ProductService] deleteProduct success", {
        productId: product.productId,
        sku: product.sku,
      });
      return product;
    } catch (error) {
      console.error("[ProductService] deleteProduct failed", { productId, error });
      throw error;
    }
  }

  // Field resolvers
  findBrand(brandId: number) {
    return productRepository.findBrand(brandId);
  }

  findSeries(seriesId: number) {
    return productRepository.findSeries(seriesId);
  }

  findCategories(productId: number) {
    return productRepository.findCategories(productId);
  }

  findImages(productId: number) {
    return productRepository.findImages(productId);
  }
}

export const productService = new ProductService();
