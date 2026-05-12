import { Context, requireAdmin, requireAuth } from "../../common/context";
import { seriesService } from "./series.service";

export const seriesResolver = {
  Series: {
    brand: (parent: { brandId: number }) =>
      seriesService.findBrand(parent.brandId),
  },

  Query: {
    seriesByBrand: (_: unknown, { brandId }: { brandId: number }, context: Context) => {
      requireAuth(context);
      return seriesService.findByBrand(brandId);
    },
    series: (_: unknown, { seriesId }: { seriesId: number }, context: Context) => {
      requireAuth(context);
      return seriesService.findById(seriesId);
    },
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
      requireAdmin(context);
      return seriesService.create(args);
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
      requireAdmin(context);
      return seriesService.update(seriesId, data);
    },

    deleteSeries: (
      _: unknown,
      { seriesId }: { seriesId: number },
      context: Context
    ) => {
      requireAdmin(context);
      return seriesService.delete(seriesId);
    },
  },
};
