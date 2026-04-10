import { prisma } from "../../prisma";

export class BrandRepository {
  findAll() {
    return prisma.brand.findMany({ orderBy: { name: "asc" } });
  }

  findById(brandId: number) {
    return prisma.brand.findUnique({ where: { brandId } });
  }

  create(data: { name: string; description?: string; logoUrl?: string }) {
    return prisma.brand.create({ data });
  }

  update(
    brandId: number,
    data: { name?: string; description?: string; logoUrl?: string }
  ) {
    return prisma.brand.update({ where: { brandId }, data });
  }

  delete(brandId: number) {
    return prisma.brand.delete({ where: { brandId } });
  }

  countProducts(brandId: number) {
    return prisma.product.count({ where: { brandId } });
  }

  findSeries(brandId: number) {
    return prisma.series.findMany({ where: { brandId } });
  }
}

export const brandRepository = new BrandRepository();
