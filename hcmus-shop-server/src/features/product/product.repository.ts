import { Prisma } from "@prisma/client";
import { prisma } from "../../prisma";
import { ProductFilterDto } from "./product.dto";

export class ProductRepository {
  async findAll(filter: ProductFilterDto) {
    const page = filter.page ?? 1;
    const pageSize = filter.pageSize ?? 10;
    const skip = (page - 1) * pageSize;

    const where: Prisma.ProductWhereInput = { isActive: true };

    if (filter.search) {
      where.OR = [
        { name: { contains: filter.search, mode: "insensitive" } },
        { sku: { contains: filter.search, mode: "insensitive" } },
      ];
    }

    if (filter.name) {
      where.name = { contains: filter.name, mode: "insensitive" };
    }

    if (filter.sku) {
      where.sku = { contains: filter.sku, mode: "insensitive" };
    }

    if (filter.brandId) {
      where.brandId = filter.brandId;
    }

    const brandIds = [
      ...(filter.brandId ? [filter.brandId] : []),
      ...(filter.brandIds ?? []),
    ].filter((value, index, values) => values.indexOf(value) === index);

    if (brandIds.length > 0) {
      where.brandId = { in: brandIds };
    }

    if (filter.categoryId) {
      where.categories = { some: { categoryId: filter.categoryId } };
    }

    const categoryIds = [
      ...(filter.categoryId ? [filter.categoryId] : []),
      ...(filter.categoryIds ?? []),
    ].filter((value, index, values) => values.indexOf(value) === index);

    if (categoryIds.length > 0) {
      where.categories = {
        some: {
          categoryId: { in: categoryIds },
        },
      };
    }

    if (filter.minPrice !== undefined || filter.maxPrice !== undefined) {
      where.sellingPrice = {};
      if (filter.minPrice !== undefined)
        where.sellingPrice.gte = filter.minPrice;
      if (filter.maxPrice !== undefined)
        where.sellingPrice.lte = filter.maxPrice;
    }

    if (filter.inStockOnly) {
      where.stockQuantity = { gt: 0 };
    }

    const sorts = filter.sorts?.length
      ? filter.sorts
      : [{ field: filter.sortBy ?? "createdAt", direction: filter.sortOrder === "asc" ? "asc" : "desc" }];

    const orderBy = sorts.map((sort) => ({
      [sort.field]: sort.direction,
    })) as Prisma.ProductOrderByWithRelationInput[];

    const [items, totalCount] = await Promise.all([
      prisma.product.findMany({ where, orderBy, skip, take: pageSize }),
      prisma.product.count({ where }),
    ]);

    return { items, totalCount, page, pageSize };
  }

  findById(productId: number) {
    return prisma.product.findUnique({ where: { productId } });
  }

  create(data: Prisma.ProductCreateInput) {
    return prisma.product.create({ data });
  }

  update(productId: number, data: Prisma.ProductUpdateInput) {
    return prisma.product.update({ where: { productId }, data });
  }

  softDelete(productId: number) {
    return prisma.product.update({
      where: { productId },
      data: { isActive: false },
    });
  }

  // Relations
  findBrand(brandId: number) {
    return prisma.brand.findUnique({ where: { brandId } });
  }

  findSeries(seriesId: number) {
    return prisma.series.findUnique({ where: { seriesId } });
  }

  async findCategories(productId: number) {
    const pcs = await prisma.productCategory.findMany({
      where: { productId },
      include: { category: true },
    });
    return pcs.map((pc) => pc.category);
  }

  findImages(productId: number) {
    return prisma.productImage.findMany({
      where: { productId },
      orderBy: { displayOrder: "asc" },
    });
  }

  // Category & image management
  async replaceCategories(productId: number, categoryIds: number[]) {
    await prisma.productCategory.deleteMany({ where: { productId } });
    if (categoryIds.length > 0) {
      await prisma.productCategory.createMany({
        data: categoryIds.map((categoryId) => ({ productId, categoryId })),
      });
    }
  }

  async replaceImages(productId: number, imageUrls: string[]) {
    await prisma.productImage.deleteMany({ where: { productId } });
    if (imageUrls.length > 0) {
      await prisma.productImage.createMany({
        data: imageUrls.map((imageUrl, i) => ({
          productId,
          imageUrl,
          displayOrder: i,
        })),
      });
    }
  }
}

export const productRepository = new ProductRepository();
