import { Context } from "../../common/context";
import { authService } from "./auth.service";

export const authResolver = {
  Query: {
    me: (_: unknown, __: unknown, context: Context) => {
      if (!context.user) return null;
      return authService.getMe(context.user.userId);
    },
  },

  Mutation: {
    login: (_: unknown, args: { username: string; password: string }) => {
      return authService.login(args.username, args.password);
    },
  },
};
