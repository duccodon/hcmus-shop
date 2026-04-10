import { brandService } from "./brand.service";

export const brandResolver = {
  Brand: {
    series: (parent: { brandId: number }) =>
      brandService.findSeries(parent.brandId),
    productCount: (parent: { brandId: number }) =>
      brandService.countProducts(parent.brandId),
  },

  Query: {
    brands: () => brandService.findAll(),
    brand: (_: unknown, { brandId }: { brandId: number }) =>
      brandService.findById(brandId),
  },

  Mutation: {
    createBrand: (
      _: unknown,
      args: { name: string; description?: string; logoUrl?: string }
    ) => {
      return brandService.create(args);
    },

    updateBrand: (
      _: unknown,
      {
        brandId,
        ...data
      }: {
        brandId: number;
        name?: string;
        description?: string;
        logoUrl?: string;
      }
    ) => {
      return brandService.update(brandId, data);
    },

    deleteBrand: (_: unknown, { brandId }: { brandId: number }) => {
      return brandService.delete(brandId);
    },
  },
};
