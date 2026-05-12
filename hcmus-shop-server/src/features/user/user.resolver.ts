import { Context, requireAdmin } from "../../common/context";
import { userService } from "./user.service";
import { CreateUserInput, UpdateUserInput, UserFilterInput } from "./user.dto";

export const userResolver = {
  Query: {
    users: (_: unknown, args: UserFilterInput, context: Context) => {
      requireAdmin(context);
      return userService.getAll(args);
    },
    user: (_: unknown, args: { userId: string }, context: Context) => {
      requireAdmin(context);
      return userService.getById(args.userId);
    },
  },

  Mutation: {
    createUser: (_: unknown, args: { input: CreateUserInput }, context: Context) => {
      requireAdmin(context);
      return userService.create(args.input);
    },
    updateUser: (_: unknown, args: { userId: string; input: UpdateUserInput }, context: Context) => {
      requireAdmin(context);
      return userService.update(args.userId, args.input);
    },
    deleteUser: (_: unknown, args: { userId: string }, context: Context) => {
      requireAdmin(context);
      return userService.delete(args.userId);
    },
  },
};
