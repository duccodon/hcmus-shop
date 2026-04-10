import { categoryService } from "./category.service";

export const categoryResolver = {
  Category: {
    productCount: (parent: { categoryId: number }) =>
      categoryService.countProducts(parent.categoryId),
  },

  Query: {
    categories: () => categoryService.findAll(),
    category: (_: unknown, { categoryId }: { categoryId: number }) =>
      categoryService.findById(categoryId),
  },

  Mutation: {
    createCategory: (
      _: unknown,
      args: { name: string; description?: string }
    ) => {
      return categoryService.create(args);
    },

    updateCategory: (
      _: unknown,
      {
        categoryId,
        ...data
      }: { categoryId: number; name?: string; description?: string }
    ) => {
      return categoryService.update(categoryId, data);
    },

    deleteCategory: (
      _: unknown,
      { categoryId }: { categoryId: number }
    ) => {
      return categoryService.delete(categoryId);
    },
  },
};
