import { categoryRepository } from "./category.repository";

export class CategoryService {
  findAll() {
    return categoryRepository.findAll();
  }

  findById(categoryId: number) {
    return categoryRepository.findById(categoryId);
  }

  create(data: { name: string; description?: string }) {
    return categoryRepository.create(data);
  }

  update(categoryId: number, data: { name?: string; description?: string }) {
    return categoryRepository.update(categoryId, data);
  }

  delete(categoryId: number) {
    return categoryRepository.delete(categoryId);
  }

  countProducts(categoryId: number) {
    return categoryRepository.countProducts(categoryId);
  }
}

export const categoryService = new CategoryService();
