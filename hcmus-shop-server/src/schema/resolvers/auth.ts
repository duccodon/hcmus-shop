import bcrypt from "bcrypt";
import { PrismaClient } from "@prisma/client";
import { generateToken } from "../../utils/jwt";
import { Context, requireAuth } from "../../middleware/auth";

export function authResolvers(prisma: PrismaClient) {
  return {
    Query: {
      me: async (_: unknown, __: unknown, context: Context) => {
        const user = requireAuth(context);
        return prisma.user.findUnique({ where: { userId: user.userId } });
      },
    },

    Mutation: {
      login: async (
        _: unknown,
        { username, password }: { username: string; password: string }
      ) => {
        const user = await prisma.user.findUnique({ where: { username } });
        if (!user) {
          throw new Error("Invalid username or password");
        }

        const valid = await bcrypt.compare(password, user.passwordHash);
        if (!valid) {
          throw new Error("Invalid username or password");
        }

        const token = generateToken({
          userId: user.userId,
          username: user.username,
          role: user.role,
        });

        return { token, user };
      },
    },
  };
}
