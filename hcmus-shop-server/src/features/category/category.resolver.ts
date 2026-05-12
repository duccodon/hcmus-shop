import { categoryService } from "./category.service";
import { Context, requireAdmin, requireAuth } from "../../common/context";

export const categoryResolver = {
  Category: {
    productCount: (parent: { categoryId: number }) =>
      categoryService.countProducts(parent.categoryId),
  },

  Query: {
    categories: (_: unknown, __: unknown, context: Context) => {
      requireAuth(context);
      return categoryService.findAll();
    },
    category: (_: unknown, { categoryId }: { categoryId: number }, context: Context) => {
      requireAuth(context);
      return categoryService.findById(categoryId);
    },
  },

  Mutation: {
    createCategory: (
      _: unknown,
      args: { name: string; description?: string },
      context: Context
    ) => {
      requireAdmin(context);
      return categoryService.create(args);
    },

    updateCategory: (
      _: unknown,
      {
        categoryId,
        ...data
      }: { categoryId: number; name?: string; description?: string },
      context: Context
    ) => {
      requireAdmin(context);
      return categoryService.update(categoryId, data);
    },

    deleteCategory: (
      _: unknown,
      { categoryId }: { categoryId: number },
      context: Context
    ) => {
      requireAdmin(context);
      return categoryService.delete(categoryId);
    },
  },
};
