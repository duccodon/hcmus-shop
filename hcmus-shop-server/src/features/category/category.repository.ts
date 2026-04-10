import { prisma } from "../../prisma";

export class CategoryRepository {
  findAll() {
    return prisma.category.findMany({ orderBy: { name: "asc" } });
  }

  findById(categoryId: number) {
    return prisma.category.findUnique({ where: { categoryId } });
  }

  create(data: { name: string; description?: string }) {
    return prisma.category.create({ data });
  }

  update(categoryId: number, data: { name?: string; description?: string }) {
    return prisma.category.update({ where: { categoryId }, data });
  }

  delete(categoryId: number) {
    return prisma.category.delete({ where: { categoryId } });
  }

  countProducts(categoryId: number) {
    return prisma.productCategory.count({ where: { categoryId } });
  }
}

export const categoryRepository = new CategoryRepository();
