import { customerRepository } from "../src/features/customer/customer.repository";
import { customerService, getCustomerRank } from "../src/features/customer/customer.service";

jest.mock("../src/features/customer/customer.repository", () => ({
  customerRepository: {
    findById: jest.fn(),
    findDuplicate: jest.fn(),
    create: jest.fn(),
    update: jest.fn(),
    delete: jest.fn(),
    findAll: jest.fn(),
  },
}));

const mockCustomerRepository = customerRepository as unknown as jest.Mocked<
  Pick<typeof customerRepository, "findById" | "findDuplicate" | "create" | "update" | "delete" | "findAll">
>;

describe("CustomerService duplicate, delete, and rank rules", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it("derives Bronze, Silver, Gold, Diamond ranks at the expected thresholds", () => {
    expect(getCustomerRank(0)).toBe("Bronze");
    expect(getCustomerRank(999999)).toBe("Bronze");
    expect(getCustomerRank(1000000)).toBe("Silver");
    expect(getCustomerRank(2000000)).toBe("Gold");
    expect(getCustomerRank(3000000)).toBe("Diamond");
  });

  it("rejects duplicate phone numbers", async () => {
    mockCustomerRepository.findDuplicate.mockResolvedValue({
      customerId: "c-1",
      name: "Existing Customer",
      phone: "0901234567",
    } as any);

    await expect(
      customerService.create({
        name: "New Customer",
        phone: "0901234567",
      })
    ).rejects.toThrow("Customer phone already exists.");
  });

  it("allows deleting a customer while preserving historical orders", async () => {
    mockCustomerRepository.findById.mockResolvedValue({
      customerId: "c-2",
      name: "Walk-in Archive",
    } as any);
    mockCustomerRepository.delete.mockResolvedValue(true);

    await expect(customerService.delete("c-2")).resolves.toBe(true);
    expect(mockCustomerRepository.delete).toHaveBeenCalledWith("c-2");
  });
});
