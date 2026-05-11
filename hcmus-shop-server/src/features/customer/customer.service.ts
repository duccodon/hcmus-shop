import { CreateCustomerDto, CustomerFilterDto, UpdateCustomerDto } from "./customer.dto";
import { customerRepository } from "./customer.repository";

export class CustomerService {
  findAll(filter: CustomerFilterDto) {
    return customerRepository.findAll(filter);
  }

  findById(customerId: string) {
    return customerRepository.findById(customerId);
  }

  async create(input: CreateCustomerDto) {
    const name = input.name?.trim();
    if (!name) {
      throw new Error("Customer name is required.");
    }

    return customerRepository.create({
      name,
      phone: normalizeOptionalText(input.phone),
      email: normalizeOptionalText(input.email),
    });
  }

  async update(customerId: string, input: UpdateCustomerDto) {
    const existing = await customerRepository.findById(customerId);
    if (!existing) {
      throw new Error("Customer not found.");
    }

    const name =
      input.name !== undefined ? input.name.trim() : existing.name;
    if (!name) {
      throw new Error("Customer name is required.");
    }

    return customerRepository.update(customerId, {
      name,
      phone: normalizeOptionalText(input.phone) ?? null,
      email: normalizeOptionalText(input.email) ?? null,
    });
  }

  async delete(customerId: string) {
    const existing = await customerRepository.findById(customerId);
    if (!existing) {
      throw new Error("Customer not found.");
    }

    const orderCount = await customerRepository.countOrders(customerId);
    if (orderCount > 0) {
      throw new Error("Cannot delete a customer with existing orders.");
    }

    return customerRepository.delete(customerId);
  }
}

function normalizeOptionalText(value?: string | null) {
  const trimmed = value?.trim();
  return trimmed ? trimmed : undefined;
}

export function getCustomerRank(loyaltyPoints: number) {
  if (loyaltyPoints >= 3000000) {
    return "Diamond";
  }

  if (loyaltyPoints >= 2000000) {
    return "Gold";
  }

  if (loyaltyPoints >= 1000000) {
    return "Silver";
  }

  return "Bronze";
}

export const customerService = new CustomerService();
