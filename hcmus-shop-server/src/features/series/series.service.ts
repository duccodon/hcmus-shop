import { seriesRepository } from "./series.repository";

export class SeriesService {
  findByBrand(brandId: number) {
    return seriesRepository.findByBrand(brandId);
  }

  findById(seriesId: number) {
    return seriesRepository.findById(seriesId);
  }

  create(data: {
    brandId: number;
    name: string;
    description?: string;
    targetSegment?: string;
  }) {
    return seriesRepository.create(data);
  }

  update(
    seriesId: number,
    data: { name?: string; description?: string; targetSegment?: string }
  ) {
    return seriesRepository.update(seriesId, data);
  }

  delete(seriesId: number) {
    return seriesRepository.delete(seriesId);
  }

  findBrand(brandId: number) {
    return seriesRepository.findBrand(brandId);
  }
}

export const seriesService = new SeriesService();
