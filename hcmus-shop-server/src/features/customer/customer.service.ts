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

    await this.ensureNoDuplicate(name, input.phone);

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

    await this.ensureNoDuplicate(name, input.phone, customerId);

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

    return customerRepository.delete(customerId);
  }

  private async ensureNoDuplicate(name: string, phone?: string | null, excludeCustomerId?: string) {
    const duplicate = await customerRepository.findDuplicate(name, phone, excludeCustomerId);
    if (!duplicate) {
      return;
    }

    if (phone?.trim() && duplicate.phone?.trim() === phone.trim()) {
      throw new Error("Customer phone already exists.");
    }

    throw new Error("Customer name already exists.");
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
