import { PrismaClient } from "@prisma/client";
import { Context, requireAuth } from "../../middleware/auth";

export function brandResolvers(prisma: PrismaClient) {
  return {
    Brand: {
      series: (parent: { brandId: number }) =>
        prisma.series.findMany({ where: { brandId: parent.brandId } }),
      productCount: (parent: { brandId: number }) =>
        prisma.product.count({ where: { brandId: parent.brandId } }),
    },

    Query: {
      brands: () => prisma.brand.findMany({ orderBy: { name: "asc" } }),
      brand: (_: unknown, { brandId }: { brandId: number }) =>
        prisma.brand.findUnique({ where: { brandId } }),
    },

    Mutation: {
      createBrand: (
        _: unknown,
        args: { name: string; description?: string; logoUrl?: string },
        context: Context
      ) => {
        requireAuth(context);
        return prisma.brand.create({ data: args });
      },

      updateBrand: (
        _: unknown,
        {
          brandId,
          ...data
        }: { brandId: number; name?: string; description?: string; logoUrl?: string },
        context: Context
      ) => {
        requireAuth(context);
        return prisma.brand.update({ where: { brandId }, data });
      },

      deleteBrand: (_: unknown, { brandId }: { brandId: number }, context: Context) => {
        requireAuth(context);
        return prisma.brand.delete({ where: { brandId } });
      },
    },
  };
}
