import { PrismaClient, Prisma } from "@prisma/client";
import { Context, requireAuth } from "../../middleware/auth";

interface ProductsArgs {
  search?: string;
  categoryId?: number;
  brandId?: number;
  minPrice?: number;
  maxPrice?: number;
  sortBy?: string;
  sortOrder?: string;
  page?: number;
  pageSize?: number;
}

interface CreateProductInput {
  sku: string;
  name: string;
  brandId: number;
  seriesId?: number;
  importPrice: number;
  sellingPrice: number;
  stockQuantity?: number;
  specifications?: unknown;
  description?: string;
  warrantyMonths?: number;
  categoryIds?: number[];
  imageUrls?: string[];
}

interface UpdateProductInput {
  sku?: string;
  name?: string;
  brandId?: number;
  seriesId?: number;
  importPrice?: number;
  sellingPrice?: number;
  stockQuantity?: number;
  specifications?: unknown;
  description?: string;
  warrantyMonths?: number;
  isActive?: boolean;
  categoryIds?: number[];
  imageUrls?: string[];
}

export function productResolvers(prisma: PrismaClient) {
  return {
    Product: {
      brand: (parent: { brandId: number }) =>
        prisma.brand.findUnique({ where: { brandId: parent.brandId } }),
      series: (parent: { seriesId: number | null }) =>
        parent.seriesId
          ? prisma.series.findUnique({ where: { seriesId: parent.seriesId } })
          : null,
      categories: async (parent: { productId: number }) => {
        const pcs = await prisma.productCategory.findMany({
          where: { productId: parent.productId },
          include: { category: true },
        });
        return pcs.map((pc) => pc.category);
      },
      images: (parent: { productId: number }) =>
        prisma.productImage.findMany({
          where: { productId: parent.productId },
          orderBy: { displayOrder: "asc" },
        }),
      importPrice: (parent: { importPrice: Prisma.Decimal }) =>
        Number(parent.importPrice),
      sellingPrice: (parent: { sellingPrice: Prisma.Decimal }) =>
        Number(parent.sellingPrice),
    },

    Query: {
      products: async (_: unknown, args: ProductsArgs) => {
        const page = args.page ?? 1;
        const pageSize = args.pageSize ?? 10;
        const skip = (page - 1) * pageSize;

        const where: Prisma.ProductWhereInput = { isActive: true };

        if (args.search) {
          where.name = { contains: args.search, mode: "insensitive" };
        }
        if (args.brandId) {
          where.brandId = args.brandId;
        }
        if (args.categoryId) {
          where.categories = { some: { categoryId: args.categoryId } };
        }
        if (args.minPrice !== undefined || args.maxPrice !== undefined) {
          where.sellingPrice = {};
          if (args.minPrice !== undefined)
            where.sellingPrice.gte = args.minPrice;
          if (args.maxPrice !== undefined)
            where.sellingPrice.lte = args.maxPrice;
        }

        const orderBy: Prisma.ProductOrderByWithRelationInput = {};
        const sortField = args.sortBy ?? "createdAt";
        const sortDir = args.sortOrder === "asc" ? "asc" : "desc";

        if (
          sortField === "name" ||
          sortField === "sellingPrice" ||
          sortField === "stockQuantity" ||
          sortField === "createdAt"
        ) {
          (orderBy as Record<string, string>)[sortField] = sortDir;
        }

        const [items, totalCount] = await Promise.all([
          prisma.product.findMany({
            where,
            orderBy,
            skip,
            take: pageSize,
          }),
          prisma.product.count({ where }),
        ]);

        return { items, totalCount, page, pageSize };
      },

      product: (_: unknown, { productId }: { productId: number }) =>
        prisma.product.findUnique({ where: { productId } }),
    },

    Mutation: {
      createProduct: async (
        _: unknown,
        { input }: { input: CreateProductInput },
        context: Context
      ) => {
        requireAuth(context);

        const { categoryIds, imageUrls, ...productData } = input;

        const product = await prisma.product.create({
          data: {
            ...productData,
            specifications: productData.specifications as Prisma.InputJsonValue,
            categories: categoryIds?.length
              ? {
                  create: categoryIds.map((categoryId) => ({ categoryId })),
                }
              : undefined,
            images: imageUrls?.length
              ? {
                  create: imageUrls.map((imageUrl, i) => ({
                    imageUrl,
                    displayOrder: i,
                  })),
                }
              : undefined,
          },
        });

        return product;
      },

      updateProduct: async (
        _: unknown,
        { productId, input }: { productId: number; input: UpdateProductInput },
        context: Context
      ) => {
        requireAuth(context);

        const { categoryIds, imageUrls, ...productData } = input;

        // Update categories if provided
        if (categoryIds !== undefined) {
          await prisma.productCategory.deleteMany({ where: { productId } });
          if (categoryIds.length > 0) {
            await prisma.productCategory.createMany({
              data: categoryIds.map((categoryId) => ({
                productId,
                categoryId,
              })),
            });
          }
        }

        // Update images if provided
        if (imageUrls !== undefined) {
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

        return prisma.product.update({
          where: { productId },
          data: {
            ...productData,
            specifications: productData.specifications as Prisma.InputJsonValue,
          },
        });
      },

      deleteProduct: (
        _: unknown,
        { productId }: { productId: number },
        context: Context
      ) => {
        requireAuth(context);
        return prisma.product.update({
          where: { productId },
          data: { isActive: false },
        });
      },
    },
  };
}
