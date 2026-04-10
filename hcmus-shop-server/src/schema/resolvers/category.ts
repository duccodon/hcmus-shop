import { PrismaClient } from "@prisma/client";
import { Context, requireAuth } from "../../middleware/auth";

export function categoryResolvers(prisma: PrismaClient) {
  return {
    Category: {
      productCount: (parent: { categoryId: number }) =>
        prisma.productCategory.count({
          where: { categoryId: parent.categoryId },
        }),
    },

    Query: {
      categories: () => prisma.category.findMany({ orderBy: { name: "asc" } }),
      category: (_: unknown, { categoryId }: { categoryId: number }) =>
        prisma.category.findUnique({ where: { categoryId } }),
    },

    Mutation: {
      createCategory: (
        _: unknown,
        args: { name: string; description?: string },
        context: Context,
      ) => {
        requireAuth(context);
        return prisma.category.create({ data: args });
      },

      updateCategory: (
        _: unknown,
        {
          categoryId,
          ...data
        }: { categoryId: number; name?: string; description?: string },
        context: Context,
      ) => {
        requireAuth(context);
        return prisma.category.update({ where: { categoryId }, data });
      },

      deleteCategory: (
        _: unknown,
        { categoryId }: { categoryId: number },
        context: Context,
      ) => {
        requireAuth(context);
        return prisma.category.delete({ where: { categoryId } });
      },
    },
  };
}
