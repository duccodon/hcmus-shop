import { Context, requireAdmin, requireAuth } from "../../common/context";
import { brandService } from "./brand.service";

export const brandResolver = {
  Brand: {
    series: (parent: { brandId: number }) =>
      brandService.findSeries(parent.brandId),
    productCount: (parent: { brandId: number }) =>
      brandService.countProducts(parent.brandId),
  },

  Query: {
    brands: (_: unknown, __: unknown, context: Context) => {
      requireAuth(context);
      return brandService.findAll();
    },
    brand: (_: unknown, { brandId }: { brandId: number }, context: Context) => {
      requireAuth(context);
      return brandService.findById(brandId);
    },
  },

  Mutation: {
    createBrand: (
      _: unknown,
      args: { name: string; description?: string; logoUrl?: string },
      context: Context
    ) => {
      requireAdmin(context);
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
      },
      context: Context
    ) => {
      requireAdmin(context);
      return brandService.update(brandId, data);
    },

    deleteBrand: (_: unknown, { brandId }: { brandId: number }, context: Context) => {
      requireAdmin(context);
      return brandService.delete(brandId);
    },
  },
};
