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

    return product;
  }

  async update(productId: number, dto: UpdateProductDto) {
    const { categoryIds, imageUrls, ...data } = dto;

    if (categoryIds !== undefined) {
      await productRepository.replaceCategories(productId, categoryIds);
    }

    if (imageUrls !== undefined) {
      await productRepository.replaceImages(productId, imageUrls);
    }

    return productRepository.update(productId, {
      ...data,
      specifications: data.specifications as Prisma.InputJsonValue,
    });
  }

  delete(productId: number) {
    return productRepository.softDelete(productId);
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
