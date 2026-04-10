import { PrismaClient } from "@prisma/client";
import { Context, requireAuth } from "../../middleware/auth";

export function seriesResolvers(prisma: PrismaClient) {
  return {
    Series: {
      brand: (parent: { brandId: number }) =>
        prisma.brand.findUnique({ where: { brandId: parent.brandId } }),
    },

    Query: {
      seriesByBrand: (_: unknown, { brandId }: { brandId: number }) =>
        prisma.series.findMany({
          where: { brandId },
          orderBy: { name: "asc" },
        }),
      series: (_: unknown, { seriesId }: { seriesId: number }) =>
        prisma.series.findUnique({ where: { seriesId } }),
    },

    Mutation: {
      createSeries: (
        _: unknown,
        args: {
          brandId: number;
          name: string;
          description?: string;
          targetSegment?: string;
        },
        context: Context
      ) => {
        requireAuth(context);
        return prisma.series.create({ data: args });
      },

      updateSeries: (
        _: unknown,
        {
          seriesId,
          ...data
        }: {
          seriesId: number;
          name?: string;
          description?: string;
          targetSegment?: string;
        },
        context: Context
      ) => {
        requireAuth(context);
        return prisma.series.update({ where: { seriesId }, data });
      },

      deleteSeries: (
        _: unknown,
        { seriesId }: { seriesId: number },
        context: Context
      ) => {
        requireAuth(context);
        return prisma.series.delete({ where: { seriesId } });
      },
    },
  };
}
