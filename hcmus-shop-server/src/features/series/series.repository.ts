import { prisma } from "../../prisma";

export class SeriesRepository {
  findByBrand(brandId: number) {
    return prisma.series.findMany({
      where: { brandId },
      orderBy: { name: "asc" },
    });
  }

  findById(seriesId: number) {
    return prisma.series.findUnique({ where: { seriesId } });
  }

  create(data: {
    brandId: number;
    name: string;
    description?: string;
    targetSegment?: string;
  }) {
    return prisma.series.create({ data });
  }

  update(
    seriesId: number,
    data: { name?: string; description?: string; targetSegment?: string }
  ) {
    return prisma.series.update({ where: { seriesId }, data });
  }

  delete(seriesId: number) {
    return prisma.series.delete({ where: { seriesId } });
  }

  findBrand(brandId: number) {
    return prisma.brand.findUnique({ where: { brandId } });
  }
}

export const seriesRepository = new SeriesRepository();
