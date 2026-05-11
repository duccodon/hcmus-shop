import { productRepository } from "../src/features/product/product.repository";
import { productService } from "../src/features/product/product.service";

jest.mock("../src/features/product/product.repository", () => ({
  productRepository: {
    findAll: jest.fn(),
  },
}));

const mockProductRepository = productRepository as unknown as jest.Mocked<
  Pick<typeof productRepository, "findAll">
>;

describe("ProductService Dev B validation", () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockProductRepository.findAll.mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 10,
    });
  });

  it("rejects minPrice greater than maxPrice", () => {
    expect(() =>
      productService.findAll({
        minPrice: 200,
        maxPrice: 100,
      })
    ).toThrow("Minimum price cannot be greater than maximum price.");

    expect(mockProductRepository.findAll).not.toHaveBeenCalled();
  });

  it("rejects unsupported sort field", () => {
    expect(() =>
      productService.findAll({
        sorts: [{ field: "importPrice", direction: "asc" }],
      })
    ).toThrow("Unsupported sort field 'importPrice'.");

    expect(mockProductRepository.findAll).not.toHaveBeenCalled();
  });

  it("rejects unsupported sort direction", () => {
    expect(() =>
      productService.findAll({
        sorts: [{ field: "name", direction: "ascending" }],
      })
    ).toThrow("Unsupported sort direction 'ascending'.");

    expect(mockProductRepository.findAll).not.toHaveBeenCalled();
  });

  it("accepts valid multi-sort input", async () => {
    const filter = {
      sorts: [
        { field: "name", direction: "asc" },
        { field: "sellingPrice", direction: "desc" },
        { field: "stockQuantity", direction: "asc" },
      ],
      page: 1,
      pageSize: 10,
    };

    await expect(productService.findAll(filter)).resolves.toEqual({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 10,
    });

    expect(mockProductRepository.findAll).toHaveBeenCalledWith(filter);
  });
});
