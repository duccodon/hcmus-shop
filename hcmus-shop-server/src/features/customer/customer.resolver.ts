import {
  CreateCustomerDto,
  CustomerFilterDto,
  UpdateCustomerDto,
} from "./customer.dto";
import { customerService, getCustomerRank } from "./customer.service";
import { Context, requireAdmin } from "../../common/context";

export const customerResolver = {
  Customer: {
    rank: (parent: { loyaltyPoints: number }) =>
      getCustomerRank(parent.loyaltyPoints),
  },
  Query: {
    customers: (_: unknown, args: CustomerFilterDto) =>
      customerService.findAll(args),
    customer: (
      _: unknown,
      { customerId }: { customerId: string },
      context: Context
    ) => {
      requireAdmin(context);
      return customerService.findById(customerId);
    },
  },

  Mutation: {
    createCustomer: (_: unknown, { input }: { input: CreateCustomerDto }) =>
      customerService.create(input),
    updateCustomer: (
      _: unknown,
      { customerId, input }: { customerId: string; input: UpdateCustomerDto },
      context: Context
    ) => {
      requireAdmin(context);
      return customerService.update(customerId, input);
    },
    deleteCustomer: (
      _: unknown,
      { customerId }: { customerId: string },
      context: Context
    ) => {
      requireAdmin(context);
      return customerService.delete(customerId);
    },
  },
};
