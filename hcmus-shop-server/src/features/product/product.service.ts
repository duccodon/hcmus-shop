import { Prisma } from "@prisma/client";
import { productRepository } from "./product.repository";
import {
  ProductFilterDto,
  CreateProductDto,
  UpdateProductDto,
  ProductSortDto,
} from "./product.dto";

export class ProductService {
  findAll(filter: ProductFilterDto) {
    if (
      filter.minPrice !== undefined &&
      filter.maxPrice !== undefined &&
      filter.minPrice > filter.maxPrice
    ) {
      throw new Error("Minimum price cannot be greater than maximum price.");
    }

    if (
      filter.minStock !== undefined &&
      filter.maxStock !== undefined &&
      filter.minStock > filter.maxStock
    ) {
      throw new Error("Minimum stock cannot be greater than maximum stock.");
    }

    if (filter.sorts?.length) {
      for (const sort of filter.sorts) {
        this.validateSort(sort);
      }
    } else if (filter.sortBy) {
      this.validateSort({
        field: filter.sortBy,
        direction: filter.sortOrder ?? "desc",
      });
    }

    return productRepository.findAll(filter);
  }

  private validateSort(sort: ProductSortDto) {
    const allowedFields = new Set(["name", "sellingPrice", "stockQuantity", "createdAt"]);
    const allowedDirections = new Set(["asc", "desc"]);

    if (!allowedFields.has(sort.field)) {
      throw new Error(`Unsupported sort field '${sort.field}'.`);
    }

    if (!allowedDirections.has(sort.direction)) {
      throw new Error(`Unsupported sort direction '${sort.direction}'.`);
    }
  }

  findById(productId: number) {
    return productRepository.findById(productId);
  }

  async create(dto: CreateProductDto) {
    const { categoryIds, imageUrls, brandId, seriesId, ...data } = dto;
    console.log("[ProductService] createProduct input", {
      sku: dto.sku,
      name: dto.name,
      brandId,
      seriesId: seriesId ?? null,
      categoryCount: categoryIds?.length ?? 0,
      imageCount: imageUrls?.length ?? 0,
    });

    try {
      const product = await productRepository.create({
        ...data,
        specifications: data.specifications as Prisma.InputJsonValue,
        brand: { connect: { brandId } },
        series: seriesId
          ? { connect: { seriesId } }
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

      await this.syncAvailableInstances(
        product.productId,
        product.sku,
        Math.max(0, dto.stockQuantity ?? 0)
      );

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
    const { categoryIds, imageUrls, brandId, seriesId, ...data } = dto;
    console.log("[ProductService] updateProduct input", {
      productId,
      sku: dto.sku,
      name: dto.name,
      brandId,
      seriesId: seriesId ?? null,
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
        brand:
          brandId !== undefined
            ? { connect: { brandId } }
            : undefined,
        series:
          seriesId !== undefined
            ? seriesId === null
              ? { disconnect: true }
              : { connect: { seriesId } }
            : undefined,
      });

      if (dto.stockQuantity !== undefined) {
        await this.syncAvailableInstances(
          product.productId,
          product.sku,
          Math.max(0, dto.stockQuantity)
        );
      }

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

  findInstances(productId: number) {
    return productRepository.findInstances(productId);
  }

  private async syncAvailableInstances(
    productId: number,
    sku: string,
    desiredStock: number
  ) {
    const instances = await productRepository.findInstances(productId);
    const availableInstances = instances
      .filter((instance) => instance.status === "Available")
      .sort((left, right) => left.serialNumber.localeCompare(right.serialNumber));

    if (availableInstances.length < desiredStock) {
      const missingCount = desiredStock - availableInstances.length;
      const serialNumbers = buildNextSerialNumbers(sku, instances, missingCount);
      await productRepository.createInstances(productId, serialNumbers);
    } else if (availableInstances.length > desiredStock) {
      const removableInstances = availableInstances
        .slice(desiredStock)
        .map((instance) => instance.instanceId);
      await productRepository.deleteInstances(removableInstances);
    }

    await productRepository.update(productId, {
      stockQuantity: desiredStock,
    });
  }
}

function buildNextSerialNumbers(
  sku: string,
  existingInstances: { serialNumber: string }[],
  count: number
) {
  const prefix = `${sku}-SN-`;
  const existingNumbers = existingInstances
    .map((instance) => instance.serialNumber)
    .filter((serial) => serial.startsWith(prefix))
    .map((serial) => Number.parseInt(serial.slice(prefix.length), 10))
    .filter((value) => Number.isFinite(value));

  let nextNumber = existingNumbers.length > 0 ? Math.max(...existingNumbers) + 1 : 1;
  var serials: string[] = [];
  for (let i = 0; i < count; i += 1) {
    serials.push(`${prefix}${String(nextNumber).padStart(3, "0")}`);
    nextNumber += 1;
  }

  return serials;
}

export const productService = new ProductService();
