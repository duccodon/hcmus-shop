import { seriesService } from "./series.service";

export const seriesResolver = {
  Series: {
    brand: (parent: { brandId: number }) =>
      seriesService.findBrand(parent.brandId),
  },

  Query: {
    seriesByBrand: (_: unknown, { brandId }: { brandId: number }) =>
      seriesService.findByBrand(brandId),
    series: (_: unknown, { seriesId }: { seriesId: number }) =>
      seriesService.findById(seriesId),
  },

  Mutation: {
    createSeries: (
      _: unknown,
      args: {
        brandId: number;
        name: string;
        description?: string;
        targetSegment?: string;
      }
    ) => {
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
      }
    ) => {
      return seriesService.update(seriesId, data);
    },

    deleteSeries: (
      _: unknown,
      { seriesId }: { seriesId: number }
    ) => {
      return seriesService.delete(seriesId);
    },
  },
};
