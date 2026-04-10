import { brandRepository } from "./brand.repository";

export class BrandService {
  findAll() {
    return brandRepository.findAll();
  }

  findById(brandId: number) {
    return brandRepository.findById(brandId);
  }

  create(data: { name: string; description?: string; logoUrl?: string }) {
    return brandRepository.create(data);
  }

  update(
    brandId: number,
    data: { name?: string; description?: string; logoUrl?: string }
  ) {
    return brandRepository.update(brandId, data);
  }

  delete(brandId: number) {
    return brandRepository.delete(brandId);
  }

  countProducts(brandId: number) {
    return brandRepository.countProducts(brandId);
  }

  findSeries(brandId: number) {
    return brandRepository.findSeries(brandId);
  }
}

export const brandService = new BrandService();
